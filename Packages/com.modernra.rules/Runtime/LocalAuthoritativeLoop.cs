using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct RuleSensorCoverageSource
    {
        public readonly int SensorId;
        public readonly int TeamId;
        public readonly int X;
        public readonly int Y;
        public readonly int BaseRange;
        public readonly int BaseQualityPermille;
        public readonly int ResistancePermille;
        public readonly RuleEWInterferenceSource[] Interference;

        public RuleSensorCoverageSource(int sensorId, int teamId, int x, int y, int baseRange,
            int baseQualityPermille, int resistancePermille, RuleEWInterferenceSource[] interference)
        {
            if (sensorId <= 0) throw new ArgumentOutOfRangeException(nameof(sensorId));
            if (teamId != 1 && teamId != 2) throw new ArgumentOutOfRangeException(nameof(teamId));
            if (baseRange <= 0) throw new ArgumentOutOfRangeException(nameof(baseRange));
            if (baseQualityPermille < 0 || baseQualityPermille > 1000) throw new ArgumentOutOfRangeException(nameof(baseQualityPermille));
            if (resistancePermille < 0 || resistancePermille > 1000) throw new ArgumentOutOfRangeException(nameof(resistancePermille));
            SensorId = sensorId;
            TeamId = teamId;
            X = x;
            Y = y;
            BaseRange = baseRange;
            BaseQualityPermille = baseQualityPermille;
            ResistancePermille = resistancePermille;
            Interference = interference == null ? Array.Empty<RuleEWInterferenceSource>() :
                (RuleEWInterferenceSource[])interference.Clone();
        }
    }

    public static class RuleSensorDetectionRules
    {
        public static int EffectiveRange(in RuleSensorCoverageSource sensor)
        {
            int quality = RuleElectronicWarfareRules.ApplyInterference(
                sensor.BaseQualityPermille, sensor.ResistancePermille, sensor.Interference);
            return Math.Max(1, (int)((long)sensor.BaseRange * quality / 1000L));
        }
    }

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
        public int ProtocolVersion;
        public uint AcknowledgedCommandSequence;
        public bool IsDelta;
        public VisibleEntityState[] Entities = Array.Empty<VisibleEntityState>();
        public int[] RemovedContactIds = Array.Empty<int>();
    }

    public sealed class LocalAuthoritativeServer
    {
        public const int ProtocolVersion = 1;
        private readonly ServerEntityState[] _entities;
        private readonly List<ClientCommandIntent> _commands = new List<ClientCommandIntent>();
        private readonly Dictionary<int, uint> _lastSequenceByPlayer = new Dictionary<int, uint>();
        private readonly Dictionary<int, uint> _lastExecutedSequenceByPlayer = new Dictionary<int, uint>();
        private readonly Dictionary<long, RuleIntelLevel> _intel = new Dictionary<long, RuleIntelLevel>();
        private readonly Dictionary<long, RuleIntelLevel> _sensorIntel = new Dictionary<long, RuleIntelLevel>();
        private readonly Dictionary<long, uint> _intelChangedTick = new Dictionary<long, uint>();

        public ulong ContentHash { get; }
        public uint Tick { get; private set; }

        public LocalAuthoritativeServer(ulong contentHash, ServerEntityState[] entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
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
            Array.Sort(_entities, (left, right) => left.EntityId.CompareTo(right.EntityId));
            for (int i = 0; i < _entities.Length; i++)
            {
                if (_entities[i].EntityId <= 0)
                    throw new ArgumentOutOfRangeException(nameof(entities), "server entity id must be positive");
                if (_entities[i].TeamId != 1 && _entities[i].TeamId != 2)
                    throw new ArgumentOutOfRangeException(nameof(entities), $"server entity {_entities[i].EntityId} has invalid team");
                if (i > 0 && _entities[i - 1].EntityId == _entities[i].EntityId)
                    throw new ArgumentException($"duplicate server entity {_entities[i].EntityId}", nameof(entities));
            }
        }

        public bool TryConnect(ulong clientContentHash) => TryConnect(clientContentHash, ProtocolVersion);

        public bool TryConnect(ulong clientContentHash, int clientProtocolVersion)
        {
            return clientContentHash == ContentHash && clientProtocolVersion == ProtocolVersion;
        }

        public void SetIntel(int observerTeam, int entityId, RuleIntelLevel level)
        {
            long key = IntelKey(observerTeam, entityId);
            RuleIntelLevel before = GetIntel(observerTeam, entityId);
            _intel[key] = level;
            if (GetIntel(observerTeam, entityId) != before)
                _intelChangedTick[key] = Tick + 1;
        }

        public bool RefreshSensorIntelIfDue(int observerTeam, IReadOnlyList<RuleSensorCoverageSource> sensors)
        {
            if (observerTeam != 1 && observerTeam != 2) throw new ArgumentOutOfRangeException(nameof(observerTeam));
            if (sensors == null) throw new ArgumentNullException(nameof(sensors));
            if (!DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.HighRateSensor, (int)Tick, observerTeam))
                return false;

            var ordered = new RuleSensorCoverageSource[sensors.Count];
            for (int i = 0; i < sensors.Count; i++) ordered[i] = sensors[i];
            Array.Sort(ordered, (left, right) => left.SensorId.CompareTo(right.SensorId));
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].TeamId != observerTeam)
                    throw new ArgumentException($"sensor {ordered[i].SensorId} belongs to team {ordered[i].TeamId}", nameof(sensors));
                if (i > 0 && ordered[i - 1].SensorId == ordered[i].SensorId)
                    throw new ArgumentException($"duplicate sensor {ordered[i].SensorId}", nameof(sensors));
            }

            var detected = new HashSet<int>();
            for (int sensorIndex = 0; sensorIndex < ordered.Length; sensorIndex++)
            {
                RuleSensorCoverageSource sensor = ordered[sensorIndex];
                long range = RuleSensorDetectionRules.EffectiveRange(sensor);
                long rangeSquared = range * range;
                for (int entityIndex = 0; entityIndex < _entities.Length; entityIndex++)
                {
                    ServerEntityState entity = _entities[entityIndex];
                    if (entity.TeamId == observerTeam) continue;
                    long dx = (long)entity.X - sensor.X;
                    long dy = (long)entity.Y - sensor.Y;
                    if (dx * dx + dy * dy <= rangeSquared)
                        detected.Add(entity.EntityId);
                }
            }

            for (int i = 0; i < _entities.Length; i++)
            {
                ServerEntityState entity = _entities[i];
                if (entity.TeamId == observerTeam) continue;
                long key = IntelKey(observerTeam, entity.EntityId);
                RuleIntelLevel before = GetIntel(observerTeam, entity.EntityId);
                if (detected.Contains(entity.EntityId))
                    _sensorIntel[key] = RuleIntelLevel.Detected;
                else
                    _sensorIntel.Remove(key);
                if (GetIntel(observerTeam, entity.EntityId) != before)
                    _intelChangedTick[key] = Tick + 1;
            }
            return true;
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
            _commands.Sort(static (left, right) =>
            {
                int player = left.PlayerId.CompareTo(right.PlayerId);
                return player != 0 ? player : left.Sequence.CompareTo(right.Sequence);
            });
            for (int i = 0; i < _commands.Count; i++)
            {
                ClientCommandIntent command = _commands[i];
                ServerEntityState? entity = FindEntity(command.EntityId);
                if (entity == null) continue;
                entity.TargetX = command.TargetX;
                entity.TargetY = command.TargetY;
                entity.HasMoveTarget = true;
                _lastExecutedSequenceByPlayer[command.PlayerId] = command.Sequence;
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
            if (observerTeam != 1 && observerTeam != 2) throw new ArgumentOutOfRangeException(nameof(observerTeam));
            if (baselineTick > Tick) throw new ArgumentOutOfRangeException(nameof(baselineTick));
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
                ProtocolVersion = ProtocolVersion,
                AcknowledgedCommandSequence = _lastExecutedSequenceByPlayer.TryGetValue(observerTeam, out uint sequence)
                    ? sequence : 0,
                IsDelta = baselineTick > 0,
                Entities = visible.ToArray(),
                RemovedContactIds = removed.ToArray()
            };
        }

        public AuthoritativeSnapshot BuildReconnectSnapshot(int observerTeam)
        {
            return BuildSnapshot(observerTeam, 0);
        }

        private RuleIntelLevel GetIntel(int observerTeam, int entityId)
        {
            long key = IntelKey(observerTeam, entityId);
            RuleIntelLevel manual = _intel.TryGetValue(key, out RuleIntelLevel manualLevel)
                ? manualLevel : RuleIntelLevel.Unknown;
            RuleIntelLevel sensor = _sensorIntel.TryGetValue(key, out RuleIntelLevel sensorLevel)
                ? sensorLevel : RuleIntelLevel.Unknown;
            return manual >= sensor ? manual : sensor;
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
        private bool _hasContentHash;
        private ulong _contentHash;
        public uint LastTick { get; private set; }
        public bool NeedsReconnect { get; private set; }
        public ulong ContentHash => _contentHash;
        public uint LastAcknowledgedCommandSequence { get; private set; }

        public void Apply(AuthoritativeSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.ProtocolVersion != LocalAuthoritativeServer.ProtocolVersion)
                throw new InvalidOperationException("snapshot protocol version is unsupported");
            if (_hasContentHash && snapshot.ContentHash != _contentHash)
                throw new InvalidOperationException("snapshot content hash changed during the session");
            if (!_hasContentHash)
            {
                _contentHash = snapshot.ContentHash;
                _hasContentHash = true;
            }
            if (LastTick > 0 && snapshot.Tick <= LastTick)
                return;
            ValidatePayload(snapshot);
            if (snapshot.IsDelta)
            {
                if (LastTick == 0 || snapshot.BaselineTick != LastTick || snapshot.Tick <= snapshot.BaselineTick)
                {
                    NeedsReconnect = true;
                    throw new InvalidOperationException("snapshot delta baseline does not match the client state");
                }
            }
            else if (snapshot.BaselineTick != 0)
            {
                throw new InvalidOperationException("full snapshot must use baseline tick zero");
            }
            if (snapshot.AcknowledgedCommandSequence < LastAcknowledgedCommandSequence)
                throw new InvalidOperationException("snapshot command acknowledgement moved backwards");

            if (!snapshot.IsDelta)
            {
                _current.Clear();
                _previous.Clear();
                NeedsReconnect = false;
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
            LastAcknowledgedCommandSequence = snapshot.AcknowledgedCommandSequence;
        }

        private static void ValidatePayload(AuthoritativeSnapshot snapshot)
        {
            if (snapshot.Entities == null) throw new InvalidOperationException("snapshot entity payload is missing");
            if (snapshot.RemovedContactIds == null) throw new InvalidOperationException("snapshot removal payload is missing");
            int previous = int.MinValue;
            for (int i = 0; i < snapshot.Entities.Length; i++)
            {
                int contactId = snapshot.Entities[i].ContactId;
                if (i > 0 && contactId <= previous)
                    throw new InvalidOperationException("snapshot entity contacts are not strictly ordered");
                previous = contactId;
            }
            previous = int.MinValue;
            int entityIndex = 0;
            for (int i = 0; i < snapshot.RemovedContactIds.Length; i++)
            {
                int contactId = snapshot.RemovedContactIds[i];
                if (i > 0 && contactId <= previous)
                    throw new InvalidOperationException("snapshot removed contacts are not strictly ordered");
                while (entityIndex < snapshot.Entities.Length &&
                       snapshot.Entities[entityIndex].ContactId < contactId)
                    entityIndex++;
                if (entityIndex < snapshot.Entities.Length &&
                    snapshot.Entities[entityIndex].ContactId == contactId)
                    throw new InvalidOperationException("snapshot both updates and removes the same contact");
                previous = contactId;
            }
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
