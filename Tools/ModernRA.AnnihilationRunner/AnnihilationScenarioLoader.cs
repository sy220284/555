using System.Text.Json;
using ModernRA.Rules;

internal static class AnnihilationScenarioLoader
{
    public static AnnihilationPrototypeConfig Load(string mapPath, out string mapId)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(mapPath));
        JsonElement root = document.RootElement;
        mapId = root.GetProperty("map_id").GetString() ?? throw new InvalidOperationException("map_id missing");

        RuntimeMapBootstrapData generated = GrayRangeGeneratedData.Create();
        if (!string.Equals(mapId, generated.MapId, StringComparison.Ordinal))
            throw new InvalidOperationException($"generated runtime map {generated.MapId} does not match source map {mapId}");

        bool supportsAnnihilation = false;
        foreach (JsonElement ruleset in root.GetProperty("ruleset_compatibility").EnumerateArray())
        {
            if (string.Equals(ruleset.GetString(), "RULESET_ANNIHILATION_STANDARD", StringComparison.Ordinal))
            {
                supportsAnnihilation = true;
                break;
            }
        }
        if (!supportsAnnihilation || !generated.SupportsRuleset("RULESET_ANNIHILATION_STANDARD"))
            throw new InvalidOperationException($"map {mapId} does not support standard annihilation");

        Int2? spawnA = null;
        Int2? spawnB = null;
        foreach (JsonElement spawn in root.GetProperty("spawn_sectors").EnumerateArray())
        {
            int team = spawn.GetProperty("team_slot").GetInt32();
            JsonElement center = spawn.GetProperty("center");
            var point = new Int2(center[0].GetInt32(), center[1].GetInt32());
            if (team == 1) spawnA = point;
            else if (team == 2) spawnB = point;
        }
        if (!spawnA.HasValue || !spawnB.HasValue)
            throw new InvalidOperationException("annihilation prototype requires team 1 and team 2 spawn sectors");

        var corridor = new List<Int2>();
        foreach (JsonElement road in root.GetProperty("roads").EnumerateArray())
        {
            if (!string.Equals(road.GetProperty("road_id").GetString(), "ROAD_CENTER", StringComparison.Ordinal))
                continue;
            foreach (JsonElement point in road.GetProperty("spline_points").EnumerateArray())
                corridor.Add(new Int2(point[0].GetInt32(), point[1].GetInt32()));
            break;
        }
        if (corridor.Count < 2)
            throw new InvalidOperationException("annihilation prototype requires ROAD_CENTER with at least two spline points");

        AnnihilationPrototypeConfig config = generated.CreateStandardAnnihilationConfig();
        EnsureSamePoint(spawnA.Value, config.SpawnA, "team 1 spawn");
        EnsureSamePoint(spawnB.Value, config.SpawnB, "team 2 spawn");
        if (corridor.Count != config.SharedCorridor.Length)
            throw new InvalidOperationException("generated ROAD_CENTER point count does not match source map");
        for (int i = 0; i < corridor.Count; i++)
            EnsureSamePoint(corridor[i], config.SharedCorridor[i], $"ROAD_CENTER point {i}");

        return config;
    }

    private static void EnsureSamePoint(Int2 source, Int2 generated, string label)
    {
        if (source.X != generated.X || source.Y != generated.Y)
            throw new InvalidOperationException($"generated runtime {label} does not match source map");
    }
}
