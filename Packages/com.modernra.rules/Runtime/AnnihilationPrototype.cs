using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum PrototypeAnnihilationPlan : byte
    {
        Aggressive = 0,
        Economy = 1
    }

    public enum PrototypeBuildingRole : byte
    {
        Core = 0,
        Power = 1,
        Refinery = 2,
        Factory = 3
    }

    public enum PrototypeConstructionKind : byte
    {
        None = 0,
        Power = 1,
        Refinery = 2,
        Factory = 3,
        Tank = 4
    }

    public enum PrototypeDefeatReason : byte
    {
        None = 0,
        WarSystemCollapse = 1
    }

    public sealed class PrototypeBuildingState
    {
        public PrototypeBuildingRole Role;
        public int X;
        public int Y;
        public int Health;
        public bool Alive = true;
    }

    public sealed class PrototypeCombatUnitState
    {
        public int Id;
        public int TeamId;
        public int X;
        public int Y;
        public int Health = 1000;
        public int WeaponCooldownTicks;
        public int CorridorCursor;
        public int ControlGroupId;
        public bool HoldingPosition;
        public bool Alive = true;
    }

    public sealed class PrototypeAnnihilationTeamState
    {
        public int TeamId;
        public PrototypeAnnihilationPlan Plan;
        public Int2 Spawn;
        public int Direction;
        public long IndustrialMilli;
        public long MinedMilli;
        public readonly List<PrototypeBuildingState> Buildings = new List<PrototypeBuildingState>();
        public readonly List<PrototypeCombatUnitState> Units = new List<PrototypeCombatUnitState>();
        public PrototypeConstructionKind Construction;
        public int ConstructionRemainingTicks;
        public int UnitsProduced;
        public int BuildingsCompleted;
        public PrototypeDefeatReason DefeatReason;
        public uint PlayerOverrideGeneration;
    }

    public sealed class AnnihilationPrototypeConfig
    {
        public Int2 SpawnA = new Int2(1200, 1200);
        public Int2 SpawnB = new Int2(6800, 6800);
        public Int2[] SharedCorridor =
        {
            new Int2(1200, 1200),
            new Int2(3000, 3000),
            new Int2(4000, 4000),
            new Int2(5000, 5000),
            new Int2(6800, 6800)
        };
        public long StartingIndustrialMilli = 2500L * 1000L;
        public int MaxLiveTanksPerTeam = 12;
    }

    public sealed class AnnihilationPrototypeWorld
    {
        public int Tick;
        public PrototypeAnnihilationTeamState TeamA = new PrototypeAnnihilationTeamState();
        public PrototypeAnnihilationTeamState TeamB = new PrototypeAnnihilationTeamState();
        public Int2[] SharedCorridor = Array.Empty<Int2>();
        public int ShotsFired;
        public int UnitsDestroyed;
        public int BuildingsDestroyed;
        public int MovementSteps;
        public int WinnerTeamId;
        public PrototypeDefeatReason DefeatReason;
        public int MaxLiveTanksPerTeam;

        public bool Resolved => WinnerTeamId != 0;
    }

    public readonly struct AnnihilationPrototypeResult
    {
        public readonly int WinnerTeamId;
        public readonly PrototypeDefeatReason DefeatReason;
        public readonly int ResolvedTick;
        public readonly ulong StateHash;

        public AnnihilationPrototypeResult(int winnerTeamId, PrototypeDefeatReason defeatReason, int resolvedTick, ulong stateHash)
        {
            WinnerTeamId = winnerTeamId;
            DefeatReason = defeatReason;
            ResolvedTick = resolvedTick;
            StateHash = stateHash;
        }
    }

    public static class AnnihilationPrototype
    {
        private const int Milli = 1000;
        private const long PowerCost = 800L * Milli;
        private const long RefineryCost = 1200L * Milli;
        private const long FactoryCost = 1800L * Milli;
        private const long TankCost = 900L * Milli;
        private const int PowerBuildTicks = 60;
        private const int RefineryBuildTicks = 90;
        private const int FactoryBuildTicks = 120;
        private const int TankBuildTicks = 90;
        private const long MiningPerTickMilli = 1000L;
        private const int TankSpeedPerTick = 7;
        private const int WeaponRange = 360;
        private const int WeaponRangeSq = WeaponRange * WeaponRange;
        private const int RawDamage = 240;
        private const int TankArmor = 200;
        private const int WeaponCooldownTicks = 18;
        private const long EconomyReserveMilli = 1800L * Milli;

        public static AnnihilationPrototypeWorld Create(AnnihilationPrototypeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.SharedCorridor == null || config.SharedCorridor.Length < 2)
                throw new ArgumentException("Shared corridor requires at least two points.", nameof(config));
            if (config.MaxLiveTanksPerTeam < 1)
                throw new ArgumentOutOfRangeException(nameof(config.MaxLiveTanksPerTeam));

            var world = new AnnihilationPrototypeWorld
            {
                SharedCorridor = (Int2[])config.SharedCorridor.Clone(),
                TeamA = CreateTeam(1, PrototypeAnnihilationPlan.Aggressive, config.SpawnA, 1, config.StartingIndustrialMilli),
                TeamB = CreateTeam(2, PrototypeAnnihilationPlan.Economy, config.SpawnB, -1, config.StartingIndustrialMilli),
                MaxLiveTanksPerTeam = config.MaxLiveTanksPerTeam
            };
            return world;
        }

        public static AnnihilationPrototypeResult Run(AnnihilationPrototypeWorld world, int watchdogTicks)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (watchdogTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(watchdogTicks));

            for (int i = 0; i < watchdogTicks && !world.Resolved; i++)
                Step(world);

            if (!world.Resolved)
                throw new InvalidOperationException("annihilation prototype did not resolve before test watchdog; gameplay has no time-based victory rule");

            return new AnnihilationPrototypeResult(world.WinnerTeamId, world.DefeatReason, world.Tick, ComputeStateHash(world));
        }

        public static void Step(AnnihilationPrototypeWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (world.Resolved)
                return;

            world.Tick++;
            StepEconomyAndProduction(world.TeamA, world.SharedCorridor.Length, world.MaxLiveTanksPerTeam);
            StepEconomyAndProduction(world.TeamB, world.SharedCorridor.Length, world.MaxLiveTanksPerTeam);
            StepTeamCombat(world, world.TeamA, world.TeamB, true);
            StepTeamCombat(world, world.TeamB, world.TeamA, false);
            ResolveWarSystemCollapse(world);
        }

        private static PrototypeAnnihilationTeamState CreateTeam(int teamId, PrototypeAnnihilationPlan plan, Int2 spawn, int direction, long industrialMilli)
        {
            var team = new PrototypeAnnihilationTeamState
            {
                TeamId = teamId,
                Plan = plan,
                Spawn = spawn,
                Direction = direction,
                IndustrialMilli = industrialMilli
            };
            Int2 core = BuildingPosition(team, PrototypeBuildingRole.Core);
            team.Buildings.Add(new PrototypeBuildingState
            {
                Role = PrototypeBuildingRole.Core,
                X = core.X,
                Y = core.Y,
                Health = 5000
            });
            return team;
        }

        private static void StepEconomyAndProduction(PrototypeAnnihilationTeamState team, int corridorLength, int maxLiveTanks)
        {
            if (HasAliveBuilding(team, PrototypeBuildingRole.Refinery))
            {
                team.IndustrialMilli += MiningPerTickMilli;
                team.MinedMilli += MiningPerTickMilli;
            }

            if (team.Construction != PrototypeConstructionKind.None)
            {
                if (!HasAliveBuilding(team, PrototypeBuildingRole.Core) && team.Construction != PrototypeConstructionKind.Tank)
                {
                    team.Construction = PrototypeConstructionKind.None;
                    team.ConstructionRemainingTicks = 0;
                    return;
                }

                team.ConstructionRemainingTicks--;
                if (team.ConstructionRemainingTicks <= 0)
                    CompleteConstruction(team, corridorLength);
                return;
            }

            if (!HasAliveBuilding(team, PrototypeBuildingRole.Core))
                return;

            if (!HasAliveBuilding(team, PrototypeBuildingRole.Power))
            {
                TryStartConstruction(team, PrototypeConstructionKind.Power, PowerCost, PowerBuildTicks);
                return;
            }
            if (!HasAliveBuilding(team, PrototypeBuildingRole.Refinery))
            {
                TryStartConstruction(team, PrototypeConstructionKind.Refinery, RefineryCost, RefineryBuildTicks);
                return;
            }
            if (!HasAliveBuilding(team, PrototypeBuildingRole.Factory))
            {
                TryStartConstruction(team, PrototypeConstructionKind.Factory, FactoryCost, FactoryBuildTicks);
                return;
            }

            if (CountLiveUnits(team) >= maxLiveTanks)
                return;

            long threshold = team.Plan == PrototypeAnnihilationPlan.Aggressive ? TankCost : TankCost + EconomyReserveMilli;
            if (team.IndustrialMilli >= threshold)
                TryStartConstruction(team, PrototypeConstructionKind.Tank, TankCost, TankBuildTicks);
        }

        private static bool TryStartConstruction(PrototypeAnnihilationTeamState team, PrototypeConstructionKind kind, long cost, int buildTicks)
        {
            if (team.IndustrialMilli < cost)
                return false;
            team.IndustrialMilli -= cost;
            team.Construction = kind;
            team.ConstructionRemainingTicks = buildTicks;
            return true;
        }

        private static void CompleteConstruction(PrototypeAnnihilationTeamState team, int corridorLength)
        {
            PrototypeConstructionKind completed = team.Construction;
            team.Construction = PrototypeConstructionKind.None;
            team.ConstructionRemainingTicks = 0;

            if (completed == PrototypeConstructionKind.Tank)
            {
                if (!HasAliveBuilding(team, PrototypeBuildingRole.Factory))
                    return;

                int index = team.UnitsProduced;
                int cursor = team.TeamId == 1 ? 1 : corridorLength - 2;
                team.Units.Add(new PrototypeCombatUnitState
                {
                    Id = team.TeamId * 1000 + index,
                    TeamId = team.TeamId,
                    X = team.Spawn.X + (index % 4) * 15 * team.Direction,
                    Y = team.Spawn.Y + (index / 4) * 15 * team.Direction,
                    CorridorCursor = cursor
                });
                team.UnitsProduced++;
                return;
            }

            PrototypeBuildingRole role = ToBuildingRole(completed);
            Int2 position = BuildingPosition(team, role);
            team.Buildings.Add(new PrototypeBuildingState
            {
                Role = role,
                X = position.X,
                Y = position.Y,
                Health = 3500
            });
            team.BuildingsCompleted++;
        }

        private static PrototypeBuildingRole ToBuildingRole(PrototypeConstructionKind kind)
        {
            switch (kind)
            {
                case PrototypeConstructionKind.Power: return PrototypeBuildingRole.Power;
                case PrototypeConstructionKind.Refinery: return PrototypeBuildingRole.Refinery;
                case PrototypeConstructionKind.Factory: return PrototypeBuildingRole.Factory;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "construction is not a building");
            }
        }

        private static Int2 BuildingPosition(PrototypeAnnihilationTeamState team, PrototypeBuildingRole role)
        {
            int offsetX;
            int offsetY;
            switch (role)
            {
                case PrototypeBuildingRole.Core: offsetX = 0; offsetY = 0; break;
                case PrototypeBuildingRole.Power: offsetX = -160; offsetY = -120; break;
                case PrototypeBuildingRole.Refinery: offsetX = 170; offsetY = -120; break;
                case PrototypeBuildingRole.Factory: offsetX = 0; offsetY = 190; break;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
            return new Int2(team.Spawn.X + offsetX * team.Direction, team.Spawn.Y + offsetY * team.Direction);
        }

        private static void StepTeamCombat(AnnihilationPrototypeWorld world, PrototypeAnnihilationTeamState attacker, PrototypeAnnihilationTeamState defender, bool forward)
        {
            for (int i = 0; i < attacker.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = attacker.Units[i];
                if (!unit.Alive)
                    continue;
                if (unit.WeaponCooldownTicks > 0)
                    unit.WeaponCooldownTicks--;

                PrototypeCombatUnitState? targetUnit = FindNearestLiveUnitInRange(unit, defender.Units);
                if (targetUnit != null)
                {
                    if (unit.WeaponCooldownTicks == 0)
                    {
                        unit.WeaponCooldownTicks = WeaponCooldownTicks;
                        world.ShotsFired++;
                        ApplyUnitDamage(targetUnit, ResolveUnitDamage(RawDamage, TankArmor), world);
                    }
                    continue;
                }

                PrototypeBuildingState? targetBuilding = FindPriorityBuildingInRange(unit, defender);
                if (targetBuilding != null)
                {
                    if (unit.WeaponCooldownTicks == 0)
                    {
                        unit.WeaponCooldownTicks = WeaponCooldownTicks;
                        world.ShotsFired++;
                        ApplyBuildingDamage(targetBuilding, RawDamage, world);
                    }
                    continue;
                }

                if (unit.HoldingPosition)
                    continue;

                int oldX = unit.X;
                int oldY = unit.Y;
                MoveAlongSharedCorridor(unit, world.SharedCorridor, forward);
                if (unit.X != oldX || unit.Y != oldY)
                    world.MovementSteps++;
            }
        }

        private static PrototypeCombatUnitState? FindNearestLiveUnitInRange(PrototypeCombatUnitState source, List<PrototypeCombatUnitState> candidates)
        {
            PrototypeCombatUnitState? best = null;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                PrototypeCombatUnitState candidate = candidates[i];
                if (!candidate.Alive)
                    continue;
                long distance = DistanceSq(source.X, source.Y, candidate.X, candidate.Y);
                if (distance > WeaponRangeSq || distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private static PrototypeBuildingState? FindPriorityBuildingInRange(PrototypeCombatUnitState source, PrototypeAnnihilationTeamState defender)
        {
            PrototypeBuildingRole[] priority =
            {
                PrototypeBuildingRole.Factory,
                PrototypeBuildingRole.Core,
                PrototypeBuildingRole.Refinery,
                PrototypeBuildingRole.Power
            };

            for (int p = 0; p < priority.Length; p++)
            {
                for (int i = 0; i < defender.Buildings.Count; i++)
                {
                    PrototypeBuildingState building = defender.Buildings[i];
                    if (!building.Alive || building.Role != priority[p])
                        continue;
                    if (DistanceSq(source.X, source.Y, building.X, building.Y) <= WeaponRangeSq)
                        return building;
                }
            }
            return null;
        }

        private static int ResolveUnitDamage(int rawDamage, int armor)
        {
            return Math.Max(40, rawDamage - armor / 4);
        }

        private static void ApplyUnitDamage(PrototypeCombatUnitState target, int damage, AnnihilationPrototypeWorld world)
        {
            if (!target.Alive)
                return;
            target.Health -= damage;
            if (target.Health > 0)
                return;
            target.Health = 0;
            target.Alive = false;
            world.UnitsDestroyed++;
        }

        private static void ApplyBuildingDamage(PrototypeBuildingState target, int damage, AnnihilationPrototypeWorld world)
        {
            if (!target.Alive)
                return;
            target.Health -= damage;
            if (target.Health > 0)
                return;
            target.Health = 0;
            target.Alive = false;
            world.BuildingsDestroyed++;
        }

        private static void MoveAlongSharedCorridor(PrototypeCombatUnitState unit, Int2[] corridor, bool forward)
        {
            if (corridor.Length == 0)
                return;
            int cursor = unit.CorridorCursor;
            if (cursor < 0) cursor = 0;
            if (cursor >= corridor.Length) cursor = corridor.Length - 1;
            Int2 target = corridor[cursor];
            MoveAxis(ref unit.X, target.X, TankSpeedPerTick);
            MoveAxis(ref unit.Y, target.Y, TankSpeedPerTick);

            if (Math.Abs(unit.X - target.X) <= TankSpeedPerTick && Math.Abs(unit.Y - target.Y) <= TankSpeedPerTick)
            {
                unit.X = target.X;
                unit.Y = target.Y;
                if (forward && unit.CorridorCursor < corridor.Length - 1)
                    unit.CorridorCursor++;
                else if (!forward && unit.CorridorCursor > 0)
                    unit.CorridorCursor--;
            }
        }

        private static void MoveAxis(ref int value, int target, int maxStep)
        {
            int delta = target - value;
            if (delta > maxStep) value += maxStep;
            else if (delta < -maxStep) value -= maxStep;
            else value = target;
        }

        private static long DistanceSq(int ax, int ay, int bx, int by)
        {
            long dx = ax - bx;
            long dy = ay - by;
            return dx * dx + dy * dy;
        }

        private static bool HasAliveBuilding(PrototypeAnnihilationTeamState team, PrototypeBuildingRole role)
        {
            for (int i = 0; i < team.Buildings.Count; i++)
                if (team.Buildings[i].Alive && team.Buildings[i].Role == role)
                    return true;
            return false;
        }

        private static int CountLiveUnits(PrototypeAnnihilationTeamState team)
        {
            int count = 0;
            for (int i = 0; i < team.Units.Count; i++)
                if (team.Units[i].Alive)
                    count++;
            return count;
        }

        private static void ResolveWarSystemCollapse(AnnihilationPrototypeWorld world)
        {
            bool collapsedA = IsWarSystemCollapsed(world.TeamA);
            bool collapsedB = IsWarSystemCollapsed(world.TeamB);
            if (!collapsedA && !collapsedB)
                return;

            if (collapsedA && collapsedB)
            {
                world.WinnerTeamId = -1;
                world.DefeatReason = PrototypeDefeatReason.WarSystemCollapse;
                world.TeamA.DefeatReason = PrototypeDefeatReason.WarSystemCollapse;
                world.TeamB.DefeatReason = PrototypeDefeatReason.WarSystemCollapse;
                return;
            }

            PrototypeAnnihilationTeamState loser = collapsedA ? world.TeamA : world.TeamB;
            world.WinnerTeamId = collapsedA ? world.TeamB.TeamId : world.TeamA.TeamId;
            world.DefeatReason = PrototypeDefeatReason.WarSystemCollapse;
            loser.DefeatReason = PrototypeDefeatReason.WarSystemCollapse;
        }

        private static bool IsWarSystemCollapsed(PrototypeAnnihilationTeamState team)
        {
            bool hasCore = HasAliveBuilding(team, PrototypeBuildingRole.Core);
            bool hasFactory = HasAliveBuilding(team, PrototypeBuildingRole.Factory);
            bool factoryUnderConstruction = team.Construction == PrototypeConstructionKind.Factory;
            return !hasCore && !hasFactory && !factoryUnderConstruction;
        }

        public static ulong ComputeStateHash(AnnihilationPrototypeWorld world)
        {
            ulong hash = StateHash64.Begin();
            hash = StateHash64.Add(hash, world.Tick);
            hash = StateHash64.Add(hash, world.WinnerTeamId);
            hash = StateHash64.Add(hash, (int)world.DefeatReason);
            hash = StateHash64.Add(hash, world.ShotsFired);
            hash = StateHash64.Add(hash, world.UnitsDestroyed);
            hash = StateHash64.Add(hash, world.BuildingsDestroyed);
            hash = StateHash64.Add(hash, world.MovementSteps);
            HashTeam(ref hash, world.TeamA);
            HashTeam(ref hash, world.TeamB);
            return hash;
        }

        private static void HashTeam(ref ulong hash, PrototypeAnnihilationTeamState team)
        {
            hash = StateHash64.Add(hash, team.TeamId);
            hash = StateHash64.Add(hash, (int)team.Plan);
            hash = StateHash64.Add(hash, team.IndustrialMilli);
            hash = StateHash64.Add(hash, team.MinedMilli);
            hash = StateHash64.Add(hash, (int)team.Construction);
            hash = StateHash64.Add(hash, team.ConstructionRemainingTicks);
            hash = StateHash64.Add(hash, team.UnitsProduced);
            hash = StateHash64.Add(hash, team.BuildingsCompleted);
            hash = StateHash64.Add(hash, (int)team.DefeatReason);
            if (team.PlayerOverrideGeneration != 0)
            {
                hash = StateHash64.Add(hash, 0x4F565244);
                hash = StateHash64.Add(hash, (ulong)team.PlayerOverrideGeneration);
            }

            for (int i = 0; i < team.Buildings.Count; i++)
            {
                PrototypeBuildingState building = team.Buildings[i];
                hash = StateHash64.Add(hash, (int)building.Role);
                hash = StateHash64.Add(hash, building.X);
                hash = StateHash64.Add(hash, building.Y);
                hash = StateHash64.Add(hash, building.Health);
                hash = StateHash64.Add(hash, building.Alive ? 1 : 0);
            }
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                hash = StateHash64.Add(hash, unit.Id);
                hash = StateHash64.Add(hash, unit.X);
                hash = StateHash64.Add(hash, unit.Y);
                hash = StateHash64.Add(hash, unit.Health);
                hash = StateHash64.Add(hash, unit.WeaponCooldownTicks);
                hash = StateHash64.Add(hash, unit.CorridorCursor);
                if (unit.ControlGroupId != 0)
                {
                    hash = StateHash64.Add(hash, 0x47525049);
                    hash = StateHash64.Add(hash, unit.ControlGroupId);
                }
                if (unit.HoldingPosition)
                    hash = StateHash64.Add(hash, 0x484F4C44);
                hash = StateHash64.Add(hash, unit.Alive ? 1 : 0);
            }
        }

        public static int CountAliveBuildings(PrototypeAnnihilationTeamState team)
        {
            int count = 0;
            for (int i = 0; i < team.Buildings.Count; i++)
                if (team.Buildings[i].Alive)
                    count++;
            return count;
        }

        public static int CountAliveUnits(PrototypeAnnihilationTeamState team)
        {
            return CountLiveUnits(team);
        }
    }
}
