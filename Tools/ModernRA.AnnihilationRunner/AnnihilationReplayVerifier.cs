using ModernRA.Rules;

internal readonly struct AnnihilationReplayCheckpoint
{
    public readonly int Tick;
    public readonly ulong StateHash;

    public AnnihilationReplayCheckpoint(int tick, ulong stateHash)
    {
        Tick = tick;
        StateHash = stateHash;
    }
}

internal sealed class AnnihilationReplayTape
{
    public readonly List<AnnihilationReplayCheckpoint> Checkpoints = new List<AnnihilationReplayCheckpoint>();
    public int WinnerTeamId;
    public int ResolvedTick;
    public ulong FinalStateHash;
}

internal static class AnnihilationReplayVerifier
{
    public static AnnihilationReplayTape Record(AnnihilationPrototypeConfig config, int checkpointIntervalTicks, int watchdogTicks)
    {
        if (checkpointIntervalTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(checkpointIntervalTicks));
        if (watchdogTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(watchdogTicks));

        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        var tape = new AnnihilationReplayTape();
        while (!world.Resolved && world.Tick < watchdogTicks)
        {
            AnnihilationPrototype.Step(world);
            if (world.Tick % checkpointIntervalTicks == 0 || world.Resolved)
                tape.Checkpoints.Add(new AnnihilationReplayCheckpoint(world.Tick, AnnihilationPrototype.ComputeStateHash(world)));
        }

        if (!world.Resolved)
            throw new InvalidOperationException("annihilation replay recording exceeded watchdog");
        if (tape.Checkpoints.Count == 0 || tape.Checkpoints[tape.Checkpoints.Count - 1].Tick != world.Tick)
            throw new InvalidOperationException("annihilation replay is missing final checkpoint");

        tape.WinnerTeamId = world.WinnerTeamId;
        tape.ResolvedTick = world.Tick;
        tape.FinalStateHash = AnnihilationPrototype.ComputeStateHash(world);
        return tape;
    }

    public static void Verify(AnnihilationPrototypeConfig config, AnnihilationReplayTape tape, int watchdogTicks)
    {
        if (tape == null)
            throw new ArgumentNullException(nameof(tape));
        if (tape.Checkpoints.Count == 0)
            throw new InvalidOperationException("annihilation replay has no checkpoints");

        AnnihilationPrototypeWorld world = AnnihilationPrototype.Create(config);
        int checkpointIndex = 0;
        while (!world.Resolved && world.Tick < watchdogTicks)
        {
            AnnihilationPrototype.Step(world);
            if (checkpointIndex >= tape.Checkpoints.Count)
                continue;

            AnnihilationReplayCheckpoint checkpoint = tape.Checkpoints[checkpointIndex];
            if (world.Tick < checkpoint.Tick)
                continue;
            if (world.Tick > checkpoint.Tick)
                throw new InvalidOperationException($"replay skipped checkpoint tick {checkpoint.Tick}");

            ulong currentHash = AnnihilationPrototype.ComputeStateHash(world);
            if (currentHash != checkpoint.StateHash)
                throw new InvalidOperationException($"replay divergence at tick {checkpoint.Tick}: {currentHash:X16} != {checkpoint.StateHash:X16}");
            checkpointIndex++;
        }

        if (!world.Resolved)
            throw new InvalidOperationException("annihilation replay verification exceeded watchdog");
        if (checkpointIndex != tape.Checkpoints.Count)
            throw new InvalidOperationException("annihilation replay did not consume every checkpoint");

        ulong finalHash = AnnihilationPrototype.ComputeStateHash(world);
        if (world.WinnerTeamId != tape.WinnerTeamId || world.Tick != tape.ResolvedTick || finalHash != tape.FinalStateHash)
            throw new InvalidOperationException("annihilation replay final result diverged");
    }
}
