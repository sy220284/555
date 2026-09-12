using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct SpatialEntity
    {
        public readonly int EntityId;
        public readonly int TeamId;
        public readonly int X;
        public readonly int Y;

        public SpatialEntity(int entityId, int teamId, int x, int y)
        {
            EntityId = entityId;
            TeamId = teamId;
            X = x;
            Y = y;
        }
    }

    public struct SpatialQueryStats
    {
        public int CellsVisited;
        public int CandidatesVisited;
        public int Matches;

        public void Add(SpatialQueryStats other)
        {
            CellsVisited += other.CellsVisited;
            CandidatesVisited += other.CandidatesVisited;
            Matches += other.Matches;
        }
    }

    public sealed class DeterministicSpatialHash
    {
        private readonly int _cellSize;
        private readonly Dictionary<long, List<SpatialEntity>> _cells = new Dictionary<long, List<SpatialEntity>>();
        private int _count;

        public DeterministicSpatialHash(int cellSize)
        {
            if (cellSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            _cellSize = cellSize;
        }

        public int CellSize => _cellSize;
        public int Count => _count;
        public int OccupiedCellCount => _cells.Count;

        public void Clear()
        {
            foreach (List<SpatialEntity> bucket in _cells.Values)
                bucket.Clear();
            _count = 0;
        }

        public void Insert(SpatialEntity entity)
        {
            int cellX = FloorDiv(entity.X, _cellSize);
            int cellY = FloorDiv(entity.Y, _cellSize);
            long key = CellKey(cellX, cellY);
            if (!_cells.TryGetValue(key, out List<SpatialEntity>? bucket))
            {
                bucket = new List<SpatialEntity>(8);
                _cells.Add(key, bucket);
            }
            bucket.Add(entity);
            _count++;
        }

        public bool FindNearestEnemy(int x, int y, int ownTeamId, int radius, out SpatialEntity nearest, out SpatialQueryStats stats)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));

            stats = default;
            nearest = default;
            bool found = false;
            long bestDistanceSq = long.MaxValue;
            long radiusSq = (long)radius * radius;
            int minCellX = FloorDiv(x - radius, _cellSize);
            int maxCellX = FloorDiv(x + radius, _cellSize);
            int minCellY = FloorDiv(y - radius, _cellSize);
            int maxCellY = FloorDiv(y + radius, _cellSize);

            for (int cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    stats.CellsVisited++;
                    if (!_cells.TryGetValue(CellKey(cellX, cellY), out List<SpatialEntity>? bucket))
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        SpatialEntity candidate = bucket[i];
                        stats.CandidatesVisited++;
                        if (candidate.TeamId == ownTeamId)
                            continue;

                        long dx = (long)candidate.X - x;
                        long dy = (long)candidate.Y - y;
                        long distanceSq = dx * dx + dy * dy;
                        if (distanceSq > radiusSq)
                            continue;
                        stats.Matches++;

                        if (!found || distanceSq < bestDistanceSq || (distanceSq == bestDistanceSq && candidate.EntityId < nearest.EntityId))
                        {
                            found = true;
                            nearest = candidate;
                            bestDistanceSq = distanceSq;
                        }
                    }
                }
            }
            return found;
        }

        public void QueryRadius(int x, int y, int radius, List<SpatialEntity> output, out SpatialQueryStats stats)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));

            output.Clear();
            stats = default;
            long radiusSq = (long)radius * radius;
            int minCellX = FloorDiv(x - radius, _cellSize);
            int maxCellX = FloorDiv(x + radius, _cellSize);
            int minCellY = FloorDiv(y - radius, _cellSize);
            int maxCellY = FloorDiv(y + radius, _cellSize);

            for (int cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    stats.CellsVisited++;
                    if (!_cells.TryGetValue(CellKey(cellX, cellY), out List<SpatialEntity>? bucket))
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        SpatialEntity candidate = bucket[i];
                        stats.CandidatesVisited++;
                        long dx = (long)candidate.X - x;
                        long dy = (long)candidate.Y - y;
                        if (dx * dx + dy * dy <= radiusSq)
                        {
                            output.Add(candidate);
                            stats.Matches++;
                        }
                    }
                }
            }

            output.Sort(static (a, b) => a.EntityId.CompareTo(b.EntityId));
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && value < 0)
                quotient--;
            return quotient;
        }

        private static long CellKey(int cellX, int cellY)
        {
            return ((long)cellX << 32) ^ (uint)cellY;
        }
    }

    public readonly struct SharedRouteKey : IEquatable<SharedRouteKey>
    {
        public readonly int StartRegionId;
        public readonly int EndRegionId;
        public readonly int MovementClass;

        public SharedRouteKey(int startRegionId, int endRegionId, int movementClass)
        {
            StartRegionId = startRegionId;
            EndRegionId = endRegionId;
            MovementClass = movementClass;
        }

        public bool Equals(SharedRouteKey other)
        {
            return StartRegionId == other.StartRegionId && EndRegionId == other.EndRegionId && MovementClass == other.MovementClass;
        }

        public override bool Equals(object? obj)
        {
            return obj is SharedRouteKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StartRegionId;
                hash = hash * 397 ^ EndRegionId;
                hash = hash * 397 ^ MovementClass;
                return hash;
            }
        }
    }

    public sealed class SharedRouteCache
    {
        private readonly Dictionary<SharedRouteKey, Int2[]> _routes = new Dictionary<SharedRouteKey, Int2[]>();
        private int _topologyVersion;

        public int TopologyVersion => _topologyVersion;
        public int BuildCount { get; private set; }
        public int HitCount { get; private set; }
        public int InvalidationCount { get; private set; }
        public int Count => _routes.Count;

        public SharedRouteCache(int initialTopologyVersion = 1)
        {
            if (initialTopologyVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(initialTopologyVersion));
            _topologyVersion = initialTopologyVersion;
        }

        public bool TryGet(SharedRouteKey key, out Int2[] route)
        {
            if (_routes.TryGetValue(key, out Int2[]? cached))
            {
                HitCount++;
                route = cached;
                return true;
            }
            route = Array.Empty<Int2>();
            return false;
        }

        public void Store(SharedRouteKey key, Int2[] route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));
            if (route.Length < 2)
                throw new ArgumentException("shared route requires at least two points", nameof(route));

            _routes[key] = (Int2[])route.Clone();
            BuildCount++;
        }

        public void SetTopologyVersion(int topologyVersion)
        {
            if (topologyVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(topologyVersion));
            if (topologyVersion == _topologyVersion)
                return;

            _topologyVersion = topologyVersion;
            _routes.Clear();
            InvalidationCount++;
        }
    }
}
