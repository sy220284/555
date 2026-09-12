using System;
using System.Collections.Generic;

namespace ModernRA.Rules
{
    public enum RuleRobotBehaviorState : byte
    {
        Idle = 0,
        Follow = 1,
        Search = 2,
        Attack = 3,
        Evade = 4,
        Damaged = 5,
        Return = 6,
        Disabled = 7
    }

    public enum RuleRobotMission : byte
    {
        Idle = 0,
        Follow = 1,
        Patrol = 2,
        Defend = 3,
        Attack = 4,
        Return = 5
    }

    public readonly struct RuleEWInterferenceSource
    {
        public readonly int SourceId;
        public readonly int StrengthPermille;
        public readonly int ExposurePermille;

        public RuleEWInterferenceSource(int sourceId, int strengthPermille, int exposurePermille)
        {
            if (sourceId <= 0) throw new ArgumentOutOfRangeException(nameof(sourceId));
            SourceId = sourceId;
            StrengthPermille = ValidatePermille(strengthPermille, nameof(strengthPermille));
            ExposurePermille = ValidatePermille(exposurePermille, nameof(exposurePermille));
        }

        private static int ValidatePermille(int value, string name)
        {
            if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public static class RuleElectronicWarfareRules
    {
        public const int MinimumQualityPermille = 150;

        public static int DefaultResistancePermille(RuleAutonomyLevel autonomy, bool fiberControlled = false)
        {
            if (fiberControlled) return 1000;
            return autonomy switch
            {
                RuleAutonomyLevel.A1 => 100,
                RuleAutonomyLevel.A2 => 350,
                RuleAutonomyLevel.A3 => 550,
                RuleAutonomyLevel.A4 => 700,
                _ => throw new ArgumentOutOfRangeException(nameof(autonomy))
            };
        }

        public static int ApplyInterference(
            int baseQualityPermille,
            int resistancePermille,
            IReadOnlyList<RuleEWInterferenceSource> sources)
        {
            if (baseQualityPermille < 0 || baseQualityPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(baseQualityPermille));
            if (resistancePermille < 0 || resistancePermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(resistancePermille));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var ordered = new RuleEWInterferenceSource[sources.Count];
            for (int i = 0; i < sources.Count; i++) ordered[i] = sources[i];
            Array.Sort(ordered, (left, right) => left.SourceId.CompareTo(right.SourceId));
            for (int i = 1; i < ordered.Length; i++)
                if (ordered[i - 1].SourceId == ordered[i].SourceId)
                    throw new ArgumentException($"duplicate EW source {ordered[i].SourceId}", nameof(sources));

            long quality = baseQualityPermille;
            int vulnerability = 1000 - resistancePermille;
            for (int i = 0; i < ordered.Length; i++)
            {
                RuleEWInterferenceSource source = ordered[i];
                long effectPermille = (long)source.StrengthPermille * source.ExposurePermille * vulnerability / 1_000_000L;
                quality = quality * (1000L - effectPermille) / 1000L;
            }

            if (quality < MinimumQualityPermille) quality = MinimumQualityPermille;
            if (quality > 1000) quality = 1000;
            return (int)quality;
        }
    }

    public readonly struct RuleRobotBehaviorInput
    {
        public readonly RuleRobotBehaviorState CurrentState;
        public readonly RuleRobotMission Mission;
        public readonly RuleAutonomyLevel ConfiguredAutonomy;
        public readonly RuleLinkState Link;
        public readonly RuleEWLevel EW;
        public readonly int ComputeGranted;
        public readonly int ComputeRequested;
        public readonly int HealthPermille;
        public readonly int SystemStress;
        public readonly int NavigationQualityPermille;
        public readonly int CommsQualityPermille;
        public readonly bool TargetConfirmed;
        public readonly bool UnderImmediateThreat;
        public readonly bool HoldAtAllCosts;
        public readonly int LostLinkTicks;

        public RuleRobotBehaviorInput(
            RuleRobotBehaviorState currentState,
            RuleRobotMission mission,
            RuleAutonomyLevel configuredAutonomy,
            RuleLinkState link,
            RuleEWLevel ew,
            int computeGranted,
            int computeRequested,
            int healthPermille,
            int systemStress,
            int navigationQualityPermille,
            int commsQualityPermille,
            bool targetConfirmed,
            bool underImmediateThreat,
            bool holdAtAllCosts,
            int lostLinkTicks)
        {
            if (computeGranted < 0) throw new ArgumentOutOfRangeException(nameof(computeGranted));
            if (computeRequested < 0) throw new ArgumentOutOfRangeException(nameof(computeRequested));
            if (healthPermille < 0 || healthPermille > 1000) throw new ArgumentOutOfRangeException(nameof(healthPermille));
            if (systemStress < 0 || systemStress > 100) throw new ArgumentOutOfRangeException(nameof(systemStress));
            if (navigationQualityPermille < 0 || navigationQualityPermille > 1000) throw new ArgumentOutOfRangeException(nameof(navigationQualityPermille));
            if (commsQualityPermille < 0 || commsQualityPermille > 1000) throw new ArgumentOutOfRangeException(nameof(commsQualityPermille));
            if (lostLinkTicks < 0) throw new ArgumentOutOfRangeException(nameof(lostLinkTicks));
            CurrentState = currentState;
            Mission = mission;
            ConfiguredAutonomy = configuredAutonomy;
            Link = link;
            EW = ew;
            ComputeGranted = computeGranted;
            ComputeRequested = computeRequested;
            HealthPermille = healthPermille;
            SystemStress = systemStress;
            NavigationQualityPermille = navigationQualityPermille;
            CommsQualityPermille = commsQualityPermille;
            TargetConfirmed = targetConfirmed;
            UnderImmediateThreat = underImmediateThreat;
            HoldAtAllCosts = holdAtAllCosts;
            LostLinkTicks = lostLinkTicks;
        }
    }

    public readonly struct RuleRobotBehaviorDecision
    {
        public readonly RuleRobotBehaviorState State;
        public readonly RuleAutonomyLevel EffectiveAutonomy;
        public readonly RuleLinkState EffectiveLink;
        public readonly RobotFallbackMode Fallback;
        public readonly int MoveSpeedPermille;
        public readonly bool AllowComplexManeuver;
        public readonly bool CanEnterUnknownArea;
        public readonly bool CanAcquireNewMissionTarget;
        public readonly bool CanAttackConfirmedTarget;
        public readonly bool CanCoordinatePeers;
        public readonly int LostLinkTicks;

        public RuleRobotBehaviorDecision(
            RuleRobotBehaviorState state,
            RuleAutonomyLevel effectiveAutonomy,
            RuleLinkState effectiveLink,
            RobotFallbackMode fallback,
            int moveSpeedPermille,
            bool allowComplexManeuver,
            bool canEnterUnknownArea,
            bool canAcquireNewMissionTarget,
            bool canAttackConfirmedTarget,
            bool canCoordinatePeers,
            int lostLinkTicks)
        {
            State = state;
            EffectiveAutonomy = effectiveAutonomy;
            EffectiveLink = effectiveLink;
            Fallback = fallback;
            MoveSpeedPermille = moveSpeedPermille;
            AllowComplexManeuver = allowComplexManeuver;
            CanEnterUnknownArea = canEnterUnknownArea;
            CanAcquireNewMissionTarget = canAcquireNewMissionTarget;
            CanAttackConfirmedTarget = canAttackConfirmedTarget;
            CanCoordinatePeers = canCoordinatePeers;
            LostLinkTicks = lostLinkTicks;
        }
    }

    public static class RuleRobotBehaviorRules
    {
        public const int LowHealthPermille = 350;
        public const int HighSystemStress = 80;
        public const int LowNavigationQualityPermille = 400;
        public const int LostCommsQualityPermille = 300;
        public const int DegradedCommsQualityPermille = 600;
        public const int A1ReturnDelayTicks = 30 * DeterministicUpdateBudget.TickRate;

        public static RuleRobotBehaviorDecision Step(in RuleRobotBehaviorInput input)
        {
            RuleLinkState link = ResolveLink(input.Link, input.CommsQualityPermille);
            RobotRuntimeDecision degradation = RoboticsDegradationRules.Evaluate(
                input.ConfiguredAutonomy,
                link,
                input.ComputeGranted,
                input.ComputeRequested,
                input.EW);

            RuleAutonomyLevel autonomy = degradation.EffectiveAutonomy;
            if (input.SystemStress >= HighSystemStress && autonomy > RuleAutonomyLevel.A1)
                autonomy = (RuleAutonomyLevel)((int)autonomy - 1);

            int lostTicks = degradation.EffectiveLink == RuleLinkState.Lost
                ? input.LostLinkTicks == int.MaxValue ? int.MaxValue : input.LostLinkTicks + 1
                : 0;
            bool lowNavigation = input.NavigationQualityPermille < LowNavigationQualityPermille;
            int speedPermille = lowNavigation ? 700 : 1000;
            bool allowComplex = !lowNavigation;
            bool lost = degradation.EffectiveLink == RuleLinkState.Lost;
            bool canEnterUnknown = !lost && autonomy >= RuleAutonomyLevel.A2;
            bool canAcquireNewMissionTarget = !lost && autonomy >= RuleAutonomyLevel.A3;
            bool canAttackConfirmed = !lost || autonomy >= RuleAutonomyLevel.A3;
            bool canCoordinatePeers = autonomy == RuleAutonomyLevel.A4;

            RuleRobotBehaviorState state = ResolveState(input, autonomy, degradation.EffectiveLink, lostTicks, canAttackConfirmed);
            return new RuleRobotBehaviorDecision(
                state,
                autonomy,
                degradation.EffectiveLink,
                degradation.Fallback,
                speedPermille,
                allowComplex,
                canEnterUnknown,
                canAcquireNewMissionTarget,
                canAttackConfirmed,
                canCoordinatePeers,
                lostTicks);
        }

        private static RuleLinkState ResolveLink(RuleLinkState configured, int commsQualityPermille)
        {
            if (commsQualityPermille < LostCommsQualityPermille)
                return RuleLinkState.Lost;
            if (commsQualityPermille < DegradedCommsQualityPermille && configured == RuleLinkState.Connected)
                return RuleLinkState.Degraded;
            return configured;
        }

        private static RuleRobotBehaviorState ResolveState(
            in RuleRobotBehaviorInput input,
            RuleAutonomyLevel autonomy,
            RuleLinkState link,
            int lostTicks,
            bool canAttackConfirmed)
        {
            if (input.HealthPermille <= 0)
                return RuleRobotBehaviorState.Disabled;

            if (input.HealthPermille < LowHealthPermille && !input.HoldAtAllCosts)
            {
                if (input.CurrentState == RuleRobotBehaviorState.Damaged || input.CurrentState == RuleRobotBehaviorState.Return)
                    return RuleRobotBehaviorState.Return;
                return RuleRobotBehaviorState.Damaged;
            }

            if (link == RuleLinkState.Lost && autonomy == RuleAutonomyLevel.A1)
                return lostTicks >= A1ReturnDelayTicks ? RuleRobotBehaviorState.Return : RuleRobotBehaviorState.Idle;

            if (input.UnderImmediateThreat && !input.HoldAtAllCosts && autonomy >= RuleAutonomyLevel.A2)
                return RuleRobotBehaviorState.Evade;

            if (link == RuleLinkState.Lost)
            {
                if (input.Mission == RuleRobotMission.Return)
                    return RuleRobotBehaviorState.Return;
                if (input.Mission == RuleRobotMission.Attack && input.TargetConfirmed && canAttackConfirmed)
                    return RuleRobotBehaviorState.Attack;
                if (input.Mission == RuleRobotMission.Defend && input.TargetConfirmed && canAttackConfirmed)
                    return RuleRobotBehaviorState.Attack;
                if (input.Mission == RuleRobotMission.Follow)
                    return RuleRobotBehaviorState.Follow;
                return RuleRobotBehaviorState.Search;
            }

            return input.Mission switch
            {
                RuleRobotMission.Idle => RuleRobotBehaviorState.Idle,
                RuleRobotMission.Follow => RuleRobotBehaviorState.Follow,
                RuleRobotMission.Patrol => RuleRobotBehaviorState.Search,
                RuleRobotMission.Defend => input.TargetConfirmed ? RuleRobotBehaviorState.Attack : RuleRobotBehaviorState.Search,
                RuleRobotMission.Attack => input.TargetConfirmed ? RuleRobotBehaviorState.Attack : RuleRobotBehaviorState.Search,
                RuleRobotMission.Return => RuleRobotBehaviorState.Return,
                _ => RuleRobotBehaviorState.Idle
            };
        }
    }
}
