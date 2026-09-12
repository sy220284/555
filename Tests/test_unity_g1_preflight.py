import importlib.util
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "project_structure_validator.py"
SPEC = importlib.util.spec_from_file_location("project_structure_validator", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class UnityG1PreflightTests(unittest.TestCase):
    def test_current_repository_passes_structural_preflight(self):
        errors, package_count, assembly_count = MODULE.validate_project(ROOT)
        self.assertEqual([], errors)
        self.assertGreaterEqual(package_count, 9)
        self.assertGreaterEqual(assembly_count, 10)

    def test_preview_and_experimental_versions_are_rejected(self):
        self.assertTrue(MODULE.has_forbidden_version_marker("1.0.0-preview.1"))
        self.assertTrue(MODULE.has_forbidden_version_marker("2.0.0-experimental"))
        self.assertTrue(MODULE.has_forbidden_version_marker("3.0.0-pre.2"))
        self.assertFalse(MODULE.has_forbidden_version_marker("1.4.3"))

    def test_editor_version_mismatch_is_reported(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = pathlib.Path(temp_dir)
            (root / "ProjectSettings").mkdir(parents=True)
            (root / "Packages").mkdir(parents=True)
            (root / "ProjectSettings" / "ProjectVersion.txt").write_text(
                "m_EditorVersion: 6000.3.23f1\n"
                "m_EditorVersionWithRevision: 6000.3.23f1 (badrevision)\n",
                encoding="utf-8",
            )
            (root / "Packages" / "manifest.json").write_text(
                '{"dependencies": {}}', encoding="utf-8"
            )
            errors, _, _ = MODULE.validate_project(root)
            self.assertTrue(any("ProjectVersion editor" in error for error in errors))
            self.assertTrue(any("ProjectVersion revision" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
