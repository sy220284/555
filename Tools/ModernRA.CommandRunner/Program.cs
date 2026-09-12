using ModernRA.Rules;

internal static class Program
{
    private const int WatchdogTicks = 60000;
    private const int Repetitions = 8;

    private static int Main()
    {
        try
        {
            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            AnnihilationPrototypeConfig config = map.CreateStandardAnnihilationConfig();
            PrototypePlayerCommand[] commands = CreateCommands();

            PrototypePlayerCommand[] reversed = (PrototypePlayerCommand[])commands.Clone();
            Array.Reverse(reversed);
            var canonical = new DeterministicCommandTimeline(commands);
            var reversedTimeline = new DeterministicCommandTimeline(reversed);
            if (canonical.ComputeCanonicalHash() != reversedTimeline.ComputeCanonicalHash())
                throw new InvalidOperationException("command timeline hash depends on insertion order");

            CommandRunResult? expected = null;
            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                PrototypePlayerCommand[] input = repetition % 2 == 0 ? commands : reversed;
                CommandRunResult result = Run(config, input);
                if (expected.HasValue && !expected.Value.Equals(result))
                    throw new InvalidOperationException($"commanded run drifted on repetition {repetition}");
                expected = result;
            }

            AnnihilationPrototypeWorld baselineWorld = AnnihilationPrototype.Create(config);
            AnnihilationPrototypeResult baseline = AnnihilationPrototype.Run(baselineWorld, WatchdogTicks);
            CommandRunResult commanded = expected ?? throw new InvalidOperationException("no commanded result produced");
            if (commanded.StateHash == baseline.StateHash && commanded.ResolvedTick == baseline.ResolvedTick)
                throw new InvalidOperationException("command stream did not affect authoritative match result");

            AssertInvalidCommandsAreRejected();
            ReplayRoundTrip(config, canonical, commanded);

            Console.WriteLine("PLAYER COMMAND GATE PASSED");
            Console.WriteLine($"commands={commands.Length} command_hash={canonical.ComputeCanonicalHash():X16}");
            Console.WriteLine($"commanded winner={commanded.WinnerTeamId} tick={commanded.ResolvedTick} hash={commanded.StateHash:X16}");
            Console.WriteLine($"baseline winner={baseline.WinnerTeamId} tick={baseline.ResolvedTick} hash={baseline.StateHash:X16}");
            Console.WriteLine($"repetitions={Repetitions} insertion_order_independent=true");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PLAYER COMMAND GATE FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static CommandRunResult Run(AnnihilationPrototypeConfig config, PrototypePlayerCommand[] commands)
    {
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        var timeline = new DeterministicCommandTimeline(commands);
        for (int i = 0; i < WatchdogTicks && !world.Resolved; i++)
            DeterministicCommandTimeline.StepWithCommands(world, timeline);
        if (!world.Resolved)
            throw new InvalidOperationException("commanded annihilation scenario did not resolve before watchdog");
        return new CommandRunResult(world.WinnerTeamId, world.Tick, AnnihilationPrototype.ComputeStateHash(world));
    }

    private static PrototypePlayerCommand[] CreateCommands()
    {
        return new[]
        {
            // Permanently swap the prototype production plans at the first authoritative tick.
            // This guarantees the command stream changes authoritative inputs instead of
            // temporarily diverging and later returning to the baseline plans.
            new PrototypePlayerCommand(1, 10, 2, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Aggressive),
            new PrototypePlayerCommand(1, 10, 1, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Economy)
        };
    }

    private static void ReplayRoundTrip(AnnihilationPrototypeConfig config, DeterministicCommandTimeline canonical, CommandRunResult expected)
    {
        PlayerCommandReplayDocument document = PlayerCommandReplayFile.Create(
            GrayRangeGeneratedData.MapId,
            canonical.ToCanonicalArray(),
            canonical.ComputeCanonicalHash(),
            expected.WinnerTeamId,
            expected.ResolvedTick,
            expected.StateHash);

        string path = Path.Combine(Path.GetTempPath(), $"modernra-command-{Guid.NewGuid():N}.json");
        try
        {
            PlayerCommandReplayFile.Write(path, document);
            byte[] firstBytes = File.ReadAllBytes(path);
            PlayerCommandReplayDocument loaded = PlayerCommandReplayFile.Read(path);
            PrototypePlayerCommand[] loadedCommands = PlayerCommandReplayFile.ToCommands(loaded);
            CommandRunResult replayed = Run(config, loadedCommands);
            if (!replayed.Equals(expected))
                throw new InvalidOperationException("persisted command replay changed authoritative result");
            PlayerCommandReplayFile.ValidateOutcome(loaded, replayed.WinnerTeamId, replayed.ResolvedTick, replayed.StateHash);

            byte[] secondBytes = PlayerCommandReplayFile.Serialize(loaded);
            if (!firstBytes.AsSpan().SequenceEqual(secondBytes))
                throw new InvalidOperationException("command replay serialization is not byte-stable");

            Console.WriteLine($"command_replay_bytes={firstBytes.Length} command_replay_sha256={PlayerCommandReplayFile.Sha256Hex(firstBytes)}");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void AssertInvalidCommandsAreRejected()
    {
        ExpectArgumentFailure(new[]
        {
            new PrototypePlayerCommand(10, 1, 1, PrototypePlayerCommandKind.SetPlan, 0),
            new PrototypePlayerCommand(10, 1, 1, PrototypePlayerCommandKind.SetPlan, 1)
        });
        ExpectArgumentFailure(new[]
        {
            new PrototypePlayerCommand(0, 1, 1, PrototypePlayerCommandKind.SetPlan, 0)
        });
        ExpectArgumentFailure(new[]
        {
            new PrototypePlayerCommand(10, 1, 3, PrototypePlayerCommandKind.SetPlan, 0)
        });
    }

    private static void ExpectArgumentFailure(PrototypePlayerCommand[] commands)
    {
        try
        {
            _ = new DeterministicCommandTimeline(commands);
        }
        catch (ArgumentException)
        {
            return;
        }
        throw new InvalidOperationException("invalid command set was accepted");
    }

    private readonly record struct CommandRunResult(int WinnerTeamId, int ResolvedTick, ulong StateHash);
}
