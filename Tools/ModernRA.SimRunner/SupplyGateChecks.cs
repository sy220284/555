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

        VerifyPrototypeSupplyNodeOrderIsCanonical();
        VerifyPrototypeSupplyCadenceAndRecovery();
        VerifyBattleGroupRoutesToSupplyNode();

        Console.WriteLine("supply_gate=passed interval_ticks=150 capacities=120+40 levels=sufficient+insufficient+cutoff runtime=true battle_group_resupply=true");
    }

    private static void VerifyPrototypeSupplyNodeOrderIsCanonical()
    {
        AnnihilationPrototypeConfig Build(bool reverse)
        {
            var first = new RuleSupplyNode(1, 1, 0, 0, 120, 700);
            var second = new RuleSupplyNode(2, 2, 3000, 0, 120, 700);
            return new AnnihilationPrototypeConfig
            {
                SpawnA = new Int2(0, 0),
                SpawnB = new Int2(3000, 0),
                SharedCorridor = new[] { new Int2(0, 0), new Int2(1500, 0), new Int2(3000, 0) },
                StartingIndustrialMilli = 0,
                MaxLiveTanksPerTeam = 1,
                SupplyNodes = reverse ? new[] { second, first } : new[] { first, second }
            };
        }

        AnnihilationPrototypeWorld canonical = AnnihilationPrototype.Create(Build(false));
        AnnihilationPrototypeWorld reversed = AnnihilationPrototype.Create(Build(true));
        Check(canonical.SupplyNodes[0].NodeId == 1 && reversed.SupplyNodes[0].NodeId == 1,
            "prototype did not canonicalize supply node order by stable node id");
        Check(AnnihilationPrototype.ComputeStateHash(canonical) == AnnihilationPrototype.ComputeStateHash(reversed),
            "equivalent supply-node input order changed authoritative state hash");
    }

    private static void VerifyPrototypeSupplyCadenceAndRecovery()
    {
        var config = new AnnihilationPrototypeConfig
        {
            SpawnA = new Int2(0, 0),
            SpawnB = new Int2(3000, 0),
            SharedCorridor = new[] { new Int2(0, 0), new Int2(1000, 0), new Int2(2000, 0), new Int2(3000, 0) },
            StartingIndustrialMilli = 0,
            MaxLiveTanksPerTeam = 1,
            SupplyNodes = new[]
            {
                new RuleSupplyNode(1, 1, 0, 0, 5, 100),
                new RuleSupplyNode(2, 2, 3000, 0, 5, 100)
            }
        };
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        Check(world.LastSupplyAllocationTick == 0 && world.SupplyReallocationCount == 1,
            "prototype did not initialize supply state at authoritative tick zero");

        var unit = new PrototypeCombatUnitState
        {
            Id = 9001,
            TeamId = 1,
            X = 500,
            Y = 0,
            CorridorCursor = 1,
            HoldingPosition = true
        };
        world.TeamA.Units.Add(unit);
        for (int i = 0; i < 149; i++)
            AnnihilationPrototype.Step(world);
        Check(unit.SupplyLevel == RuleSupplyLevel.Sufficient && world.LastSupplyAllocationTick == 0,
            "prototype supply changed before the five-second authoritative cadence");

        AnnihilationPrototype.Step(world);
        Check(world.Tick == 150 && world.LastSupplyAllocationTick == 150 && world.SupplyReallocationCount == 2,
            "prototype did not execute supply allocation on tick 150");
        Check(unit.SupplyLevel == RuleSupplyLevel.CutOff && unit.AllocatedSupply == 0 && unit.PrimarySupplyNodeId == -1,
            "out-of-radius prototype unit did not become cut off");

        unit.X = 0;
        unit.Y = 0;
        for (int i = 0; i < 150; i++)
            AnnihilationPrototype.Step(world);
        Check(world.Tick == 300 && world.LastSupplyAllocationTick == 300 && world.SupplyReallocationCount == 3,
            "prototype supply cadence drifted after the second interval");
        Check(unit.SupplyLevel == RuleSupplyLevel.Sufficient && unit.AllocatedSupply == 5 && unit.PrimarySupplyNodeId == 1,
            "unit did not recover supply after returning inside an eligible logistics radius");
    }

    private static void VerifyBattleGroupRoutesToSupplyNode()
    {
        var config = new AnnihilationPrototypeConfig
        {
            SpawnA = new Int2(0, 0),
            SpawnB = new Int2(3000, 0),
            SharedCorridor = new[] { new Int2(0, 0), new Int2(1000, 0), new Int2(2000, 0), new Int2(3000, 0) },
            StartingIndustrialMilli = 0,
            MaxLiveTanksPerTeam = 1,
            SupplyNodes = new[]
            {
                new RuleSupplyNode(11, 1, 2000, 0, 40, 900),
                new RuleSupplyNode(21, 2, 3000, 0, 40, 900)
            }
        };
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        var unit = new PrototypeCombatUnitState
        {
            Id = 9101,
            TeamId = 1,
            X = 2600,
            Y = 0,
            Health = 300,
            CorridorCursor = 3,
            ControlGroupId = 2,
            SupplyLevel = RuleSupplyLevel.CutOff,
            AllocatedSupply = 0,
            PrimarySupplyNodeId = -1
        };
        world.TeamA.Units.Add(unit);

        Check(PrototypeBattleGroupAI.TryPlan(
                world,
                1,
                5,
                2,
                PrototypeBattleGroupStance.Balanced,
                Array.Empty<PrototypeBattleGroupTarget>(),
                out PrototypeBattleGroupDecision damaged),
            "damaged battle group did not produce a resupply decision");
        Check(damaged.Phase == PrototypeBattleGroupPhase.Resupply && damaged.WaypointIndex == 2,
            "battle group resupply did not route to the actual logistics node");

        unit.Health = 1000;
        Check(PrototypeBattleGroupAI.TryPlan(
                world,
                1,
                5,
                2,
                PrototypeBattleGroupStance.Balanced,
                Array.Empty<PrototypeBattleGroupTarget>(),
                out PrototypeBattleGroupDecision idleCutOff),
            "cut-off idle battle group did not produce a logistics decision");
        Check(idleCutOff.Phase == PrototypeBattleGroupPhase.Resupply && idleCutOff.WaypointIndex == 2,
            "cut-off idle battle group ignored its nearest actual supply node");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
