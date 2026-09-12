using ModernRA.Simulation;
using NUnit.Framework;
using Unity.Entities;

namespace ModernRA.Tests
{
    public sealed class FixedStepRateTests
    {
        [Test]
        public void Bootstrap_ConfiguresFixedStepGroupToThirtyHertz()
        {
            using var world = new World("FixedStepRateTest");
            var fixedGroup = world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            world.GetOrCreateSystemManaged<FixedStepRateBootstrapSystem>();

            Assert.That(fixedGroup.Timestep, Is.EqualTo(SimulationRate.SecondsPerTick).Within(0.000001f));
        }
    }
}
