using Unity.Entities;

namespace ModernRA.IntelEW
{
    public enum IntelLevel : byte { Unknown, Anomaly, Detected, Classified, Confirmed, Tracked }
    public enum EWLevel : byte { Normal, LightInterference, HeavyInterference, Blackout }
    public struct IntelState : IComponentData { public IntelLevel Level; public uint LastConfirmedTick; public int ObserverTeam; }
    public struct ElectronicWarfareState : IComponentData { public EWLevel Level; public ushort SensorPenaltyPermille; public ushort LinkPenaltyPermille; }
    public struct SignatureState : IComponentData { public ushort Radar; public ushort Thermal; public ushort Optical; public ushort Acoustic; }
}
