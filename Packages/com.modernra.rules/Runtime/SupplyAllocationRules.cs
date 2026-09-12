using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum RuleSupplyLevel : byte
    {
        Sufficient = 0,
        Insufficient = 1,
        CutOff = 2
    }

    public readonly struct RuleSupplyNode
    {
        public readonly int NodeId;
        public readonly int TeamId;
        public readonly int X;
        public readonly int Y;
        public readonly int Capacity;
        public readonly int Radius;
        public readonly bool Enabled;

        public RuleSupplyNode(int nodeId, int teamId, int x, int y, int capacity, int radius, bool enabled = true)
        {
            if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(nodeId));
            if (teamId <= 0) throw new ArgumentOutOfRangeException(nameof(teamId));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            NodeId = nodeId;
            TeamId = teamId;
            X = x;
            Y = y;
            Capacity = capacity;
            Radius = radius;
            Enabled = enabled;
        }
    }

    public readonly struct RuleSupplyConsumer
    {
        public readonly int EntityId;
        public readonly int TeamId;
        public readonly int X;
        public readonly int Y;
        public readonly int SupplyUse;

        public RuleSupplyConsumer(int entityId, int teamId, int x, int y, int supplyUse)
        {
            if (entityId <= 0) throw new ArgumentOutOfRangeException(nameof(entityId));
            if (teamId <= 0) throw new ArgumentOutOfRangeException(nameof(teamId));
            if (supplyUse <= 0) throw new ArgumentOutOfRangeException(nameof(supplyUse));
            EntityId = entityId;
            TeamId = teamId;
            X = x;
            Y = y;
            SupplyUse = supplyUse;
        }
    }

    public readonly struct RuleSupplyAllocation
    {
        public readonly int EntityId;
        public readonly int Requested;
        public readonly int Allocated;
        public readonly int PrimaryNodeId;
        public readonly RuleSupplyLevel Level;

        public RuleSupplyAllocation(int entityId, int requested, int allocated, int primaryNodeId, RuleSupplyLevel level)
        {
            EntityId = entityId;
            Requested = requested;
            Allocated = allocated;
            PrimaryNodeId = primaryNodeId;
            Level = level;
        }
    }

    public readonly struct RuleSupplyNodeRemainder
    {
        public readonly int NodeId;
        public readonly int RemainingCapacity;

        public RuleSupplyNodeRemainder(int nodeId, int remainingCapacity)
        {
            NodeId = nodeId;
            RemainingCapacity = remainingCapacity;
        }
    }

    public sealed class RuleSupplyAllocationResult
    {
        public RuleSupplyAllocationResult(RuleSupplyAllocation[] allocations, RuleSupplyNodeRemainder[] nodeRemainders)
        {
            Allocations = allocations ?? throw new ArgumentNullException(nameof(allocations));
            NodeRemainders = nodeRemainders ?? throw new ArgumentNullException(nameof(nodeRemainders));
        }

        public RuleSupplyAllocation[] Allocations { get; }
        public RuleSupplyNodeRemainder[] NodeRemainders { get; }
    }

    public static class RuleSupplyAllocator
    {
        public const int ReallocationIntervalTicks = 5 * 30;

        public static bool ShouldReallocate(int authoritativeTick)
        {
            if (authoritativeTick < 0) throw new ArgumentOutOfRangeException(nameof(authoritativeTick));
            return authoritativeTick % ReallocationIntervalTicks == 0;
        }

        public static RuleSupplyAllocationResult Allocate(
            IReadOnlyList<RuleSupplyNode> nodes,
            IReadOnlyList<RuleSupplyConsumer> consumers)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (consumers == null) throw new ArgumentNullException(nameof(consumers));

            RuleSupplyNode[] sortedNodes = CopyAndValidateNodes(nodes);
            RuleSupplyConsumer[] sortedConsumers = CopyAndValidateConsumers(consumers);
            var remaining = new int[sortedNodes.Length];
            for (int i = 0; i < sortedNodes.Length; i++)
                remaining[i] = sortedNodes[i].Enabled ? sortedNodes[i].Capacity : 0;

            var allocations = new RuleSupplyAllocation[sortedConsumers.Length];
            var candidateIndices = new List<int>(sortedNodes.Length);
            for (int c = 0; c < sortedConsumers.Length; c++)
            {
                RuleSupplyConsumer consumer = sortedConsumers[c];
                candidateIndices.Clear();
                for (int n = 0; n < sortedNodes.Length; n++)
                {
                    RuleSupplyNode node = sortedNodes[n];
                    if (!node.Enabled || node.TeamId != consumer.TeamId || remaining[n] <= 0)
                        continue;
                    long dx = (long)consumer.X - node.X;
                    long dy = (long)consumer.Y - node.Y;
                    if (dx * dx + dy * dy <= (long)node.Radius * node.Radius)
                        candidateIndices.Add(n);
                }
                candidateIndices.Sort((left, right) => CompareNodeDistance(consumer, sortedNodes[left], sortedNodes[right]));

                int need = consumer.SupplyUse;
                int allocated = 0;
                int primaryNodeId = -1;
                for (int i = 0; i < candidateIndices.Count && need > 0; i++)
                {
                    int nodeIndex = candidateIndices[i];
                    int take = Math.Min(need, remaining[nodeIndex]);
                    if (take <= 0)
                        continue;
                    if (primaryNodeId < 0)
                        primaryNodeId = sortedNodes[nodeIndex].NodeId;
                    remaining[nodeIndex] -= take;
                    allocated += take;
                    need -= take;
                }

                RuleSupplyLevel level = allocated >= consumer.SupplyUse
                    ? RuleSupplyLevel.Sufficient
                    : allocated > 0 ? RuleSupplyLevel.Insufficient : RuleSupplyLevel.CutOff;
                allocations[c] = new RuleSupplyAllocation(
                    consumer.EntityId,
                    consumer.SupplyUse,
                    allocated,
                    primaryNodeId,
                    level);
            }

            var remainders = new RuleSupplyNodeRemainder[sortedNodes.Length];
            for (int i = 0; i < sortedNodes.Length; i++)
                remainders[i] = new RuleSupplyNodeRemainder(sortedNodes[i].NodeId, remaining[i]);
            return new RuleSupplyAllocationResult(allocations, remainders);
        }

        private static RuleSupplyNode[] CopyAndValidateNodes(IReadOnlyList<RuleSupplyNode> nodes)
        {
            var copy = new RuleSupplyNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) copy[i] = nodes[i];
            Array.Sort(copy, (left, right) => left.NodeId.CompareTo(right.NodeId));
            for (int i = 1; i < copy.Length; i++)
                if (copy[i - 1].NodeId == copy[i].NodeId)
                    throw new ArgumentException($"duplicate supply node {copy[i].NodeId}", nameof(nodes));
            return copy;
        }

        private static RuleSupplyConsumer[] CopyAndValidateConsumers(IReadOnlyList<RuleSupplyConsumer> consumers)
        {
            var copy = new RuleSupplyConsumer[consumers.Count];
            for (int i = 0; i < consumers.Count; i++) copy[i] = consumers[i];
            Array.Sort(copy, (left, right) => left.EntityId.CompareTo(right.EntityId));
            for (int i = 1; i < copy.Length; i++)
                if (copy[i - 1].EntityId == copy[i].EntityId)
                    throw new ArgumentException($"duplicate supply consumer {copy[i].EntityId}", nameof(consumers));
            return copy;
        }

        private static int CompareNodeDistance(RuleSupplyConsumer consumer, RuleSupplyNode left, RuleSupplyNode right)
        {
            long leftDx = (long)consumer.X - left.X;
            long leftDy = (long)consumer.Y - left.Y;
            long rightDx = (long)consumer.X - right.X;
            long rightDy = (long)consumer.Y - right.Y;
            long leftDistance = leftDx * leftDx + leftDy * leftDy;
            long rightDistance = rightDx * rightDx + rightDy * rightDy;
            int distance = leftDistance.CompareTo(rightDistance);
            return distance != 0 ? distance : left.NodeId.CompareTo(right.NodeId);
        }
    }

    public static class RuleSupplyEffects
    {
        public static int RepairPermille(RuleSupplyLevel level)
        {
            return level switch
            {
                RuleSupplyLevel.Sufficient => 1000,
                RuleSupplyLevel.Insufficient => 700,
                RuleSupplyLevel.CutOff => 250,
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };
        }

        public static int AdvancedAmmoReplenishmentPermille(RuleSupplyLevel level)
        {
            return level switch
            {
                RuleSupplyLevel.Sufficient => 1000,
                RuleSupplyLevel.Insufficient => 600,
                RuleSupplyLevel.CutOff => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };
        }

        public static int SkillCooldownPermille(RuleSupplyLevel level)
        {
            return level switch
            {
                RuleSupplyLevel.Sufficient => 1000,
                RuleSupplyLevel.Insufficient => 1150,
                RuleSupplyLevel.CutOff => 1150,
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };
        }
    }
}
