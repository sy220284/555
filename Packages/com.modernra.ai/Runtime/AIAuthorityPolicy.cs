using ModernRA.Rules;

namespace ModernRA.AI
{
    public static class AIAuthorityPolicy
    {
        public static bool IsAllowed(in AIAuthority authority, AIAuthorityLevel required, int playerId, int regionId, AIForbiddenAction action, uint orderGeneration, uint playerOverrideGeneration)
        {
            var rulesAuthority = new RuleAIAuthority(
                (RuleAIAuthorityLevel)authority.Level,
                authority.OwnerPlayerId,
                authority.RegionId,
                (RuleAIForbiddenAction)authority.Forbidden,
                playerOverrideGeneration);
            var order = new RuleAIOrder(playerId, regionId, ToRuleAction(action), (RuleAIAuthorityLevel)required, orderGeneration);
            return AIAuthorityRules.IsAllowed(rulesAuthority, order);
        }

        private static RuleAIAction ToRuleAction(AIForbiddenAction action)
        {
            if ((action & AIForbiddenAction.StrategicWeapon) != 0) return RuleAIAction.StrategicWeapon;
            if ((action & AIForbiddenAction.StrategicReserve) != 0) return RuleAIAction.StrategicReserve;
            if ((action & AIForbiddenAction.ChangeMainTech) != 0) return RuleAIAction.ChangeMainTech;
            if ((action & AIForbiddenAction.DemolishCore) != 0) return RuleAIAction.DemolishCore;
            if ((action & AIForbiddenAction.FullRetreat) != 0) return RuleAIAction.FullRetreat;
            return RuleAIAction.Move;
        }
    }
}
