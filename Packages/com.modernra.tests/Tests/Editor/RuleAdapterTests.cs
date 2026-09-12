using ModernRA.AI;
using ModernRA.IntelEW;
using ModernRA.Network;
using ModernRA.Robotics;
using ModernRA.Rules;
using NUnit.Framework;

namespace ModernRA.Tests
{
    public sealed class RuleAdapterTests
    {
        [Test]
        public void AIAdapterRejectsForbiddenAction()
        {
            var authority = new AIAuthority
            {
                Level = AIAuthorityLevel.BattleGroup,
                OwnerPlayerId = 1,
                RegionId = 7,
                Forbidden = AIForbiddenAction.StrategicWeapon
            };
            Assert.That(AIAuthorityPolicy.IsAllowed(authority, AIAuthorityLevel.Tactical, 1, 7, AIForbiddenAction.StrategicWeapon, 3, 3), Is.False);
            Assert.That(AIAuthorityPolicy.IsAllowed(authority, AIAuthorityLevel.Tactical, 1, 7, AIForbiddenAction.None, 3, 3), Is.True);
        }

        [Test]
        public void IntelAndNetworkAdaptersKeepConfirmedDistinct()
        {
            Assert.That(IntelReplicationPolicy.TryBuildVisibleState(1, 1234, 876, 5, 830, IntelLevel.Confirmed, out VisibleEntityState visible), Is.True);
            Assert.That(visible.Detail, Is.EqualTo(RuleReplicationDetail.Confirmed));
            Assert.That(NetworkVisibilityPolicy.FromIntel(RuleIntelLevel.Confirmed), Is.EqualTo(ReplicationVisibility.Confirmed));
            Assert.That(NetworkVisibilityPolicy.FromIntel(RuleIntelLevel.Tracked), Is.EqualTo(ReplicationVisibility.Full));
        }

        [Test]
        public void RoboticsAdapterPreservesA4LostLinkCoordination()
        {
            var state = new RoboticsState { Autonomy = AutonomyLevel.A4, Link = LinkState.Lost, ComputeDemand = 100 };
            var compute = new ComputeAllocation { Requested = 100, Granted = 100 };
            RobotRuntimeDecision decision = RoboticsPolicy.Evaluate(state, compute, RuleEWLevel.Normal);
            Assert.That(decision.EffectiveAutonomy, Is.EqualTo(RuleAutonomyLevel.A4));
            Assert.That(decision.Fallback, Is.EqualTo(RobotFallbackMode.LocalCoordinate));
        }
    }
}
