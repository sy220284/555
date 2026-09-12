import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = ROOT / "Tools/ModernRA.AnnihilationRunner"


class AnnihilationReplayStaticTests(unittest.TestCase):
    def test_replay_reuses_authoritative_rule_step_and_hash(self):
        text = (RUNNER / "AnnihilationReplayVerifier.cs").read_text(encoding="utf-8")
        self.assertIn("AnnihilationPrototype.Step(world);", text)
        self.assertIn("AnnihilationPrototype.ComputeStateHash(world)", text)
        self.assertIn("replay divergence", text)
        self.assertNotIn("TankCost", text)
        self.assertNotIn("RawDamage", text)
        self.assertNotIn("MiningPerTick", text)

    def test_gate_records_and_verifies_replay(self):
        text = (RUNNER / "AnnihilationGateChecks.cs").read_text(encoding="utf-8")
        self.assertIn("AnnihilationReplayVerifier.Record", text)
        self.assertIn("AnnihilationReplayVerifier.Verify", text)
        self.assertIn("replay_final_hash", text)
        self.assertIn("replay_checkpoints", text)

    def test_checkpoint_tape_keeps_final_result_contract(self):
        text = (RUNNER / "AnnihilationReplayVerifier.cs").read_text(encoding="utf-8")
        for field in ("WinnerTeamId", "ResolvedTick", "FinalStateHash", "Checkpoints"):
            self.assertIn(field, text)
        self.assertIn("did not consume every checkpoint", text)
        self.assertIn("final result diverged", text)


if __name__ == "__main__":
    unittest.main()
