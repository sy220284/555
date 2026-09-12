using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum PrototypePlayerCommandKind : byte
    {
        SetPlan = 1,
        SetTeamRallyWaypoint = 2,
        HoldUnit = 3,
        ResumeUnit = 4
    }

    public readonly struct PrototypePlayerCommand
    {
        public readonly int Tick;
        public readonly int Sequence;
        public readonly int PlayerId;
        public readonly PrototypePlayerCommandKind Kind;
        public readonly int IntValue;

        public PrototypePlayerCommand(int tick, int sequence, int playerId, PrototypePlayerCommandKind kind, int intValue)
        {
            Tick = tick;
            Sequence = sequence;
            PlayerId = playerId;
            Kind = kind;
            IntValue = intValue;
        }
    }

    public sealed class DeterministicCommandTimeline
    {
        private const ulong FnvOffset = 1469598103934665603UL;
        private const ulong FnvPrime = 1099511628211UL;
        private readonly List<PrototypePlayerCommand> _commands;
        private int _cursor;

        public DeterministicCommandTimeline(IEnumerable<PrototypePlayerCommand> commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            _commands = new List<PrototypePlayerCommand>();
            foreach (PrototypePlayerCommand command in commands)
            {
                Validate(command);
                _commands.Add(command);
            }
            _commands.Sort(CompareCommands);
            RejectDuplicateKeys(_commands);
        }

        public int Count => _commands.Count;

        public void Reset()
        {
            _cursor = 0;
        }

        public PrototypePlayerCommand[] ToCanonicalArray()
        {
            return _commands.ToArray();
        }

        public ulong ComputeCanonicalHash()
        {
            ulong hash = FnvOffset;
            for (int i = 0; i < _commands.Count; i++)
            {
                PrototypePlayerCommand command = _commands[i];
                HashInt(ref hash, command.Tick);
                HashInt(ref hash, command.Sequence);
                HashInt(ref hash, command.PlayerId);
                HashInt(ref hash, (int)command.Kind);
                HashInt(ref hash, command.IntValue);
            }
            return hash;
        }

        public void ApplyForNextTick(AnnihilationPrototypeWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (world.Resolved)
                return;

            int nextTick = world.Tick + 1;
            if (_cursor < _commands.Count && _commands[_cursor].Tick < nextTick)
                throw new InvalidOperationException("command timeline cursor fell behind authoritative world tick");

            while (_cursor < _commands.Count && _commands[_cursor].Tick == nextTick)
            {
                Apply(world, _commands[_cursor]);
                _cursor++;
            }
        }

        public static void StepWithCommands(AnnihilationPrototypeWorld world, DeterministicCommandTimeline timeline)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            timeline.ApplyForNextTick(world);
            AnnihilationPrototype.Step(world);
        }

        private static int CompareCommands(PrototypePlayerCommand left, PrototypePlayerCommand right)
        {
            int value = left.Tick.CompareTo(right.Tick);
            if (value != 0) return value;
            value = left.PlayerId.CompareTo(right.PlayerId);
            if (value != 0) return value;
            return left.Sequence.CompareTo(right.Sequence);
        }

        private static void RejectDuplicateKeys(List<PrototypePlayerCommand> commands)
        {
            for (int i = 1; i < commands.Count; i++)
            {
                PrototypePlayerCommand previous = commands[i - 1];
                PrototypePlayerCommand current = commands[i];
                if (previous.Tick == current.Tick &&
                    previous.PlayerId == current.PlayerId &&
                    previous.Sequence == current.Sequence)
                {
                    throw new ArgumentException(
                        $"duplicate command key tick={current.Tick} player={current.PlayerId} sequence={current.Sequence}",
                        nameof(commands));
                }
            }
        }

        private static void Validate(PrototypePlayerCommand command)
        {
            if (command.Tick <= 0)
                throw new ArgumentOutOfRangeException(nameof(command), "command tick must be positive");
            if (command.Sequence < 0)
                throw new ArgumentOutOfRangeException(nameof(command), "command sequence must be non-negative");
            if (command.PlayerId != 1 && command.PlayerId != 2)
                throw new ArgumentOutOfRangeException(nameof(command), "prototype player id must be 1 or 2");
            switch (command.Kind)
            {
                case PrototypePlayerCommandKind.SetPlan:
                    if (command.IntValue != (int)PrototypeAnnihilationPlan.Aggressive &&
                        command.IntValue != (int)PrototypeAnnihilationPlan.Economy)
                    {
                        throw new ArgumentOutOfRangeException(nameof(command), command.IntValue, "invalid annihilation plan value");
                    }
                    break;
                case PrototypePlayerCommandKind.SetTeamRallyWaypoint:
                    if (command.IntValue < 0 || command.IntValue > 64)
                        throw new ArgumentOutOfRangeException(nameof(command), command.IntValue, "invalid rally waypoint index");
                    break;
                case PrototypePlayerCommandKind.HoldUnit:
                case PrototypePlayerCommandKind.ResumeUnit:
                    if (command.IntValue <= 0)
                        throw new ArgumentOutOfRangeException(nameof(command), command.IntValue, "unit id must be positive");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command.Kind, "unsupported prototype command kind");
            }
        }

        private static void Apply(AnnihilationPrototypeWorld world, PrototypePlayerCommand command)
        {
            PrototypeAnnihilationTeamState team = command.PlayerId == 1 ? world.TeamA : world.TeamB;
            switch (command.Kind)
            {
                case PrototypePlayerCommandKind.SetPlan:
                    team.Plan = (PrototypeAnnihilationPlan)command.IntValue;
                    break;
                case PrototypePlayerCommandKind.SetTeamRallyWaypoint:
                    if (command.IntValue >= world.SharedCorridor.Length)
                        throw new ArgumentOutOfRangeException(nameof(command), command.IntValue, "rally waypoint is outside the shared corridor");
                    for (int i = 0; i < team.Units.Count; i++)
                    {
                        PrototypeCombatUnitState unit = team.Units[i];
                        if (unit.Alive)
                            unit.CorridorCursor = command.IntValue;
                    }
                    break;
                case PrototypePlayerCommandKind.HoldUnit:
                    SetUnitHoldingState(team, command.IntValue, true);
                    break;
                case PrototypePlayerCommandKind.ResumeUnit:
                    SetUnitHoldingState(team, command.IntValue, false);
                    break;
                default:
                    throw new InvalidOperationException($"unsupported prototype command kind {command.Kind}");
            }
        }

        private static void SetUnitHoldingState(PrototypeAnnihilationTeamState team, int unitId, bool holding)
        {
            for (int i = 0; i < team.Units.Count; i++)
            {
                PrototypeCombatUnitState unit = team.Units[i];
                if (unit.Id != unitId)
                    continue;
                if (!unit.Alive)
                    throw new InvalidOperationException($"unit {unitId} is destroyed");
                unit.HoldingPosition = holding;
                return;
            }
            throw new InvalidOperationException($"unit {unitId} is not owned by player {team.TeamId}");
        }

        private static void HashInt(ref ulong hash, int value)
        {
            unchecked
            {
                uint raw = (uint)value;
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(raw >> (i * 8));
                    hash *= FnvPrime;
                }
            }
        }
    }
}
