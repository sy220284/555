namespace ModernRA.Network
{
    public enum ReplicationVisibility : byte { Hidden, ContactOnly, Classified, Full }

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
}
