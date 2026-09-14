import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RULES = ROOT / "Packages/com.modernra.rules/Runtime/SpatialNavigationRules.cs"
ANNIHILATION = ROOT / "Packages/com.modernra.rules/Runtime/AnnihilationPrototype.cs"
GATE = ROOT / "Tools/ModernRA.SimRunner/SpatialNavigationGateChecks.cs"
ANNIHILATION_GATE = ROOT / "Tools/ModernRA.AnnihilationRunner/AnnihilationGateChecks.cs"


class SpatialNavigationStaticTests(unittest.TestCase):
    def test_spatial_hash_is_deterministic_and_has_negative_coordinate_flooring(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("FindNearestEnemy", text)
        self.assertIn("candidate.EntityId < nearest.EntityId", text)
        self.assertIn("FloorDiv", text)
        self.assertIn("value < 0", text)

    def test_hot_nearest_query_does_not_sort_or_allocate_output_collection(self):
        text = RULES.read_text(encoding="utf-8")
        start = text.index("public bool FindNearestEnemy")
        end = text.index("public void QueryRadius", start)
        body = text[start:end]
        self.assertNotIn("new List", body)
        self.assertNotIn("Sort(", body)

    def test_spatial_hash_supports_deterministic_unit_removal(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("public bool Remove(in SpatialEntity entity)", text)
        self.assertIn("bucket.RemoveAt(i)", text)
        self.assertIn("_count--", text)

    def test_route_cache_is_invalidated_by_topology_version(self):
        text = RULES.read_text(encoding="utf-8")
        self.assertIn("SetTopologyVersion", text)
        self.assertIn("_routes.Clear()", text)
        self.assertIn("InvalidationCount++", text)

    def test_runtime_gate_requires_large_candidate_reduction_and_order_independence(self):
        text = GATE.read_text(encoding="utf-8")
        self.assertIn("entityCount = 1000", text)
        self.assertIn("naiveCandidateVisits / 5", text)
        self.assertIn("insertion order", text)
        self.assertIn("VerifySharedRouteInvalidation", text)

    def test_annihilation_movement_uses_fixed_rate_spatial_local_avoidance(self):
        rules = ANNIHILATION.read_text(encoding="utf-8")
        gate = ANNIHILATION_GATE.read_text(encoding="utf-8")
        self.assertIn("LocalAvoidanceIntervalTicks = 2", rules)
        self.assertIn("RebuildMovementSpatial(world)", rules)
        self.assertIn("DeterministicLocalAvoidance.Solve", rules)
        self.assertIn("DeterministicLocalAvoidance.ApplyStep", rules)
        self.assertIn("AvoidanceCandidateVisits", gate)
        self.assertIn("AvoidanceNeighborsResolved", gate)

    def test_annihilation_target_search_uses_shared_spatial_index(self):
        rules = ANNIHILATION.read_text(encoding="utf-8")
        gate = ANNIHILATION_GATE.read_text(encoding="utf-8")
        self.assertIn("RebuildCombatSpatial(world)", rules)
        self.assertIn("CombatSpatial.FindNearestEnemy", rules)
        self.assertIn("CombatSpatial.Remove", rules)
        self.assertNotIn("FindNearestLiveUnitInRange(unit, defender.Units)", rules)
        self.assertIn("CombatCandidateVisits", gate)


if __name__ == "__main__":
    unittest.main()
