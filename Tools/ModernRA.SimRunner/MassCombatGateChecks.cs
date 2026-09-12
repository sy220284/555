using ModernRA.Rules;

internal static class MassCombatGateChecks
{
    public static void Run()
    {
        const int units = 1000;
        const int ticks = 300;
        const int repetitions = 4;
        ulong expectedHash = 0;
        MassCombatWorld? final = null;

        for (int i = 0; i < repetitions; i++)
        {
            MassCombatWorld world = MassCombatPrototype.Create(units);
            MassCombatPrototype.Run(world, ticks);
            ulong hash = MassCombatPrototype.ComputeStateHash(world);
            if (i == 0)
                expectedHash = hash;
            else
                Check(hash == expectedHash, "mass combat state hash diverged between repetitions");
            final = world;
        }

        Check(final != null, "mass combat gate did not run");
        MassCombatWorld verified = final!;
        Check(verified.ShotsFired > 0, "mass combat produced no fire events");
        Check(verified.UnitsDestroyed > 0, "mass combat produced no casualties");
        Check(verified.SpatialCandidateVisits > 0 && verified.NaiveCandidateVisits > 0, "mass combat candidate accounting did not run");
        Check(verified.SpatialCandidateVisits < verified.NaiveCandidateVisits / 5, "mass combat spatial broad phase did not cut candidate visits by at least 80 percent");

        long reductionPermille = 1000 - verified.SpatialCandidateVisits * 1000 / verified.NaiveCandidateVisits;
        Console.WriteLine(
            $"mass_combat_gate=passed units={units} ticks={ticks} repetitions={repetitions} hash={expectedHash:X16} " +
            $"shots={verified.ShotsFired} destroyed={verified.UnitsDestroyed} spatial_visits={verified.SpatialCandidateVisits} " +
            $"naive_visits={verified.NaiveCandidateVisits} reduction_permille={reductionPermille}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
