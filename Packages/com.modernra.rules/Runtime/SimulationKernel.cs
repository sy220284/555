using System;

namespace ModernRA.Rules
{
    public sealed class TeamState
    {
        public int TeamId;
        public long IndustrialMilli;
        public long StrategicMilli;
    }

    public sealed class ResourceNodeState
    {
        public string Id = string.Empty;
        public int OwnerTeam;
        public long CapacityMilli;
        public int BaseRatePerMinute;
        public int ExtractionSlots;
    }

    public sealed class HarvesterState
    {
        public int Id;
        public int TeamId;
        public int ResourceIndex;
        public int SlotIndex;
        public bool Alive = true;
        public long MinedMilli;
    }

    public sealed class TankState
    {
        public int Id;
        public int TeamId;
        public int X;
        public int Y;
        public int Health = 1000;
        public int Armor = 200;
        public int WeaponCooldownTicks;
        public int CorridorCursor;
        public bool Alive = true;
    }

    public sealed class TankPairState
    {
        public TankState A = new TankState();
        public TankState B = new TankState();
    }

    public sealed class ScenarioConfig
    {
        public Int2 SpawnA;
        public Int2 SpawnB;
        public Int2[] SharedCorridor = Array.Empty<Int2>();
        public ResourceNodeState[] Resources = Array.Empty<ResourceNodeState>();
        public int TankPairs;
        public ulong Seed = 1;
        public int ScheduledFailureTick = 2500;
    }

    public sealed class WorldState
    {
        public const int TickRate = 30;
        public const int Milli = 1000;

        public int Tick;
        public TeamState TeamA = new TeamState { TeamId = 1 };
        public TeamState TeamB = new TeamState { TeamId = 2 };
        public ResourceNodeState[] Resources = Array.Empty<ResourceNodeState>();
        public HarvesterState[] Harvesters = Array.Empty<HarvesterState>();
        public TankPairState[] TankPairs = Array.Empty<TankPairState>();
        public Int2[] SharedCorridor = Array.Empty<Int2>();
        public DeterministicRng Rng;
        public int ScheduledFailureTick;
        public int ShotsFired;
        public int UnitsDestroyed;
        public int CorridorBuildCount;
        public int ScheduledFailuresApplied;

        public long PrimaryMinedMilli
        {
            get
            {
                long total = 0;
                for (int i = 0; i < Harvesters.Length; i++)
                    if (Harvesters[i].SlotIndex == 0)
                        total += Harvesters[i].MinedMilli;
                return total;
            }
        }

        public long SecondaryMinedMilli
        {
            get
            {
                long total = 0;
                for (int i = 0; i < Harvesters.Length; i++)
                    if (Harvesters[i].SlotIndex == 1)
                        total += Harvesters[i].MinedMilli;
                return total;
            }
        }
    }

    public static class ScenarioFactory
    {
        public static WorldState Create(ScenarioConfig config)
        {
            if (config.SharedCorridor == null || config.SharedCorridor.Length < 2)
                throw new ArgumentException("Shared corridor requires at least two points.", nameof(config));
            if (config.TankPairs < 1)
                throw new ArgumentOutOfRangeException(nameof(config.TankPairs));

            var world = new WorldState
            {
                Resources = CloneResources(config.Resources),
                SharedCorridor = (Int2[])config.SharedCorridor.Clone(),
                Rng = new DeterministicRng(config.Seed),
                ScheduledFailureTick = config.ScheduledFailureTick,
                CorridorBuildCount = 2
            };

            int harvesterCount = 0;
            for (int i = 0; i < world.Resources.Length; i++)
                harvesterCount += Math.Min(2, Math.Max(1, world.Resources[i].ExtractionSlots));

            world.Harvesters = new HarvesterState[harvesterCount];
            int harvesterId = 1;
            int h = 0;
            for (int i = 0; i < world.Resources.Length; i++)
            {
                int slots = Math.Min(2, Math.Max(1, world.Resources[i].ExtractionSlots));
                for (int slot = 0; slot < slots; slot++)
                {
                    world.Harvesters[h++] = new HarvesterState
                    {
                        Id = harvesterId++,
                        TeamId = world.Resources[i].OwnerTeam,
                        ResourceIndex = i,
                        SlotIndex = slot
                    };
                }
            }

            world.TankPairs = new TankPairState[config.TankPairs];
            for (int i = 0; i < config.TankPairs; i++)
            {
                int spread = (i % 16) * 12;
                int row = (i / 16) % 16;
                world.TankPairs[i] = new TankPairState
                {
                    A = new TankState
                    {
                        Id = 10000 + i,
                        TeamId = 1,
                        X = config.SpawnA.X + spread,
                        Y = config.SpawnA.Y + row * 12,
                        CorridorCursor = 1
                    },
                    B = new TankState
                    {
                        Id = 20000 + i,
                        TeamId = 2,
                        X = config.SpawnB.X - spread,
                        Y = config.SpawnB.Y - row * 12,
                        CorridorCursor = config.SharedCorridor.Length - 2
                    }
                };
            }

            return world;
        }

