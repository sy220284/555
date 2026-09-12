using ModernRA.Simulation;
using NUnit.Framework;
using Unity.Entities;

namespace ModernRA.Tests
{
    public sealed class AnnihilationRuleBridgeSmokeTests
    {
        [Test]
        public void RuleBridge_StartsFromGrayRangeAndMirrorsAuthoritativeState()
        {
            using var world = new World("AnnihilationRuleBridgeSmoke");
            world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            world.GetOrCreateSystemManaged<FixedStepRateBootstrapSystem>();
            world.GetOrCreateSystemManaged<GrayRangeBootstrapSystem>();
            var bridge = world.GetOrCreateSystemManaged<AnnihilationRuleBridgeSystem>();

            bridge.Update();

            using EntityQuery matchQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AnnihilationMatchState>());
            using EntityQuery ruleEntityQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AnnihilationRuleEntity>(),
                ComponentType.ReadOnly<SimPosition>(),
                ComponentType.ReadOnly<HealthState>());

            Assert.That(matchQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(ruleEntityQuery.CalculateEntityCount(), Is.GreaterThan(0));

            AnnihilationMatchState first = matchQuery.GetSingleton<AnnihilationMatchState>();
            Assert.That(first.Tick, Is.GreaterThanOrEqualTo(1));
            Assert.That(first.StateHash, Is.Not.EqualTo(0UL));
            Assert.That(first.TeamAAliveBuildings, Is.GreaterThan(0));
            Assert.That(first.TeamBAliveBuildings, Is.GreaterThan(0));

            bridge.Update();
            AnnihilationMatchState second = matchQuery.GetSingleton<AnnihilationMatchState>();
            Assert.That(second.Tick, Is.GreaterThan(first.Tick));
            Assert.That(second.StateHash, Is.Not.EqualTo(0UL));
        }
    }
}
