using ModernRA.Rules;
using System.Text.Json;

internal static class Program
{
    private const int WatchdogTicks = 60000;
    private const int Repetitions = 8;
    private const int ReplayCheckpointIntervalTicks = 120;

    private static int Main()
    {
        try
        {
            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            AnnihilationPrototypeConfig config = map.CreateStandardAnnihilationConfig();
            PrototypePlayerCommand[] planCommands = CreatePlanCommands();
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

            CommandRunResult plansOnly = Run(config, planCommands);
            if (commanded.StateHash == plansOnly.StateHash && commanded.ResolvedTick == plansOnly.ResolvedTick)
                throw new InvalidOperationException("rally waypoint command did not affect authoritative match result");

            AssertInvalidCommandsAreRejected();
            ReplayRoundTrip(config, map.MapId, canonical, commanded);

            Console.WriteLine("PLAYER COMMAND GATE PASSED");
            Console.WriteLine($"commands={commands.Length} command_hash={canonical.ComputeCanonicalHash():X16}");
            Console.WriteLine($"commanded winner={commanded.WinnerTeamId} tick={commanded.ResolvedTick} hash={commanded.StateHash:X16}");
            Console.WriteLine($"plans_only winner={plansOnly.WinnerTeamId} tick={plansOnly.ResolvedTick} hash={plansOnly.StateHash:X16}");
            Console.WriteLine($"baseline winner={baseline.WinnerTeamId} tick={baseline.ResolvedTick} hash={baseline.StateHash:X16}");
            Console.WriteLine($"repetitions={Repetitions} insertion_order_independent=true tactical_rally=true");
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

    private static PrototypePlayerCommand[] CreatePlanCommands()
    {
        return new[]
        {
            new PrototypePlayerCommand(1, 10, 2, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Aggressive),
            new PrototypePlayerCommand(1, 10, 1, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Economy)
        };
    }

    private static PrototypePlayerCommand[] CreateCommands()
    {
        return new[]
        {
            // Permanently swap the production plans, then redirect the current aggressive
            // battle group toward the enemy end of the authoritative shared corridor.
            new PrototypePlayerCommand(1, 10, 2, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Aggressive),
            new PrototypePlayerCommand(1, 10, 1, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Economy),
            new PrototypePlayerCommand(600, 20, 2, PrototypePlayerCommandKind.SetTeamRallyWaypoint, 0)
        };
    }

    private static void ReplayRoundTrip(AnnihilationPrototypeConfig config, string scenarioId, DeterministicCommandTimeline canonical, CommandRunResult expected)
    {
        PrototypePlayerCommand[] canonicalCommands = canonical.ToCanonicalArray();
        PlayerCommandCheckpointRecord[] checkpoints = RecordCheckpoints(config, canonicalCommands,
            out PrototypeAuthorityEvent[] authorityEvents);
        PlayerCommandReplayDocument document = PlayerCommandReplayFile.Create(
            scenarioId,
            config,
            canonicalCommands,
            canonical.ComputeCanonicalHash(),
            expected.WinnerTeamId,
            expected.ResolvedTick,
            expected.StateHash,
            checkpoints,
            authorityEvents);

        string path = Path.Combine(Path.GetTempPath(), $"modernra-command-{Guid.NewGuid():N}.json");
        try
        {
            PlayerCommandReplayFile.Write(path, document);
            byte[] firstBytes = File.ReadAllBytes(path);
            PlayerCommandReplayDocument loaded = PlayerCommandReplayFile.Read(path);
            PlayerCommandReplayFile.ValidateScenario(loaded, scenarioId);
            AnnihilationPrototypeConfig replayConfig = PlayerCommandReplayFile.ToInitialConfig(loaded);
            PrototypePlayerCommand[] loadedCommands = PlayerCommandReplayFile.ToCommands(loaded);
            PlayerCommandCheckpointRecord[] loadedCheckpoints = PlayerCommandReplayFile.ToCheckpoints(loaded);
            CommandRunResult replayed = Run(replayConfig, loadedCommands);
            if (!replayed.Equals(expected))
                throw new InvalidOperationException("persisted command replay changed authoritative result");
            PlayerCommandReplayFile.ValidateOutcome(loaded, replayed.WinnerTeamId, replayed.ResolvedTick, replayed.StateHash);
            VerifyCheckpoints(replayConfig, loadedCommands, loadedCheckpoints,
                PlayerCommandReplayFile.ToAuthorityEvents(loaded));

            byte[] secondBytes = PlayerCommandReplayFile.Serialize(loaded);
            if (!firstBytes.AsSpan().SequenceEqual(secondBytes))
                throw new InvalidOperationException("command replay serialization is not byte-stable");

            var versionTwo = new PlayerCommandReplayV2Document
            {
                FormatVersion = 2,
                GameVersion = PlayerCommandReplayFile.GameVersion,
                ContentHash = GrayRangeGeneratedData.SourceMapSha256,
                MapId = scenarioId,
                TimestampUtc = DateTimeOffset.UnixEpoch,
                ProtocolVersion = PlayerCommandReplayFile.ProtocolVersion,
                ScenarioId = scenarioId,
                MapSourceSha256 = GrayRangeGeneratedData.SourceMapSha256,
                RulesetId = PlayerCommandReplayFile.RulesetId,
                CommandHash = canonical.ComputeCanonicalHash().ToString("X16"),
                WinnerTeamId = expected.WinnerTeamId,
                ResolvedTick = expected.ResolvedTick,
                FinalStateHash = expected.StateHash.ToString("X16"),
                Commands = ToLegacyRecords(canonicalCommands),
                Checkpoints = checkpoints
            };
            File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(versionTwo, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }));
            PlayerCommandReplayDocument migratedV2 = PlayerCommandReplayFile.Read(path);
            if (migratedV2.FormatVersion != PlayerCommandReplayFile.FormatVersion ||
                PlayerCommandReplayFile.ToCommands(migratedV2).Length != loadedCommands.Length ||
                PlayerCommandReplayFile.ToCheckpoints(migratedV2).Length != checkpoints.Length)
                throw new InvalidOperationException("command replay v2 migration lost events or initial state");

            var legacy = new PlayerCommandReplayV1Document
            {
                SchemaVersion = 1,
                ScenarioId = scenarioId,
                MapSourceSha256 = GrayRangeGeneratedData.SourceMapSha256,
                RulesetId = PlayerCommandReplayFile.RulesetId,
                CommandHash = canonical.ComputeCanonicalHash().ToString("X16"),
                WinnerTeamId = expected.WinnerTeamId,
                ResolvedTick = expected.ResolvedTick,
                FinalStateHash = expected.StateHash.ToString("X16"),
                Commands = ToLegacyRecords(canonicalCommands)
            };
            byte[] legacyBytes = JsonSerializer.SerializeToUtf8Bytes(legacy, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            File.WriteAllBytes(path, legacyBytes);
            PlayerCommandReplayDocument migrated = PlayerCommandReplayFile.Read(path);
            if (migrated.FormatVersion != PlayerCommandReplayFile.FormatVersion ||
                PlayerCommandReplayFile.ToCommands(migrated).Length != loadedCommands.Length)
                throw new InvalidOperationException("command replay v1 migration lost commands or version metadata");

            Console.WriteLine($"command_replay_bytes={firstBytes.Length} command_replay_sha256={PlayerCommandReplayFile.Sha256Hex(firstBytes)} checkpoints={checkpoints.Length} unified_events={document.Events.Length} migration_v1_v2_v3=true");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static PlayerCommandRecord[] ToLegacyRecords(PrototypePlayerCommand[] commands)
    {
        var records = new PlayerCommandRecord[commands.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            PrototypePlayerCommand command = commands[i];
            records[i] = new PlayerCommandRecord
            {
                Tick = command.Tick,
                Sequence = command.Sequence,
                PlayerId = command.PlayerId,
                Kind = (int)command.Kind,
                IntValue = command.IntValue
            };
        }
        return records;
    }

    private static PlayerCommandCheckpointRecord[] RecordCheckpoints(
        AnnihilationPrototypeConfig config, PrototypePlayerCommand[] commands,
        out PrototypeAuthorityEvent[] authorityEvents)
    {
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        var timeline = new DeterministicCommandTimeline(commands);
        var checkpoints = new List<PlayerCommandCheckpointRecord>();
        while (!world.Resolved && world.Tick < WatchdogTicks)
        {
            DeterministicCommandTimeline.StepWithCommands(world, timeline);
            if (world.Tick % ReplayCheckpointIntervalTicks == 0 || world.Resolved)
            {
                checkpoints.Add(new PlayerCommandCheckpointRecord
                {
                    Tick = world.Tick,
                    StateHash = AnnihilationPrototype.ComputeStateHash(world).ToString("X16")
                });
            }
        }
        if (!world.Resolved)
            throw new InvalidOperationException("command checkpoint recording exceeded watchdog");
        authorityEvents = world.AuthorityEvents.ToArray();
        return checkpoints.ToArray();
    }

    private static void VerifyCheckpoints(AnnihilationPrototypeConfig config,
        PrototypePlayerCommand[] commands, PlayerCommandCheckpointRecord[] checkpoints,
        PrototypeAuthorityEvent[] authorityEvents)
    {
        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        var timeline = new DeterministicCommandTimeline(commands);
        int checkpointIndex = 0;
        while (!world.Resolved && world.Tick < WatchdogTicks)
        {
            DeterministicCommandTimeline.StepWithCommands(world, timeline);
            if (checkpointIndex >= checkpoints.Length || world.Tick < checkpoints[checkpointIndex].Tick)
                continue;
            PlayerCommandCheckpointRecord expected = checkpoints[checkpointIndex];
            if (world.Tick != expected.Tick ||
                !string.Equals(AnnihilationPrototype.ComputeStateHash(world).ToString("X16"), expected.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"command replay checkpoint diverged at tick {expected.Tick}");
            checkpointIndex++;
        }
        if (!world.Resolved || checkpointIndex != checkpoints.Length)
            throw new InvalidOperationException("command replay did not consume every state checkpoint");
        VerifyAuthorityEvents(world.AuthorityEvents, authorityEvents);
    }

    private static void VerifyAuthorityEvents(IReadOnlyList<PrototypeAuthorityEvent> actual,
        IReadOnlyList<PrototypeAuthorityEvent> expected)
    {
        if (actual.Count != expected.Count)
            throw new InvalidOperationException("command replay authority event count diverged");
        for (int i = 0; i < actual.Count; i++)
        {
            PrototypeAuthorityEvent left = actual[i];
            PrototypeAuthorityEvent right = expected[i];
            if (left.Tick != right.Tick || left.Sequence != right.Sequence || left.Kind != right.Kind ||
                left.SourceTeamId != right.SourceTeamId || left.TargetTeamId != right.TargetTeamId ||
                left.TargetId != right.TargetId || left.Value != right.Value)
                throw new InvalidOperationException($"command replay authority event diverged at sequence {right.Sequence}");
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
        ExpectArgumentFailure(new[]
        {
            new PrototypePlayerCommand(10, 2, 1, PrototypePlayerCommandKind.SetTeamRallyWaypoint, 65)
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