        private static ResourceNodeState[] CloneResources(ResourceNodeState[] source)
        {
            if (source == null)
                return Array.Empty<ResourceNodeState>();
            var result = new ResourceNodeState[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = new ResourceNodeState
                {
                    Id = source[i].Id,
                    OwnerTeam = source[i].OwnerTeam,
                    CapacityMilli = source[i].CapacityMilli,
                    BaseRatePerMinute = source[i].BaseRatePerMinute,
                    ExtractionSlots = source[i].ExtractionSlots
                };
            }
            return result;
        }
    }

    public static class SimulationKernel
    {
        private const int SecondaryEfficiencyPermille = 800;
        private const int TankSpeedPerTick = 5;
        private const int WeaponRange = 360;
        private const int WeaponRangeSq = WeaponRange * WeaponRange;
        private const int WeaponCooldown = 18;
        private const int AccuracyPermille = 850;
        private const int RawDamage = 240;

        public static void Step(WorldState world)
        {
            world.Tick++;
            ApplyScheduledFailures(world);
            StepEconomy(world);
            StepCombat(world);
        }

        public static void Run(WorldState world, int ticks)
        {
            if (ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(ticks));
            for (int i = 0; i < ticks; i++)
                Step(world);
        }

        private static void ApplyScheduledFailures(WorldState world)
        {
            if (world.Tick != world.ScheduledFailureTick)
                return;

            bool failedA = false;
            bool failedB = false;
            for (int i = 0; i < world.Harvesters.Length; i++)
            {
                var h = world.Harvesters[i];
                if (!h.Alive || h.SlotIndex != 1)
                    continue;
                if (h.TeamId == 1 && !failedA)
                {
                    h.Alive = false;
                    failedA = true;
                    world.ScheduledFailuresApplied++;
                }
                else if (h.TeamId == 2 && !failedB)
                {
                    h.Alive = false;
                    failedB = true;
                    world.ScheduledFailuresApplied++;
                }
                if (failedA && failedB)
                    break;
            }
        }

        private static void StepEconomy(WorldState world)
        {
            for (int i = 0; i < world.Harvesters.Length; i++)
            {
                var harvester = world.Harvesters[i];
                if (!harvester.Alive)
                    continue;

                var node = world.Resources[harvester.ResourceIndex];
                if (node.CapacityMilli <= 0)
                    continue;

                long perTick = (long)node.BaseRatePerMinute * WorldState.Milli / (60 * WorldState.TickRate);
                int efficiency = harvester.SlotIndex == 0 ? 1000 : SecondaryEfficiencyPermille;
                long amount = perTick * efficiency / 1000;
                if (amount <= 0)
                    amount = 1;
                if (amount > node.CapacityMilli)
                    amount = node.CapacityMilli;

                node.CapacityMilli -= amount;
                harvester.MinedMilli += amount;
                if (harvester.TeamId == 1)
                    world.TeamA.IndustrialMilli += amount;
                else if (harvester.TeamId == 2)
                    world.TeamB.IndustrialMilli += amount;
            }
        }

