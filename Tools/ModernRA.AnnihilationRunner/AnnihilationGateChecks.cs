using ModernRA.Rules;
using System.Text.Json;

internal static class AnnihilationGateChecks
{
    public static void Run(AnnihilationPrototypeConfig config, string scenarioId)
    {
        const int repetitions = 8;
        const int watchdogTicks = 60000;
        const int replayCheckpointIntervalTicks = 120;
        ulong expectedHash = 0;
        int expectedWinner = 0;
        int expectedTick = 0;
        AnnihilationPrototypeWorld? finalWorld = null;

        for (int i = 0; i < repetitions; i++)
        {
            AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
            AnnihilationPrototypeResult result = AnnihilationPrototype.Run(world, watchdogTicks);
            if (i == 0)
            {
                expectedHash = result.StateHash;
                expectedWinner = result.WinnerTeamId;
                expectedTick = result.ResolvedTick;
            }
            else
            {
                Check(result.StateHash == expectedHash, "annihilation prototype state hash diverged");
                Check(result.WinnerTeamId == expectedWinner, "annihilation prototype winner diverged");
                Check(result.ResolvedTick == expectedTick, "annihilation prototype resolution tick diverged");
            }
            finalWorld = world;
        }

        Check(finalWorld != null, "annihilation prototype did not execute");
        AnnihilationPrototypeWorld verified = finalWorld!;
        Check(verified.WinnerTeamId == 1,
            $"aggressive reference plan should defeat economy reference plan; actual winner={verified.WinnerTeamId} tick={verified.Tick}");
        Check(verified.DefeatReason == PrototypeDefeatReason.WarSystemCollapse, "annihilation resolved for wrong reason");
        Check(verified.TeamA.BuildingsCompleted >= 3 && verified.TeamB.BuildingsCompleted >= 3, "base construction loop did not complete");
        Check(verified.TeamA.MinedMilli > 0 && verified.TeamB.MinedMilli > 0, "resource collection loop did not run");
        Check(verified.TeamA.UnitsProduced + verified.TeamB.UnitsProduced > 0, "production loop produced no combat units");
        Check(verified.MovementSteps > 0, "produced units never moved");
        Check(verified.AvoidanceCandidateVisits > 0, "annihilation movement never queried local spatial avoidance");
        Check(verified.AvoidanceNeighborsResolved > 0, "annihilation movement never resolved nearby unit separation");
        Check(verified.CombatCandidateVisits > 0, "annihilation combat never queried the shared spatial index");
        Check(verified.ShotsFired > 0 && verified.BuildingsDestroyed > 0, "combat loop did not destroy war infrastructure");
        Check(verified.TeamA.IndustrialMilli >= 0 && verified.TeamB.IndustrialMilli >= 0, "resource pool became negative");

        PrototypeAnnihilationTeamState loser = verified.TeamB;
        Check(loser.DefeatReason == PrototypeDefeatReason.WarSystemCollapse, "loser did not record war-system collapse");
        Check(AnnihilationPrototype.CountAliveBuildings(loser) > 0, "annihilation still requires clearing every enemy building");

        AnnihilationReplayTape replay = AnnihilationReplayVerifier.Record(config, replayCheckpointIntervalTicks, watchdogTicks);
        AnnihilationReplayVerifier.Verify(config, replay, watchdogTicks);
        Check(replay.WinnerTeamId == expectedWinner, "replay winner does not match deterministic gate");
        Check(replay.ResolvedTick == expectedTick, "replay resolution tick does not match deterministic gate");
        Check(replay.FinalStateHash == expectedHash, "replay final hash does not match deterministic gate");
        Check(replay.Checkpoints.Count >= 10, "replay checkpoint coverage is too sparse");

        PlayerCommandCheckpointRecord[] checkpoints = ToCheckpointRecords(replay);
        PrototypePlayerCommand[] noCommands = Array.Empty<PrototypePlayerCommand>();
        ulong emptyCommandHash = new DeterministicCommandTimeline(noCommands).ComputeCanonicalHash();
        PlayerCommandReplayDocument document = PlayerCommandReplayFile.Create(
            scenarioId, config, noCommands, emptyCommandHash, replay.WinnerTeamId,
            replay.ResolvedTick, replay.FinalStateHash, checkpoints, replay.AuthorityEvents.ToArray());
        string replayPath = Path.Combine(Path.GetTempPath(), "modernra-annihilation-replay.json");
        PlayerCommandReplayFile.Write(replayPath, document);
        byte[] replayBytes = File.ReadAllBytes(replayPath);
        PlayerCommandReplayDocument replayFile = PlayerCommandReplayFile.Read(replayPath);
        PlayerCommandReplayFile.ValidateScenario(replayFile, scenarioId);
        Check(PlayerCommandReplayFile.ToCommands(replayFile).Length == 0,
            "uncommanded replay acquired player commands");
        AnnihilationPrototypeConfig replayConfig = PlayerCommandReplayFile.ToInitialConfig(replayFile);
        AnnihilationReplayTape loadedTape = ToReplayTape(replayFile);
        AnnihilationReplayVerifier.Verify(replayConfig, loadedTape, watchdogTicks);
        byte[] replayRoundTrip = PlayerCommandReplayFile.Serialize(replayFile);
        Check(replayBytes.AsSpan().SequenceEqual(replayRoundTrip),
            "unified replay file round-trip changed canonical content");

        var legacy = new AnnihilationReplayV1Document
        {
            SchemaVersion = 1,
            ScenarioId = scenarioId,
            SourceMapSha256 = GrayRangeGeneratedData.SourceMapSha256,
            RulesetId = PlayerCommandReplayFile.RulesetId,
            CheckpointIntervalTicks = replayCheckpointIntervalTicks,
            WinnerTeamId = replay.WinnerTeamId,
            ResolvedTick = replay.ResolvedTick,
            FinalStateHash = replay.FinalStateHash.ToString("X16"),
            Checkpoints = ToLegacyCheckpointRecords(replay)
        };
        File.WriteAllBytes(replayPath, JsonSerializer.SerializeToUtf8Bytes(legacy, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }));
        PlayerCommandReplayDocument migrated = PlayerCommandReplayFile.Read(replayPath);
        File.Delete(replayPath);
        Check(migrated.FormatVersion == PlayerCommandReplayFile.FormatVersion &&
              PlayerCommandReplayFile.ToCommands(migrated).Length == 0 &&
              PlayerCommandReplayFile.ToCheckpoints(migrated).Length == replay.Checkpoints.Count,
            "legacy annihilation replay migration lost timeline data");
        AnnihilationReplayVerifier.Verify(PlayerCommandReplayFile.ToInitialConfig(migrated),
            ToReplayTape(migrated), watchdogTicks);
        string replayFileSha256 = PlayerCommandReplayFile.Sha256Hex(replayBytes);

