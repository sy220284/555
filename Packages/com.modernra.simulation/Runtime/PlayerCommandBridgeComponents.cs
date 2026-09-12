using Unity.Entities;

namespace ModernRA.Simulation
{
    public struct PlayerCommandQueueState : IComponentData
    {
        public int AcceptedCount;
        public int RejectedCount;
        public int LastSequence;
        public byte LastAdmissionResult;
    }

    public struct PlayerCommandRequest : IBufferElementData
    {
        public int PlayerId;
        public int Sequence;
        public byte Kind;
        public int IntValue;
    }
}
