using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct LocalAvoidanceResult
    {
        public readonly Int2 Adjustment;
        public readonly int NeighborCount;
        public readonly int CandidateVisits;

        public LocalAvoidanceResult(Int2 adjustment, int neighborCount, int candidateVisits)
        {
            Adjustment = adjustment;
            NeighborCount = neighborCount;
            CandidateVisits = candidateVisits;
        }
    }

    public static class DeterministicLocalAvoidance
    {
        public static LocalAvoidanceResult Solve(
            DeterministicSpatialHash spatial,
            in SpatialEntity self,
            int queryRadius,
            int separationRadius,
            int maxAdjustment,
            List<SpatialEntity> scratch)
        {
            if (spatial == null) throw new ArgumentNullException(nameof(spatial));
            if (scratch == null) throw new ArgumentNullException(nameof(scratch));
            if (separationRadius <= 0) throw new ArgumentOutOfRangeException(nameof(separationRadius));
            if (queryRadius < separationRadius) throw new ArgumentOutOfRangeException(nameof(queryRadius));
            if (maxAdjustment <= 0) throw new ArgumentOutOfRangeException(nameof(maxAdjustment));

            spatial.QueryRadius(self.X, self.Y, queryRadius, scratch, out SpatialQueryStats stats);
            long sumX = 0;
            long sumY = 0;
            int neighbors = 0;
            long separationSq = (long)separationRadius * separationRadius;

            for (int i = 0; i < scratch.Count; i++)
            {
                SpatialEntity other = scratch[i];
                if (other.EntityId == self.EntityId)
                    continue;

                long dx = (long)self.X - other.X;
                long dy = (long)self.Y - other.Y;
                long distanceSq = checked(dx * dx + dy * dy);
                if (distanceSq >= separationSq)
                    continue;

                if (distanceSq == 0)
                {
                    // Stable IDs provide a deterministic symmetric escape direction for exact overlaps.
                    dx = self.EntityId < other.EntityId ? -1 : 1;
                    dy = 0;
                    distanceSq = 1;
                }

                long distance = IntegerSqrtRounded((ulong)distanceSq);
                if (distance <= 0)
                    distance = 1;
                long penetration = separationRadius - distance;
                if (penetration <= 0)
                    continue;

                sumX = checked(sumX + dx * penetration / distance);
                sumY = checked(sumY + dy * penetration / distance);
                neighbors++;
            }

            Int2 adjustment = ClampVector(sumX, sumY, maxAdjustment);
            return new LocalAvoidanceResult(adjustment, neighbors, stats.CandidatesVisited);
        }

        public static Int2 ApplyStep(Int2 current, Int2 desiredDelta, Int2 avoidanceAdjustment, int maxStep)
        {
            if (maxStep <= 0) throw new ArgumentOutOfRangeException(nameof(maxStep));
            long x = checked((long)desiredDelta.X + avoidanceAdjustment.X);
            long y = checked((long)desiredDelta.Y + avoidanceAdjustment.Y);
            Int2 step = ClampVector(x, y, maxStep);
            return new Int2(checked(current.X + step.X), checked(current.Y + step.Y));
        }

        private static Int2 ClampVector(long x, long y, int maxMagnitude)
        {
            if (x == 0 && y == 0)
                return new Int2(0, 0);

            ulong squared = checked((ulong)(x * x + y * y));
            long magnitude = IntegerSqrtRounded(squared);
            if (magnitude <= maxMagnitude)
                return new Int2(checked((int)x), checked((int)y));

            long scaledX = x * maxMagnitude / magnitude;
            long scaledY = y * maxMagnitude / magnitude;
            if (scaledX == 0 && x != 0) scaledX = x > 0 ? 1 : -1;
            if (scaledY == 0 && y != 0) scaledY = y > 0 ? 1 : -1;
            return new Int2(checked((int)scaledX), checked((int)scaledY));
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
}
