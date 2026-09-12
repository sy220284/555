using ModernRA.Rules;

internal static class SupplyGateChecks
{
    public static void Run()
    {
        Check(RuleSupplyAllocator.ReallocationIntervalTicks == 150, "supply reallocation interval must remain 5 seconds at 30Hz");
        Check(RuleSupplyAllocator.ShouldReallocate(0), "initial supply allocation should run at tick zero");
        Check(!RuleSupplyAllocator.ShouldReallocate(149), "supply reallocated before the five-second interval");
        Check(RuleSupplyAllocator.ShouldReallocate(150), "supply did not reallocate at the five-second interval");

        var nodes = new[]
        {
            new RuleSupplyNode(20, 1, 500, 0, 40, 2000),
            new RuleSupplyNode(10, 1, 0, 0, 120, 2000),
            new RuleSupplyNode(30, 2, 0, 0, 120, 2000)
        };
        var consumers = new[]
        {
            new RuleSupplyConsumer(3, 1, 5000, 0, 10),
            new RuleSupplyConsumer(2, 1, 100, 0, 60),
            new RuleSupplyConsumer(1, 1, 400, 0, 120),
            new RuleSupplyConsumer(4, 2, 0, 0, 30)
        };

        RuleSupplyAllocationResult result = RuleSupplyAllocator.Allocate(nodes, consumers);
        Check(result.Allocations.Length == 4, "supply allocator lost consumers");
        Check(result.Allocations[0].EntityId == 1 && result.Allocations[0].Level == RuleSupplyLevel.Sufficient,
            "overlapping supply nodes did not fully satisfy the first consumer");
        Check(result.Allocations[0].PrimaryNodeId == 20,
            "consumer did not draw from the nearest eligible supply node first");
        Check(result.Allocations[1].EntityId == 2 && result.Allocations[1].Level == RuleSupplyLevel.Insufficient,
            "capacity exhaustion did not produce an insufficient supply state");
        Check(result.Allocations[1].Allocated == 40,
            "remaining supply capacity was not allocated deterministically");
        Check(result.Allocations[2].EntityId == 3 && result.Allocations[2].Level == RuleSupplyLevel.CutOff,
            "out-of-radius consumer was not marked cut off");
        Check(result.Allocations[3].EntityId == 4 && result.Allocations[3].Level == RuleSupplyLevel.Sufficient,
            "enemy-team supply node did not remain isolated by team");

        RuleSupplyAllocationResult reverse = RuleSupplyAllocator.Allocate(
            new[] { nodes[2], nodes[1], nodes[0] },
            new[] { consumers[3], consumers[2], consumers[1], consumers[0] });
        for (int i = 0; i < result.Allocations.Length; i++)
        {
            Check(result.Allocations[i].EntityId == reverse.Allocations[i].EntityId, "supply consumer order changed canonical output");
            Check(result.Allocations[i].Allocated == reverse.Allocations[i].Allocated, "supply allocation depends on input ordering");
            Check(result.Allocations[i].Level == reverse.Allocations[i].Level, "supply level depends on input ordering");
            Check(result.Allocations[i].PrimaryNodeId == reverse.Allocations[i].PrimaryNodeId, "primary supply node depends on input ordering");
        }

        Check(RuleSupplyEffects.RepairPermille(RuleSupplyLevel.Sufficient) == 1000, "sufficient repair multiplier mismatch");
        Check(RuleSupplyEffects.RepairPermille(RuleSupplyLevel.Insufficient) == 700, "insufficient repair multiplier mismatch");
        Check(RuleSupplyEffects.RepairPermille(RuleSupplyLevel.CutOff) == 250, "cut-off repair multiplier mismatch");
        Check(RuleSupplyEffects.AdvancedAmmoReplenishmentPermille(RuleSupplyLevel.Insufficient) == 600,
            "insufficient advanced-ammo multiplier mismatch");
        Check(RuleSupplyEffects.AdvancedAmmoReplenishmentPermille(RuleSupplyLevel.CutOff) == 0,
            "cut-off units must not replenish advanced ammunition");

        Console.WriteLine("supply_gate=passed interval_ticks=150 capacities=120+40 levels=sufficient+insufficient+cutoff");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
