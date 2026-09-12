using ModernRA.Rules;
using NUnit.Framework;

namespace ModernRA.Tests
{
    public sealed class DeterministicRulesTests
    {
        [Test]
        public void SameInputProducesSameHash()
        {
            var config = BuildConfig();
            ulong first = RunHash(config, 2000);
            ulong second = RunHash(config, 2000);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void SecondaryMiningIsReducedAndFailureStopsThroughput()
        {
            var world = ScenarioFactory.Create(BuildConfig());
            SimulationKernel.Run(world, 2000);
            Assert.That(world.PrimaryMinedMilli, Is.GreaterThan(world.SecondaryMinedMilli));
            Assert.That(world.SecondaryMinedMilli, Is.GreaterThan(0));
            Assert.That(world.ScheduledFailuresApplied, Is.EqualTo(2));
            Assert.That(world.TeamA.IndustrialMilli, Is.EqualTo(world.TeamB.IndustrialMilli));
        }

        [Test]
        public void SharedCorridorAndDirectFireRemainActive()
        {
            var world = ScenarioFactory.Create(BuildConfig());
            SimulationKernel.Run(world, 2000);
            Assert.That(world.CorridorBuildCount, Is.EqualTo(2));
            Assert.That(world.ShotsFired, Is.GreaterThan(0));
            Assert.That(world.UnitsDestroyed, Is.GreaterThan(0));
        }

        private static ulong RunHash(ScenarioConfig config, int ticks)
        {
            var world = ScenarioFactory.Create(config);
            SimulationKernel.Run(world, ticks);
            return SimulationKernel.ComputeStateHash(world);
        }

        private static ScenarioConfig BuildConfig()
        {
            return new ScenarioConfig
            {
                SpawnA = new Int2(0, 0),
                SpawnB = new Int2(2000, 2000),
                SharedCorridor = new[] { new Int2(0, 0), new Int2(1000, 1000), new Int2(2000, 2000) },
                Resources = new[]
                {
                    new ResourceNodeState { Id = "TEST_A", OwnerTeam = 1, CapacityMilli = 60000000, BaseRatePerMinute = 900, ExtractionSlots = 2 },
                    new ResourceNodeState { Id = "TEST_B", OwnerTeam = 2, CapacityMilli = 60000000, BaseRatePerMinute = 900, ExtractionSlots = 2 }
                },
                TankPairs = 4,
                Seed = 42,
                ScheduledFailureTick = 500
            };
        }
    }
}
