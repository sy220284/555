import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RULES = ROOT / "Packages/com.modernra.rules/Runtime/PrototypePlayerCommands.cs"
RUNNER = ROOT / "Tools/ModernRA.CommandRunner/Program.cs"
REPLAY = ROOT / "Tools/ModernRA.CommandRunner/PlayerCommandReplayFile.cs"
PROJECT = ROOT / "Tools/ModernRA.CommandRunner/ModernRA.CommandRunner.csproj"
WORKFLOW = ROOT / ".github/workflows/simulation-gates.yml"


class PlayerCommandStreamStaticTests(unittest.TestCase):
    def test_timeline_canonicalizes_and_rejects_duplicate_command_keys(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("_commands.Sort(CompareCommands)", text)
        self.assertIn("RejectDuplicateKeys", text)
        self.assertIn("previous.Tick == current.Tick", text)
        self.assertIn("previous.PlayerId == current.PlayerId", text)
        self.assertIn("previous.Sequence == current.Sequence", text)

    def test_commands_apply_before_authoritative_step(self):
        text = RULES.read_text(encoding="utf-8")
        method = text[text.index("public static void StepWithCommands"):]
        self.assertLess(method.index("timeline.ApplyForNextTick(world)"), method.index("AnnihilationPrototype.Step(world)"))

    def test_rules_command_stream_stays_pure_and_deterministic(self):
        text = RULES.read_text(encoding="utf-8")
        for forbidden in ("UnityEngine", "Unity.Entities", "DateTime", "Random(", "Guid.NewGuid", "Task.Run"):
            self.assertNotIn(forbidden, text)
        self.assertIn("ComputeCanonicalHash", text)

    def test_runner_proves_repetition_order_independence_and_authoritative_effect(self):
        text = RUNNER.read_text(encoding="utf-8")
        self.assertIn("Repetitions = 8", text)
        self.assertIn("Array.Reverse(reversed)", text)
        self.assertIn("command timeline hash depends on insertion order", text)
        self.assertIn("command stream did not affect authoritative match result", text)
        self.assertIn("DeterministicCommandTimeline.StepWithCommands", text)

    def test_gate_scenario_keeps_plan_swap_and_adds_tactical_rally(self):
        text = RUNNER.read_text(encoding="utf-8")
        create = text[text.index("private static PrototypePlayerCommand[] CreateCommands()"):text.index("private static void ReplayRoundTrip")]
        self.assertEqual(3, create.count("new PrototypePlayerCommand("))
        self.assertIn("1, 10, 1, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Economy", create)
        self.assertIn("1, 10, 2, PrototypePlayerCommandKind.SetPlan, (int)PrototypeAnnihilationPlan.Aggressive", create)
        self.assertIn("600, 20, 2, PrototypePlayerCommandKind.SetTeamRallyWaypoint, 0", create)
        self.assertIn("rally waypoint command did not affect authoritative match result", text)
        self.assertIn("tactical_rally=true", text)

    def test_tactical_rally_command_moves_alive_team_units_via_authoritative_corridor(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("SetTeamRallyWaypoint = 2", text)
        self.assertIn("rally waypoint is outside the shared corridor", text)
        self.assertIn("unit.CorridorCursor = command.IntValue", text)
        self.assertIn("if (unit.Alive)", text)
        self.assertNotIn("UnityEngine", text)

    def test_command_replay_binds_content_and_round_trips_authoritative_result(self):
        replay = REPLAY.read_text(encoding="utf-8")
        runner = RUNNER.read_text(encoding="utf-8")
        self.assertIn("GrayRangeGeneratedData.SourceMapSha256", replay)
        self.assertIn("RULESET_ANNIHILATION_STANDARD", replay)
        self.assertIn("command replay canonical hash mismatch", replay)
        self.assertIn("command replay scenario id mismatch", replay)
        self.assertIn("ValidateScenario", replay)
        self.assertIn("ValidateOutcome", replay)
        self.assertIn("ReplayRoundTrip(config, map.MapId, canonical, commanded)", runner)
        self.assertNotIn("GrayRangeGeneratedData.MapId", runner)
        self.assertIn("PlayerCommandReplayFile.ValidateScenario", runner)
        self.assertIn("PlayerCommandReplayFile.ValidateOutcome", runner)
        self.assertIn("persisted command replay changed authoritative result", runner)
        self.assertIn("command replay serialization is not byte-stable", runner)

    def test_command_runner_compiles_all_rule_sources_and_is_in_simulation_gate(self):
        project = PROJECT.read_text(encoding="utf-8")
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("com.modernra.rules/Runtime/*.cs", project)
        self.assertIn("ModernRA.CommandRunner", workflow)
        self.assertIn("Run deterministic player command stream gate", workflow)


if __name__ == "__main__":
    unittest.main()
