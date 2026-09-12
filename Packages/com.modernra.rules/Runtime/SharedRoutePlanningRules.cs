using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct SharedRouteCandidate
    {
        public readonly string RouteId;
        public readonly Int2[] Points;
        public readonly int WidthMeters;
        public readonly int TerrainCostPermille;
        public readonly string[] TopologyDependencies;

        public SharedRouteCandidate(
            string routeId,
            Int2[] points,
            int widthMeters,
            int terrainCostPermille,
            string[] topologyDependencies)
        {
            if (string.IsNullOrWhiteSpace(routeId)) throw new ArgumentException("route id is required", nameof(routeId));
            if (points == null || points.Length < 2) throw new ArgumentException("route requires at least two points", nameof(points));
            if (widthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(widthMeters));
            if (terrainCostPermille <= 0) throw new ArgumentOutOfRangeException(nameof(terrainCostPermille));

            RouteId = routeId;
            Points = (Int2[])points.Clone();
            WidthMeters = widthMeters;
            TerrainCostPermille = terrainCostPermille;
            TopologyDependencies = NormalizeDependencies(topologyDependencies);
        }

        private static string[] NormalizeDependencies(string[] dependencies)
        {
            if (dependencies == null || dependencies.Length == 0)
                return Array.Empty<string>();

            var copy = new string[dependencies.Length];
            Array.Copy(dependencies, copy, dependencies.Length);
            Array.Sort(copy, StringComparer.Ordinal);
            for (int i = 0; i < copy.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(copy[i]))
                    throw new ArgumentException("topology dependency id cannot be empty", nameof(dependencies));
                if (i > 0 && string.Equals(copy[i - 1], copy[i], StringComparison.Ordinal))
                    throw new ArgumentException($"duplicate topology dependency {copy[i]}", nameof(dependencies));
            }
            return copy;
        }
    }

    public readonly struct SharedRouteSelection
    {
        public readonly string RouteId;
        public readonly Int2[] Points;
        public readonly long DistanceMeters;
        public readonly long TraversalCost;
        public readonly int WidthMeters;
        public readonly string[] TopologyDependencies;

        public SharedRouteSelection(
            string routeId,
            Int2[] points,
            long distanceMeters,
            long traversalCost,
            int widthMeters,
            string[] topologyDependencies)
        {
            RouteId = routeId;
            Points = points;
            DistanceMeters = distanceMeters;
            TraversalCost = traversalCost;
            WidthMeters = widthMeters;
            TopologyDependencies = topologyDependencies;
        }
    }

    public static class SharedRouteSelector
    {
        public static bool TrySelect(
            IReadOnlyList<SharedRouteCandidate> candidates,
            int requiredWidthMeters,
            out SharedRouteSelection selection)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (requiredWidthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(requiredWidthMeters));

            bool found = false;
            SharedRouteCandidate selected = default;
            long selectedDistance = 0;
            long selectedCost = long.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                SharedRouteCandidate candidate = candidates[i];
                if (candidate.WidthMeters < requiredWidthMeters)
                    continue;

                long distance = ComputePolylineDistance(candidate.Points);
                long cost = checked((distance * candidate.TerrainCostPermille + 999L) / 1000L);
                if (!found || cost < selectedCost ||
                    (cost == selectedCost && string.CompareOrdinal(candidate.RouteId, selected.RouteId) < 0))
                {
                    found = true;
                    selected = candidate;
                    selectedDistance = distance;
                    selectedCost = cost;
                }
            }

            if (!found)
            {
                selection = default;
                return false;
            }

            selection = new SharedRouteSelection(
                selected.RouteId,
                (Int2[])selected.Points.Clone(),
                selectedDistance,
                selectedCost,
                selected.WidthMeters,
                (string[])selected.TopologyDependencies.Clone());
            return true;
        }

        public static long ComputePolylineDistance(Int2[] points)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("route requires at least two points", nameof(points));
            long distance = 0;
            for (int i = 1; i < points.Length; i++)
            {
                long dx = (long)points[i].X - points[i - 1].X;
                long dy = (long)points[i].Y - points[i - 1].Y;
                ulong squared = checked((ulong)(dx * dx + dy * dy));
                distance = checked(distance + IntegerSqrtRounded(squared));
            }
            return distance;
        }

        private static long IntegerSqrtRounded(ulong value)
        {
            if (value == 0) return 0;
            ulong low = 1;
            ulong high = Math.Min(value, 3037000500UL);
            while (low <= high)
            {
                ulong mid = low + ((high - low) >> 1);
                ulong quotient = value / mid;
                if (mid == quotient)
                {
                    ulong square = mid * mid;
                    ulong next = mid + 1;
                    ulong nextSquare = next > 3037000500UL ? ulong.MaxValue : next * next;
                    return (long)(value - square <= nextSquare - value ? mid : next);
                }
                if (mid < quotient)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            ulong floor = high;
            ulong floorSquare = floor * floor;
            ulong ceil = floor + 1;
            ulong ceilSquare = ceil > 3037000500UL ? ulong.MaxValue : ceil * ceil;
            return (long)(value - floorSquare <= ceilSquare - value ? floor : ceil);
        }
    }

    public sealed class LocalizedSharedRouteCache
    {
        private sealed class Entry
        {
            public readonly Int2[] Route;
            public readonly string[] Dependencies;

            public Entry(Int2[] route, string[] dependencies)
            {
                Route = route;
                Dependencies = dependencies;
            }
        }

        private readonly Dictionary<SharedRouteKey, Entry> _routes = new Dictionary<SharedRouteKey, Entry>();

        public int Count => _routes.Count;
        public int StoreCount { get; private set; }
        public int HitCount { get; private set; }
        public int LocalInvalidationEvents { get; private set; }
        public int RoutesInvalidated { get; private set; }

        public void Store(SharedRouteKey key, Int2[] route, IReadOnlyList<string> topologyDependencies)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (route.Length < 2) throw new ArgumentException("shared route requires at least two points", nameof(route));
            if (topologyDependencies == null) throw new ArgumentNullException(nameof(topologyDependencies));

            string[] dependencies = CopyDependencies(topologyDependencies);
            _routes[key] = new Entry((Int2[])route.Clone(), dependencies);
            StoreCount++;
        }

        public bool TryGet(SharedRouteKey key, out Int2[] route)
        {
            if (_routes.TryGetValue(key, out Entry? entry))
            {
                HitCount++;
                route = (Int2[])entry.Route.Clone();
                return true;
            }
            route = Array.Empty<Int2>();
            return false;
        }

        public int InvalidateDependency(string topologyDependencyId)
        {
            if (string.IsNullOrWhiteSpace(topologyDependencyId))
                throw new ArgumentException("topology dependency id is required", nameof(topologyDependencyId));

            var remove = new List<SharedRouteKey>();
            foreach (KeyValuePair<SharedRouteKey, Entry> pair in _routes)
            {
                if (ContainsDependency(pair.Value.Dependencies, topologyDependencyId))
                    remove.Add(pair.Key);
            }

            for (int i = 0; i < remove.Count; i++)
                _routes.Remove(remove[i]);

            if (remove.Count > 0)
            {
                LocalInvalidationEvents++;
                RoutesInvalidated += remove.Count;
            }
            return remove.Count;
        }

        public void ClearAll()
        {
            if (_routes.Count == 0)
                return;
            RoutesInvalidated += _routes.Count;
            _routes.Clear();
            LocalInvalidationEvents++;
        }

        private static string[] CopyDependencies(IReadOnlyList<string> dependencies)
        {
            var copy = new string[dependencies.Count];
            for (int i = 0; i < dependencies.Count; i++)
            {
                string dependency = dependencies[i];
                if (string.IsNullOrWhiteSpace(dependency))
                    throw new ArgumentException("topology dependency id cannot be empty", nameof(dependencies));
                copy[i] = dependency;
            }
            Array.Sort(copy, StringComparer.Ordinal);
            for (int i = 1; i < copy.Length; i++)
                if (string.Equals(copy[i - 1], copy[i], StringComparison.Ordinal))
                    throw new ArgumentException($"duplicate topology dependency {copy[i]}", nameof(dependencies));
            return copy;
        }

        private static bool ContainsDependency(string[] dependencies, string target)
        {
            return Array.BinarySearch(dependencies, target, StringComparer.Ordinal) >= 0;
        }
    }
}
