using Unity.Burst;
using Unity.Entities;

namespace ModernRA.Simulation
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct SimulationTickSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SimulationClock>())
            {
                var entity = state.EntityManager.CreateEntity(typeof(SimulationClock));
                state.EntityManager.SetComponentData(entity, new SimulationClock { Tick = 0, FixedDeltaSeconds = SimulationRate.SecondsPerTick });
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clock = SystemAPI.GetSingletonRW<SimulationClock>();
            clock.ValueRW.Tick++;
            clock.ValueRW.FixedDeltaSeconds = SimulationRate.SecondsPerTick;
        }
    }
}
