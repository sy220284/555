using Unity.Entities;
using Unity.Mathematics;

namespace ModernRA.Navigation
{
    public enum NavigationLayer : byte { Foot, Wheeled, Tracked, Heavy, Amphibious, Naval, Air }
    public struct NavigationAgent : IComponentData { public NavigationLayer Layer; public float Radius; public float PreferredSpeed; }
    public struct PathCorridor : IComponentData { public int CorridorId; public int Version; public int WaypointIndex; }
    public struct NavigationGoal : IComponentData { public float3 Position; public int RegionId; }
    public struct LocalAvoidance : IComponentData { public float3 Steering; public uint LastSolveTick; }
}
