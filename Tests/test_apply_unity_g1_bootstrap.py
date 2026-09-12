import hashlib
import importlib.util
import json
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "apply_unity_g1_bootstrap.py"
SPEC = importlib.util.spec_from_file_location("apply_unity_g1_bootstrap", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def digest(data: bytes):
    return hashlib.sha256(data).hexdigest()


class ApplyUnityG1BootstrapTests(unittest.TestCase):
    def _fixture(self, temp_dir):
        base = pathlib.Path(temp_dir)
        repo = base / "repo"
        artifact = base / "artifact"
        (repo / "ProjectSettings").mkdir(parents=True)
        (artifact / "Artifacts/unity-g1/generated-project-settings").mkdir(parents=True)
        (artifact / "Packages").mkdir(parents=True)
        version = b"m_EditorVersion: 6000.3.24f1\n"
        (repo / "ProjectSettings/ProjectVersion.txt").write_bytes(version)

        files = [{
            "path": "ProjectSettings/ProjectVersion.txt",
            "size": len(version),
            "sha256": digest(version),
        }]
        lock = b'{"dependencies": {}}\n'
        (artifact / "Packages/packages-lock.json").write_bytes(lock)
        files.append({
            "path": "Packages/packages-lock.json",
            "size": len(lock),
            "sha256": digest(lock),
        })
        for index, name in enumerate(MODULE.REQUIRED_PROJECT_SETTINGS):
            data = f"unity:{index}:{name}\n".encode()
            (artifact / "Artifacts/unity-g1/generated-project-settings" / name).write_bytes(data)
            files.append({
                "path": f"ProjectSettings/{name}",
                "size": len(data),
                "sha256": digest(data),
            })
        manifest = {
            "format_version": 1,
            "unity_editor_version": "6000.3.24f1",
            "baseline_complete": True,
            "missing_project_settings": [],
            "files": files,
        }
        (artifact / "Artifacts/unity-g1/bootstrap-manifest.json").write_text(
            json.dumps(manifest), encoding="utf-8"
        )
        return repo, artifact, manifest

    def test_good_artifact_builds_whitelisted_apply_plan(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            repo, artifact, _ = self._fixture(temp_dir)
            plan = MODULE.build_apply_plan(artifact, repo)
            self.assertEqual(MODULE.ALLOWED_COPY_PATHS, {item[0] for item in plan})

    def test_tampered_generated_file_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            repo, artifact, _ = self._fixture(temp_dir)
            target = artifact / "Artifacts/unity-g1/generated-project-settings/ProjectSettings.asset"
            target.write_text("tampered", encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "mismatch"):
                MODULE.build_apply_plan(artifact, repo)

    def test_unexpected_manifest_path_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            repo, artifact, manifest = self._fixture(temp_dir)
            manifest["files"].append({
                "path": "../../outside.txt",
                "size": 1,
                "sha256": "0" * 64,
            })
            (artifact / "Artifacts/unity-g1/bootstrap-manifest.json").write_text(
                json.dumps(manifest), encoding="utf-8"
            )
            with self.assertRaisesRegex(RuntimeError, "unsafe bootstrap path"):
                MODULE.build_apply_plan(artifact, repo)

    def test_apply_copies_verified_baseline_without_overwriting_version_file(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            repo, artifact, _ = self._fixture(temp_dir)
            before = (repo / "ProjectSettings/ProjectVersion.txt").read_bytes()
            plan = MODULE.build_apply_plan(artifact, repo)
            MODULE.apply_plan(plan)
            self.assertEqual(before, (repo / "ProjectSettings/ProjectVersion.txt").read_bytes())
            self.assertTrue((repo / "Packages/packages-lock.json").exists())
            for name in MODULE.REQUIRED_PROJECT_SETTINGS:
                self.assertTrue((repo / "ProjectSettings" / name).exists())


if __name__ == "__main__":
    unittest.main()
