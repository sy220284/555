using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum PrototypeCommandAdmissionResult : byte
    {
        Accepted = 0,
        InvalidCommand = 1,
        TickNotInFuture = 2,
        TickTooFarFuture = 3,
        DuplicateSequence = 4,
        SequenceTooOld = 5,
        MatchResolved = 6
    }

    public sealed class PrototypeCommandInbox
    {
        private const int ReplayWindowBits = 64;
        private readonly int _maxLeadTicks;
        private readonly Dictionary<int, ReplayWindow> _replayByPlayer = new Dictionary<int, ReplayWindow>();
        private readonly List<PrototypePlayerCommand> _pending = new List<PrototypePlayerCommand>();

        private struct ReplayWindow
        {
            public bool Initialized;
            public int HighestSequence;
            public ulong SeenMask;
        }

        public PrototypeCommandInbox(int maxLeadTicks)
        {
            if (maxLeadTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLeadTicks));
            _maxLeadTicks = maxLeadTicks;
        }

        public int PendingCount => _pending.Count;
        public int MaxLeadTicks => _maxLeadTicks;

        public PrototypeCommandAdmissionResult TryAccept(int authoritativeTick, PrototypePlayerCommand command)
        {
            if (authoritativeTick < 0)
                throw new ArgumentOutOfRangeException(nameof(authoritativeTick));

            if (command.Tick <= authoritativeTick)
                return PrototypeCommandAdmissionResult.TickNotInFuture;
            if ((long)command.Tick > (long)authoritativeTick + _maxLeadTicks)
                return PrototypeCommandAdmissionResult.TickTooFarFuture;

            if (!IsStructurallyValid(command))
                return PrototypeCommandAdmissionResult.InvalidCommand;

            ReplayWindow window;
            _replayByPlayer.TryGetValue(command.PlayerId, out window);
            PrototypeCommandAdmissionResult replayResult = CheckAndAdvanceReplayWindow(ref window, command.Sequence);
            if (replayResult != PrototypeCommandAdmissionResult.Accepted)
                return replayResult;

            _replayByPlayer[command.PlayerId] = window;
            _pending.Add(command);
            return PrototypeCommandAdmissionResult.Accepted;
        }

        public PrototypePlayerCommand[] DrainForTick(int authoritativeTick)
        {
            if (authoritativeTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(authoritativeTick));

            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].Tick < authoritativeTick)
                    throw new InvalidOperationException("command inbox fell behind authoritative tick");
            }

            var ready = new List<PrototypePlayerCommand>();
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PrototypePlayerCommand command = _pending[i];
                if (command.Tick != authoritativeTick)
                    continue;
                ready.Add(command);
                _pending.RemoveAt(i);
            }

            ready.Sort(CompareSameTickCommands);
            return ready.ToArray();
        }

        private static bool IsStructurallyValid(PrototypePlayerCommand command)
        {
            try
            {
                _ = new DeterministicCommandTimeline(new[] { command });
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static PrototypeCommandAdmissionResult CheckAndAdvanceReplayWindow(ref ReplayWindow window, int sequence)
        {
            if (!window.Initialized)
            {
                window.Initialized = true;
                window.HighestSequence = sequence;
                window.SeenMask = 1UL;
                return PrototypeCommandAdmissionResult.Accepted;
            }

            if (sequence > window.HighestSequence)
            {
                int advance = sequence - window.HighestSequence;
                window.SeenMask = advance >= ReplayWindowBits
                    ? 1UL
                    : (window.SeenMask << advance) | 1UL;
                window.HighestSequence = sequence;
                return PrototypeCommandAdmissionResult.Accepted;
            }

            int age = window.HighestSequence - sequence;
            if (age >= ReplayWindowBits)
                return PrototypeCommandAdmissionResult.SequenceTooOld;

            ulong bit = 1UL << age;
            if ((window.SeenMask & bit) != 0)
                return PrototypeCommandAdmissionResult.DuplicateSequence;

            window.SeenMask |= bit;
            return PrototypeCommandAdmissionResult.Accepted;
        }

        private static int CompareSameTickCommands(PrototypePlayerCommand left, PrototypePlayerCommand right)
        {
            int value = left.PlayerId.CompareTo(right.PlayerId);
            if (value != 0)
                return value;
            return left.Sequence.CompareTo(right.Sequence);
        }
    }
}
