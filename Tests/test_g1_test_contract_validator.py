import importlib.util
import pathlib
import shutil
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "g1_test_contract_validator.py"
SPEC = importlib.util.spec_from_file_location("g1_test_contract_validator", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class G1TestContractValidatorTests(unittest.TestCase):
    def test_current_local_contract_passes(self):
        self.assertEqual([], MODULE.validate_contract(ROOT))

    def test_missing_test_source_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            shutil.copytree(ROOT / "Tools", root / "Tools")
            shutil.copytree(ROOT / "Packages", root / "Packages")
            missing = root / "Packages/com.modernra.tests/Tests/Editor/FixedStepRateTests.cs"
            missing.unlink()
            errors = MODULE.validate_contract(root)
            self.assertTrue(any("source missing" in error for error in errors))

    def test_renamed_method_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            shutil.copytree(ROOT / "Tools", root / "Tools")
            shutil.copytree(ROOT / "Packages", root / "Packages")
            source = root / "Packages/com.modernra.tests/Tests/Editor/UnityWorldBootstrapSmokeTests.cs"
            source.write_text(
                source.read_text(encoding="utf-8").replace(
                    "GrayRangeBootstrap_MaterializesRuntimeStateAndAllAnchors",
                    "RenamedSmokeTest",
                ),
                encoding="utf-8",
            )
            errors = MODULE.validate_contract(root)
            self.assertTrue(any("missing [Test] method" in error for error in errors))

    def test_runner_contract_drift_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            shutil.copytree(ROOT / "Tools", root / "Tools")
            shutil.copytree(ROOT / "Packages", root / "Packages")
            runner = root / "Tools/run_unity_g1.py"
            runner.write_text(
                runner.read_text(encoding="utf-8").replace(
                    "ModernRA.Tests.AnnihilationRuleBridgeSmokeTests.RuleBridge_StartsFromGrayRangeAndMirrorsAuthoritativeState",
                    "ModernRA.Tests.AnnihilationRuleBridgeSmokeTests.Renamed",
                ),
                encoding="utf-8",
            )
            errors = MODULE.validate_contract(root)
            self.assertTrue(any("does not require EditMode test" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
