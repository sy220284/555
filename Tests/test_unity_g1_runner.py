import importlib.util
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


if __name__ == "__main__":
    unittest.main()
