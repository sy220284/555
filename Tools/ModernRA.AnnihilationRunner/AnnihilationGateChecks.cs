using ModernRA.Rules;

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
        Check(verified.WinnerTeamId == 1, "aggressive reference plan should defeat economy reference plan");
        Check(verified.DefeatReason == PrototypeDefeatReason.WarSystemCollapse, "annihilation resolved for wrong reason");
        Check(verified.TeamA.BuildingsCompleted >= 3 && verified.TeamB.BuildingsCompleted >= 3, "base construction loop did not complete");
        Check(verified.TeamA.MinedMilli > 0 && verified.TeamB.MinedMilli > 0, "resource collection loop did not run");
        Check(verified.TeamA.UnitsProduced + verified.TeamB.UnitsProduced > 0, "production loop produced no combat units");
        Check(verified.MovementSteps > 0, "produced units never moved");
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

        string replayJson = AnnihilationReplayFile.Write(replay, scenarioId, GrayRangeGeneratedData.SourceMapSha256, replayCheckpointIntervalTicks);
        string replayPath = Path.Combine(Path.GetTempPath(), "modernra-annihilation-replay.json");
        File.WriteAllText(replayPath, replayJson);
        string persistedReplayJson = File.ReadAllText(replayPath);
        File.Delete(replayPath);
        AnnihilationReplayFileData replayFile = AnnihilationReplayFile.Read(persistedReplayJson, scenarioId, GrayRangeGeneratedData.SourceMapSha256);
        AnnihilationReplayVerifier.Verify(config, replayFile.Tape, watchdogTicks);
        string replayRoundTrip = AnnihilationReplayFile.Write(replayFile.Tape, replayFile.ScenarioId, replayFile.SourceMapSha256, replayFile.CheckpointIntervalTicks);
        Check(string.Equals(replayJson, replayRoundTrip, StringComparison.Ordinal), "replay file round-trip changed canonical content");
        string replayFileSha256 = AnnihilationReplayFile.ComputeSha256(replayJson);

        Console.WriteLine(
            $"annihilation_gate=passed scenario={scenarioId} winner={verified.WinnerTeamId} tick={verified.Tick} hash={expectedHash:X16} " +
            $"mined_a={verified.TeamA.MinedMilli / 1000.0:F3} mined_b={verified.TeamB.MinedMilli / 1000.0:F3} " +
            $"produced_a={verified.TeamA.UnitsProduced} produced_b={verified.TeamB.UnitsProduced} " +
            $"shots={verified.ShotsFired} buildings_destroyed={verified.BuildingsDestroyed} loser_buildings_remaining={AnnihilationPrototype.CountAliveBuildings(loser)} " +
            $"replay_checkpoints={replay.Checkpoints.Count} replay_final_hash={replay.FinalStateHash:X16} replay_file_bytes={System.Text.Encoding.UTF8.GetByteCount(replayJson)} replay_file_sha256={replayFileSha256}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
