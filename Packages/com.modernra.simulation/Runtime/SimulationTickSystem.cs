using Unity.Burst;
using Unity.Entities;

namespace ModernRA.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SimulationTickSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SimulationClock>())
            {
                var entity = state.EntityManager.CreateEntity(typeof(SimulationClock));
                state.EntityManager.SetComponentData(entity, new SimulationClock
                {
                    Tick = 0,
                    FixedDeltaSeconds = 1f / 30f
                });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clock = SystemAPI.GetSingletonRW<SimulationClock>();
            clock.ValueRW.Tick++;
            clock.ValueRW.FixedDeltaSeconds = 1f / 30f;
        }
    }
}
