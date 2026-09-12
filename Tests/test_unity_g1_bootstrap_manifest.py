import importlib.util
import json
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "run_unity_g1.py"
SPEC = importlib.util.spec_from_file_location("run_unity_g1", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class UnityG1BootstrapManifestTests(unittest.TestCase):
    def _prepare_root(self, root, complete=True):
        settings = root / "ProjectSettings"
        packages = root / "Packages"
        settings.mkdir(parents=True)
        packages.mkdir(parents=True)
        (settings / "ProjectVersion.txt").write_text(
            "m_EditorVersion: 6000.3.24f1\n", encoding="utf-8"
        )
        (packages / "packages-lock.json").write_text(
            '{"dependencies": {}}\n', encoding="utf-8"
        )
        names = MODULE.REQUIRED_VERSIONED_PROJECT_SETTINGS
        for name in (names if complete else names[:-1]):
            (settings / name).write_text(f"unity:{name}\n", encoding="utf-8")

    def test_complete_baseline_manifest_is_stable_and_complete(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir) / "repo"
            artifacts = pathlib.Path(temp_dir) / "artifacts"
            self._prepare_root(root, complete=True)
            first = MODULE.write_bootstrap_manifest(root, artifacts)
            first_bytes = (artifacts / "bootstrap-manifest.json").read_bytes()
            second = MODULE.write_bootstrap_manifest(root, artifacts)
            second_bytes = (artifacts / "bootstrap-manifest.json").read_bytes()
            self.assertEqual(first, second)
            self.assertEqual(first_bytes, second_bytes)
            self.assertTrue(first["baseline_complete"])
            self.assertEqual([], first["missing_project_settings"])
            self.assertEqual(
                len(MODULE.REQUIRED_VERSIONED_PROJECT_SETTINGS) + 2,
                len(first["files"]),
            )

    def test_incomplete_baseline_records_missing_project_setting(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir) / "repo"
            artifacts = pathlib.Path(temp_dir) / "artifacts"
            self._prepare_root(root, complete=False)
            manifest = MODULE.write_bootstrap_manifest(root, artifacts)
            self.assertFalse(manifest["baseline_complete"])
            self.assertEqual(
                [MODULE.REQUIRED_VERSIONED_PROJECT_SETTINGS[-1]],
                manifest["missing_project_settings"],
            )

    def test_manifest_contains_relative_paths_hashes_and_sizes_only(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir) / "repo"
            artifacts = pathlib.Path(temp_dir) / "artifacts"
            self._prepare_root(root, complete=True)
            MODULE.write_bootstrap_manifest(root, artifacts)
            payload = json.loads(
                (artifacts / "bootstrap-manifest.json").read_text(encoding="utf-8")
            )
            for entry in payload["files"]:
                self.assertFalse(pathlib.PurePosixPath(entry["path"]).is_absolute())
                self.assertEqual(64, len(entry["sha256"]))
                self.assertGreater(entry["size"], 0)
                self.assertNotIn(temp_dir, entry["path"])


if __name__ == "__main__":
    unittest.main()
