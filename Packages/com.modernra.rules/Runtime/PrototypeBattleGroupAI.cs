using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum PrototypeBattleGroupStance : byte
    {
        Conservative = 0,
        Balanced = 1,
        Aggressive = 2
    }

    public enum PrototypeBattleGroupPhase : byte
    {
        Assemble = 0,
        Move = 1,
        Recon = 2,
        Engage = 3,
        Consolidate = 4,
        Resupply = 5,
        Withdraw = 6
    }

    public readonly struct PrototypeBattleGroupTarget
    {
        public readonly int TargetId;
        public readonly int WaypointIndex;
        public readonly int Threat;
        public readonly int MissionValue;
        public readonly int Vulnerability;
        public readonly int CounterMatch;
        public readonly int EstimatedCombatPower;
        public readonly RuleIntelLevel Intel;

        public PrototypeBattleGroupTarget(
            int targetId,
            int waypointIndex,
            int threat,
            int missionValue,
            int vulnerability,
            int counterMatch,
            int estimatedCombatPower,
            RuleIntelLevel intel)
        {
            TargetId = targetId;
            WaypointIndex = waypointIndex;
            Threat = ValidateNormalized(threat, nameof(threat));
            MissionValue = ValidateNormalized(missionValue, nameof(missionValue));
            Vulnerability = ValidateNormalized(vulnerability, nameof(vulnerability));
            CounterMatch = ValidateNormalized(counterMatch, nameof(counterMatch));
            EstimatedCombatPower = Math.Max(0, estimatedCombatPower);
            Intel = intel;
        }

        private static int ValidateNormalized(int value, string name)
        {
            if (value < 0 || value > 1000)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public readonly struct PrototypeBattleGroupDecision
    {
        public readonly int PlayerId;
        public readonly int RegionId;
        public readonly int GroupId;
        public readonly uint Generation;
        public readonly PrototypeBattleGroupPhase Phase;
        public readonly int TargetId;
        public readonly int WaypointIndex;
        public readonly int UtilityScore;

        public PrototypeBattleGroupDecision(
            int playerId,
            int regionId,
            int groupId,
            uint generation,
            PrototypeBattleGroupPhase phase,
            int targetId,
            int waypointIndex,
            int utilityScore)
        {
            PlayerId = playerId;
            RegionId = regionId;
            GroupId = groupId;
            Generation = generation;
            Phase = phase;
            TargetId = targetId;
            WaypointIndex = waypointIndex;
            UtilityScore = utilityScore;
        }
    }

    public static class PrototypeBattleGroupAI
    {
        public static bool TryPlan(
            AnnihilationPrototypeWorld world,
            int playerId,
            int regionId,
            int groupId,
            PrototypeBattleGroupStance stance,
            IReadOnlyList<PrototypeBattleGroupTarget> visibleTargets,
            out PrototypeBattleGroupDecision decision)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (visibleTargets == null)
                throw new ArgumentNullException(nameof(visibleTargets));
            if ((playerId != 1 && playerId != 2) || !PrototypeControlGroupPayload.IsValidGroupId(groupId))
            {
                decision = default;
                return false;
            }

            PrototypeAnnihilationTeamState team = playerId == 1 ? world.TeamA : world.TeamB;
            int aliveCount = 0;
            int totalHealth = 0;
            int totalCursor = 0;
            long totalX = 0;
            long totalY = 0;
            bool needsSupply = false;
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId)
                    continue;
                aliveCount++;
                totalHealth += unit.Health;
                totalCursor += unit.CorridorCursor;
                totalX += unit.X;
                totalY += unit.Y;
                if (unit.SupplyLevel != RuleSupplyLevel.Sufficient)
                    needsSupply = true;
            }
            if (aliveCount == 0)
            {
                decision = default;
                return false;
            }

            int currentWaypoint = totalCursor / aliveCount;
            int homeWaypoint = playerId == 1 ? 0 : world.SharedCorridor.Length - 1;
            int resupplyWaypoint = homeWaypoint;
            PrototypeSupplyRuntime.TryFindResupplyWaypoint(
                world,
                playerId,
                (int)(totalX / aliveCount),
                (int)(totalY / aliveCount),
                out resupplyWaypoint);
            if (resupplyWaypoint < 0)
                resupplyWaypoint = homeWaypoint;
            if (totalHealth / aliveCount < 350)
            {
                decision = CreateDecision(team, regionId, groupId, PrototypeBattleGroupPhase.Resupply, -1, resupplyWaypoint, 0);
                return true;
            }

            bool found = TrySelectTarget(visibleTargets, currentWaypoint, world.SharedCorridor.Length, out PrototypeBattleGroupTarget target, out int score);
            if (!found)
            {
                if (needsSupply)
                {
                    decision = CreateDecision(team, regionId, groupId, PrototypeBattleGroupPhase.Resupply, -1, resupplyWaypoint, 0);
                    return true;
                }
                decision = CreateDecision(team, regionId, groupId, PrototypeBattleGroupPhase.Recon, -1, currentWaypoint, 0);
                return true;
            }

            int ownCombatPower = totalHealth;
            int attackPermille = AttackThreshold(stance);
            int withdrawPermille = WithdrawThreshold(stance);
            if (target.EstimatedCombatPower > 0 && ownCombatPower * 1000 < target.EstimatedCombatPower * withdrawPermille)
            {
                decision = CreateDecision(team, regionId, groupId, PrototypeBattleGroupPhase.Withdraw, target.TargetId, homeWaypoint, score);
                return true;
            }

            int ratioPermille = target.EstimatedCombatPower == 0 ? int.MaxValue : ownCombatPower * 1000 / target.EstimatedCombatPower;
            if (ratioPermille < attackPermille)
            {
                decision = CreateDecision(team, regionId, groupId, PrototypeBattleGroupPhase.Recon, target.TargetId, currentWaypoint, score);
                return true;
            }
            PrototypeBattleGroupPhase phase = Math.Abs(currentWaypoint - target.WaypointIndex) <= 1
                ? PrototypeBattleGroupPhase.Engage
                : PrototypeBattleGroupPhase.Move;
            decision = CreateDecision(team, regionId, groupId, phase, target.TargetId, target.WaypointIndex, score);
            return true;
        }

        public static bool TryExecute(
            AnnihilationPrototypeWorld world,
            in RuleAIAuthority authority,
            in PrototypeBattleGroupDecision decision)
        {
            var order = new RuleAIOrder(
                decision.PlayerId,
                decision.RegionId,
                RuleAIAction.Move,
                RuleAIAuthorityLevel.BattleGroup,
                decision.Generation);
            if (!AIAuthorityRules.IsAllowed(authority, order))
                return false;
            if (decision.Phase == PrototypeBattleGroupPhase.Recon ||
                decision.Phase == PrototypeBattleGroupPhase.Consolidate)
            {
                return PrototypeControlGroupRules.TryApplyAIHolding(
                    world,
                    decision.PlayerId,
                    decision.Generation,
                    decision.GroupId);
            }
            return PrototypeControlGroupRules.TryApplyAIWaypoint(
                world,
                decision.PlayerId,
                decision.Generation,
                decision.GroupId,
                decision.WaypointIndex);
        }

        private static bool TrySelectTarget(
            IReadOnlyList<PrototypeBattleGroupTarget> targets,
            int currentWaypoint,
            int corridorLength,
            out PrototypeBattleGroupTarget selected,
            out int selectedScore)
        {
            selected = default;
            selectedScore = int.MinValue;
            bool found = false;
            int maxDistance = Math.Max(1, corridorLength - 1);
            for (int i = 0; i < targets.Count; i++)
            {
                PrototypeBattleGroupTarget target = targets[i];
                if (target.Intel < RuleIntelLevel.Detected || target.WaypointIndex < 0 || target.WaypointIndex >= corridorLength)
                    continue;
                int distance = Math.Abs(currentWaypoint - target.WaypointIndex);
                int distanceFactor = Math.Max(0, 1000 - distance * 1000 / maxDistance);
                int score = target.Threat * 30 + target.MissionValue * 25 + target.Vulnerability * 15 +
                    distanceFactor * 10 + target.CounterMatch * 10 + IntelConfidence(target.Intel) * 10;
                score /= 100;
                if (!found || score > selectedScore || (score == selectedScore && target.TargetId < selected.TargetId))
                {
                    selected = target;
                    selectedScore = score;
                    found = true;
                }
            }
            return found;
        }

        private static PrototypeBattleGroupDecision CreateDecision(
            PrototypeAnnihilationTeamState team,
            int regionId,
            int groupId,
            PrototypeBattleGroupPhase phase,
            int targetId,
            int waypointIndex,
            int score)
        {
            return new PrototypeBattleGroupDecision(
                team.TeamId,
                regionId,
                groupId,
                team.PlayerOverrideGeneration,
                phase,
                targetId,
                waypointIndex,
                score);
        }

        private static int IntelConfidence(RuleIntelLevel intel)
        {
            return intel switch
            {
                RuleIntelLevel.Detected => 400,
                RuleIntelLevel.Classified => 600,
                RuleIntelLevel.Confirmed => 800,
                RuleIntelLevel.Tracked => 1000,
                _ => 0
            };
        }

        private static int AttackThreshold(PrototypeBattleGroupStance stance)
        {
            return stance switch
            {
                PrototypeBattleGroupStance.Conservative => 1400,
                PrototypeBattleGroupStance.Balanced => 1150,
                PrototypeBattleGroupStance.Aggressive => 950,
                _ => throw new ArgumentOutOfRangeException(nameof(stance))
            };
        }

        private static int WithdrawThreshold(PrototypeBattleGroupStance stance)
        {
            return stance switch
            {
                PrototypeBattleGroupStance.Conservative => 900,
                PrototypeBattleGroupStance.Balanced => 750,
                PrototypeBattleGroupStance.Aggressive => 600,
                _ => throw new ArgumentOutOfRangeException(nameof(stance))
            };
        }
    }
}
