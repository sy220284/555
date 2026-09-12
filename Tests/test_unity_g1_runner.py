import importlib.util
import json
import pathlib
import tempfile
import unittest
from types import SimpleNamespace
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "run_unity_g1.py"
SPEC = importlib.util.spec_from_file_location("run_unity_g1", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class UnityG1RunnerTests(unittest.TestCase):
    @mock.patch.object(MODULE.subprocess, "run")
    def test_verify_editor_binary_accepts_frozen_version(self, run_mock):
        run_mock.return_value = SimpleNamespace(
            returncode=0,
            stdout="6000.3.24f1\n",
            stderr="",
        )
        MODULE.verify_editor_binary(pathlib.Path("/fake/Unity"))
        run_mock.assert_called_once()
        self.assertEqual("-version", run_mock.call_args.args[0][1])

    @mock.patch.object(MODULE.subprocess, "run")
    def test_verify_editor_binary_rejects_wrong_version(self, run_mock):
        run_mock.return_value = SimpleNamespace(
            returncode=0,
            stdout="6000.3.23f1\n",
            stderr="",
        )
        with self.assertRaisesRegex(RuntimeError, "wrong editor"):
            MODULE.verify_editor_binary(pathlib.Path("/fake/Unity"))

    @mock.patch.object(MODULE.subprocess, "run")
    def test_verify_editor_binary_rejects_failed_invocation(self, run_mock):
        run_mock.return_value = SimpleNamespace(
            returncode=1,
            stdout="",
            stderr="license/tool failure",
        )
        with self.assertRaisesRegex(RuntimeError, "-version failed"):
            MODULE.verify_editor_binary(pathlib.Path("/fake/Unity"))

    def test_remove_stale_artifact_deletes_previous_result(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            artifact = pathlib.Path(temp_dir) / "stale.xml"
            artifact.write_text("old", encoding="utf-8")
            MODULE.remove_stale_artifact(artifact)
            self.assertFalse(artifact.exists())

    def test_base_command_keeps_apiupdate_timestamps_and_project_path(self):
        command = MODULE.base_editor_command(pathlib.Path("/fake/Unity"))
        self.assertIn("-accept-apiupdate", command)
        self.assertIn("-timestamps", command)
        self.assertIn("-projectPath", command)
        self.assertNotIn("-quit", command)

    def test_package_lock_accepts_matching_direct_dependencies(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            packages = root / "Packages"
            packages.mkdir()
            (packages / "manifest.json").write_text(
                json.dumps({"dependencies": {"com.unity.entities": "1.4.3", "com.unity.burst": "1.8.26"}}),
                encoding="utf-8",
            )
            (packages / "packages-lock.json").write_text(
                json.dumps({
                    "dependencies": {
                        "com.unity.entities": {"version": "1.4.3", "depth": 0, "source": "registry"},
                        "com.unity.burst": {"version": "1.8.26", "depth": 0, "source": "registry"},
                        "com.unity.mathematics": {"version": "1.3.2", "depth": 1, "source": "registry"},
                    }
                }),
                encoding="utf-8",
            )
            MODULE.verify_package_lock(root)

    def test_package_lock_rejects_missing_lock_file(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            packages = root / "Packages"
            packages.mkdir()
            (packages / "manifest.json").write_text(
                json.dumps({"dependencies": {"com.unity.entities": "1.4.3"}}),
                encoding="utf-8",
            )
            with self.assertRaisesRegex(RuntimeError, "did not produce"):
                MODULE.verify_package_lock(root)

    def test_package_lock_rejects_version_or_depth_drift(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            packages = root / "Packages"
            packages.mkdir()
            (packages / "manifest.json").write_text(
                json.dumps({"dependencies": {"com.unity.entities": "1.4.3"}}),
                encoding="utf-8",
            )
            (packages / "packages-lock.json").write_text(
                json.dumps({
                    "dependencies": {
                        "com.unity.entities": {"version": "1.4.2", "depth": 1, "source": "registry"}
                    }
                }),
                encoding="utf-8",
            )
            with self.assertRaisesRegex(RuntimeError, "package lock mismatch"):
                MODULE.verify_package_lock(root)


if __name__ == "__main__":
    unittest.main()
