using Unity.Entities;

namespace ModernRA.Simulation
{
    public enum GrayRangeAnchorKind : byte
    {
        Spawn = 1,
        IndustrialResource = 2,
        StrategicResource = 3,
        StrategicSite = 4,
        RoadPoint = 5,
        ControlRegionVertex = 6,
        BuildableVertex = 7
    }

    public struct GrayRangeRuntimeState : IComponentData
    {
        public int MapWidthMeters;
        public int MapHeightMeters;
        public int SpawnCount;
        public int ResourceCount;
        public int StrategicSiteCount;
        public int RoadCount;
        public int ControlRegionCount;
        public int BuildableAreaCount;
    }

    public struct GrayRangeAnchor : IComponentData
    {
        public GrayRangeAnchorKind Kind;
        public int PrimaryIndex;
        public int SecondaryIndex;
        public int TeamSlot;
    }

    public struct AnnihilationRuleEntity : IComponentData
    {
        public int StableId;
        public int TeamId;
        public byte EntityKind;
        public byte Role;
    }

    public struct AnnihilationMatchState : IComponentData
    {
        public int Tick;
        public int WinnerTeamId;
        public int DefeatReason;
        public ulong StateHash;
        public long TeamAIndustrialMilli;
        public long TeamBIndustrialMilli;
        public long TeamAMinedMilli;
        public long TeamBMinedMilli;
        public int TeamAAliveUnits;
        public int TeamBAliveUnits;
        public int TeamAAliveBuildings;
        public int TeamBAliveBuildings;
        public int ShotsFired;
        public int UnitsDestroyed;
        public int BuildingsDestroyed;
        public byte Resolved;
    }
}
