using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct PrototypeBattleGroupOrderSpec
    {
        public readonly int PlayerId;
        public readonly int RegionId;
        public readonly int GroupId;
        public readonly int DesiredUnitCount;
        public readonly PrototypeBattleGroupStance Stance;
        public readonly RuleAIAuthorityLevel AuthorityLevel;
        public readonly RuleAIForbiddenAction Forbidden;
        public readonly int MinimumSupplyPermille;

        public PrototypeBattleGroupOrderSpec(int playerId, int regionId, int groupId, int desiredUnitCount,
            PrototypeBattleGroupStance stance, RuleAIAuthorityLevel authorityLevel, RuleAIForbiddenAction forbidden,
            int minimumSupplyPermille = 800)
        {
            if (playerId != 1 && playerId != 2) throw new ArgumentOutOfRangeException(nameof(playerId));
            if (!PrototypeControlGroupPayload.IsValidGroupId(groupId)) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (desiredUnitCount <= 0) throw new ArgumentOutOfRangeException(nameof(desiredUnitCount));
            if (authorityLevel < RuleAIAuthorityLevel.BattleGroup) throw new ArgumentOutOfRangeException(nameof(authorityLevel));
            if (minimumSupplyPermille < 0 || minimumSupplyPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(minimumSupplyPermille));
            PlayerId = playerId;
            RegionId = regionId;
            GroupId = groupId;
            DesiredUnitCount = desiredUnitCount;
            Stance = stance;
            AuthorityLevel = authorityLevel;
            Forbidden = forbidden;
            MinimumSupplyPermille = minimumSupplyPermille;
        }
    }

    public sealed class PrototypeBattleGroupRuntimeState
    {
        internal PrototypeBattleGroupRuntimeState(PrototypeBattleGroupOrderSpec spec, uint generation, int tick)
        {
            Spec = spec;
            AuthorizedGeneration = generation;
            PhaseEnteredTick = tick;
            ActiveRegionId = spec.RegionId;
        }

        public PrototypeBattleGroupOrderSpec Spec { get; }
        public uint AuthorizedGeneration { get; internal set; }
        public int ActiveRegionId { get; internal set; }
        public PrototypeBattleGroupPhase Phase { get; internal set; } = PrototypeBattleGroupPhase.Assemble;
        public int PhaseEnteredTick { get; internal set; }
        public int TargetId { get; internal set; } = -1;
        public int DecisionsPlanned { get; internal set; }
        public int DecisionsExecuted { get; internal set; }
        public int DecisionsRejected { get; internal set; }
    }

    public sealed class PrototypeBattleGroupScheduler
    {
        private const int AssembleTimeoutTicks = 30 * DeterministicUpdateBudget.TickRate;
        private readonly List<PrototypeBattleGroupRuntimeState> _groups = new List<PrototypeBattleGroupRuntimeState>();
        private readonly PrototypeSupplyRuntime _supply = new PrototypeSupplyRuntime();
        private PrototypeBattleGroupTarget[] _playerOneTargets = Array.Empty<PrototypeBattleGroupTarget>();
        private PrototypeBattleGroupTarget[] _playerTwoTargets = Array.Empty<PrototypeBattleGroupTarget>();
        private PrototypeZoneCandidate[] _playerOneZones = Array.Empty<PrototypeZoneCandidate>();
        private PrototypeZoneCandidate[] _playerTwoZones = Array.Empty<PrototypeZoneCandidate>();

        public bool PlanningEnabled { get; set; } = true;
        public int GroupCount => _groups.Count;

        public PrototypeBattleGroupRuntimeState Register(AnnihilationPrototypeWorld world, PrototypeBattleGroupOrderSpec spec)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (Find(spec.PlayerId, spec.GroupId) != null)
                throw new InvalidOperationException($"battle group {spec.PlayerId}:{spec.GroupId} is already registered");
            var state = new PrototypeBattleGroupRuntimeState(spec, GetTeam(world, spec.PlayerId).PlayerOverrideGeneration, world.Tick);
            _groups.Add(state);
            _groups.Sort(CompareGroups);
            return state;
        }

        public void SetVisibleTargets(int playerId, IEnumerable<PrototypeBattleGroupTarget> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            var sorted = new List<PrototypeBattleGroupTarget>(targets);
            sorted.Sort((left, right) => left.TargetId.CompareTo(right.TargetId));
            for (int i = 1; i < sorted.Count; i++)
                if (sorted[i - 1].TargetId == sorted[i].TargetId)
                    throw new ArgumentException($"duplicate visible target {sorted[i].TargetId}", nameof(targets));
            if (playerId == 1) _playerOneTargets = sorted.ToArray();
            else if (playerId == 2) _playerTwoTargets = sorted.ToArray();
            else throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        public void SetSupplyNodes(int playerId, IEnumerable<RuleSupplyNode> nodes)
        {
            _supply.SetSupplyNodes(playerId, nodes);
        }

        public bool TryGetUnitSupplyState(int entityId, out PrototypeUnitSupplyState state)
        {
            return _supply.TryGetUnitState(entityId, out state);
        }

        public void ApplyIntelSnapshot(int playerId, PrototypeBattleGroupIntelAdapter adapter, AuthoritativeSnapshot snapshot)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            adapter.Apply(snapshot);
            SetVisibleTargets(playerId, adapter.BuildTargets());
        }

        public void SetZoneCandidates(int playerId, IEnumerable<PrototypeZoneCandidate> zones)
        {
            if (zones == null) throw new ArgumentNullException(nameof(zones));
            var copy = new List<PrototypeZoneCandidate>(zones);
            PrototypeZoneAI.Rank(copy);
            copy.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            if (playerId == 1) _playerOneZones = copy.ToArray();
            else if (playerId == 2) _playerTwoZones = copy.ToArray();
            else throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        public void RefreshAuthorization(AnnihilationPrototypeWorld world, int playerId, int groupId)
        {
            PrototypeBattleGroupRuntimeState state = Find(playerId, groupId) ??
                throw new InvalidOperationException($"battle group {playerId}:{groupId} is not registered");
            state.AuthorizedGeneration = GetTeam(world, playerId).PlayerOverrideGeneration;
        }

        public void Step(AnnihilationPrototypeWorld world, int decisionTick)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (decisionTick != world.Tick + 1)
                throw new ArgumentOutOfRangeException(nameof(decisionTick), "planning must run for the next authoritative tick");
            if (world.Resolved) return;

            _supply.RefreshIfDue(world);
            if (!PlanningEnabled) return;

            ReallocateZonesIfDue(1, decisionTick, _playerOneZones);
            ReallocateZonesIfDue(2, decisionTick, _playerTwoZones);

            for (int i = 0; i < _groups.Count; i++)
            {
                PrototypeBattleGroupRuntimeState state = _groups[i];
                int stableId = state.Spec.PlayerId * 16 + state.Spec.GroupId;
                if (DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.BattleGroupAI, decisionTick, stableId))
                    StepGroup(world, decisionTick, state);
            }
        }

        public PrototypeBattleGroupRuntimeState GetState(int playerId, int groupId)
        {
            return Find(playerId, groupId) ??
                throw new InvalidOperationException($"battle group {playerId}:{groupId} is not registered");
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, PlanningEnabled ? 1 : 0);
            for (int i = 0; i < _groups.Count; i++)
            {
                PrototypeBattleGroupRuntimeState state = _groups[i];
                hash = StateHash64.Add(hash, state.Spec.PlayerId);
                hash = StateHash64.Add(hash, state.Spec.RegionId);
                hash = StateHash64.Add(hash, state.ActiveRegionId);
                hash = StateHash64.Add(hash, state.Spec.GroupId);
                hash = StateHash64.Add(hash, state.Spec.DesiredUnitCount);
                hash = StateHash64.Add(hash, (int)state.Spec.Stance);
                hash = StateHash64.Add(hash, (int)state.Spec.AuthorityLevel);
                hash = StateHash64.Add(hash, (int)state.Spec.Forbidden);
                if (state.Spec.MinimumSupplyPermille != 800)
                {
                    hash = StateHash64.Add(hash, 0x53555054);
                    hash = StateHash64.Add(hash, state.Spec.MinimumSupplyPermille);
                }
                hash = StateHash64.Add(hash, state.AuthorizedGeneration);
                hash = StateHash64.Add(hash, (int)state.Phase);
                hash = StateHash64.Add(hash, state.PhaseEnteredTick);
                hash = StateHash64.Add(hash, state.TargetId);
                hash = StateHash64.Add(hash, state.DecisionsPlanned);
                hash = StateHash64.Add(hash, state.DecisionsExecuted);
                hash = StateHash64.Add(hash, state.DecisionsRejected);
            }
            HashTargets(ref hash, _playerOneTargets);
            HashTargets(ref hash, _playerTwoTargets);
            HashZones(ref hash, _playerOneZones);
            HashZones(ref hash, _playerTwoZones);
            if (_supply.HasAnyConfiguration)
            {
                hash = StateHash64.Add(hash, 0x53555050);
                hash = StateHash64.Add(hash, _supply.ComputeStateHash());
            }
            return hash;
        }

        private void StepGroup(AnnihilationPrototypeWorld world, int tick, PrototypeBattleGroupRuntimeState state)
        {
            PrototypeAnnihilationTeamState team = GetTeam(world, state.Spec.PlayerId);
            AssignAvailableUnits(team, state.Spec.GroupId, state.Spec.DesiredUnitCount);
            int assigned = CountAliveGroupUnits(team, state.Spec.GroupId);
            bool assembled = assigned * 100 >= state.Spec.DesiredUnitCount * 80;
            bool timedOut = tick - state.PhaseEnteredTick >= AssembleTimeoutTicks;
            if (assigned == 0 || (!assembled && !timedOut))
            {
                if (assigned > 0)
                {
                    var assemble = new PrototypeBattleGroupDecision(state.Spec.PlayerId, state.ActiveRegionId,
                        state.Spec.GroupId, state.AuthorizedGeneration, PrototypeBattleGroupPhase.Assemble,
                        -1, AverageWaypoint(team, state.Spec.GroupId), 0);
                    ExecuteDecision(world, tick, team, state, assemble);
                }
                else
                {
                    Transition(state, PrototypeBattleGroupPhase.Assemble, tick, -1);
                }
                return;
            }

            PrototypeBattleGroupDecision decision;
            int supplyCoveragePermille = _supply.GetGroupCoveragePermille(team, state.Spec.GroupId);
            if (supplyCoveragePermille < state.Spec.MinimumSupplyPermille)
            {
                int homeWaypoint = state.Spec.PlayerId == 1 ? 0 : world.SharedCorridor.Length - 1;
                decision = new PrototypeBattleGroupDecision(
                    state.Spec.PlayerId,
                    state.ActiveRegionId,
                    state.Spec.GroupId,
                    state.AuthorizedGeneration,
                    PrototypeBattleGroupPhase.Resupply,
                    -1,
                    homeWaypoint,
                    0);
            }
            else
            {
                IReadOnlyList<PrototypeBattleGroupTarget> targets = state.Spec.PlayerId == 1 ? _playerOneTargets : _playerTwoTargets;
                if (targets.Count == 0 && state.TargetId >= 0)
                {
                    decision = new PrototypeBattleGroupDecision(state.Spec.PlayerId, state.ActiveRegionId, state.Spec.GroupId,
                        state.AuthorizedGeneration, PrototypeBattleGroupPhase.Consolidate, -1,
                        AverageWaypoint(team, state.Spec.GroupId), 0);
                }
                else if (!PrototypeBattleGroupAI.TryPlan(world, state.Spec.PlayerId, state.ActiveRegionId, state.Spec.GroupId,
                    state.Spec.Stance, targets, out decision))
                {
                    return;
                }
                else
                {
                    decision = new PrototypeBattleGroupDecision(decision.PlayerId, decision.RegionId, decision.GroupId,
                        state.AuthorizedGeneration, decision.Phase, decision.TargetId, decision.WaypointIndex, decision.UtilityScore);
                }
            }

            if (ExecuteDecision(world, tick, team, state, decision) &&
                decision.Phase == PrototypeBattleGroupPhase.Resupply)
                _supply.ServiceGroupAtHome(world, team, state.Spec.GroupId);
        }

        private static bool ExecuteDecision(AnnihilationPrototypeWorld world, int tick,
            PrototypeAnnihilationTeamState team, PrototypeBattleGroupRuntimeState state,
            PrototypeBattleGroupDecision decision)
        {
            state.DecisionsPlanned++;
            var authority = new RuleAIAuthority(state.Spec.AuthorityLevel, state.Spec.PlayerId, state.ActiveRegionId,
                state.Spec.Forbidden, team.PlayerOverrideGeneration);
            if (!PrototypeBattleGroupAI.TryExecute(world, authority, decision))
            {
                state.DecisionsRejected++;
                return false;
            }
            state.DecisionsExecuted++;
            Transition(state, decision.Phase, tick, decision.TargetId);
            return true;
        }

        private void ReallocateZonesIfDue(int playerId, int tick, PrototypeZoneCandidate[] zones)
        {
            if (zones.Length == 0 || !DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.TheaterAI, tick, playerId))
                return;
            var groupIds = new List<int>();
            for (int i = 0; i < _groups.Count; i++)
            {
                PrototypeBattleGroupRuntimeState state = _groups[i];
                if (state.Spec.PlayerId == playerId && state.Spec.AuthorityLevel >= RuleAIAuthorityLevel.Theater)
                    groupIds.Add(state.Spec.GroupId);
            }
            PrototypeZoneAssignment[] assignments = PrototypeZoneAI.BuildAllocationPlan(playerId, groupIds, zones);
            for (int i = 0; i < assignments.Length; i++)
            {
                PrototypeZoneAssignment assignment = assignments[i];
                PrototypeBattleGroupRuntimeState? state = Find(assignment.PlayerId, assignment.GroupId);
                if (state != null) state.ActiveRegionId = assignment.RegionId;
            }
        }

        private static void AssignAvailableUnits(PrototypeAnnihilationTeamState team, int groupId, int desiredCount)
        {
            int assigned = CountAliveGroupUnits(team, groupId);
            for (int i = 0; i < team.Units.Count && assigned < desiredCount; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != 0) continue;
                PrototypeControlGroupRules.AssignUnitFromAI(team, unit.Id, groupId);
                assigned++;
            }
        }

        private static int CountAliveGroupUnits(PrototypeAnnihilationTeamState team, int groupId)
        {
            int count = 0;
            for (int i = 0; i < team.Units.Count; i++)
                if (team.Units[i].Alive && team.Units[i].ControlGroupId == groupId) count++;
            return count;
        }

        private static int AverageWaypoint(PrototypeAnnihilationTeamState team, int groupId)
        {
            int total = 0;
            int count = 0;
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId) continue;
                total += unit.CorridorCursor;
                count++;
            }
            return count == 0 ? 0 : total / count;
        }

        private static void Transition(PrototypeBattleGroupRuntimeState state, PrototypeBattleGroupPhase phase, int tick, int targetId)
        {
            if (state.Phase != phase)
            {
                state.Phase = phase;
                state.PhaseEnteredTick = tick;
            }
            state.TargetId = targetId;
        }

        private PrototypeBattleGroupRuntimeState? Find(int playerId, int groupId)
        {
            for (int i = 0; i < _groups.Count; i++)
                if (_groups[i].Spec.PlayerId == playerId && _groups[i].Spec.GroupId == groupId) return _groups[i];
            return null;
        }

        private static int CompareGroups(PrototypeBattleGroupRuntimeState left, PrototypeBattleGroupRuntimeState right)
        {
            int player = left.Spec.PlayerId.CompareTo(right.Spec.PlayerId);
            return player != 0 ? player : left.Spec.GroupId.CompareTo(right.Spec.GroupId);
        }

        private static PrototypeAnnihilationTeamState GetTeam(AnnihilationPrototypeWorld world, int playerId)
        {
            if (playerId == 1) return world.TeamA;
            if (playerId == 2) return world.TeamB;
            throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        private static void HashTargets(ref ulong hash, PrototypeBattleGroupTarget[] targets)
        {
            hash = StateHash64.Add(hash, targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                PrototypeBattleGroupTarget target = targets[i];
                hash = StateHash64.Add(hash, target.TargetId);
                hash = StateHash64.Add(hash, target.WaypointIndex);
                hash = StateHash64.Add(hash, target.Threat);
                hash = StateHash64.Add(hash, target.MissionValue);
                hash = StateHash64.Add(hash, target.Vulnerability);
                hash = StateHash64.Add(hash, target.CounterMatch);
                hash = StateHash64.Add(hash, target.EstimatedCombatPower);
                hash = StateHash64.Add(hash, (int)target.Intel);
            }
        }

        private static void HashZones(ref ulong hash, PrototypeZoneCandidate[] zones)
        {
            hash = StateHash64.Add(hash, zones.Length);
            for (int i = 0; i < zones.Length; i++)
            {
                PrototypeZoneCandidate zone = zones[i];
                hash = StateHash64.Add(hash, zone.RegionId);
                hash = StateHash64.Add(hash, zone.ObjectiveValue);
                hash = StateHash64.Add(hash, zone.EnemyPressure);
                hash = StateHash64.Add(hash, zone.ResourceValue);
                hash = StateHash64.Add(hash, zone.StrategicConnectivity);
                hash = StateHash64.Add(hash, zone.PlayerDirective);
                hash = StateHash64.Add(hash, zone.ExplicitPlayerDirective ? 1 : 0);
                hash = StateHash64.Add(hash, zone.AllocationSlots);
            }
        }
    }
}