        Console.WriteLine(
            $"annihilation_gate=passed scenario={scenarioId} winner={verified.WinnerTeamId} tick={verified.Tick} hash={expectedHash:X16} " +
            $"mined_a={verified.TeamA.MinedMilli / 1000.0:F3} mined_b={verified.TeamB.MinedMilli / 1000.0:F3} " +
            $"produced_a={verified.TeamA.UnitsProduced} produced_b={verified.TeamB.UnitsProduced} " +
            $"avoidance_candidates={verified.AvoidanceCandidateVisits} avoidance_neighbors={verified.AvoidanceNeighborsResolved} " +
            $"combat_candidates={verified.CombatCandidateVisits} " +
            $"shots={verified.ShotsFired} buildings_destroyed={verified.BuildingsDestroyed} loser_buildings_remaining={AnnihilationPrototype.CountAliveBuildings(loser)} " +
            $"replay_checkpoints={replay.Checkpoints.Count} replay_final_hash={replay.FinalStateHash:X16} replay_file_bytes={replayBytes.Length} replay_file_sha256={replayFileSha256} replay_format=v3 migration_v1_v3=true");
    }

    private static PlayerCommandCheckpointRecord[] ToCheckpointRecords(AnnihilationReplayTape replay)
    {
        var records = new PlayerCommandCheckpointRecord[replay.Checkpoints.Count];
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = new PlayerCommandCheckpointRecord
            {
                Tick = replay.Checkpoints[i].Tick,
                StateHash = replay.Checkpoints[i].StateHash.ToString("X16")
            };
        }
        return records;
    }

    private static AnnihilationReplayV1Checkpoint[] ToLegacyCheckpointRecords(AnnihilationReplayTape replay)
    {
        var records = new AnnihilationReplayV1Checkpoint[replay.Checkpoints.Count];
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = new AnnihilationReplayV1Checkpoint
            {
                Tick = replay.Checkpoints[i].Tick,
                StateHash = replay.Checkpoints[i].StateHash.ToString("X16")
            };
        }
        return records;
    }

    private static AnnihilationReplayTape ToReplayTape(PlayerCommandReplayDocument document)
    {
        PlayerCommandCheckpointRecord[] checkpoints = PlayerCommandReplayFile.ToCheckpoints(document);
        var tape = new AnnihilationReplayTape
        {
            WinnerTeamId = document.WinnerTeamId,
            ResolvedTick = document.ResolvedTick,
            FinalStateHash = Convert.ToUInt64(document.FinalStateHash, 16)
        };
        for (int i = 0; i < checkpoints.Length; i++)
        {
            tape.Checkpoints.Add(new AnnihilationReplayCheckpoint(
                checkpoints[i].Tick, Convert.ToUInt64(checkpoints[i].StateHash, 16)));
        }
        tape.AuthorityEvents.AddRange(PlayerCommandReplayFile.ToAuthorityEvents(document));
        return tape;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