        private static void StepCombat(WorldState world)
        {
            for (int i = 0; i < world.TankPairs.Length; i++)
            {
                var pair = world.TankPairs[i];
                var a = pair.A;
                var b = pair.B;
                if (a.WeaponCooldownTicks > 0) a.WeaponCooldownTicks--;
                if (b.WeaponCooldownTicks > 0) b.WeaponCooldownTicks--;

                if (!a.Alive || !b.Alive)
                    continue;

                int dx = a.X - b.X;
                int dy = a.Y - b.Y;
                long distSq = (long)dx * dx + (long)dy * dy;
                if (distSq > WeaponRangeSq)
                {
                    MoveAlongSharedCorridor(a, world.SharedCorridor, true);
                    MoveAlongSharedCorridor(b, world.SharedCorridor, false);
                    continue;
                }

                int damageToA = 0;
                int damageToB = 0;
                if (a.WeaponCooldownTicks == 0)
                {
                    world.ShotsFired++;
                    a.WeaponCooldownTicks = WeaponCooldown;
                    if (world.Rng.NextPermille() < AccuracyPermille)
                        damageToB = ResolveDamage(RawDamage, b.Armor);
                }
                if (b.WeaponCooldownTicks == 0)
                {
                    world.ShotsFired++;
                    b.WeaponCooldownTicks = WeaponCooldown;
                    if (world.Rng.NextPermille() < AccuracyPermille)
                        damageToA = ResolveDamage(RawDamage, a.Armor);
                }

                if (damageToA > 0)
                    ApplyDamage(a, damageToA, world);
                if (damageToB > 0)
                    ApplyDamage(b, damageToB, world);
            }
        }

        private static int ResolveDamage(int rawDamage, int armor)
        {
            int result = rawDamage - armor / 4;
            return Math.Max(40, result);
        }

        private static void ApplyDamage(TankState target, int damage, WorldState world)
        {
            if (!target.Alive)
                return;
            target.Health -= damage;
            if (target.Health <= 0)
            {
                target.Health = 0;
                target.Alive = false;
                world.UnitsDestroyed++;
            }
        }

        private static void MoveAlongSharedCorridor(TankState tank, Int2[] corridor, bool forward)
        {
            if (!tank.Alive)
                return;

            int cursor = tank.CorridorCursor;
            if (cursor < 0) cursor = 0;
            if (cursor >= corridor.Length) cursor = corridor.Length - 1;
            var target = corridor[cursor];
            MoveAxis(ref tank.X, target.X, TankSpeedPerTick);
            MoveAxis(ref tank.Y, target.Y, TankSpeedPerTick);

            if (Math.Abs(tank.X - target.X) <= TankSpeedPerTick && Math.Abs(tank.Y - target.Y) <= TankSpeedPerTick)
            {
                tank.X = target.X;
                tank.Y = target.Y;
                if (forward && tank.CorridorCursor < corridor.Length - 1)
                    tank.CorridorCursor++;
                else if (!forward && tank.CorridorCursor > 0)
                    tank.CorridorCursor--;
            }
        }

        private static void MoveAxis(ref int value, int target, int maxStep)
        {
            int delta = target - value;
            if (delta > maxStep) value += maxStep;
            else if (delta < -maxStep) value -= maxStep;
            else value = target;
        }

        public static ulong ComputeStateHash(WorldState world)
        {
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, world.Tick);
            hash = StateHash64.Add(hash, world.TeamA.IndustrialMilli);
            hash = StateHash64.Add(hash, world.TeamB.IndustrialMilli);
            hash = StateHash64.Add(hash, world.Rng.State);
            hash = StateHash64.Add(hash, world.ShotsFired);
            hash = StateHash64.Add(hash, world.UnitsDestroyed);
            hash = StateHash64.Add(hash, world.ScheduledFailuresApplied);

            for (int i = 0; i < world.Resources.Length; i++)
            {
                var node = world.Resources[i];
                hash = StateHash64.Add(hash, node.Id);
                hash = StateHash64.Add(hash, node.OwnerTeam);
                hash = StateHash64.Add(hash, node.CapacityMilli);
            }
            for (int i = 0; i < world.Harvesters.Length; i++)
            {
                var h = world.Harvesters[i];
                hash = StateHash64.Add(hash, h.Id);
                hash = StateHash64.Add(hash, h.Alive ? 1 : 0);
                hash = StateHash64.Add(hash, h.MinedMilli);
            }
            for (int i = 0; i < world.TankPairs.Length; i++)
            {
                HashTank(ref hash, world.TankPairs[i].A);
                HashTank(ref hash, world.TankPairs[i].B);
            }
            return hash;
        }

        private static void HashTank(ref ulong hash, TankState tank)
        {
            hash = StateHash64.Add(hash, tank.Id);
            hash = StateHash64.Add(hash, tank.X);
            hash = StateHash64.Add(hash, tank.Y);
            hash = StateHash64.Add(hash, tank.Health);
            hash = StateHash64.Add(hash, tank.WeaponCooldownTicks);
            hash = StateHash64.Add(hash, tank.CorridorCursor);
            hash = StateHash64.Add(hash, tank.Alive ? 1 : 0);
        }
    }
}
