namespace ModernRA.Simulation
{
    public static class SimulationRate
    {
        public const int TicksPerSecond = 30;
        public const float SecondsPerTick = 1f / TicksPerSecond;
        public static uint SecondsToTicks(float seconds) => (uint)(seconds * TicksPerSecond + 0.5f);
    }
}
