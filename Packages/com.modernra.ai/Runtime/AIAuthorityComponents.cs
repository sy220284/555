using System;
using Unity.Entities;

namespace ModernRA.AI
{
    public enum AIAuthorityLevel : byte { None, Tactical, BattleGroup, Theater, Deputy }
    [Flags] public enum AIForbiddenAction : ushort { None=0, StrategicWeapon=1, StrategicReserve=2, ChangeMainTech=4, DemolishCore=8, FullRetreat=16 }
    public struct AIAuthority : IComponentData { public AIAuthorityLevel Level; public int OwnerPlayerId; public int RegionId; public AIForbiddenAction Forbidden; }
    public struct AIUpdateCadence : IComponentData { public ushort IntervalTicks; public uint NextTick; }
    public struct AIOrderGeneration : IComponentData { public uint Generation; public uint PlayerOverrideGeneration; }
}
