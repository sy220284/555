using System;
using ModernRA.Rules;

internal static class RobotBehaviorGateChecks
{
    public static void Run()
    {
        VerifyElectronicWarfare();
        VerifyRobotBehavior();
        Console.WriteLine("robot_ew_gate=passed jammer_order=stable a1_return=900ticks a3_confirmed_attack=allowed navigation_low=700permille");
    }

    private static void VerifyElectronicWarfare()
    {
        var a = new[]
        {
            new RuleEWInterferenceSource(20, 300, 1000),
            new RuleEWInterferenceSource(10, 150, 1000)
        };
        var b = new[] { a[1], a[0] };
        int resistance = RuleElectronicWarfareRules.DefaultResistancePermille(RuleAutonomyLevel.A3);
        int qa = RuleElectronicWarfareRules.ApplyInterference(1000, resistance, a);
        int qb = RuleElectronicWarfareRules.ApplyInterference(1000, resistance, b);
        Check(qa == qb, "EW quality depends on jammer input ordering");
        Check(qa >= RuleElectronicWarfareRules.MinimumQualityPermille && qa < 1000, "EW quality clamp/decay invalid");
        Check(RuleElectronicWarfareRules.ApplyInterference(1000, 1000, a) == 1000, "fiber-controlled resistance did not fully resist wireless jamming");
    }

    private static void VerifyRobotBehavior()
    {
        var a1Hold = Step(RuleAutonomyLevel.A1, RuleLinkState.Lost, RuleEWLevel.Normal, 100, 100, 1000, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Attack, 0);
        Check(a1Hold.State == RuleRobotBehaviorState.Idle, "A1 lost link did not enter safe hold");
        Check(a1Hold.LostLinkTicks == 1, "A1 lost-link tick did not advance");

        var a1Return = Step(RuleAutonomyLevel.A1, RuleLinkState.Lost, RuleEWLevel.Normal, 100, 100, 1000, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Idle, RuleRobotBehaviorRules.A1ReturnDelayTicks - 1);
        Check(a1Return.State == RuleRobotBehaviorState.Return, "A1 did not return after 30 seconds disconnected");

        var a3 = Step(RuleAutonomyLevel.A3, RuleLinkState.Lost, RuleEWLevel.Normal, 100, 100, 1000, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Attack, 10);
        Check(a3.State == RuleRobotBehaviorState.Attack && a3.CanAttackConfirmedTarget, "A3 did not continue confirmed target while disconnected");
        Check(!a3.CanAcquireNewMissionTarget && !a3.CanEnterUnknownArea, "disconnected A3 gained forbidden retask/unknown-area authority");

        var stressed = Step(RuleAutonomyLevel.A4, RuleLinkState.Connected, RuleEWLevel.Normal, 100, 100, 1000, 80, 1000, 1000,
            RuleRobotMission.Follow, false, false, false, RuleRobotBehaviorState.Follow, 0);
        Check(stressed.EffectiveAutonomy == RuleAutonomyLevel.A3, "system stress did not reduce autonomy by one level");
        Check(!stressed.CanCoordinatePeers, "stress-degraded A4 retained A4 peer coordination");

        var lowNav = Step(RuleAutonomyLevel.A4, RuleLinkState.Connected, RuleEWLevel.Normal, 100, 100, 1000, 0, 399, 1000,
            RuleRobotMission.Patrol, false, false, false, RuleRobotBehaviorState.Search, 0);
        Check(lowNav.MoveSpeedPermille == 700 && !lowNav.AllowComplexManeuver, "low navigation quality did not reduce speed/complex maneuver");

        var damaged = Step(RuleAutonomyLevel.A4, RuleLinkState.Connected, RuleEWLevel.Normal, 100, 100, 349, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Attack, 0);
        Check(damaged.State == RuleRobotBehaviorState.Damaged, "low-health robot did not enter damaged state");
        var returning = Step(RuleAutonomyLevel.A4, RuleLinkState.Connected, RuleEWLevel.Normal, 100, 100, 349, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Damaged, 0);
        Check(returning.State == RuleRobotBehaviorState.Return, "damaged robot did not transition to return");

        var blackout = Step(RuleAutonomyLevel.A4, RuleLinkState.Connected, RuleEWLevel.Blackout, 100, 100, 1000, 0, 1000, 1000,
            RuleRobotMission.Attack, true, false, false, RuleRobotBehaviorState.Attack, 0);
        Check(blackout.EffectiveLink == RuleLinkState.Lost, "EW blackout did not sever robot link");
    }

    private static RuleRobotBehaviorDecision Step(
        RuleAutonomyLevel autonomy, RuleLinkState link, RuleEWLevel ew, int computeGranted, int computeRequested,
        int hp, int stress, int nav, int comms, RuleRobotMission mission, bool targetConfirmed,
        bool threat, bool hold, RuleRobotBehaviorState current, int lostTicks)
    {
        var input = new RuleRobotBehaviorInput(current, mission, autonomy, link, ew, computeGranted, computeRequested,
            hp, stress, nav, comms, targetConfirmed, threat, hold, lostTicks);
        return RuleRobotBehaviorRules.Step(input);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
