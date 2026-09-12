using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct PrototypeUnitSupplyState
    {
        public readonly int EntityId;
        public readonly int TeamId;
        public readonly int Requested;
        public readonly int Allocated;
        public readonly int PrimaryNodeId;
        public readonly RuleSupplyLevel Level;

        public PrototypeUnitSupplyState(
            int entityId,
            int teamId,
            int requested,
            int allocated,
            int primaryNodeId,
            RuleSupplyLevel level)
        {
            EntityId = entityId;
            TeamId = teamId;
            Requested = requested;
            Allocated = allocated;
            PrimaryNodeId = primaryNodeId;
            Level = level;
        }
    }

    public sealed class PrototypeSupplyRuntime
    {
        public const int DefaultCombatUnitSupplyUse = 5;

        private RuleSupplyNode[] _playerOneNodes = Array.Empty<RuleSupplyNode>();
        private RuleSupplyNode[] _playerTwoNodes = Array.Empty<RuleSupplyNode>();
        private bool _playerOneConfigured;
        private bool _playerTwoConfigured;
        private readonly Dictionary<int, PrototypeUnitSupplyState> _unitStates =
            new Dictionary<int, PrototypeUnitSupplyState>();

        public bool HasAnyConfiguration => _playerOneConfigured || _playerTwoConfigured;

        public void SetSupplyNodes(int playerId, IEnumerable<RuleSupplyNode> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (playerId != 1 && playerId != 2)
                throw new ArgumentOutOfRangeException(nameof(playerId));

            var sorted = new List<RuleSupplyNode>(nodes);
            sorted.Sort((left, right) => left.NodeId.CompareTo(right.NodeId));
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].TeamId != playerId)
                    throw new ArgumentException(
                        $"supply node {sorted[i].NodeId} belongs to team {sorted[i].TeamId}, expected team {playerId}",
                        nameof(nodes));
                if (i > 0 && sorted[i - 1].NodeId == sorted[i].NodeId)
                    throw new ArgumentException($"duplicate supply node {sorted[i].NodeId}", nameof(nodes));
            }

            if (playerId == 1)
            {
                _playerOneNodes = sorted.ToArray();
                _playerOneConfigured = true;
            }
            else
            {
                _playerTwoNodes = sorted.ToArray();
                _playerTwoConfigured = true;
            }
        }

        public bool RefreshIfDue(AnnihilationPrototypeWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (!RuleSupplyAllocator.ShouldReallocate(world.Tick))
                return false;

            if (_playerOneConfigured)
                RefreshTeam(world.TeamA, _playerOneNodes);
            if (_playerTwoConfigured)
                RefreshTeam(world.TeamB, _playerTwoNodes);
            return HasAnyConfiguration;
        }

        public int GetGroupCoveragePermille(PrototypeAnnihilationTeamState team, int groupId)
        {
            if (team == null)
                throw new ArgumentNullException(nameof(team));
            if (!PrototypeControlGroupPayload.IsValidGroupId(groupId))
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (!IsConfigured(team.TeamId))
                return 1000;

            long requested = 0;
            long allocated = 0;
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId)
                    continue;
                requested += DefaultCombatUnitSupplyUse;
                if (_unitStates.TryGetValue(unit.Id, out PrototypeUnitSupplyState state))
                    allocated += Math.Min(DefaultCombatUnitSupplyUse, Math.Max(0, state.Allocated));
            }

            if (requested <= 0)
                return 1000;
            return (int)Math.Min(1000L, allocated * 1000L / requested);
        }

        public bool TryGetUnitState(int entityId, out PrototypeUnitSupplyState state)
        {
            return _unitStates.TryGetValue(entityId, out state);
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, _playerOneConfigured ? 1 : 0);
            hash = StateHash64.Add(hash, _playerTwoConfigured ? 1 : 0);
            HashNodes(ref hash, _playerOneNodes);
            HashNodes(ref hash, _playerTwoNodes);

            var ids = new List<int>(_unitStates.Keys);
            ids.Sort();
            hash = StateHash64.Add(hash, ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                PrototypeUnitSupplyState state = _unitStates[ids[i]];
                hash = StateHash64.Add(hash, state.EntityId);
                hash = StateHash64.Add(hash, state.TeamId);
                hash = StateHash64.Add(hash, state.Requested);
                hash = StateHash64.Add(hash, state.Allocated);
                hash = StateHash64.Add(hash, state.PrimaryNodeId);
                hash = StateHash64.Add(hash, (int)state.Level);
            }
            return hash;
        }

        private bool IsConfigured(int playerId)
        {
            if (playerId == 1) return _playerOneConfigured;
            if (playerId == 2) return _playerTwoConfigured;
            return false;
        }

        private void RefreshTeam(PrototypeAnnihilationTeamState team, RuleSupplyNode[] nodes)
        {
            RemoveTeamStates(team.TeamId);

            var consumers = new List<RuleSupplyConsumer>();
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive)
                    continue;
                consumers.Add(new RuleSupplyConsumer(
                    unit.Id,
                    unit.TeamId,
                    unit.X,
                    unit.Y,
                    DefaultCombatUnitSupplyUse));
            }

            RuleSupplyAllocationResult result = RuleSupplyAllocator.Allocate(nodes, consumers);
            for (int i = 0; i < result.Allocations.Length; i++)
            {
                RuleSupplyAllocation allocation = result.Allocations[i];
                _unitStates[allocation.EntityId] = new PrototypeUnitSupplyState(
                    allocation.EntityId,
                    team.TeamId,
                    allocation.Requested,
                    allocation.Allocated,
                    allocation.PrimaryNodeId,
                    allocation.Level);
            }
        }

        private void RemoveTeamStates(int teamId)
        {
            var remove = new List<int>();
            foreach (KeyValuePair<int, PrototypeUnitSupplyState> pair in _unitStates)
                if (pair.Value.TeamId == teamId)
                    remove.Add(pair.Key);
            for (int i = 0; i < remove.Count; i++)
                _unitStates.Remove(remove[i]);
        }

        private static void HashNodes(ref ulong hash, RuleSupplyNode[] nodes)
        {
            hash = StateHash64.Add(hash, nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                RuleSupplyNode node = nodes[i];
                hash = StateHash64.Add(hash, node.NodeId);
                hash = StateHash64.Add(hash, node.TeamId);
                hash = StateHash64.Add(hash, node.X);
                hash = StateHash64.Add(hash, node.Y);
                hash = StateHash64.Add(hash, node.Capacity);
                hash = StateHash64.Add(hash, node.Radius);
                hash = StateHash64.Add(hash, node.Enabled ? 1 : 0);
            }
        }
    }
}
