using System.Globalization;
using System.Text.Json;
using ModernRA.Rules;

internal static class Program
{
    private sealed class MapAudit
    {
        public int RoadCount;
        public int ControlRegionCount;
        public long TeamAHomeCapacityMilli;
        public long TeamBHomeCapacityMilli;
        public int TeamAHomeNodes;
        public int TeamBHomeNodes;
    }

    private static int Main(string[] args)
    {
        try
        {
            string mapPath = GetStringArg(args, "--map", "Data/Maps/MAP_GRAY_RANGE.map.json");
            int ticks = GetIntArg(args, "--ticks", 10000);
            int repetitions = GetIntArg(args, "--repetitions", 10);
            int requestedUnits = GetIntArg(args, "--units", 1000);
            if (ticks <= 0 || repetitions < 2 || requestedUnits < 10)
                throw new ArgumentException("ticks must be >0, repetitions >=2, units >=10");

            ScenarioConfig config = LoadGrayRange(mapPath, requestedUnits, out MapAudit audit, out int actualUnits);
            VerifyMapAudit(audit);

            ulong expectedHash = 0;
            WorldState? finalWorld = null;
            for (int i = 0; i < repetitions; i++)
            {
                var world = ScenarioFactory.Create(config);
                SimulationKernel.Run(world, ticks);
                ulong hash = SimulationKernel.ComputeStateHash(world);
                if (i == 0)
                    expectedHash = hash;
                else if (hash != expectedHash)
                    throw new InvalidOperationException($"determinism mismatch at repetition {i + 1}: {hash:X16} != {expectedHash:X16}");
                finalWorld = world;
            }

            if (finalWorld == null)
                throw new InvalidOperationException("simulation did not run");
            VerifySimulation(finalWorld);
            AdvancedGateChecks.Run();

            Console.WriteLine("SIMULATION GATES PASSED");
            Console.WriteLine($"hash={expectedHash:X16}");
            Console.WriteLine($"ticks={ticks} repetitions={repetitions} units={actualUnits}");
            Console.WriteLine($"roads={audit.RoadCount} regions={audit.ControlRegionCount} corridor_builds={finalWorld.CorridorBuildCount}");
            Console.WriteLine($"industrial_team_a={finalWorld.TeamA.IndustrialMilli / 1000.0:F3} industrial_team_b={finalWorld.TeamB.IndustrialMilli / 1000.0:F3}");
            Console.WriteLine($"primary_mined={finalWorld.PrimaryMinedMilli / 1000.0:F3} secondary_mined={finalWorld.SecondaryMinedMilli / 1000.0:F3} harvester_failures={finalWorld.ScheduledFailuresApplied}");
            Console.WriteLine($"shots={finalWorld.ShotsFired} destroyed={finalWorld.UnitsDestroyed}");
            Console.WriteLine("advanced_gates=intel+ai+robotics+local_network_passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("SIMULATION GATES FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static ScenarioConfig LoadGrayRange(string path, int requestedUnits, out MapAudit audit, out int actualUnits)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        if (root.GetProperty("map_id").GetString() != "MAP_GRAY_RANGE")
            throw new InvalidOperationException("SimRunner currently requires MAP_GRAY_RANGE");

        Int2 spawnA = default;
        Int2 spawnB = default;
        foreach (JsonElement spawn in root.GetProperty("spawn_sectors").EnumerateArray())
        {
            int team = spawn.GetProperty("team_slot").GetInt32();
            JsonElement center = spawn.GetProperty("center");
            var point = new Int2(center[0].GetInt32(), center[1].GetInt32());
            if (team == 1) spawnA = point;
            if (team == 2) spawnB = point;
        }
        if (spawnA.X == 0 && spawnA.Y == 0)
            throw new InvalidOperationException("team 1 spawn missing");
        if (spawnB.X == 0 && spawnB.Y == 0)
            throw new InvalidOperationException("team 2 spawn missing");

        var corridor = new List<Int2>();
        int roadCount = 0;
        foreach (JsonElement road in root.GetProperty("roads").EnumerateArray())
        {
            roadCount++;
            if (road.GetProperty("road_id").GetString() != "ROAD_CENTER")
                continue;
            foreach (JsonElement point in road.GetProperty("spline_points").EnumerateArray())
                corridor.Add(new Int2(point[0].GetInt32(), point[1].GetInt32()));
        }
        if (corridor.Count < 3)
            throw new InvalidOperationException("ROAD_CENTER requires at least three points");

        var resources = new List<ResourceNodeState>();
        audit = new MapAudit
        {
            RoadCount = roadCount,
            ControlRegionCount = root.GetProperty("control_regions").GetArrayLength()
        };

        foreach (JsonElement node in root.GetProperty("resource_nodes").EnumerateArray())
        {
            if (node.GetProperty("mineral_id").GetString() != "MIN_BASE_METALS")
                continue;
            string region = node.GetProperty("control_region_id").GetString() ?? string.Empty;
            int owner = region == "REGION_HOME_A" ? 1 : region == "REGION_HOME_B" ? 2 : 0;
            if (owner == 0)
                continue;

            long capacityMilli = checked(node.GetProperty("capacity").GetInt64() * 1000L);
            resources.Add(new ResourceNodeState
            {
                Id = node.GetProperty("node_id").GetString() ?? throw new InvalidOperationException("resource node id missing"),
                OwnerTeam = owner,
                CapacityMilli = capacityMilli,
                BaseRatePerMinute = node.GetProperty("base_income_rate").GetInt32(),
                ExtractionSlots = node.GetProperty("extraction_slots").GetInt32()
            });
            if (owner == 1)
            {
                audit.TeamAHomeNodes++;
                audit.TeamAHomeCapacityMilli += capacityMilli;
            }
            else
            {
                audit.TeamBHomeNodes++;
                audit.TeamBHomeCapacityMilli += capacityMilli;
            }
        }

        int harvesterCount = 0;
        foreach (ResourceNodeState node in resources)
            harvesterCount += Math.Min(2, Math.Max(1, node.ExtractionSlots));
        int tankPairs = Math.Max(1, (requestedUnits - harvesterCount) / 2);
        actualUnits = harvesterCount + tankPairs * 2;

        return new ScenarioConfig
        {
            SpawnA = spawnA,
            SpawnB = spawnB,
            SharedCorridor = corridor.ToArray(),
            Resources = resources.ToArray(),
            TankPairs = tankPairs,
            Seed = 0x4D4F4445524E5241UL,
            ScheduledFailureTick = 2500
        };
    }

    private static void VerifyMapAudit(MapAudit audit)
    {
        if (audit.RoadCount < 2)
            throw new InvalidOperationException("gray range must expose at least two strategic roads");
        if (audit.ControlRegionCount < 5)
            throw new InvalidOperationException("gray range control region graph is incomplete");
        if (audit.TeamAHomeNodes < 2 || audit.TeamBHomeNodes < 2)
            throw new InvalidOperationException("each team requires at least two home base-metal nodes");
        if (audit.TeamAHomeNodes != audit.TeamBHomeNodes || audit.TeamAHomeCapacityMilli != audit.TeamBHomeCapacityMilli)
            throw new InvalidOperationException("home base-metal economy is not symmetric");
    }

    private static void VerifySimulation(WorldState world)
    {
        if (world.Tick <= 0)
            throw new InvalidOperationException("world did not advance");
        if (world.TeamA.IndustrialMilli < 0 || world.TeamB.IndustrialMilli < 0)
            throw new InvalidOperationException("negative resource pool detected");
        if (world.TeamA.IndustrialMilli != world.TeamB.IndustrialMilli)
            throw new InvalidOperationException("symmetric economy diverged between teams");
        foreach (ResourceNodeState node in world.Resources)
            if (node.CapacityMilli < 0)
                throw new InvalidOperationException($"resource node {node.Id} went negative");
        if (world.PrimaryMinedMilli <= 0 || world.SecondaryMinedMilli <= 0)
            throw new InvalidOperationException("mining did not produce resources");
        if (world.SecondaryMinedMilli >= world.PrimaryMinedMilli)
            throw new InvalidOperationException("secondary extraction efficiency reduction is not active");
        if (world.ScheduledFailuresApplied != 2)
            throw new InvalidOperationException("harvester destruction gate did not execute symmetrically");
        if (world.CorridorBuildCount != 2)
            throw new InvalidOperationException("shared navigation corridor was rebuilt per unit");
        if (world.ShotsFired <= 0)
            throw new InvalidOperationException("direct-fire combat never fired");
        if (world.UnitsDestroyed <= 0)
            throw new InvalidOperationException("direct-fire combat produced no destruction");
    }

    private static int GetIntArg(string[] args, string name, int fallback)
    {
        string value = GetStringArg(args, name, fallback.ToString(CultureInfo.InvariantCulture));
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string GetStringArg(string[] args, string name, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.Ordinal))
                return args[i + 1];
        return fallback;
    }
}
