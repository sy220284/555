using ModernRA.Rules;
using ModernRA.Simulation;
using NUnit.Framework;
using Unity.Entities;

namespace ModernRA.Tests
{
    public sealed class UnityWorldBootstrapSmokeTests
    {
        [Test]
        public void GrayRangeBootstrap_MaterializesRuntimeStateAndAllAnchors()
        {
            using var world = new World("GrayRangeBootstrapSmoke");
            world.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            world.GetOrCreateSystemManaged<FixedStepRateBootstrapSystem>();
            world.GetOrCreateSystemManaged<GrayRangeBootstrapSystem>();

            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            int expectedAnchors = map.Spawns.Length + map.Resources.Length + map.StrategicSites.Length;
            for (int i = 0; i < map.Roads.Length; i++)
                expectedAnchors += map.Roads[i].SplinePoints.Length;
            for (int i = 0; i < map.ControlRegions.Length; i++)
                expectedAnchors += map.ControlRegions[i].Polygon.Length;
            for (int i = 0; i < map.BuildableAreas.Length; i++)
                expectedAnchors += map.BuildableAreas[i].Polygon.Length;

            using EntityQuery stateQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GrayRangeRuntimeState>());
            using EntityQuery anchorQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GrayRangeAnchor>(),
                ComponentType.ReadOnly<SimPosition>());

            Assert.That(stateQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(anchorQuery.CalculateEntityCount(), Is.EqualTo(expectedAnchors));

            GrayRangeRuntimeState state = stateQuery.GetSingleton<GrayRangeRuntimeState>();
            Assert.That(state.MapWidthMeters, Is.EqualTo(map.SizeMeters.X));
            Assert.That(state.MapHeightMeters, Is.EqualTo(map.SizeMeters.Y));
            Assert.That(state.SpawnCount, Is.EqualTo(map.Spawns.Length));
            Assert.That(state.ResourceCount, Is.EqualTo(map.Resources.Length));
            Assert.That(state.StrategicSiteCount, Is.EqualTo(map.StrategicSites.Length));
            Assert.That(state.RoadCount, Is.EqualTo(map.Roads.Length));
            Assert.That(state.ControlRegionCount, Is.EqualTo(map.ControlRegions.Length));
            Assert.That(state.BuildableAreaCount, Is.EqualTo(map.BuildableAreas.Length));
        }
    }
}
