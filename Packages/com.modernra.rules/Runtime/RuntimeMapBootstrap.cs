using System;

namespace ModernRA.Rules
{
    public readonly struct RuntimeSpawnAnchor
    {
        public readonly string SpawnId;
        public readonly int TeamSlot;
        public readonly Int2 Center;
        public readonly int FacingDegrees;
        public readonly int SafeRadiusMeters;
        public readonly Int2[] InitialBuildPolygon;
        public readonly Int2[] RallyPoints;

        public RuntimeSpawnAnchor(string spawnId, int teamSlot, Int2 center, int facingDegrees, int safeRadiusMeters, Int2[] initialBuildPolygon, Int2[] rallyPoints)
        {
            SpawnId = spawnId ?? throw new ArgumentNullException(nameof(spawnId));
            TeamSlot = teamSlot;
            Center = center;
            FacingDegrees = facingDegrees;
            SafeRadiusMeters = safeRadiusMeters;
            InitialBuildPolygon = initialBuildPolygon ?? Array.Empty<Int2>();
            RallyPoints = rallyPoints ?? Array.Empty<Int2>();
        }
    }

    public readonly struct RuntimeResourceAnchor
    {
        public readonly string NodeId;
        public readonly string ResourceClass;
        public readonly string MineralId;
        public readonly Int2 Position;
        public readonly long Capacity;
        public readonly int BaseIncomeRate;
        public readonly int ExtractionSlots;
        public readonly string ControlRegionId;

        public RuntimeResourceAnchor(string nodeId, string resourceClass, string mineralId, Int2 position, long capacity, int baseIncomeRate, int extractionSlots, string controlRegionId)
        {
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            ResourceClass = resourceClass ?? throw new ArgumentNullException(nameof(resourceClass));
            MineralId = mineralId ?? throw new ArgumentNullException(nameof(mineralId));
            Position = position;
            Capacity = capacity;
            BaseIncomeRate = baseIncomeRate;
            ExtractionSlots = extractionSlots;
            ControlRegionId = controlRegionId ?? throw new ArgumentNullException(nameof(controlRegionId));
        }
    }

    public readonly struct RuntimeStrategicSiteAnchor
    {
        public readonly string SiteId;
        public readonly string SiteType;
        public readonly Int2 Position;
        public readonly string ControlRegionId;

        public RuntimeStrategicSiteAnchor(string siteId, string siteType, Int2 position, string controlRegionId)
        {
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            SiteType = siteType ?? throw new ArgumentNullException(nameof(siteType));
            Position = position;
            ControlRegionId = controlRegionId ?? throw new ArgumentNullException(nameof(controlRegionId));
        }
    }

    public readonly struct RuntimeRoad
    {
        public readonly string RoadId;
        public readonly Int2[] SplinePoints;
        public readonly int WidthMeters;
        public readonly int TerrainCostPermille;

        public RuntimeRoad(string roadId, Int2[] splinePoints, int widthMeters, int terrainCostPermille)
        {
            RoadId = roadId ?? throw new ArgumentNullException(nameof(roadId));
            SplinePoints = splinePoints ?? Array.Empty<Int2>();
            WidthMeters = widthMeters;
            TerrainCostPermille = terrainCostPermille;
        }
    }

    public readonly struct RuntimePolygonArea
    {
        public readonly string AreaId;
        public readonly int TeamSlot;
        public readonly Int2[] Polygon;

        public RuntimePolygonArea(string areaId, int teamSlot, Int2[] polygon)
        {
            AreaId = areaId ?? throw new ArgumentNullException(nameof(areaId));
            TeamSlot = teamSlot;
            Polygon = polygon ?? Array.Empty<Int2>();
        }
    }

    public readonly struct RuntimeControlRegion
    {
        public readonly string RegionId;
        public readonly Int2[] Polygon;
        public readonly string[] Neighbors;
        public readonly int StrategicWeightPermille;

        public RuntimeControlRegion(string regionId, Int2[] polygon, string[] neighbors, int strategicWeightPermille)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Polygon = polygon ?? Array.Empty<Int2>();
            Neighbors = neighbors ?? Array.Empty<string>();
            StrategicWeightPermille = strategicWeightPermille;
        }
    }

    public sealed class RuntimeMapBootstrapData
    {
        public string MapId = string.Empty;
        public Int2 SizeMeters;
        public RuntimeSpawnAnchor[] Spawns = Array.Empty<RuntimeSpawnAnchor>();
        public RuntimeResourceAnchor[] Resources = Array.Empty<RuntimeResourceAnchor>();
        public RuntimeStrategicSiteAnchor[] StrategicSites = Array.Empty<RuntimeStrategicSiteAnchor>();
        public RuntimeRoad[] Roads = Array.Empty<RuntimeRoad>();
        public RuntimePolygonArea[] BuildableAreas = Array.Empty<RuntimePolygonArea>();
        public RuntimeControlRegion[] ControlRegions = Array.Empty<RuntimeControlRegion>();
        public string[] RulesetCompatibility = Array.Empty<string>();

        public RuntimeSpawnAnchor GetSpawn(int teamSlot)
        {
            for (int i = 0; i < Spawns.Length; i++)
                if (Spawns[i].TeamSlot == teamSlot)
                    return Spawns[i];
            throw new InvalidOperationException($"map {MapId} is missing spawn for team {teamSlot}");
        }

        public RuntimeRoad GetRoad(string roadId)
        {
            for (int i = 0; i < Roads.Length; i++)
                if (string.Equals(Roads[i].RoadId, roadId, StringComparison.Ordinal))
                    return Roads[i];
            throw new InvalidOperationException($"map {MapId} is missing road {roadId}");
        }

        public bool SupportsRuleset(string rulesetId)
        {
            for (int i = 0; i < RulesetCompatibility.Length; i++)
                if (string.Equals(RulesetCompatibility[i], rulesetId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public AnnihilationPrototypeConfig CreateStandardAnnihilationConfig(long startingIndustrialMilli = 12000L * 1000L, int maxLiveTanksPerTeam = 12)
        {
            if (!SupportsRuleset("RULESET_ANNIHILATION_STANDARD"))
                throw new InvalidOperationException($"map {MapId} does not support standard annihilation");
            RuntimeSpawnAnchor a = GetSpawn(1);
            RuntimeSpawnAnchor b = GetSpawn(2);
            RuntimeRoad center = GetRoad("ROAD_CENTER");
            if (center.SplinePoints.Length < 2)
                throw new InvalidOperationException($"map {MapId} central road is incomplete");
            return new AnnihilationPrototypeConfig
            {
                SpawnA = a.Center,
                SpawnB = b.Center,
                SharedCorridor = (Int2[])center.SplinePoints.Clone(),
                StartingIndustrialMilli = startingIndustrialMilli,
                MaxLiveTanksPerTeam = maxLiveTanksPerTeam,
                SupplyNodes = new[]
                {
                    new RuleSupplyNode(1, 1, a.Center.X, a.Center.Y, PrototypeSupplyRuntime.StandardBaseSupplyCapacity, a.SafeRadiusMeters),
                    new RuleSupplyNode(2, 2, b.Center.X, b.Center.Y, PrototypeSupplyRuntime.StandardBaseSupplyCapacity, b.SafeRadiusMeters)
                }
            };
        }
    }
}
