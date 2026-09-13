using ModernRA.Rules;

internal static class Program
{
    private static int Main()
    {
        try
        {
            const int currentTick = 100;
            var canonicalInbox = new PrototypeCommandInbox(maxLeadTicks: 6);
            PrototypePlayerCommand p1Seq100 = Plan(102, 100, 1, PrototypeAnnihilationPlan.Economy);
            PrototypePlayerCommand p1Seq101 = Plan(102, 101, 1, PrototypeAnnihilationPlan.Economy);
            PrototypePlayerCommand p1Seq102 = Plan(102, 102, 1, PrototypeAnnihilationPlan.Aggressive);
            PrototypePlayerCommand p2Seq8 = Plan(101, 8, 2, PrototypeAnnihilationPlan.Aggressive);

            Expect(canonicalInbox.TryAccept(currentTick, p1Seq100), PrototypeCommandAdmissionResult.Accepted);
            Expect(canonicalInbox.TryAccept(currentTick, p1Seq102), PrototypeCommandAdmissionResult.Accepted);
            Expect(canonicalInbox.TryAccept(currentTick, p1Seq101), PrototypeCommandAdmissionResult.Accepted);
            Expect(canonicalInbox.TryAccept(currentTick, p2Seq8), PrototypeCommandAdmissionResult.Accepted);
            Expect(canonicalInbox.TryAccept(currentTick, p1Seq101), PrototypeCommandAdmissionResult.DuplicateSequence);
            Expect(canonicalInbox.TryAccept(currentTick, Plan(102, 30, 1, PrototypeAnnihilationPlan.Aggressive)), PrototypeCommandAdmissionResult.SequenceTooOld);
            Expect(canonicalInbox.TryAccept(currentTick, Plan(100, 103, 1, PrototypeAnnihilationPlan.Aggressive)), PrototypeCommandAdmissionResult.TickNotInFuture);

            PrototypePlayerCommand tooFar = Plan(107, 200, 1, PrototypeAnnihilationPlan.Aggressive);
            Expect(canonicalInbox.TryAccept(currentTick, tooFar), PrototypeCommandAdmissionResult.TickTooFarFuture);
            Expect(canonicalInbox.TryAccept(currentTick, Plan(106, 200, 1, PrototypeAnnihilationPlan.Aggressive)), PrototypeCommandAdmissionResult.Accepted);

            PrototypePlayerCommand invalidPlan = new PrototypePlayerCommand(106, 201, 1, PrototypePlayerCommandKind.SetPlan, 99);
            Expect(canonicalInbox.TryAccept(currentTick, invalidPlan), PrototypeCommandAdmissionResult.InvalidCommand);
            Expect(canonicalInbox.TryAccept(currentTick, Plan(106, 201, 1, PrototypeAnnihilationPlan.Economy)), PrototypeCommandAdmissionResult.Accepted);

            PrototypePlayerCommand[] tick101 = canonicalInbox.DrainForTick(101);
            PrototypePlayerCommand[] tick102 = canonicalInbox.DrainForTick(102);
            AssertOrder(tick101, (2, 8));
            AssertOrder(tick102, (1, 100), (1, 101), (1, 102));

            var reorderedInbox = new PrototypeCommandInbox(maxLeadTicks: 6);
            Expect(reorderedInbox.TryAccept(currentTick, p2Seq8), PrototypeCommandAdmissionResult.Accepted);
            Expect(reorderedInbox.TryAccept(currentTick, p1Seq102), PrototypeCommandAdmissionResult.Accepted);
            Expect(reorderedInbox.TryAccept(currentTick, p1Seq100), PrototypeCommandAdmissionResult.Accepted);
            Expect(reorderedInbox.TryAccept(currentTick, p1Seq101), PrototypeCommandAdmissionResult.Accepted);
            PrototypePlayerCommand[] reordered101 = reorderedInbox.DrainForTick(101);
            PrototypePlayerCommand[] reordered102 = reorderedInbox.DrainForTick(102);

            ulong canonicalHash = HashCombined(tick101, tick102);
            ulong reorderedHash = HashCombined(reordered101, reordered102);
            if (canonicalHash != reorderedHash)
                throw new InvalidOperationException("admitted command order depends on packet arrival order");

            RunBattleGroupAIExecution();
            RunScheduledBattleGroups();
            RunAutonomousBattleGroupMatch();
            RunLiveCommandedMatch();

            Console.WriteLine("PLAYER COMMAND INBOX GATE PASSED");
            Console.WriteLine($"canonical_hash={canonicalHash:X16} pending={canonicalInbox.PendingCount} max_lead_ticks={canonicalInbox.MaxLeadTicks}");
            Console.WriteLine("replay_window_bits=64 duplicate_rejected=true too_old_rejected=true future_window_enforced=true");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PLAYER COMMAND INBOX GATE FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void RunBattleGroupAIExecution()
    {
        AnnihilationPrototypeConfig config = GrayRangeGeneratedData.Create().CreateStandardAnnihilationConfig();
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        while (world.TeamA.Units.Count < 2)
            AnnihilationPrototype.Step(world);

        PrototypeControlGroupRules.AssignUnitFromAI(world.TeamA, world.TeamA.Units[0].Id, 2);
        PrototypeControlGroupRules.AssignUnitFromAI(world.TeamA, world.TeamA.Units[1].Id, 2);
        int attackWaypoint = world.SharedCorridor.Length - 1;
        var visibleTargets = new[]
        {
            new PrototypeBattleGroupTarget(1, attackWaypoint, 1000, 1000, 1000, 1000, 1, RuleIntelLevel.Unknown),
            new PrototypeBattleGroupTarget(20, attackWaypoint, 700, 900, 500, 700, 1200, RuleIntelLevel.Confirmed),
            new PrototypeBattleGroupTarget(10, attackWaypoint, 700, 900, 500, 700, 1200, RuleIntelLevel.Confirmed)
        };

        if (!PrototypeBattleGroupAI.TryPlan(world, 1, 5, 2, PrototypeBattleGroupStance.Balanced, visibleTargets, out PrototypeBattleGroupDecision decision))
            throw new InvalidOperationException("battle-group AI did not produce an executable decision");
        if (decision.TargetId != 10 || decision.Phase != PrototypeBattleGroupPhase.Move)
            throw new InvalidOperationException("battle-group AI target selection was not deterministic or used hidden intelligence");

        var authority = new RuleAIAuthority(
            RuleAIAuthorityLevel.BattleGroup,
            ownerPlayerId: 1,
            regionId: 5,
            forbidden: RuleAIForbiddenAction.None,
            playerOverrideGeneration: world.TeamA.PlayerOverrideGeneration);
        if (!PrototypeBattleGroupAI.TryExecute(world, authority, decision))
            throw new InvalidOperationException("authorized battle-group AI decision was not applied");
        if (world.TeamA.Units.Count(unit => unit.Alive && unit.ControlGroupId == 2 && unit.CorridorCursor == attackWaypoint) != 2)
            throw new InvalidOperationException("battle-group AI did not take control of all assigned units");

        PrototypeControlGroupRules.SetGroupHolding(world.TeamA, 2, true);
        var refreshedAuthority = new RuleAIAuthority(
            RuleAIAuthorityLevel.BattleGroup,
            ownerPlayerId: 1,
            regionId: 5,
            forbidden: RuleAIForbiddenAction.None,
            playerOverrideGeneration: world.TeamA.PlayerOverrideGeneration);
        if (PrototypeBattleGroupAI.TryExecute(world, refreshedAuthority, decision))
            throw new InvalidOperationException("stale battle-group AI decision survived player takeover");

        Console.WriteLine($"battle_group_ai target={decision.TargetId} phase={decision.Phase} score={decision.UtilityScore} units=2 player_takeover=true");
    }

    private static void RunScheduledBattleGroups()
    {
        (ulong worldHash, ulong schedulerHash) first = RunScheduledBattleGroupScenario();
        (ulong worldHash, ulong schedulerHash) replay = RunScheduledBattleGroupScenario();
        if (first != replay)
            throw new InvalidOperationException("multi-group scheduler replay drifted from identical authoritative input");
        Console.WriteLine($"battle_group_scheduler world={first.worldHash:X16} scheduler={first.schedulerHash:X16} groups=2 dynamic_intel=true zone_allocation=true cadence=10hz");
    }

    private static (ulong worldHash, ulong schedulerHash) RunScheduledBattleGroupScenario()
    {
        AnnihilationPrototypeConfig config = GrayRangeGeneratedData.Create().CreateStandardAnnihilationConfig();
        var session = new LiveCommandedAnnihilationSession(config, maxCommandLeadTicks: 12);
        while (session.World.Tick < 500)
            session.Step();

        var scheduler = new PrototypeBattleGroupScheduler();
        var groupOne = scheduler.Register(session.World, new PrototypeBattleGroupOrderSpec(
            1, 5, 1, 1, PrototypeBattleGroupStance.Balanced,
            RuleAIAuthorityLevel.BattleGroup, RuleAIForbiddenAction.None));
        var groupTwo = scheduler.Register(session.World, new PrototypeBattleGroupOrderSpec(
            1, 5, 2, 1, PrototypeBattleGroupStance.Aggressive,
            RuleAIAuthorityLevel.Theater, RuleAIForbiddenAction.None));
        scheduler.SetZoneCandidates(1, new[]
        {
            new PrototypeZoneCandidate(5, 300, 300, 300, 700, 100, false),
            new PrototypeZoneCandidate(7, 800, 700, 600, 800, 1000, true)
        });
        int targetWaypoint = session.World.SharedCorridor.Length - 1;
        scheduler.SetVisibleTargets(1, new[]
        {
            new PrototypeBattleGroupTarget(50, targetWaypoint, 700, 900, 500, 700, 800, RuleIntelLevel.Confirmed)
        });
        session.AttachBattleGroupScheduler(scheduler);

        for (int i = 0; i < 6; i++)
            session.Step();
        if (groupOne.DecisionsExecuted == 0 || groupTwo.DecisionsExecuted == 0)
            throw new InvalidOperationException("staggered multi-group scheduler did not execute both groups");
        if (groupOne.ActiveRegionId != 5 || groupTwo.ActiveRegionId != 7)
            throw new InvalidOperationException("theater allocation reassigned a low-authority group or missed the authorized group");
        if (!PrototypeControlGroupRules.HasAliveMember(session.World.TeamA, 1) ||
            !PrototypeControlGroupRules.HasAliveMember(session.World.TeamA, 2))
        {
            throw new InvalidOperationException("multi-group scheduler did not allocate available units deterministically");
        }

        int takeoverTick = session.World.Tick + 1;
        while (!DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.BattleGroupAI, takeoverTick, 17))
            takeoverTick++;
        Expect(session.Submit(new PrototypePlayerCommand(takeoverTick, 900, 1, PrototypePlayerCommandKind.HoldControlGroup, 1)),
            PrototypeCommandAdmissionResult.Accepted);
        int rejectedBefore = groupOne.DecisionsRejected;
        while (session.World.Tick < takeoverTick)
            session.Step();
        if (groupOne.DecisionsRejected != rejectedBefore + 1)
            throw new InvalidOperationException("scheduled stale AI task survived same-tick player takeover");

        scheduler.RefreshAuthorization(session.World, 1, 1);
        scheduler.SetVisibleTargets(1, Array.Empty<PrototypeBattleGroupTarget>());
        int consolidateTick = session.World.Tick + 1;
        while (!DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.BattleGroupAI, consolidateTick, 17))
            consolidateTick++;
        while (session.World.Tick < consolidateTick)
            session.Step();
        if (groupOne.Phase != PrototypeBattleGroupPhase.Consolidate || groupOne.TargetId != -1)
            throw new InvalidOperationException("dynamic intelligence removal did not transition the group to consolidate");

        PrototypeCombatUnitState serviceUnit = session.World.TeamA.Units.First(unit => unit.Alive && unit.ControlGroupId == 1);
        serviceUnit.Health = 300;
        serviceUnit.X = session.World.SharedCorridor[0].X;
        serviceUnit.Y = session.World.SharedCorridor[0].Y;
        serviceUnit.CorridorCursor = 0;
        serviceUnit.HoldingPosition = true;
        scheduler.RefreshAuthorization(session.World, 1, 1);
        scheduler.SetVisibleTargets(1, new[]
        {
            new PrototypeBattleGroupTarget(51, targetWaypoint, 700, 900, 500, 700, 800, RuleIntelLevel.Confirmed)
        });
        int resupplyTick = session.World.Tick + 1;
        while (!DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.BattleGroupAI, resupplyTick, 17))
            resupplyTick++;
        while (session.World.Tick < resupplyTick - 1)
            session.Step();
        serviceUnit.X = session.World.SharedCorridor[0].X;
        serviceUnit.Y = session.World.SharedCorridor[0].Y;
        serviceUnit.HoldingPosition = false;
        session.Step();
        if (groupOne.Phase != PrototypeBattleGroupPhase.Resupply || !serviceUnit.HoldingPosition || serviceUnit.Health <= 300)
            throw new InvalidOperationException("resupply group did not hold and repair after reaching its service waypoint");

