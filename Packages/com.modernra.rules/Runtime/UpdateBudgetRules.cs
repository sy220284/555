using System;

namespace ModernRA.Rules
{
    public enum RuleUpdateLane : byte
    {
        CombatAuthority = 0,
        HighRateSensor = 1,
        UnitAutonomy = 2,
        BattleGroupAI = 3,
        TheaterAI = 4,
        StrategicAI = 5
    }

    public static class DeterministicUpdateBudget
    {
        public const int TickRate = 30;

        public static int GetPeriodTicks(RuleUpdateLane lane)
        {
            return lane switch
            {
                RuleUpdateLane.CombatAuthority => 1,
                RuleUpdateLane.HighRateSensor => 2,
                RuleUpdateLane.UnitAutonomy => 2,
                RuleUpdateLane.BattleGroupAI => 3,
                RuleUpdateLane.TheaterAI => 6,
                RuleUpdateLane.StrategicAI => 15,
                _ => throw new ArgumentOutOfRangeException(nameof(lane))
            };
        }

        public static bool ShouldRun(RuleUpdateLane lane, int tick, int stableId)
        {
            if (tick < 0)
                throw new ArgumentOutOfRangeException(nameof(tick));
            int period = GetPeriodTicks(lane);
            if (period == 1)
                return true;
            int phase = PositiveModulo(stableId, period);
            return tick % period == phase;
        }

        public static int CountScheduled(RuleUpdateLane lane, int tick, int firstStableId, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            int scheduled = 0;
            for (int i = 0; i < count; i++)
                if (ShouldRun(lane, tick, firstStableId + i))
                    scheduled++;
            return scheduled;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
