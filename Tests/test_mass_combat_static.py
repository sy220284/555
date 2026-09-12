import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RULES = ROOT / "Packages/com.modernra.rules/Runtime/MassCombatRules.cs"
GATE = ROOT / "Tools/ModernRA.SimRunner/MassCombatGateChecks.cs"

class MassCombatStaticTests(unittest.TestCase):
    def test_mass_combat_uses_spatial_target_acquisition(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("DeterministicSpatialHash", text)
        self.assertIn("FindNearestEnemy", text)
        self.assertNotIn("for (int j = 0; j < world.Units.Length", text)

    def test_damage_is_accumulated_before_apply(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("pendingDamage[targetIndex] += Damage", text)
        self.assertIn("Array.Clear(pendingDamage", text)
        self.assertLess(text.index("pendingDamage[targetIndex] += Damage"), text.index("target.Health -= pendingDamage[i]"))

    def test_gate_requires_repeat_hash_and_broadphase_reduction(self):
        text = GATE.read_text(encoding="utf-8")
        self.assertIn("repetitions = 4", text)
        self.assertIn("hash == expectedHash", text)
        self.assertIn("NaiveCandidateVisits / 5", text)

if __name__ == "__main__":
    unittest.main()
