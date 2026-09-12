using ModernRA.Rules;

namespace ModernRA.Robotics
{
    public static class RoboticsPolicy
    {
        public static RobotRuntimeDecision Evaluate(in RoboticsState state, in ComputeAllocation compute, RuleEWLevel ew)
        {
            return RoboticsDegradationRules.Evaluate(
                (RuleAutonomyLevel)state.Autonomy,
                (RuleLinkState)state.Link,
                compute.Granted,
                compute.Requested,
                ew);
        }
    }
}
