import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = ROOT / "Tools/ModernRA.AnnihilationRunner"


class AnnihilationReplayFileStaticTests(unittest.TestCase):
    def test_file_binds_scenario_map_hash_and_ruleset(self):
        text = (RUNNER / "AnnihilationReplayFile.cs").read_text(encoding="utf-8")
        self.assertIn('"scenario_id"', text)
        self.assertIn('"source_map_sha256"', text)
        self.assertIn('"RULESET_ANNIHILATION_STANDARD"', text)
        self.assertIn('SchemaVersion = 1', text)

    def test_file_round_trip_is_exercised_by_gate(self):
        text = (RUNNER / "AnnihilationGateChecks.cs").read_text(encoding="utf-8")
        self.assertIn("File.WriteAllText", text)
        self.assertIn("File.ReadAllText", text)
        self.assertIn("AnnihilationReplayFile.Read", text)
        self.assertIn("replay file round-trip changed canonical content", text)
        self.assertIn("replay_file_sha256", text)

    def test_replay_file_contains_no_gameplay_rules(self):
        text = (RUNNER / "AnnihilationReplayFile.cs").read_text(encoding="utf-8")
        for forbidden in ("TankCost", "RawDamage", "MiningPerTick", "WeaponRange"):
            self.assertNotIn(forbidden, text)

    def test_reader_rejects_content_and_format_mismatch(self):
        text = (RUNNER / "AnnihilationReplayFile.cs").read_text(encoding="utf-8")
        self.assertIn("unsupported replay schema", text)
        self.assertIn("source map hash does not match current content", text)
        self.assertIn("final checkpoint does not match replay result", text)


if __name__ == "__main__":
    unittest.main()
