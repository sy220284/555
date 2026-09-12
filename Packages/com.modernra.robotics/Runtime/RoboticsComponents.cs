using Unity.Entities;

namespace ModernRA.Robotics
{
    public enum AutonomyLevel : byte { A1 = 1, A2 = 2, A3 = 3, A4 = 4 }
    public enum LinkState : byte { Connected, Degraded, Lost }
    public struct RoboticsState : IComponentData { public AutonomyLevel Autonomy; public LinkState Link; public ushort ComputeDemand; public ushort PowerDemand; }
    public struct ComputeAllocation : IComponentData { public ushort Granted; public ushort Requested; }
}
