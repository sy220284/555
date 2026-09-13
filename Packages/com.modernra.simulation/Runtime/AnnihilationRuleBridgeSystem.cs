using System.Collections.Generic;
using ModernRA.Rules;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ModernRA.Simulation
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(SimulationTickSystem))]
    public partial class AnnihilationRuleBridgeSystem : SystemBase
    {
        private struct RuleMirrorSnapshot
        {
            public float3 Position;
            public int Health;
            public int MaximumHealth;
        }

        [BurstCompile]
        private partial struct ApplyRuleMirrorSnapshotJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int, RuleMirrorSnapshot> Snapshots;

            private void Execute(in AnnihilationRuleEntity identity, ref SimPosition position, ref HealthState health)
            {
                if (!Snapshots.TryGetValue(identity.StableId, out RuleMirrorSnapshot snapshot))
                    return;
                position.Value = snapshot.Position;
                health.Current = snapshot.Health;
                health.Maximum = snapshot.MaximumHealth;
            }
        }

        private const int MaxCommandLeadTicks = 8;
        private readonly Dictionary<int, Entity> _unitEntities = new Dictionary<int, Entity>();
        private readonly Dictionary<int, Entity> _buildingEntities = new Dictionary<int, Entity>();
        private LiveCommandedAnnihilationSession _session;
        private Entity _matchStateEntity;
        private Entity _commandQueueEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GrayRangeRuntimeState>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (_session != null)
                return;

            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            _session = new LiveCommandedAnnihilationSession(map.CreateStandardAnnihilationConfig(), MaxCommandLeadTicks);
            _matchStateEntity = EntityManager.CreateEntity(typeof(AnnihilationMatchState));
            _commandQueueEntity = EntityManager.CreateEntity(typeof(PlayerCommandQueueState));
            EntityManager.AddBuffer<PlayerCommandRequest>(_commandQueueEntity);
            SyncAll(_session.World);
        }

        protected override void OnUpdate()
        {
            if (_session == null || _session.World.Resolved)
                return;

            ProcessPendingPlayerCommands();
            _session.Step();
            SyncAll(_session.World);
            if (_session.World.Resolved)
                Enabled = false;
        }

        private void ProcessPendingPlayerCommands()
        {
            if (_commandQueueEntity == Entity.Null || !EntityManager.Exists(_commandQueueEntity))
                return;

            DynamicBuffer<PlayerCommandRequest> pending = EntityManager.GetBuffer<PlayerCommandRequest>(_commandQueueEntity);
            if (pending.Length == 0)
                return;

            PlayerCommandQueueState queueState = EntityManager.GetComponentData<PlayerCommandQueueState>(_commandQueueEntity);
            int executeTick = checked(_session.World.Tick + 1);
            for (int i = 0; i < pending.Length; i++)
            {
                PlayerCommandRequest request = pending[i];
                var command = new PrototypePlayerCommand(
                    executeTick,
                    request.Sequence,
                    request.PlayerId,
                    (PrototypePlayerCommandKind)request.Kind,
                    request.IntValue);
                PrototypeCommandAdmissionResult result = _session.Submit(command);
                if (result == PrototypeCommandAdmissionResult.Accepted)
                    queueState.AcceptedCount++;
                else
                    queueState.RejectedCount++;
                queueState.LastSequence = request.Sequence;
                queueState.LastAdmissionResult = (byte)result;
            }

            pending.Clear();
            EntityManager.SetComponentData(_commandQueueEntity, queueState);
        }

        private void SyncAll(AnnihilationPrototypeWorld world)
        {
            Dependency.Complete();
            SyncTeamStructure(world.TeamA);
            SyncTeamStructure(world.TeamB);
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
            ScheduleBatchMirror(world);
        }

        private void SyncTeamStructure(PrototypeAnnihilationTeamState team)
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

                EnsureEntity(_buildingEntities, key, team.TeamId, 1, (byte)building.Role);
            }

            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive)
                {
                    RemoveEntity(_unitEntities, unit.Id);
                    continue;
                }

                EnsureEntity(_unitEntities, unit.Id, team.TeamId, 2, 0);
            }
        }

        private void ScheduleBatchMirror(AnnihilationPrototypeWorld world)
        {
            int capacity = AnnihilationPrototype.CountAliveBuildings(world.TeamA) +
                AnnihilationPrototype.CountAliveBuildings(world.TeamB) +
                AnnihilationPrototype.CountAliveUnits(world.TeamA) +
                AnnihilationPrototype.CountAliveUnits(world.TeamB);
            var snapshots = new NativeParallelHashMap<int, RuleMirrorSnapshot>(math.max(1, capacity), Allocator.TempJob);
            AddTeamSnapshots(world.TeamA, snapshots);
            AddTeamSnapshots(world.TeamB, snapshots);
            JobHandle applyHandle = new ApplyRuleMirrorSnapshotJob { Snapshots = snapshots }
                .ScheduleParallel(Dependency);
            Dependency = snapshots.Dispose(applyHandle);
        }

        private static void AddTeamSnapshots(PrototypeAnnihilationTeamState team,
            NativeParallelHashMap<int, RuleMirrorSnapshot> snapshots)
        {
            for (int i = 0; i < team.Buildings.Count; i++)
            {
                PrototypeBuildingState building = team.Buildings[i];
                if (!building.Alive)
                    continue;
                int key = team.TeamId * 100000 + i;
                if (!snapshots.TryAdd(key, new RuleMirrorSnapshot
                {
                    Position = new float3(building.X, 0f, building.Y),
                    Health = building.Health,
                    MaximumHealth = building.Role == PrototypeBuildingRole.Core ? 5000 : 3500
                }))
                    throw new System.InvalidOperationException($"duplicate rule mirror id {key}");
            }

            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (!unit.Alive)
                    continue;
                if (!snapshots.TryAdd(unit.Id, new RuleMirrorSnapshot
                {
                    Position = new float3(unit.X, 0f, unit.Y),
                    Health = unit.Health,
                    MaximumHealth = 1000
                }))
                    throw new System.InvalidOperationException($"duplicate rule mirror id {unit.Id}");
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

        protected override void OnDestroy()
        {
            Dependency.Complete();
            base.OnDestroy();
        }
    }
}
