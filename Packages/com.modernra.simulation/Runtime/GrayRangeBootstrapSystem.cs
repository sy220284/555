using ModernRA.Rules;
using Unity.Entities;
using Unity.Mathematics;

namespace ModernRA.Simulation
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(FixedStepRateBootstrapSystem))]
    public partial class GrayRangeBootstrapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            if (SystemAPI.HasSingleton<GrayRangeRuntimeState>())
            {
                Enabled = false;
                return;
            }

            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            CreateRuntimeState(map);
            CreateSpawnAnchors(map);
            CreateResourceAnchors(map);
            CreateStrategicSiteAnchors(map);
            CreateRoadPoints(map);
            CreateControlRegionVertices(map);
            CreateBuildableVertices(map);
            Enabled = false;
        }

        protected override void OnUpdate() { }

        private void CreateRuntimeState(RuntimeMapBootstrapData map)
        {
            Entity entity = EntityManager.CreateEntity(typeof(GrayRangeRuntimeState));
            EntityManager.SetComponentData(entity, new GrayRangeRuntimeState
            {
                MapWidthMeters = map.SizeMeters.X,
                MapHeightMeters = map.SizeMeters.Y,
                SpawnCount = map.Spawns.Length,
                ResourceCount = map.Resources.Length,
                StrategicSiteCount = map.StrategicSites.Length,
                RoadCount = map.Roads.Length,
                ControlRegionCount = map.ControlRegions.Length,
                BuildableAreaCount = map.BuildableAreas.Length
            });
        }

        private void CreateSpawnAnchors(RuntimeMapBootstrapData map)
        {
            for (int i = 0; i < map.Spawns.Length; i++)
            {
                RuntimeSpawnAnchor spawn = map.Spawns[i];
                CreateAnchor(GrayRangeAnchorKind.Spawn, i, 0, spawn.TeamSlot, spawn.Center);
            }
        }

        private void CreateResourceAnchors(RuntimeMapBootstrapData map)
        {
            for (int i = 0; i < map.Resources.Length; i++)
            {
                RuntimeResourceAnchor resource = map.Resources[i];
                GrayRangeAnchorKind kind = string.Equals(resource.ResourceClass, "STRATEGIC", System.StringComparison.Ordinal)
                    ? GrayRangeAnchorKind.StrategicResource
                    : GrayRangeAnchorKind.IndustrialResource;
                CreateAnchor(kind, i, 0, 0, resource.Position);
            }
        }

        private void CreateStrategicSiteAnchors(RuntimeMapBootstrapData map)
        {
            for (int i = 0; i < map.StrategicSites.Length; i++)
                CreateAnchor(GrayRangeAnchorKind.StrategicSite, i, 0, 0, map.StrategicSites[i].Position);
        }

        private void CreateRoadPoints(RuntimeMapBootstrapData map)
        {
            for (int roadIndex = 0; roadIndex < map.Roads.Length; roadIndex++)
            {
                RuntimeRoad road = map.Roads[roadIndex];
                for (int pointIndex = 0; pointIndex < road.SplinePoints.Length; pointIndex++)
                    CreateAnchor(GrayRangeAnchorKind.RoadPoint, roadIndex, pointIndex, 0, road.SplinePoints[pointIndex]);
            }
        }

        private void CreateControlRegionVertices(RuntimeMapBootstrapData map)
        {
            for (int regionIndex = 0; regionIndex < map.ControlRegions.Length; regionIndex++)
            {
                RuntimeControlRegion region = map.ControlRegions[regionIndex];
                for (int vertexIndex = 0; vertexIndex < region.Polygon.Length; vertexIndex++)
                    CreateAnchor(GrayRangeAnchorKind.ControlRegionVertex, regionIndex, vertexIndex, 0, region.Polygon[vertexIndex]);
            }
        }

        private void CreateBuildableVertices(RuntimeMapBootstrapData map)
        {
            for (int areaIndex = 0; areaIndex < map.BuildableAreas.Length; areaIndex++)
            {
                RuntimePolygonArea area = map.BuildableAreas[areaIndex];
                for (int vertexIndex = 0; vertexIndex < area.Polygon.Length; vertexIndex++)
                    CreateAnchor(GrayRangeAnchorKind.BuildableVertex, areaIndex, vertexIndex, area.TeamSlot, area.Polygon[vertexIndex]);
            }
        }

        private void CreateAnchor(GrayRangeAnchorKind kind, int primaryIndex, int secondaryIndex, int teamSlot, Int2 position)
        {
            Entity entity = EntityManager.CreateEntity(typeof(GrayRangeAnchor), typeof(SimPosition));
            EntityManager.SetComponentData(entity, new GrayRangeAnchor
            {
                Kind = kind,
                PrimaryIndex = primaryIndex,
                SecondaryIndex = secondaryIndex,
                TeamSlot = teamSlot
            });
            EntityManager.SetComponentData(entity, new SimPosition
            {
                Value = new float3(position.X, 0f, position.Y)
            });
        }
    }
}
