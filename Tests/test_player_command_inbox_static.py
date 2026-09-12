import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
INBOX = ROOT / "Packages/com.modernra.rules/Runtime/PrototypeCommandInbox.cs"
RUNNER = ROOT / "Tools/ModernRA.CommandInboxRunner/Program.cs"
PROJECT = ROOT / "Tools/ModernRA.CommandInboxRunner/ModernRA.CommandInboxRunner.csproj"
WORKFLOW = ROOT / ".github/workflows/simulation-gates.yml"


class PlayerCommandInboxStaticTests(unittest.TestCase):
    def test_inbox_uses_sliding_replay_window_and_future_tick_window(self):
        text = INBOX.read_text(encoding="utf-8")
        self.assertIn("ReplayWindowBits = 64", text)
        self.assertIn("TickNotInFuture", text)
        self.assertIn("TickTooFarFuture", text)
        self.assertIn("DuplicateSequence", text)
        self.assertIn("SequenceTooOld", text)
        self.assertIn("SeenMask", text)

    def test_invalid_or_rejected_commands_do_not_enter_pending_queue(self):
        text = INBOX.read_text(encoding="utf-8")
        append_index = text.index("_pending.Add(command)")
        self.assertLess(text.index("IsStructurallyValid(command)"), append_index)
        self.assertLess(text.index("CheckAndAdvanceReplayWindow"), append_index)

    def test_drain_is_canonical_by_player_and_sequence(self):
        text = INBOX.read_text(encoding="utf-8")
        self.assertIn("ready.Sort(CompareSameTickCommands)", text)
        self.assertIn("left.PlayerId.CompareTo(right.PlayerId)", text)
        self.assertIn("left.Sequence.CompareTo(right.Sequence)", text)
        self.assertIn("command inbox fell behind authoritative tick", text)

    def test_runner_checks_reordered_packets_and_rejection_paths(self):
        text = RUNNER.read_text(encoding="utf-8")
        self.assertIn("admitted command order depends on packet arrival order", text)
        self.assertIn("PrototypeCommandAdmissionResult.DuplicateSequence", text)
        self.assertIn("PrototypeCommandAdmissionResult.SequenceTooOld", text)
        self.assertIn("PrototypeCommandAdmissionResult.TickTooFarFuture", text)
        self.assertIn("PrototypeCommandAdmissionResult.InvalidCommand", text)
        self.assertIn("PrototypeCommandAdmissionResult.InvalidTarget", text)
        self.assertIn("PrototypePlayerCommandKind.HoldUnit", text)
        self.assertIn("PrototypePlayerCommandKind.SetUnitWaypoint", text)
        self.assertIn("PrototypePlayerCommandKind.AssignUnitToControlGroup", text)
        self.assertIn("PrototypePlayerCommandKind.SetControlGroupWaypoint", text)
        self.assertIn("stale AI order survived", text)

    def test_runner_compiles_all_rules_and_is_wired_to_simulation_gate(self):
        project = PROJECT.read_text(encoding="utf-8")
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("com.modernra.rules/Runtime/*.cs", project)
        self.assertIn("ModernRA.CommandInboxRunner", workflow)
        self.assertIn("Run live player command admission gate", workflow)


if __name__ == "__main__":
    unittest.main()
