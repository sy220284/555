using System.Diagnostics;
using ModernRA.Rules;

internal static class SpatialNavigationGateChecks
{
    public static void Run()
    {
        const int entityCount = 1000;
        const int columns = 40;
        const int spacing = 160;
        const int queryRadius = 500;
        var entities = new SpatialEntity[entityCount];
        var index = new DeterministicSpatialHash(256);

        for (int i = 0; i < entityCount; i++)
        {
            int x = (i % columns) * spacing - 3000;
            int y = (i / columns) * spacing - 1800;
            int team = (i & 1) == 0 ? 1 : 2;
            entities[i] = new SpatialEntity(i + 1, team, x, y);
            index.Insert(entities[i]);
        }

        Check(index.Count == entityCount, "spatial hash lost entities during insertion");
        Check(index.OccupiedCellCount > 0 && index.OccupiedCellCount < entityCount, "spatial hash occupancy is invalid");

        var nearestIds = new int[entityCount];
        SpatialQueryStats total = default;
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < entities.Length; i++)
        {
            SpatialEntity source = entities[i];
            Check(index.FindNearestEnemy(source.X, source.Y, source.TeamId, queryRadius, out SpatialEntity nearest, out SpatialQueryStats stats), "nearby enemy query failed");
            Check(nearest.TeamId != source.TeamId, "spatial query returned friendly entity as enemy");
            nearestIds[i] = nearest.EntityId;
            total.Add(stats);
        }
        timer.Stop();

        long naiveCandidateVisits = (long)entityCount * entityCount;
        Check(total.CandidatesVisited < naiveCandidateVisits / 5, "spatial broad phase did not reduce candidate visits by at least 80 percent");
        Check(total.Matches > 0, "spatial queries produced no matches");

        var reverse = new DeterministicSpatialHash(256);
        for (int i = entities.Length - 1; i >= 0; i--)
            reverse.Insert(entities[i]);
        for (int i = 0; i < entities.Length; i++)
        {
            SpatialEntity source = entities[i];
            Check(reverse.FindNearestEnemy(source.X, source.Y, source.TeamId, queryRadius, out SpatialEntity nearest, out _), "reverse insertion nearest query failed");
            Check(nearest.EntityId == nearestIds[i], "spatial nearest result depends on insertion order");
        }

        var radiusResults = new List<SpatialEntity>();
        index.QueryRadius(0, 0, 700, radiusResults, out SpatialQueryStats radiusStats);
        Check(radiusResults.Count > 0, "radius query returned no entities");
        for (int i = 1; i < radiusResults.Count; i++)
            Check(radiusResults[i - 1].EntityId < radiusResults[i].EntityId, "radius query output is not stable by entity id");

        VerifySharedRouteInvalidation();

        Console.WriteLine(
            $"spatial_navigation_gate=passed entities={entityCount} cells={index.OccupiedCellCount} " +
            $"candidate_visits={total.CandidatesVisited} naive_visits={naiveCandidateVisits} " +
            $"reduction_permille={1000 - (int)(total.CandidatesVisited * 1000L / naiveCandidateVisits)} " +
            $"query_elapsed_ms={timer.Elapsed.TotalMilliseconds:F3} radius_matches={radiusStats.Matches}");
    }

    private static void VerifySharedRouteInvalidation()
    {
        var cache = new SharedRouteCache(7);
        var key = new SharedRouteKey(10, 20, 1);
        var original = new[] { new Int2(0, 0), new Int2(100, 0), new Int2(200, 0) };
        cache.Store(key, original);
        Check(cache.BuildCount == 1 && cache.Count == 1, "shared route was not cached");
        Check(cache.TryGet(key, out Int2[] first) && first.Length == 3, "shared route cache miss on unchanged topology");
        Check(cache.HitCount == 1, "shared route hit counter invalid");

        cache.SetTopologyVersion(7);
        Check(cache.Count == 1 && cache.InvalidationCount == 0, "unchanged topology invalidated route cache");
        cache.SetTopologyVersion(8);
        Check(cache.Count == 0 && cache.InvalidationCount == 1, "topology change did not invalidate shared routes");
        Check(!cache.TryGet(key, out _), "stale route survived topology invalidation");

        var rebuilt = new[] { new Int2(0, 0), new Int2(100, 50), new Int2(200, 100) };
        cache.Store(key, rebuilt);
        Check(cache.BuildCount == 2, "route rebuild counter invalid after topology change");
        Check(cache.TryGet(key, out Int2[] second) && second[1].Y == 50, "rebuilt route was not returned");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
