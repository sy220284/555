using Unity.Entities;
using Unity.Mathematics;

namespace ModernRA.Simulation
{
    public struct SimulationClock : IComponentData
    {
        public uint Tick;
        public float FixedDeltaSeconds;
    }

    public struct SimPosition : IComponentData { public float3 Value; }
    public struct SimVelocity : IComponentData { public float3 Value; }
    public struct SimHeading : IComponentData { public float Radians; }
    public struct HealthState : IComponentData { public int Current; public int Maximum; }
    public struct SupplyState : IComponentData { public byte Level; }
    public struct SensorFlags : IComponentData { public uint Value; }
    public struct CommandOwner : IComponentData { public int PlayerId; }
}
