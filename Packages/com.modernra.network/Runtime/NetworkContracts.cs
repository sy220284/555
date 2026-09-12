using ModernRA.Rules;

namespace ModernRA.Network
{
    public enum ReplicationVisibility : byte { Hidden, ContactOnly, Classified, Confirmed, Full }

    public readonly struct CommandEnvelope
    {
        public readonly uint Tick;
        public readonly int PlayerId;
        public readonly uint Sequence;
        public CommandEnvelope(uint tick, int playerId, uint sequence) { Tick=tick; PlayerId=playerId; Sequence=sequence; }
    }

    public readonly struct SnapshotHeader
    {
        public readonly uint Tick;
        public readonly uint BaselineTick;
        public readonly ulong ContentHash;
        public SnapshotHeader(uint tick, uint baselineTick, ulong contentHash) { Tick=tick; BaselineTick=baselineTick; ContentHash=contentHash; }
    }

    public static class NetworkVisibilityPolicy
    {
        public static ReplicationVisibility FromIntel(RuleIntelLevel level)
        {
            return level switch
            {
                RuleIntelLevel.Unknown => ReplicationVisibility.Hidden,
                RuleIntelLevel.Anomaly => ReplicationVisibility.Hidden,
                RuleIntelLevel.Detected => ReplicationVisibility.ContactOnly,
                RuleIntelLevel.Classified => ReplicationVisibility.Classified,
                RuleIntelLevel.Confirmed => ReplicationVisibility.Confirmed,
                RuleIntelLevel.Tracked => ReplicationVisibility.Full,
                _ => ReplicationVisibility.Hidden
            };
        }
    }
}
