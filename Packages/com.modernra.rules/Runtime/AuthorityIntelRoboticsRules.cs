using System;

namespace ModernRA.Rules
{
    public enum RuleIntelLevel : byte { Unknown, Anomaly, Detected, Classified, Confirmed, Tracked }
    public enum RuleReplicationDetail : byte { Hidden, Contact, Class, Confirmed, Full }
    public enum RuleAIAuthorityLevel : byte { None, Tactical, BattleGroup, Theater, Deputy }
    [Flags] public enum RuleAIForbiddenAction : ushort { None=0, StrategicWeapon=1, StrategicReserve=2, ChangeMainTech=4, DemolishCore=8, FullRetreat=16 }
    public enum RuleAIAction : byte { Move, Attack, StrategicWeapon, StrategicReserve, ChangeMainTech, DemolishCore, FullRetreat }
    public enum RuleAutonomyLevel : byte { A1=1, A2=2, A3=3, A4=4 }
    public enum RuleLinkState : byte { Connected, Degraded, Lost }
    public enum RuleEWLevel : byte { Normal, LightInterference, HeavyInterference, Blackout }
    public enum RobotFallbackMode : byte { SafeStopOrReturn, LocalNavigate, ContinueMission, LocalCoordinate }

    public readonly struct VisibleEntityState
    {
        public readonly int ContactId;
        public readonly RuleReplicationDetail Detail;
        public readonly int X;
        public readonly int Y;
        public readonly int UnitClass;
        public readonly int Health;
        public readonly bool Friendly;

        public VisibleEntityState(int contactId, RuleReplicationDetail detail, int x, int y, int unitClass, int health, bool friendly = false)
        {
            ContactId = contactId;
            Detail = detail;
            X = x;
            Y = y;
            UnitClass = unitClass;
            Health = health;
            Friendly = friendly;
        }
    }

    public static class IntelReplicationRules
    {
        public static bool TryBuildVisibleState(int contactId, int x, int y, int unitClass, int health, RuleIntelLevel intel, out VisibleEntityState state, bool friendly = false)
        {
            switch (intel)
            {
                case RuleIntelLevel.Unknown:
                case RuleIntelLevel.Anomaly:
                    state = default;
                    return false;
                case RuleIntelLevel.Detected:
                    state = new VisibleEntityState(contactId, RuleReplicationDetail.Contact, Quantize(x, 500), Quantize(y, 500), -1, -1, friendly);
                    return true;
                case RuleIntelLevel.Classified:
                    state = new VisibleEntityState(contactId, RuleReplicationDetail.Class, Quantize(x, 250), Quantize(y, 250), unitClass, -1, friendly);
                    return true;
                case RuleIntelLevel.Confirmed:
                    state = new VisibleEntityState(contactId, RuleReplicationDetail.Confirmed, Quantize(x, 50), Quantize(y, 50), unitClass, Quantize(Math.Max(0, health), 250), friendly);
                    return true;
                case RuleIntelLevel.Tracked:
                    state = new VisibleEntityState(contactId, RuleReplicationDetail.Full, x, y, unitClass, Math.Max(0, health), friendly);
                    return true;
                default:
                    state = default;
                    return false;
            }
        }

        private static int Quantize(int value, int step)
        {
            if (step <= 1) return value;
            int remainder = value % step;
            return value - remainder;
        }
    }

    public readonly struct RuleAIAuthority
    {
        public readonly RuleAIAuthorityLevel Level;
        public readonly int OwnerPlayerId;
        public readonly int RegionId;
        public readonly RuleAIForbiddenAction Forbidden;
        public readonly uint PlayerOverrideGeneration;

        public RuleAIAuthority(RuleAIAuthorityLevel level, int ownerPlayerId, int regionId, RuleAIForbiddenAction forbidden, uint playerOverrideGeneration)
        {
            Level = level;
            OwnerPlayerId = ownerPlayerId;
            RegionId = regionId;
            Forbidden = forbidden;
            PlayerOverrideGeneration = playerOverrideGeneration;
        }
    }

