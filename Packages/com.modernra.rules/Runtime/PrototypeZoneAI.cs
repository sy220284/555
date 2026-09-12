using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct PrototypeZoneCandidate
    {
        public readonly int RegionId;
        public readonly int ObjectiveValue;
        public readonly int EnemyPressure;
        public readonly int ResourceValue;
        public readonly int StrategicConnectivity;
        public readonly int PlayerDirective;
        public readonly bool ExplicitPlayerDirective;
        public readonly int AllocationSlots;

        public PrototypeZoneCandidate(
            int regionId,
            int objectiveValue,
            int enemyPressure,
            int resourceValue,
            int strategicConnectivity,
            int playerDirective,
            bool explicitPlayerDirective,
            int allocationSlots = 1)
        {
            if (regionId < 0) throw new ArgumentOutOfRangeException(nameof(regionId));
            if (allocationSlots <= 0) throw new ArgumentOutOfRangeException(nameof(allocationSlots));
            RegionId = regionId;
            ObjectiveValue = ValidateNormalized(objectiveValue, nameof(objectiveValue));
            EnemyPressure = ValidateNormalized(enemyPressure, nameof(enemyPressure));
            ResourceValue = ValidateNormalized(resourceValue, nameof(resourceValue));
            StrategicConnectivity = ValidateNormalized(strategicConnectivity, nameof(strategicConnectivity));
            PlayerDirective = ValidateNormalized(playerDirective, nameof(playerDirective));
            ExplicitPlayerDirective = explicitPlayerDirective;
            AllocationSlots = allocationSlots;
        }

        private static int ValidateNormalized(int value, string name)
        {
            if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public readonly struct PrototypeZonePriority
    {
        public readonly int RegionId;
        public readonly int Score;
        public readonly int AllocationSlots;

        public PrototypeZonePriority(int regionId, int score, int allocationSlots)
        {
            RegionId = regionId;
            Score = score;
            AllocationSlots = allocationSlots;
        }
    }

    public readonly struct PrototypeZoneAssignment
    {
        public readonly int PlayerId;
        public readonly int GroupId;
        public readonly int RegionId;
        public readonly int PriorityScore;

        public PrototypeZoneAssignment(int playerId, int groupId, int regionId, int priorityScore)
        {
            PlayerId = playerId;
            GroupId = groupId;
            RegionId = regionId;
            PriorityScore = priorityScore;
        }
    }

    public static class PrototypeZoneAI
    {
        private const int ObjectiveWeight = 350;
        private const int EnemyPressureWeight = 250;
        private const int ResourceWeight = 150;
        private const int ConnectivityWeight = 150;
        private const int DefaultDirectiveWeight = 100;
        private const int ExplicitDirectiveWeight = 500;

        public static int ComputePriority(in PrototypeZoneCandidate candidate)
        {
            int directiveWeight = candidate.ExplicitPlayerDirective ? ExplicitDirectiveWeight : DefaultDirectiveWeight;
            int totalWeight = ObjectiveWeight + EnemyPressureWeight + ResourceWeight + ConnectivityWeight + directiveWeight;
            long weighted = (long)candidate.ObjectiveValue * ObjectiveWeight +
                (long)candidate.EnemyPressure * EnemyPressureWeight +
                (long)candidate.ResourceValue * ResourceWeight +
                (long)candidate.StrategicConnectivity * ConnectivityWeight +
                (long)candidate.PlayerDirective * directiveWeight;
            return (int)(weighted / totalWeight);
        }

        public static bool TrySelectHighestPriority(
            IReadOnlyList<PrototypeZoneCandidate> zones,
            out PrototypeZonePriority selected)
        {
            PrototypeZonePriority[] ranked = Rank(zones);
            if (ranked.Length == 0)
            {
                selected = default;
                return false;
            }
            selected = ranked[0];
            return true;
        }

        public static PrototypeZonePriority[] Rank(IReadOnlyList<PrototypeZoneCandidate> zones)
        {
            if (zones == null) throw new ArgumentNullException(nameof(zones));
            var priorities = new List<PrototypeZonePriority>(zones.Count);
            var seen = new HashSet<int>();
            for (int i = 0; i < zones.Count; i++)
            {
                PrototypeZoneCandidate zone = zones[i];
                if (!seen.Add(zone.RegionId))
                    throw new ArgumentException($"duplicate zone {zone.RegionId}", nameof(zones));
                priorities.Add(new PrototypeZonePriority(zone.RegionId, ComputePriority(zone), zone.AllocationSlots));
            }
            priorities.Sort(ComparePriorities);
            return priorities.ToArray();
        }

        public static PrototypeZoneAssignment[] BuildAllocationPlan(
            int playerId,
            IReadOnlyList<int> groupIds,
            IReadOnlyList<PrototypeZoneCandidate> zones)
        {
            if (playerId != 1 && playerId != 2) throw new ArgumentOutOfRangeException(nameof(playerId));
            if (groupIds == null) throw new ArgumentNullException(nameof(groupIds));
            if (zones == null) throw new ArgumentNullException(nameof(zones));

            int[] groups = CopyAndValidateGroups(groupIds);
            PrototypeZonePriority[] priorities = Rank(zones);
            if (groups.Length == 0 || priorities.Length == 0)
                return Array.Empty<PrototypeZoneAssignment>();

            var assignments = new List<PrototypeZoneAssignment>(groups.Length);
            int groupCursor = 0;
            for (int zoneIndex = 0; zoneIndex < priorities.Length && groupCursor < groups.Length; zoneIndex++)
            {
                PrototypeZonePriority priority = priorities[zoneIndex];
                for (int slot = 0; slot < priority.AllocationSlots && groupCursor < groups.Length; slot++)
                {
                    assignments.Add(new PrototypeZoneAssignment(
                        playerId,
                        groups[groupCursor],
                        priority.RegionId,
                        priority.Score));
                    groupCursor++;
                }
            }
            return assignments.ToArray();
        }

        public static ulong ComputePlanHash(IReadOnlyList<PrototypeZoneAssignment> assignments)
        {
            if (assignments == null) throw new ArgumentNullException(nameof(assignments));
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, assignments.Count);
            for (int i = 0; i < assignments.Count; i++)
            {
                PrototypeZoneAssignment assignment = assignments[i];
                hash = StateHash64.Add(hash, assignment.PlayerId);
                hash = StateHash64.Add(hash, assignment.GroupId);
                hash = StateHash64.Add(hash, assignment.RegionId);
                hash = StateHash64.Add(hash, assignment.PriorityScore);
            }
            return hash;
        }

        private static int[] CopyAndValidateGroups(IReadOnlyList<int> groupIds)
        {
            var groups = new int[groupIds.Count];
            var seen = new HashSet<int>();
            for (int i = 0; i < groupIds.Count; i++)
            {
                int groupId = groupIds[i];
                if (!PrototypeControlGroupPayload.IsValidGroupId(groupId))
                    throw new ArgumentOutOfRangeException(nameof(groupIds), $"invalid group id {groupId}");
                if (!seen.Add(groupId))
                    throw new ArgumentException($"duplicate group id {groupId}", nameof(groupIds));
                groups[i] = groupId;
            }
            Array.Sort(groups);
            return groups;
        }

        private static int ComparePriorities(PrototypeZonePriority left, PrototypeZonePriority right)
        {
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : left.RegionId.CompareTo(right.RegionId);
        }
    }
}
