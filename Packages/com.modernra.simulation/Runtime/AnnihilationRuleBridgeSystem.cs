using System.Collections.Generic;
using ModernRA.Rules;
using Unity.Entities;
using Unity.Mathematics;

namespace ModernRA.Simulation
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(SimulationTickSystem))]
    public partial class AnnihilationRuleBridgeSystem : SystemBase
    {
        private readonly Dictionary<int, Entity> _unitEntities = new Dictionary<int, Entity>();
        private readonly Dictionary<int, Entity> _buildingEntities = new Dictionary<int, Entity>();
        private AnnihilationPrototypeWorld _world;
        private Entity _matchStateEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GrayRangeRuntimeState>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (_world != null)
                return;

            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            _world = AnnihilationPrototype.Create(map.CreateStandardAnnihilationConfig());
            _matchStateEntity = EntityManager.CreateEntity(typeof(AnnihilationMatchState));
            SyncAll(_world);
        }

        protected override void OnUpdate()
        {
            if (_world == null || _world.Resolved)
                return;

            AnnihilationPrototype.Step(_world);
            SyncAll(_world);
            if (_world.Resolved)
                Enabled = false;
        }

        private void SyncAll(AnnihilationPrototypeWorld world)
        {
            SyncTeam(world.TeamA);
            SyncTeam(world.TeamB);
            EntityManager.SetComponentData(_matchStateEntity, new AnnihilationMatchState
            {
                Tick = world.Tick,
                WinnerTeamId = world.WinnerTeamId,
                DefeatReason = (int)world.DefeatReason,
                StateHash = AnnihilationPrototype.ComputeStateHash(world),
                TeamAIndustrialMilli = world.TeamA.IndustrialMilli,
                TeamBIndustrialMilli = world.TeamB.IndustrialMilli,
                TeamAMinedMilli = world.TeamA.MinedMilli,
                TeamBMinedMilli = world.TeamB.MinedMilli,
                TeamAAliveUnits = AnnihilationPrototype.CountAliveUnits(world.TeamA),
                TeamBAliveUnits = AnnihilationPrototype.CountAliveUnits(world.TeamB),
                TeamAAliveBuildings = AnnihilationPrototype.CountAliveBuildings(world.TeamA),
                TeamBAliveBuildings = AnnihilationPrototype.CountAliveBuildings(world.TeamB),
                ShotsFired = world.ShotsFired,
                UnitsDestroyed = world.UnitsDestroyed,
                BuildingsDestroyed = world.BuildingsDestroyed,
                Resolved = world.Resolved ? (byte)1 : (byte)0
            });
        }

        private void SyncTeam(PrototypeAnnihilationTeamState team)
        {
            for (int i = 0; i < team.Buildings.Count; i++)
            {
                PrototypeBuildingState building = team.Buildings[i];
                int key = team.TeamId * 100000 + i;
                if (!building.Alive)
                {
                    RemoveEntity(_buildingEntities, key);
                    continue;
                }

                Entity entity = EnsureEntity(_buildingEntities, key, team.TeamId, 1, (byte)building.Role);
                EntityManager.SetComponentData(entity, new SimPosition { Value = new float3(building.X, 0f, building.Y) });
                EntityManager.SetComponentData(entity, new HealthState { Current = building.Health, Maximum = building.Role == PrototypeBuildingRole.Core ? 5000 : 3500 });
            }

            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive)
                {
                    RemoveEntity(_unitEntities, unit.Id);
                    continue;
                }

                Entity entity = EnsureEntity(_unitEntities, unit.Id, team.TeamId, 2, 0);
                EntityManager.SetComponentData(entity, new SimPosition { Value = new float3(unit.X, 0f, unit.Y) });
                EntityManager.SetComponentData(entity, new HealthState { Current = unit.Health, Maximum = 1000 });
            }
        }

        private Entity EnsureEntity(Dictionary<int, Entity> index, int key, int teamId, byte entityKind, byte role)
        {
            if (index.TryGetValue(key, out Entity existing) && EntityManager.Exists(existing))
                return existing;

            Entity entity = EntityManager.CreateEntity(typeof(AnnihilationRuleEntity), typeof(CommandOwner), typeof(SimPosition), typeof(HealthState));
            EntityManager.SetComponentData(entity, new AnnihilationRuleEntity
            {
                StableId = key,
                TeamId = teamId,
                EntityKind = entityKind,
                Role = role
            });
            EntityManager.SetComponentData(entity, new CommandOwner { PlayerId = teamId });
            index[key] = entity;
            return entity;
        }

        private void RemoveEntity(Dictionary<int, Entity> index, int key)
        {
            if (!index.TryGetValue(key, out Entity entity))
                return;
            if (EntityManager.Exists(entity))
                EntityManager.DestroyEntity(entity);
            index.Remove(key);
        }
    }
}
