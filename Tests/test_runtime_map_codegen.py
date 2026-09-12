import json
import pathlib
import tempfile
import unittest

import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "Tools"))
import runtime_map_codegen as codegen

ROOT = pathlib.Path(__file__).resolve().parents[1]
MAP = ROOT / "Data/Maps/MAP_GRAY_RANGE.map.json"


class RuntimeMapCodegenTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads(MAP.read_text(encoding="utf-8"))

    def test_deterministic_source(self):
        a = codegen.generate_source(self.data)
        b = codegen.generate_source(self.data)
        self.assertEqual(a, b)

    def test_runtime_profile_contains_required_map_data(self):
        source = codegen.generate_source(self.data)
        self.assertIn('MapId = "MAP_GRAY_RANGE"', source)
        self.assertIn('"ROAD_CENTER"', source)
        self.assertIn('"RES_A_I1"', source)
        self.assertIn('"SITE_DATA_CENTER"', source)
        self.assertIn('"REGION_CENTER"', source)
        self.assertIn('"RULESET_ANNIHILATION_STANDARD"', source)

    def test_spawns_are_ordered_and_symmetric(self):
        source = codegen.generate_source(self.data)
        a = source.index('"SPAWN_A"')
        b = source.index('"SPAWN_B"')
        self.assertLess(a, b)
        self.assertIn('new Int2(1200, 1200)', source)
        self.assertIn('new Int2(6800, 6800)', source)

    def test_invalid_map_without_center_road_is_rejected(self):
        broken = json.loads(json.dumps(self.data))
        broken["roads"] = [r for r in broken["roads"] if r["road_id"] != "ROAD_CENTER"]
        with self.assertRaises(ValueError):
            codegen.generate_source(broken)

    def test_invalid_map_without_annihilation_support_is_rejected(self):
        broken = json.loads(json.dumps(self.data))
        broken["ruleset_compatibility"] = ["RULESET_CONQUEST_STANDARD"]
        with self.assertRaises(ValueError):
            codegen.generate_source(broken)

    def test_checked_in_generated_source_is_current(self):
        expected = codegen.generate_source(self.data)
        generated = (ROOT / "Packages/com.modernra.rules/Runtime/GrayRangeGeneratedData.cs").read_text(encoding="utf-8")
        self.assertEqual(expected, generated)

    def test_cli_output_matches_library_output(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = pathlib.Path(tmp) / "GrayRangeGeneratedData.cs"
            out.write_text(codegen.generate_source(self.data), encoding="utf-8")
            self.assertEqual(out.read_text(encoding="utf-8"), codegen.generate_source(self.data))


if __name__ == "__main__":
    unittest.main()
