using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public sealed class LiveCommandedAnnihilationSession
    {
        private readonly PrototypeCommandInbox _inbox;
        private readonly List<PrototypePlayerCommand> _executedCommands = new List<PrototypePlayerCommand>();

        public LiveCommandedAnnihilationSession(AnnihilationPrototypeConfig config, int maxCommandLeadTicks)
        {
            World = AnnihilationPrototype.Create(config ?? throw new ArgumentNullException(nameof(config)));
            _inbox = new PrototypeCommandInbox(maxCommandLeadTicks);
        }

        public AnnihilationPrototypeWorld World { get; }
        public int PendingCommandCount => _inbox.PendingCount;
        public int ExecutedCommandCount => _executedCommands.Count;

        public PrototypeCommandAdmissionResult Submit(PrototypePlayerCommand command)
        {
            if (World.Resolved)
                return PrototypeCommandAdmissionResult.MatchResolved;
            return _inbox.TryAccept(World.Tick, command);
        }

        public void Step()
        {
            if (World.Resolved)
                return;

            int nextTick = checked(World.Tick + 1);
            PrototypePlayerCommand[] commands = _inbox.DrainForTick(nextTick);
            if (commands.Length == 0)
            {
                AnnihilationPrototype.Step(World);
                return;
            }

            var timeline = new DeterministicCommandTimeline(commands);
            DeterministicCommandTimeline.StepWithCommands(World, timeline);
            _executedCommands.AddRange(commands);
        }

        public AnnihilationPrototypeResult RunUntilResolved(int watchdogTicks)
        {
            if (watchdogTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(watchdogTicks));

            for (int i = 0; i < watchdogTicks && !World.Resolved; i++)
                Step();

            if (!World.Resolved)
                throw new InvalidOperationException("live commanded match did not resolve before watchdog");

            return new AnnihilationPrototypeResult(
                World.WinnerTeamId,
                World.DefeatReason,
                World.Tick,
                AnnihilationPrototype.ComputeStateHash(World));
        }

        public PrototypePlayerCommand[] GetExecutedCommands()
        {
            return new DeterministicCommandTimeline(_executedCommands).ToCanonicalArray();
        }

        public ulong ComputeExecutedCommandHash()
        {
            return new DeterministicCommandTimeline(_executedCommands).ComputeCanonicalHash();
        }
    }
}
