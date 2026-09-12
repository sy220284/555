using ModernRA.AI;
using ModernRA.Robotics;
using ModernRA.Simulation;
using NUnit.Framework;

namespace ModernRA.Tests
{
    public sealed class SimulationContractTests
    {
        [Test] public void AuthoritativeTickRate_IsThirtyHertz() => Assert.That(SimulationRate.TicksPerSecond, Is.EqualTo(30));
        [Test] public void HighestRobotAutonomy_IsA4() => Assert.That((byte)AutonomyLevel.A4, Is.EqualTo(4));
        [Test] public void DeputyAI_CannotImplicitlyMeanUnrestricted() => Assert.That(AIAuthorityLevel.Deputy, Is.Not.EqualTo(AIAuthorityLevel.None));
    }
}
