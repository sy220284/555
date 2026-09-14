import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = ROOT / "Tools/ModernRA.AnnihilationRunner"
REPLAY = ROOT / "Tools/ModernRA.CommandRunner/PlayerCommandReplayFile.cs"


class AnnihilationReplayFileStaticTests(unittest.TestCase):
    def test_file_binds_scenario_map_hash_and_ruleset(self):
        text = REPLAY.read_text(encoding="utf-8")
        self.assertIn("ScenarioId", text)
        self.assertIn("MapSourceSha256", text)
        self.assertIn('"RULESET_ANNIHILATION_STANDARD"', text)
        self.assertIn("FormatVersion = 3", text)

    def test_file_round_trip_is_exercised_by_gate(self):
        text = (RUNNER / "AnnihilationGateChecks.cs").read_text(encoding="utf-8")
        self.assertIn("PlayerCommandReplayFile.Write", text)
        self.assertIn("PlayerCommandReplayFile.Read", text)
        self.assertIn("unified replay file round-trip changed canonical content", text)
        self.assertIn("migration_v1_v3=true", text)
        self.assertIn("replay_file_sha256", text)
        self.assertIn("equivalent annihilation replay creation is not byte-stable", text)
        self.assertIn("DateTimeOffset.UnixEpoch", text)

    def test_replay_file_contains_no_gameplay_rules(self):
        text = REPLAY.read_text(encoding="utf-8")
        for forbidden in ("TankCost", "RawDamage", "MiningPerTick", "WeaponRange"):
            self.assertNotIn(forbidden, text)

    def test_reader_rejects_content_and_format_mismatch(self):
        text = REPLAY.read_text(encoding="utf-8")
        self.assertIn("unsupported command replay schema", text)
        self.assertIn("command replay content hash mismatch", text)
        self.assertIn("final keyframe does not match outcome", text)


if __name__ == "__main__":
    unittest.main()
