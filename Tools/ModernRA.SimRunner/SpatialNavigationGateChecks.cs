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
        VerifyRouteCandidateSelection();
        VerifyLocalizedRouteInvalidation();
        VerifyDeterministicLocalAvoidance();

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

    private static void VerifyRouteCandidateSelection()
    {
        var west = new SharedRouteCandidate(
            "ROAD_WEST",
            new[] { new Int2(1200, 1200), new Int2(2600, 3000), new Int2(4000, 4000), new Int2(6200, 5900), new Int2(6800, 6800) },
            60,
            850,
            new[] { "ROAD_WEST" });
        var center = new SharedRouteCandidate(
            "ROAD_CENTER",
            new[] { new Int2(1200, 1200), new Int2(3000, 3000), new Int2(4000, 4000), new Int2(5000, 5000), new Int2(6800, 6800) },
            80,
            800,
            new[] { "ROAD_CENTER" });
        var east = new SharedRouteCandidate(
            "ROAD_EAST",
            new[] { new Int2(1200, 1200), new Int2(3000, 2600), new Int2(5000, 4600), new Int2(6800, 6800) },
            60,
            850,
            new[] { "ROAD_EAST" });
        var candidates = new[] { west, center, east };

        Check(SharedRouteSelector.TrySelect(candidates, 60, out SharedRouteSelection standard), "route selector rejected all Gray Range roads");
        Check(standard.RouteId == "ROAD_CENTER", "route selector did not choose the lowest deterministic traversal cost");
        Check(standard.DistanceMeters > 0 && standard.TraversalCost > 0, "route selector returned invalid route cost");

        Check(SharedRouteSelector.TrySelect(candidates, 70, out SharedRouteSelection wide), "wide formation failed to find eligible route");
        Check(wide.RouteId == "ROAD_CENTER" && wide.WidthMeters == 80, "wide formation was assigned to an undersized road");
        Check(!SharedRouteSelector.TrySelect(candidates, 90, out _), "oversized formation should not fit any Gray Range road");

        var tieB = new SharedRouteCandidate("ROAD_B", new[] { new Int2(0, 0), new Int2(100, 0) }, 60, 1000, Array.Empty<string>());
        var tieA = new SharedRouteCandidate("ROAD_A", new[] { new Int2(0, 0), new Int2(100, 0) }, 60, 1000, Array.Empty<string>());
        Check(SharedRouteSelector.TrySelect(new[] { tieB, tieA }, 35, out SharedRouteSelection tie), "tie route selection failed");
        Check(tie.RouteId == "ROAD_A", "route tie-break is not stable by route id");
    }

    private static void VerifyLocalizedRouteInvalidation()
    {
        var cache = new LocalizedSharedRouteCache();
        var westKey = new SharedRouteKey(1, 5, 1);
        var centerKey = new SharedRouteKey(1, 5, 2);
        var eastKey = new SharedRouteKey(1, 5, 3);
        cache.Store(westKey, new[] { new Int2(0, 0), new Int2(100, 100) }, new[] { "ROAD_WEST" });
        cache.Store(centerKey, new[] { new Int2(0, 0), new Int2(100, 0) }, new[] { "ROAD_CENTER", "BRIDGE_CENTER" });
        cache.Store(eastKey, new[] { new Int2(0, 0), new Int2(100, -100) }, new[] { "ROAD_EAST" });
        Check(cache.Count == 3, "localized route cache did not store all candidates");

        int removed = cache.InvalidateDependency("BRIDGE_CENTER");
        Check(removed == 1, "bridge invalidation removed the wrong number of shared routes");
        Check(cache.Count == 2, "local topology change cleared unrelated routes");
        Check(cache.TryGet(westKey, out _) && cache.TryGet(eastKey, out _), "unrelated route did not survive local invalidation");
        Check(!cache.TryGet(centerKey, out _), "route crossing invalidated bridge survived local invalidation");
        Check(cache.LocalInvalidationEvents == 1 && cache.RoutesInvalidated == 1, "localized invalidation counters are incorrect");
    }

    private static void VerifyDeterministicLocalAvoidance()
    {
        var source = new SpatialEntity(10, 1, 0, 0);
        var overlap = new SpatialEntity(20, 1, 0, 0);
        var close = new SpatialEntity(30, 2, 30, 0);
        var far = new SpatialEntity(40, 2, 500, 0);
        var forward = new DeterministicSpatialHash(64);
        forward.Insert(source);
        forward.Insert(overlap);
        forward.Insert(close);
        forward.Insert(far);

        var scratch = new List<SpatialEntity>();
        LocalAvoidanceResult first = DeterministicLocalAvoidance.Solve(forward, source, 120, 80, 20, scratch);
        Check(first.NeighborCount == 2, "local avoidance did not use the expected close neighbors");
        Check(first.Adjustment.X < 0, "local avoidance did not steer away from overlapping/right-side neighbors");
        Check(Math.Abs(first.Adjustment.X) <= 20 && Math.Abs(first.Adjustment.Y) <= 20, "local avoidance exceeded adjustment budget");
        Check(first.CandidateVisits < 4, "local avoidance query scanned unrelated far entities");

        var reversed = new DeterministicSpatialHash(64);
        reversed.Insert(far);
        reversed.Insert(close);
        reversed.Insert(overlap);
        reversed.Insert(source);
        LocalAvoidanceResult second = DeterministicLocalAvoidance.Solve(reversed, source, 120, 80, 20, scratch);
        Check(first.Adjustment.X == second.Adjustment.X && first.Adjustment.Y == second.Adjustment.Y,
            "local avoidance depends on spatial insertion order");

        Check(forward.Remove(close), "spatial hash failed to remove an indexed unit");
        LocalAvoidanceResult afterRemoval = DeterministicLocalAvoidance.Solve(forward, source, 120, 80, 20, scratch);
        Check(afterRemoval.NeighborCount == 1 && forward.Count == 3,
            "spatial hash removal left a stale local-avoidance candidate");
        Check(!forward.Remove(close), "spatial hash removed the same unit twice");

        Int2 next = DeterministicLocalAvoidance.ApplyStep(new Int2(0, 0), new Int2(12, 0), first.Adjustment, 12);
        long stepSq = (long)next.X * next.X + (long)next.Y * next.Y;
        Check(stepSq <= 12L * 12L, "combined path and avoidance step exceeded movement budget");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
