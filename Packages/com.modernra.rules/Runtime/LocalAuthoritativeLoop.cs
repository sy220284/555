using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public sealed class ServerEntityState
    {
        public int EntityId;
        public int TeamId;
        public int X;
        public int Y;
        public int UnitClass;
        public int Health;
        public uint LastChangedTick;
        public int TargetX;
        public int TargetY;
        public bool HasMoveTarget;
    }

    public readonly struct ClientCommandIntent
    {
        public readonly int PlayerId;
        public readonly int EntityId;
        public readonly uint Sequence;
        public readonly int TargetX;
        public readonly int TargetY;

        public ClientCommandIntent(int playerId, int entityId, uint sequence, int targetX, int targetY)
        {
            PlayerId = playerId;
            EntityId = entityId;
            Sequence = sequence;
            TargetX = targetX;
            TargetY = targetY;
        }
    }

    public sealed class AuthoritativeSnapshot
    {
        public uint Tick;
        public uint BaselineTick;
        public ulong ContentHash;
        public bool IsDelta;
        public VisibleEntityState[] Entities = Array.Empty<VisibleEntityState>();
        public int[] RemovedContactIds = Array.Empty<int>();
    }

    public sealed class LocalAuthoritativeServer
    {
        private readonly ServerEntityState[] _entities;
        private readonly List<ClientCommandIntent> _commands = new List<ClientCommandIntent>();
        private readonly Dictionary<int, uint> _lastSequenceByPlayer = new Dictionary<int, uint>();
        private readonly Dictionary<long, RuleIntelLevel> _intel = new Dictionary<long, RuleIntelLevel>();
        private readonly Dictionary<long, uint> _intelChangedTick = new Dictionary<long, uint>();

        public ulong ContentHash { get; }
        public uint Tick { get; private set; }

        public LocalAuthoritativeServer(ulong contentHash, ServerEntityState[] entities)
        {
            ContentHash = contentHash;
            _entities = new ServerEntityState[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                var source = entities[i];
                _entities[i] = new ServerEntityState
                {
                    EntityId = source.EntityId,
                    TeamId = source.TeamId,
                    X = source.X,
                    Y = source.Y,
                    UnitClass = source.UnitClass,
                    Health = source.Health,
                    TargetX = source.X,
                    TargetY = source.Y
                };
            }
        }

        public bool TryConnect(ulong clientContentHash) => clientContentHash == ContentHash;

        public void SetIntel(int observerTeam, int entityId, RuleIntelLevel level)
        {
            long key = IntelKey(observerTeam, entityId);
            _intel[key] = level;
            _intelChangedTick[key] = Tick + 1;
        }

        public bool SubmitIntent(in ClientCommandIntent command)
        {
            ServerEntityState? entity = FindEntity(command.EntityId);
            if (entity == null || entity.TeamId != command.PlayerId)
                return false;
            if (_lastSequenceByPlayer.TryGetValue(command.PlayerId, out uint previous) && command.Sequence <= previous)
                return false;
            _lastSequenceByPlayer[command.PlayerId] = command.Sequence;
            _commands.Add(command);
            return true;
        }

        public void AdvanceOneTick()
        {
            Tick++;
            for (int i = 0; i < _commands.Count; i++)
            {
                ClientCommandIntent command = _commands[i];
                ServerEntityState? entity = FindEntity(command.EntityId);
                if (entity == null) continue;
                entity.TargetX = command.TargetX;
                entity.TargetY = command.TargetY;
                entity.HasMoveTarget = true;
            }
            _commands.Clear();

            for (int i = 0; i < _entities.Length; i++)
            {
                ServerEntityState entity = _entities[i];
                if (!entity.HasMoveTarget) continue;
                int beforeX = entity.X;
                int beforeY = entity.Y;
                MoveAxis(ref entity.X, entity.TargetX, 10);
                MoveAxis(ref entity.Y, entity.TargetY, 10);
                if (entity.X == entity.TargetX && entity.Y == entity.TargetY)
                    entity.HasMoveTarget = false;
                if (entity.X != beforeX || entity.Y != beforeY)
                    entity.LastChangedTick = Tick;
            }
        }

        public AuthoritativeSnapshot BuildSnapshot(int observerTeam, uint baselineTick)
        {
            var visible = new List<VisibleEntityState>(_entities.Length);
            var removed = new List<int>();
            for (int i = 0; i < _entities.Length; i++)
            {
                ServerEntityState entity = _entities[i];
                bool friendly = entity.TeamId == observerTeam;
                RuleIntelLevel level = friendly ? RuleIntelLevel.Tracked : GetIntel(observerTeam, entity.EntityId);
                long intelKey = IntelKey(observerTeam, entity.EntityId);
                uint intelChanged = _intelChangedTick.TryGetValue(intelKey, out uint changed) ? changed : 0;
                if (baselineTick > 0 && entity.LastChangedTick <= baselineTick && intelChanged <= baselineTick)
                    continue;
                int contactId = unchecked(entity.EntityId * 31 + observerTeam);
                if (IntelReplicationRules.TryBuildVisibleState(contactId, entity.X, entity.Y, entity.UnitClass, entity.Health, level, out VisibleEntityState view, friendly))
                    visible.Add(view);
                else if (baselineTick > 0 && intelChanged > baselineTick)
                    removed.Add(contactId);
            }

            return new AuthoritativeSnapshot
            {
                Tick = Tick,
                BaselineTick = baselineTick,
                ContentHash = ContentHash,
                IsDelta = baselineTick > 0,
                Entities = visible.ToArray(),
                RemovedContactIds = removed.ToArray()
            };
        }

        private RuleIntelLevel GetIntel(int observerTeam, int entityId)
        {
            return _intel.TryGetValue(IntelKey(observerTeam, entityId), out RuleIntelLevel level) ? level : RuleIntelLevel.Unknown;
        }

        private ServerEntityState? FindEntity(int entityId)
        {
            for (int i = 0; i < _entities.Length; i++)
                if (_entities[i].EntityId == entityId)
                    return _entities[i];
            return null;
        }

        private static long IntelKey(int observerTeam, int entityId) => ((long)observerTeam << 32) | (uint)entityId;

        private static void MoveAxis(ref int value, int target, int maxStep)
        {
            int delta = target - value;
            if (delta > maxStep) value += maxStep;
            else if (delta < -maxStep) value -= maxStep;
            else value = target;
        }
    }

    public sealed class LocalClientReplica
    {
        private readonly Dictionary<int, VisibleEntityState> _current = new Dictionary<int, VisibleEntityState>();
        private readonly Dictionary<int, VisibleEntityState> _previous = new Dictionary<int, VisibleEntityState>();
        public uint LastTick { get; private set; }

        public void Apply(AuthoritativeSnapshot snapshot)
        {
            if (!snapshot.IsDelta)
            {
                _current.Clear();
                _previous.Clear();
            }
            for (int i = 0; i < snapshot.RemovedContactIds.Length; i++)
            {
                int contactId = snapshot.RemovedContactIds[i];
                _current.Remove(contactId);
                _previous.Remove(contactId);
            }
            for (int i = 0; i < snapshot.Entities.Length; i++)
            {
                VisibleEntityState next = snapshot.Entities[i];
                if (_current.TryGetValue(next.ContactId, out VisibleEntityState old))
                    _previous[next.ContactId] = old;
                _current[next.ContactId] = next;
            }
            LastTick = snapshot.Tick;
        }

        public bool ContainsContact(int contactId) => _current.ContainsKey(contactId);

        public bool TryGet(int contactId, out VisibleEntityState state) => _current.TryGetValue(contactId, out state);

        public bool TryInterpolate(int contactId, int alphaPermille, out Int2 position)
        {
            position = default;
            if (!_current.TryGetValue(contactId, out VisibleEntityState current))
                return false;
            if (!_previous.TryGetValue(contactId, out VisibleEntityState previous))
            {
                position = new Int2(current.X, current.Y);
                return true;
            }
            int alpha = Math.Max(0, Math.Min(1000, alphaPermille));
            int x = previous.X + (current.X - previous.X) * alpha / 1000;
            int y = previous.Y + (current.Y - previous.Y) * alpha / 1000;
            position = new Int2(x, y);
            return true;
        }
    }
}
