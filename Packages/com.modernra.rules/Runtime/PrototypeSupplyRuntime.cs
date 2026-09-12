using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public static class PrototypeSupplyRuntime
    {
        public const int StandardBaseSupplyCapacity = 120;
        public const int DefaultTankSupplyUse = 5;

        public static RuleSupplyNode[] CopyCanonicalNodes(RuleSupplyNode[]? nodes)
        {
            if (nodes == null || nodes.Length == 0)
                return Array.Empty<RuleSupplyNode>();

            var copy = (RuleSupplyNode[])nodes.Clone();
            Array.Sort(copy, (left, right) => left.NodeId.CompareTo(right.NodeId));
            for (int i = 1; i < copy.Length; i++)
            {
                if (copy[i - 1].NodeId == copy[i].NodeId)
                    throw new ArgumentException($"duplicate supply node {copy[i].NodeId}", nameof(nodes));
            }
            return copy;
        }

        public static void ReallocateIfDue(AnnihilationPrototypeWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (RuleSupplyAllocator.ShouldReallocate(world.Tick))
                Reallocate(world);
        }

        public static void Reallocate(AnnihilationPrototypeWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            var consumers = new List<RuleSupplyConsumer>();
            var units = new Dictionary<int, PrototypeCombatUnitState>();
            AddConsumers(world.TeamA, consumers, units);
            AddConsumers(world.TeamB, consumers, units);

            if (world.SupplyNodes.Length == 0)
            {
                foreach (PrototypeCombatUnitState unit in units.Values)
                {
                    unit.AllocatedSupply = unit.SupplyUse;
                    unit.PrimarySupplyNodeId = -1;
                    unit.SupplyLevel = RuleSupplyLevel.Sufficient;
                }
                world.SupplyNodeRemainders = Array.Empty<RuleSupplyNodeRemainder>();
                world.LastSupplyAllocationTick = world.Tick;
                world.SupplyReallocationCount++;
                return;
            }

            RuleSupplyAllocationResult result = RuleSupplyAllocator.Allocate(world.SupplyNodes, consumers);
            for (int i = 0; i < result.Allocations.Length; i++)
            {
                RuleSupplyAllocation allocation = result.Allocations[i];
                if (!units.TryGetValue(allocation.EntityId, out PrototypeCombatUnitState? unit))
                    throw new InvalidOperationException($"supply allocation referenced unknown unit {allocation.EntityId}");
                unit.AllocatedSupply = allocation.Allocated;
                unit.PrimarySupplyNodeId = allocation.PrimaryNodeId;
                unit.SupplyLevel = allocation.Level;
            }

            world.SupplyNodeRemainders = (RuleSupplyNodeRemainder[])result.NodeRemainders.Clone();
            world.LastSupplyAllocationTick = world.Tick;
            world.SupplyReallocationCount++;
        }

        public static bool TryFindResupplyWaypoint(
            AnnihilationPrototypeWorld world,
            int playerId,
            int x,
            int y,
            out int waypointIndex)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (playerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerId));

            bool found = false;
            RuleSupplyNode selected = default;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < world.SupplyNodes.Length; i++)
            {
                RuleSupplyNode node = world.SupplyNodes[i];
                if (!node.Enabled || node.TeamId != playerId || node.Capacity <= 0)
                    continue;
                long dx = (long)x - node.X;
                long dy = (long)y - node.Y;
                long distance = checked(dx * dx + dy * dy);
                if (!found || distance < bestDistance || (distance == bestDistance && node.NodeId < selected.NodeId))
                {
                    found = true;
                    selected = node;
                    bestDistance = distance;
                }
            }

            if (!found || world.SharedCorridor.Length == 0)
            {
                waypointIndex = -1;
                return false;
            }

            waypointIndex = 0;
            long bestWaypointDistance = long.MaxValue;
            for (int i = 0; i < world.SharedCorridor.Length; i++)
            {
                Int2 point = world.SharedCorridor[i];
                long dx = (long)point.X - selected.X;
                long dy = (long)point.Y - selected.Y;
                long distance = checked(dx * dx + dy * dy);
                if (distance < bestWaypointDistance)
                {
                    bestWaypointDistance = distance;
                    waypointIndex = i;
                }
            }
            return true;
        }

        private static void AddConsumers(
            PrototypeAnnihilationTeamState team,
            List<RuleSupplyConsumer> consumers,
            Dictionary<int, PrototypeCombatUnitState> units)
        {
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive)
                    continue;
                if (unit.SupplyUse <= 0)
                    throw new InvalidOperationException($"unit {unit.Id} has invalid supply use {unit.SupplyUse}");
                if (!units.TryAdd(unit.Id, unit))
                    throw new InvalidOperationException($"duplicate supply consumer unit id {unit.Id}");
                consumers.Add(new RuleSupplyConsumer(unit.Id, unit.TeamId, unit.X, unit.Y, unit.SupplyUse));
            }
        }
    }
}
