import copy
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
import graybox_generator as gg


class GrayboxGeneratorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.map_data = json.loads((ROOT / "Data/Maps/MAP_GRAY_RANGE.map.json").read_text(encoding="utf-8"))

    def test_map_validates(self):
        self.assertEqual([], gg.validate_map(self.map_data, 50))

    def test_generation_is_deterministic(self):
        a = gg.generate_manifest(copy.deepcopy(self.map_data), 50)
        b = gg.generate_manifest(copy.deepcopy(self.map_data), 50)
        self.assertEqual(a["manifest_sha256"], b["manifest_sha256"])
        self.assertEqual(a, b)

    def test_two_or_more_strategic_paths(self):
        manifest = gg.generate_manifest(self.map_data, 50)
        self.assertGreaterEqual(len(manifest["strategic_paths"]), 2)

    def test_buildable_area_is_symmetric(self):
        manifest = gg.generate_manifest(self.map_data, 50)
        counts = {x["build_id"]: x["cell_count"] for x in manifest["buildable"]}
        self.assertEqual(counts["BUILD_A"], counts["BUILD_B"])
        self.assertGreater(counts["BUILD_A"], 1000)

    def test_fairness_under_ten_percent(self):
        manifest = gg.generate_manifest(self.map_data, 50)
        self.assertLessEqual(manifest["fairness"]["max_relative_difference"], 0.10)

    def test_initial_anchors_inside_initial_build_area(self):
        self.assertEqual([], gg.validate_map(self.map_data, 50))
        manifest = gg.generate_manifest(self.map_data, 50)
        self.assertEqual(12, len(manifest["initial_spawn_anchors"]))

    def test_broken_graph_is_rejected(self):
        broken = copy.deepcopy(self.map_data)
        broken["control_regions"][0]["neighbors"].append("REGION_MISSING")
        errors = gg.validate_map(broken, 50)
        self.assertTrue(any("missing neighbor" in e for e in errors))

    def test_asymmetric_spawn_economy_is_rejected(self):
        broken = copy.deepcopy(self.map_data)
        broken["spawn_sectors"][1]["center"] = [7400, 7400]
        errors = gg.validate_map(broken, 50)
        self.assertTrue(any("fairness exceeds" in e for e in errors))


if __name__ == "__main__":
    unittest.main()
