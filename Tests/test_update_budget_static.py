import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RULES = ROOT / "Packages/com.modernra.rules/Runtime/UpdateBudgetRules.cs"
GATE = ROOT / "Tools/ModernRA.SimRunner/UpdateBudgetGateChecks.cs"


class UpdateBudgetStaticTests(unittest.TestCase):
    def test_frequencies_match_frozen_ai_budget(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("CombatAuthority => 1", text)
        self.assertIn("HighRateSensor => 2", text)
        self.assertIn("BattleGroupAI => 3", text)
        self.assertIn("TheaterAI => 6", text)
        self.assertIn("StrategicAI => 15", text)

    def test_scheduler_staggers_by_stable_id(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("PositiveModulo(stableId, period)", text)
        self.assertIn("tick % period == phase", text)

    def test_runtime_gate_proves_aggregate_and_peak_reduction(self):
        text = GATE.read_text(encoding="utf-8")
        self.assertIn("entities = 3000", text)
        self.assertIn("fullRateEverything / 2", text)
        self.assertIn("battleGroupPeak <= 1000", text)
        self.assertIn("strategicPeak <= 200", text)


if __name__ == "__main__":
    unittest.main()
