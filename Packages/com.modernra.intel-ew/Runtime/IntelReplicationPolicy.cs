using ModernRA.Rules;

namespace ModernRA.IntelEW
{
    public static class IntelReplicationPolicy
    {
        public static bool TryBuildVisibleState(int contactId, int x, int y, int unitClass, int health, IntelLevel level, out VisibleEntityState state)
        {
            return IntelReplicationRules.TryBuildVisibleState(contactId, x, y, unitClass, health, (RuleIntelLevel)level, out state);
        }
    }
}