    public readonly struct RuleAIOrder
    {
        public readonly int PlayerId;
        public readonly int RegionId;
        public readonly RuleAIAction Action;
        public readonly RuleAIAuthorityLevel RequiredLevel;
        public readonly uint Generation;

        public RuleAIOrder(int playerId, int regionId, RuleAIAction action, RuleAIAuthorityLevel requiredLevel, uint generation)
        {
            PlayerId = playerId;
            RegionId = regionId;
            Action = action;
            RequiredLevel = requiredLevel;
            Generation = generation;
        }
    }

    public static class AIAuthorityRules
    {
        public static bool IsAllowed(in RuleAIAuthority authority, in RuleAIOrder order)
        {
            if (authority.Level < order.RequiredLevel) return false;
            if (authority.OwnerPlayerId != order.PlayerId) return false;
            if (authority.RegionId != order.RegionId) return false;
            if (order.Generation < authority.PlayerOverrideGeneration) return false;
            return (authority.Forbidden & ToForbiddenFlag(order.Action)) == 0;
        }

        private static RuleAIForbiddenAction ToForbiddenFlag(RuleAIAction action)
        {
            return action switch
            {
                RuleAIAction.StrategicWeapon => RuleAIForbiddenAction.StrategicWeapon,
                RuleAIAction.StrategicReserve => RuleAIForbiddenAction.StrategicReserve,
                RuleAIAction.ChangeMainTech => RuleAIForbiddenAction.ChangeMainTech,
                RuleAIAction.DemolishCore => RuleAIForbiddenAction.DemolishCore,
                RuleAIAction.FullRetreat => RuleAIForbiddenAction.FullRetreat,
                _ => RuleAIForbiddenAction.None
            };
        }
    }

    public readonly struct RobotRuntimeDecision
    {
        public readonly RuleAutonomyLevel EffectiveAutonomy;
        public readonly RuleLinkState EffectiveLink;
        public readonly RobotFallbackMode Fallback;

        public RobotRuntimeDecision(RuleAutonomyLevel effectiveAutonomy, RuleLinkState effectiveLink, RobotFallbackMode fallback)
        {
            EffectiveAutonomy = effectiveAutonomy;
            EffectiveLink = effectiveLink;
            Fallback = fallback;
        }
    }

    public static class RoboticsDegradationRules
    {
        public static RobotRuntimeDecision Evaluate(RuleAutonomyLevel configured, RuleLinkState link, int computeGranted, int computeRequested, RuleEWLevel ew)
        {
            RuleLinkState effectiveLink = ApplyEW(link, ew);
            int level = (int)configured;
            if (computeRequested > 0)
            {
                if (computeGranted * 2 < computeRequested) level -= 2;
                else if (computeGranted < computeRequested) level -= 1;
            }
            if (effectiveLink == RuleLinkState.Degraded && level > 1) level -= 1;
            if (level < 1) level = 1;
            if (level > 4) level = 4;
            var effective = (RuleAutonomyLevel)level;
            return new RobotRuntimeDecision(effective, effectiveLink, SelectFallback(effective, effectiveLink));
        }

        private static RuleLinkState ApplyEW(RuleLinkState link, RuleEWLevel ew)
        {
            if (ew == RuleEWLevel.Blackout) return RuleLinkState.Lost;
            if (ew == RuleEWLevel.HeavyInterference && link == RuleLinkState.Connected) return RuleLinkState.Degraded;
            return link;
        }

        private static RobotFallbackMode SelectFallback(RuleAutonomyLevel autonomy, RuleLinkState link)
        {
            if (link != RuleLinkState.Lost)
                return autonomy == RuleAutonomyLevel.A4 ? RobotFallbackMode.LocalCoordinate : RobotFallbackMode.ContinueMission;
            return autonomy switch
            {
                RuleAutonomyLevel.A1 => RobotFallbackMode.SafeStopOrReturn,
                RuleAutonomyLevel.A2 => RobotFallbackMode.LocalNavigate,
                RuleAutonomyLevel.A3 => RobotFallbackMode.ContinueMission,
                RuleAutonomyLevel.A4 => RobotFallbackMode.LocalCoordinate,
                _ => RobotFallbackMode.SafeStopOrReturn
            };
        }
    }
}
