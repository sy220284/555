using ModernRA.Rules;

internal static class UpdateBudgetGateChecks
{
    public static void Run()
    {
        const int entities = 3000;
        const int ticks = 30;
        long fullRateEverything = (long)entities * ticks * 6;
        long scheduled = 0;
        int battleGroupPeak = 0;
        int theaterPeak = 0;
        int strategicPeak = 0;

        for (int tick = 0; tick < ticks; tick++)
        {
            foreach (RuleUpdateLane lane in Enum.GetValues<RuleUpdateLane>())
            {
                int count = DeterministicUpdateBudget.CountScheduled(lane, tick, 1, entities);
                scheduled += count;
                if (lane == RuleUpdateLane.BattleGroupAI) battleGroupPeak = Math.Max(battleGroupPeak, count);
                if (lane == RuleUpdateLane.TheaterAI) theaterPeak = Math.Max(theaterPeak, count);
                if (lane == RuleUpdateLane.StrategicAI) strategicPeak = Math.Max(strategicPeak, count);
            }
        }

        Check(scheduled < fullRateEverything / 2, "update budget did not cut aggregate evaluation load by at least half");
        Check(battleGroupPeak <= 1000, "battle-group AI work was not evenly staggered");
        Check(theaterPeak <= 500, "theater AI work was not evenly staggered");
        Check(strategicPeak <= 200, "strategic AI work was not evenly staggered");

        for (int id = -30; id <= 30; id++)
        {
            int battleRuns = 0;
            int strategicRuns = 0;
            for (int tick = 0; tick < ticks; tick++)
            {
                if (DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.BattleGroupAI, tick, id)) battleRuns++;
                if (DeterministicUpdateBudget.ShouldRun(RuleUpdateLane.StrategicAI, tick, id)) strategicRuns++;
            }
            Check(battleRuns == 10, "battle-group AI cadence drifted from 10 Hz");
            Check(strategicRuns == 2, "strategic AI cadence drifted from 2 Hz");
        }

        long reductionPermille = 1000 - scheduled * 1000 / fullRateEverything;
        Console.WriteLine(
            $"update_budget_gate=passed entities={entities} ticks={ticks} scheduled={scheduled} full_rate={fullRateEverything} " +
            $"reduction_permille={reductionPermille} battle_peak={battleGroupPeak} theater_peak={theaterPeak} strategic_peak={strategicPeak}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
