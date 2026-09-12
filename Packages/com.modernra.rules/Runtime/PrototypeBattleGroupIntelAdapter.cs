using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public readonly struct PrototypeBattleGroupTargetProfile
    {
        public readonly int UnitClass;
        public readonly int Threat;
        public readonly int MissionValue;
        public readonly int Vulnerability;
        public readonly int CounterMatch;
        public readonly int EstimatedCombatPower;

        public PrototypeBattleGroupTargetProfile(int unitClass, int threat, int missionValue, int vulnerability,
            int counterMatch, int estimatedCombatPower)
        {
            if (unitClass < 0) throw new ArgumentOutOfRangeException(nameof(unitClass));
            UnitClass = unitClass;
            Threat = ValidateNormalized(threat, nameof(threat));
            MissionValue = ValidateNormalized(missionValue, nameof(missionValue));
            Vulnerability = ValidateNormalized(vulnerability, nameof(vulnerability));
            CounterMatch = ValidateNormalized(counterMatch, nameof(counterMatch));
            EstimatedCombatPower = Math.Max(0, estimatedCombatPower);
        }

        private static int ValidateNormalized(int value, string name)
        {
            if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public sealed class PrototypeBattleGroupIntelAdapter
    {
        private readonly Dictionary<int, VisibleEntityState> _contacts = new Dictionary<int, VisibleEntityState>();
        private readonly PrototypeBattleGroupTargetProfile[] _profiles;
        private readonly Int2[] _corridor;

        public PrototypeBattleGroupIntelAdapter(Int2[] corridor, IEnumerable<PrototypeBattleGroupTargetProfile> profiles)
        {
            if (corridor == null || corridor.Length < 2) throw new ArgumentException("corridor requires at least two points", nameof(corridor));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            _corridor = (Int2[])corridor.Clone();
            var sorted = new List<PrototypeBattleGroupTargetProfile>(profiles);
            sorted.Sort((left, right) => left.UnitClass.CompareTo(right.UnitClass));
            for (int i = 1; i < sorted.Count; i++)
                if (sorted[i - 1].UnitClass == sorted[i].UnitClass)
                    throw new ArgumentException($"duplicate target profile {sorted[i].UnitClass}", nameof(profiles));
            _profiles = sorted.ToArray();
        }

        public int ContactCount => _contacts.Count;

        public void Apply(AuthoritativeSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!snapshot.IsDelta) _contacts.Clear();
            for (int i = 0; i < snapshot.RemovedContactIds.Length; i++)
                _contacts.Remove(snapshot.RemovedContactIds[i]);
            for (int i = 0; i < snapshot.Entities.Length; i++)
            {
                VisibleEntityState state = snapshot.Entities[i];
                if (state.Friendly) _contacts.Remove(state.ContactId);
                else _contacts[state.ContactId] = state;
            }
        }

        public PrototypeBattleGroupTarget[] BuildTargets()
        {
            var ids = new List<int>(_contacts.Keys);
            ids.Sort();
            var targets = new PrototypeBattleGroupTarget[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                VisibleEntityState contact = _contacts[ids[i]];
                PrototypeBattleGroupTargetProfile profile = FindProfile(contact.UnitClass);
                int combatPower = contact.Health >= 0 ? contact.Health : profile.EstimatedCombatPower;
                targets[i] = new PrototypeBattleGroupTarget(contact.ContactId, FindNearestWaypoint(contact.X, contact.Y),
                    profile.Threat, profile.MissionValue, profile.Vulnerability, profile.CounterMatch,
                    combatPower, ToIntelLevel(contact.Detail));
            }
            return targets;
        }

        private PrototypeBattleGroupTargetProfile FindProfile(int unitClass)
        {
            for (int i = 0; i < _profiles.Length; i++)
                if (_profiles[i].UnitClass == unitClass) return _profiles[i];
            return new PrototypeBattleGroupTargetProfile(0, 500, 500, 500, 500, 1000);
        }

        private int FindNearestWaypoint(int x, int y)
        {
            int selected = 0;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < _corridor.Length; i++)
            {
                long dx = (long)x - _corridor[i].X;
                long dy = (long)y - _corridor[i].Y;
                long distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    selected = i;
                }
            }
            return selected;
        }

        private static RuleIntelLevel ToIntelLevel(RuleReplicationDetail detail)
        {
            return detail switch
            {
                RuleReplicationDetail.Contact => RuleIntelLevel.Detected,
                RuleReplicationDetail.Class => RuleIntelLevel.Classified,
                RuleReplicationDetail.Confirmed => RuleIntelLevel.Confirmed,
                RuleReplicationDetail.Full => RuleIntelLevel.Tracked,
                _ => RuleIntelLevel.Unknown
            };
        }
    }
}