        return (AnnihilationPrototype.ComputeStateHash(session.World), scheduler.ComputeStateHash());
    }

    private static void RunAutonomousBattleGroupMatch()
    {
        (int winner, int tick, ulong worldHash, ulong schedulerHash, int decisions) first =
            RunAutonomousBattleGroupScenario();
        (int winner, int tick, ulong worldHash, ulong schedulerHash, int decisions) replay =
            RunAutonomousBattleGroupScenario();
        if (first != replay)
            throw new InvalidOperationException("autonomous battle-group match replay drifted from identical input");
        if (first.winner == 0 || first.decisions < 2)
            throw new InvalidOperationException("autonomous battle-group match did not reach a decisive AI-driven result");
        Console.WriteLine($"battle_group_auto_match winner={first.winner} tick={first.tick} world={first.worldHash:X16} scheduler={first.schedulerHash:X16} decisions={first.decisions} replay=true");
    }

    private static (int winner, int tick, ulong worldHash, ulong schedulerHash, int decisions)
        RunAutonomousBattleGroupScenario()
    {
        AnnihilationPrototypeConfig config = GrayRangeGeneratedData.Create().CreateStandardAnnihilationConfig();
        var session = new LiveCommandedAnnihilationSession(config, maxCommandLeadTicks: 12);
        var scheduler = new PrototypeBattleGroupScheduler();
        var teamA = scheduler.Register(session.World, new PrototypeBattleGroupOrderSpec(
            1, 5, 1, 6, PrototypeBattleGroupStance.Aggressive,
            RuleAIAuthorityLevel.Theater, RuleAIForbiddenAction.None));
        var teamB = scheduler.Register(session.World, new PrototypeBattleGroupOrderSpec(
            2, 6, 1, 6, PrototypeBattleGroupStance.Balanced,
            RuleAIAuthorityLevel.Theater, RuleAIForbiddenAction.None));
        scheduler.SetZoneCandidates(1, new[]
        {
            new PrototypeZoneCandidate(5, 800, 600, 400, 900, 1000, true)
        });
        scheduler.SetZoneCandidates(2, new[]
        {
            new PrototypeZoneCandidate(6, 700, 650, 500, 800, 1000, true)
        });
        scheduler.SetVisibleTargets(1, new[]
        {
            new PrototypeBattleGroupTarget(200, session.World.SharedCorridor.Length - 1,
                900, 900, 500, 800, 1200, RuleIntelLevel.Confirmed)
        });
        scheduler.SetVisibleTargets(2, new[]
        {
            new PrototypeBattleGroupTarget(100, 0,
                900, 900, 500, 800, 1200, RuleIntelLevel.Confirmed)
        });
        session.AttachBattleGroupScheduler(scheduler);

        AnnihilationPrototypeResult result = session.RunUntilResolved(60000);
        int decisions = teamA.DecisionsExecuted + teamB.DecisionsExecuted;
        if (teamA.DecisionsExecuted == 0 || teamB.DecisionsExecuted == 0)
            throw new InvalidOperationException("one side never executed an autonomous battle-group decision");
        return (result.WinnerTeamId, result.ResolvedTick, result.StateHash, scheduler.ComputeStateHash(), decisions);
    }

    private static void RunLiveCommandedMatch()
    {
        AnnihilationPrototypeConfig config = GrayRangeGeneratedData.Create().CreateStandardAnnihilationConfig();
        var session = new LiveCommandedAnnihilationSession(config, maxCommandLeadTicks: 700);

        Expect(session.Submit(Plan(1, 10, 2, PrototypeAnnihilationPlan.Aggressive)), PrototypeCommandAdmissionResult.Accepted);
        Expect(session.Submit(Plan(1, 10, 1, PrototypeAnnihilationPlan.Economy)), PrototypeCommandAdmissionResult.Accepted);
        session.Step();
        if (session.World.Tick != 1 || session.ExecutedCommandCount != 2)
            throw new InvalidOperationException("live session did not execute admitted tick-one commands");

        var rally = new PrototypePlayerCommand(600, 20, 2, PrototypePlayerCommandKind.SetTeamRallyWaypoint, 0);
        Expect(session.Submit(rally), PrototypeCommandAdmissionResult.Accepted);
        while (session.World.Tick < 500)
            session.Step();

        uint staleAIOrderGeneration = session.World.TeamB.PlayerOverrideGeneration;
        int assignFirst = PrototypeControlGroupPayload.EncodeUnitGroup(2000, 1);
        int assignSecond = PrototypeControlGroupPayload.EncodeUnitGroup(2001, 1);
        Expect(session.Submit(new PrototypePlayerCommand(501, 30, 1, PrototypePlayerCommandKind.AssignUnitToControlGroup, assignFirst)), PrototypeCommandAdmissionResult.InvalidTarget);
        Expect(session.Submit(new PrototypePlayerCommand(501, 30, 2, PrototypePlayerCommandKind.AssignUnitToControlGroup, assignFirst)), PrototypeCommandAdmissionResult.Accepted);
        Expect(session.Submit(new PrototypePlayerCommand(501, 31, 2, PrototypePlayerCommandKind.AssignUnitToControlGroup, assignSecond)), PrototypeCommandAdmissionResult.Accepted);
        session.Step();

        Expect(session.Submit(new PrototypePlayerCommand(502, 32, 2, PrototypePlayerCommandKind.HoldControlGroup, 1)), PrototypeCommandAdmissionResult.Accepted);
        session.Step();
        int heldGroupMembers = session.World.TeamB.Units.Count(unit => unit.Alive && unit.ControlGroupId == 1 && unit.HoldingPosition);
        if (heldGroupMembers != 2)
            throw new InvalidOperationException("control-group hold did not affect both assigned units");
        if (PrototypeControlGroupRules.TryApplyAIWaypoint(session.World, 2, staleAIOrderGeneration, 1, 0))
            throw new InvalidOperationException("stale AI order survived a direct player group command");

        int groupWaypoint = PrototypeControlGroupPayload.EncodeGroupWaypoint(1, 4);
        Expect(session.Submit(new PrototypePlayerCommand(503, 33, 2, PrototypePlayerCommandKind.SetControlGroupWaypoint, groupWaypoint)), PrototypeCommandAdmissionResult.Accepted);
        session.Step();
        if (session.World.TeamB.Units.Count(unit => unit.Alive && unit.ControlGroupId == 1 && !unit.HoldingPosition) != 2)
            throw new InvalidOperationException("control-group waypoint did not resume both assigned units");

        var currentInvalidEnemyHold = new PrototypePlayerCommand(504, 21, 1, PrototypePlayerCommandKind.HoldUnit, 2000);
        Expect(session.Submit(currentInvalidEnemyHold), PrototypeCommandAdmissionResult.InvalidTarget);
        var hold = new PrototypePlayerCommand(600, 21, 2, PrototypePlayerCommandKind.HoldUnit, 2000);
        Expect(session.Submit(hold), PrototypeCommandAdmissionResult.Accepted);
        while (session.World.Tick < 600)
            session.Step();
        PrototypeCombatUnitState heldUnit = session.World.TeamB.Units.Single(unit => unit.Id == 2000);
        if (!heldUnit.HoldingPosition)
            throw new InvalidOperationException("unit hold command did not update authoritative state");

        int waypointPayload = PrototypeUnitWaypointPayload.Encode(2000, 4);
        var unitWaypoint = new PrototypePlayerCommand(601, 22, 2, PrototypePlayerCommandKind.SetUnitWaypoint, waypointPayload);
        Expect(session.Submit(unitWaypoint), PrototypeCommandAdmissionResult.Accepted);
        AnnihilationPrototypeResult result = session.RunUntilResolved(60000);
        if (session.ExecutedCommandCount != 9 || session.PendingCommandCount != 0)
            throw new InvalidOperationException("live session command accounting drifted");

        PrototypePlayerCommand[] executed = session.GetExecutedCommands();
        var replayTimeline = new DeterministicCommandTimeline(executed);
        if (replayTimeline.ComputeCanonicalHash() != session.ComputeExecutedCommandHash())
            throw new InvalidOperationException("live session exported a non-canonical command stream");

        AnnihilationPrototypeWorld replayWorld = AnnihilationPrototype.Create(config);
        for (int i = 0; i < 60000 && !replayWorld.Resolved; i++)
            DeterministicCommandTimeline.StepWithCommands(replayWorld, replayTimeline);
        ulong replayHash = AnnihilationPrototype.ComputeStateHash(replayWorld);
        if (!replayWorld.Resolved || replayWorld.Tick != result.ResolvedTick || replayHash != result.StateHash)
            throw new InvalidOperationException("live admitted command stream did not replay to the authoritative result");

        Expect(session.Submit(Plan(result.ResolvedTick + 1, 21, 1, PrototypeAnnihilationPlan.Aggressive)), PrototypeCommandAdmissionResult.MatchResolved);
        Console.WriteLine($"live_match winner={result.WinnerTeamId} tick={result.ResolvedTick} hash={result.StateHash:X16} commands={executed.Length} control_group=true stale_ai_rejected=true");
    }

    private static PrototypePlayerCommand Plan(int tick, int sequence, int playerId, PrototypeAnnihilationPlan plan)
    {
        return new PrototypePlayerCommand(tick, sequence, playerId, PrototypePlayerCommandKind.SetPlan, (int)plan);
    }

    private static void Expect(PrototypeCommandAdmissionResult actual, PrototypeCommandAdmissionResult expected)
    {
        if (actual != expected)
            throw new InvalidOperationException($"expected admission {expected}, got {actual}");
    }

    private static void AssertOrder(PrototypePlayerCommand[] commands, params (int PlayerId, int Sequence)[] expected)
    {
        if (commands.Length != expected.Length)
            throw new InvalidOperationException($"expected {expected.Length} commands, got {commands.Length}");
        for (int i = 0; i < expected.Length; i++)
        {
            if (commands[i].PlayerId != expected[i].PlayerId || commands[i].Sequence != expected[i].Sequence)
                throw new InvalidOperationException($"unexpected command order at index {i}");
        }
    }

    private static ulong HashCombined(params PrototypePlayerCommand[][] batches)
    {
        var all = new List<PrototypePlayerCommand>();
        for (int i = 0; i < batches.Length; i++)
            all.AddRange(batches[i]);
        return new DeterministicCommandTimeline(all).ComputeCanonicalHash();
    }
}
