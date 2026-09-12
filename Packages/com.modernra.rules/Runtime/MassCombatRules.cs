using System;

namespace ModernRA.Rules
{
    public sealed class MassCombatUnitState
    {
        public int EntityId;
        public int TeamId;
        public int X;
        public int Y;
        public int Health = 500;
        public int CooldownTicks;
        public bool Alive = true;
    }

    public sealed class MassCombatWorld
    {
        public int Tick;
        public MassCombatUnitState[] Units = Array.Empty<MassCombatUnitState>();
        public int ShotsFired;
        public int UnitsDestroyed;
        public long SpatialCandidateVisits;
        public long NaiveCandidateVisits;
    }

    public static class MassCombatPrototype
    {
        private const int CellSize = 256;
        private const int WeaponRange = 520;
        private const int WeaponCooldownTicks = 6;
        private const int Damage = 110;

        public static MassCombatWorld Create(int unitCount)
        {
            if (unitCount < 2 || (unitCount & 1) != 0)
                throw new ArgumentOutOfRangeException(nameof(unitCount), "unit count must be an even number >= 2");

            var world = new MassCombatWorld { Units = new MassCombatUnitState[unitCount] };
            int pairs = unitCount / 2;
            const int columns = 25;
            const int spacing = 180;
            for (int i = 0; i < pairs; i++)
            {
                int gridX = (i % columns) * spacing - 2200;
                int gridY = (i / columns) * spacing - 1800;
                world.Units[i * 2] = new MassCombatUnitState
                {
                    EntityId = i * 2 + 1,
                    TeamId = 1,
                    X = gridX - 70,
                    Y = gridY
                };
                world.Units[i * 2 + 1] = new MassCombatUnitState
                {
                    EntityId = i * 2 + 2,
                    TeamId = 2,
                    X = gridX + 70,
                    Y = gridY
                };
            }
            return world;
        }

        public static void Run(MassCombatWorld world, int ticks)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(ticks));

            var spatial = new DeterministicSpatialHash(CellSize);
            var pendingDamage = new int[world.Units.Length];
            for (int i = 0; i < ticks; i++)
                Step(world, spatial, pendingDamage);
        }

        private static void Step(MassCombatWorld world, DeterministicSpatialHash spatial, int[] pendingDamage)
        {
            world.Tick++;
            spatial.Clear();
            Array.Clear(pendingDamage, 0, pendingDamage.Length);

            int aliveCount = 0;
            for (int i = 0; i < world.Units.Length; i++)
            {
                MassCombatUnitState unit = world.Units[i];
                if (!unit.Alive)
                    continue;
                aliveCount++;
                if (unit.CooldownTicks > 0)
                    unit.CooldownTicks--;
                spatial.Insert(new SpatialEntity(unit.EntityId, unit.TeamId, unit.X, unit.Y));
            }

            for (int i = 0; i < world.Units.Length; i++)
            {
                MassCombatUnitState attacker = world.Units[i];
                if (!attacker.Alive || attacker.CooldownTicks != 0)
                    continue;

                world.NaiveCandidateVisits += aliveCount;
                if (!spatial.FindNearestEnemy(attacker.X, attacker.Y, attacker.TeamId, WeaponRange, out SpatialEntity target, out SpatialQueryStats stats))
                {
                    world.SpatialCandidateVisits += stats.CandidatesVisited;
                    continue;
                }

                world.SpatialCandidateVisits += stats.CandidatesVisited;
                int targetIndex = target.EntityId - 1;
                if ((uint)targetIndex >= (uint)pendingDamage.Length)
                    throw new InvalidOperationException("spatial query returned invalid entity id");
                pendingDamage[targetIndex] += Damage;
                attacker.CooldownTicks = WeaponCooldownTicks;
                world.ShotsFired++;
            }

            for (int i = 0; i < world.Units.Length; i++)
            {
                MassCombatUnitState target = world.Units[i];
                if (!target.Alive || pendingDamage[i] <= 0)
                    continue;
                target.Health -= pendingDamage[i];
                if (target.Health <= 0)
                {
                    target.Health = 0;
                    target.Alive = false;
                    world.UnitsDestroyed++;
                }
            }
        }

        public static ulong ComputeStateHash(MassCombatWorld world)
        {
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, world.Tick);
            hash = StateHash64.Add(hash, world.ShotsFired);
            hash = StateHash64.Add(hash, world.UnitsDestroyed);
            hash = StateHash64.Add(hash, world.SpatialCandidateVisits);
            for (int i = 0; i < world.Units.Length; i++)
            {
                MassCombatUnitState unit = world.Units[i];
                hash = StateHash64.Add(hash, unit.EntityId);
                hash = StateHash64.Add(hash, unit.TeamId);
                hash = StateHash64.Add(hash, unit.Health);
                hash = StateHash64.Add(hash, unit.CooldownTicks);
                hash = StateHash64.Add(hash, unit.Alive ? 1 : 0);
            }
            return hash;
        }
    }
}
