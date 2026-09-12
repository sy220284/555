using System;

namespace ModernRA.Rules
{
    public static class PrototypeControlGroupPayload
    {
        private const int GroupBits = 4;
        private const int GroupMask = (1 << GroupBits) - 1;
        private const int WaypointBits = 7;
        private const int WaypointMask = (1 << WaypointBits) - 1;

        public static int EncodeUnitGroup(int unitId, int groupId)
        {
            ValidateGroupId(groupId);
            if (unitId <= 0 || unitId > (int.MaxValue >> GroupBits))
                throw new ArgumentOutOfRangeException(nameof(unitId));
            return (unitId << GroupBits) | groupId;
        }

        public static bool TryDecodeUnitGroup(int payload, out int unitId, out int groupId)
        {
            unitId = payload >> GroupBits;
            groupId = payload & GroupMask;
            return payload > 0 && unitId > 0 && IsValidGroupId(groupId);
        }

        public static int EncodeGroupWaypoint(int groupId, int waypointIndex)
        {
            ValidateGroupId(groupId);
            if (waypointIndex < 0 || waypointIndex > 64)
                throw new ArgumentOutOfRangeException(nameof(waypointIndex));
            return (groupId << WaypointBits) | waypointIndex;
        }

        public static bool TryDecodeGroupWaypoint(int payload, out int groupId, out int waypointIndex)
        {
            groupId = payload >> WaypointBits;
            waypointIndex = payload & WaypointMask;
            return payload > 0 && IsValidGroupId(groupId) && waypointIndex <= 64;
        }

        public static bool IsValidGroupId(int groupId) => groupId >= 1 && groupId <= 9;

        private static void ValidateGroupId(int groupId)
        {
            if (!IsValidGroupId(groupId))
                throw new ArgumentOutOfRangeException(nameof(groupId));
        }
    }

    public static class PrototypeControlGroupRules
    {
        public static bool HasAliveMember(PrototypeAnnihilationTeamState team, int groupId)
        {
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (unit.Alive && unit.ControlGroupId == groupId)
                    return true;
            }
            return false;
        }

        public static void AssignUnit(PrototypeAnnihilationTeamState team, int unitId, int groupId)
        {
            PrototypeCombatUnitState unit = RequireAliveOwnedUnit(team, unitId);
            unit.ControlGroupId = groupId;
            MarkPlayerOverride(team);
        }

        public static void SetGroupWaypoint(PrototypeAnnihilationTeamState team, int groupId, int waypointIndex)
        {
            bool applied = false;
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId)
                    continue;
                unit.CorridorCursor = waypointIndex;
                unit.HoldingPosition = false;
                applied = true;
            }
            if (!applied)
                throw new InvalidOperationException($"control group {groupId} has no alive units");
            MarkPlayerOverride(team);
        }

        public static void SetGroupHolding(PrototypeAnnihilationTeamState team, int groupId, bool holding)
        {
            bool applied = false;
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId)
                    continue;
                unit.HoldingPosition = holding;
                applied = true;
            }
            if (!applied)
                throw new InvalidOperationException($"control group {groupId} has no alive units");
            MarkPlayerOverride(team);
        }

        public static bool TryApplyAIWaypoint(AnnihilationPrototypeWorld world, int playerId, uint orderGeneration, int groupId, int waypointIndex)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            PrototypeAnnihilationTeamState team = playerId == 1 ? world.TeamA : world.TeamB;
            if (orderGeneration < team.PlayerOverrideGeneration ||
                !PrototypeControlGroupPayload.IsValidGroupId(groupId) ||
                waypointIndex < 0 || waypointIndex >= world.SharedCorridor.Length ||
                !HasAliveMember(team, groupId))
            {
                return false;
            }

            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive || unit.ControlGroupId != groupId)
                    continue;
                unit.CorridorCursor = waypointIndex;
                unit.HoldingPosition = false;
            }
            return true;
        }

        private static PrototypeCombatUnitState RequireAliveOwnedUnit(PrototypeAnnihilationTeamState team, int unitId)
        {
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (unit.Id == unitId && unit.Alive)
                    return unit;
            }
            throw new InvalidOperationException($"unit {unitId} is not an alive unit owned by player {team.TeamId}");
        }

        public static void MarkPlayerOverride(PrototypeAnnihilationTeamState team)
        {
            team.PlayerOverrideGeneration = checked(team.PlayerOverrideGeneration + 1U);
        }
    }
}
