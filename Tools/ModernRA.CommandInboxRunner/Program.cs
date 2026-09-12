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
