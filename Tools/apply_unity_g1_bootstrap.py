#!/usr/bin/env python3
import argparse
import hashlib
import json
import pathlib
import shutil
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
REQUIRED_PROJECT_SETTINGS = (
    "ProjectSettings.asset",
    "EditorBuildSettings.asset",
    "EditorSettings.asset",
    "GraphicsSettings.asset",
    "QualitySettings.asset",
    "TagManager.asset",
    "TimeManager.asset",
)
ALLOWED_COPY_PATHS = {
    "Packages/packages-lock.json",
    *(f"ProjectSettings/{name}" for name in REQUIRED_PROJECT_SETTINGS),
}
KNOWN_MANIFEST_ONLY_PATHS = {"ProjectSettings/ProjectVersion.txt"}


def sha256(path: pathlib.Path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_manifest(artifact_root: pathlib.Path):
    candidates = (
        artifact_root / "Artifacts/unity-g1/bootstrap-manifest.json",
        artifact_root / "bootstrap-manifest.json",
    )
    for path in candidates:
        if path.exists():
            payload = json.loads(path.read_text(encoding="utf-8"))
            return path, payload
    raise RuntimeError("bootstrap-manifest.json not found in Unity G1 artifact")


def resolve_artifact_file(artifact_root: pathlib.Path, repo_path: str):
    relative = pathlib.PurePosixPath(repo_path)
    if repo_path == "Packages/packages-lock.json":
        candidates = (
            artifact_root / pathlib.Path(*relative.parts),
            artifact_root / "packages-lock.json",
        )
    elif repo_path.startswith("ProjectSettings/"):
        name = relative.name
        candidates = (
            artifact_root / "Artifacts/unity-g1/generated-project-settings" / name,
            artifact_root / "generated-project-settings" / name,
            artifact_root / pathlib.Path(*relative.parts),
        )
    else:
        candidates = ()

    for candidate in candidates:
        if candidate.exists() and candidate.is_file():
            return candidate
    return None


def build_apply_plan(artifact_root: pathlib.Path, repo_root: pathlib.Path):
    _, manifest = load_manifest(artifact_root)
    if manifest.get("format_version") != 1:
        raise RuntimeError(f"unsupported bootstrap manifest format: {manifest.get('format_version')!r}")
    if not manifest.get("baseline_complete"):
        raise RuntimeError("bootstrap manifest is not complete")

    entries = manifest.get("files")
    if not isinstance(entries, list):
        raise RuntimeError("bootstrap manifest files must be an array")

    by_path = {}
    for entry in entries:
        if not isinstance(entry, dict):
            raise RuntimeError("bootstrap manifest contains invalid file entry")
        repo_path = entry.get("path")
        if not isinstance(repo_path, str) or not repo_path:
            raise RuntimeError("bootstrap manifest entry missing path")
        pure = pathlib.PurePosixPath(repo_path)
        if pure.is_absolute() or ".." in pure.parts:
            raise RuntimeError(f"unsafe bootstrap path: {repo_path}")
        if repo_path not in ALLOWED_COPY_PATHS and repo_path not in KNOWN_MANIFEST_ONLY_PATHS:
            raise RuntimeError(f"unexpected bootstrap path: {repo_path}")
        by_path[repo_path] = entry

    required = set(ALLOWED_COPY_PATHS) | KNOWN_MANIFEST_ONLY_PATHS
    missing_entries = sorted(required - set(by_path))
    if missing_entries:
        raise RuntimeError("bootstrap manifest missing required entries: " + ", ".join(missing_entries))

    version_entry = by_path["ProjectSettings/ProjectVersion.txt"]
    current_version = repo_root / "ProjectSettings/ProjectVersion.txt"
    if not current_version.exists():
        raise RuntimeError("repository ProjectSettings/ProjectVersion.txt is missing")
    if current_version.stat().st_size != int(version_entry.get("size", -1)) or sha256(current_version) != version_entry.get("sha256"):
        raise RuntimeError("repository ProjectVersion.txt does not match Unity G1 bootstrap manifest")

    plan = []
    for repo_path in sorted(ALLOWED_COPY_PATHS):
        entry = by_path[repo_path]
        source = resolve_artifact_file(artifact_root, repo_path)
        if source is None:
            raise RuntimeError(f"artifact file missing for {repo_path}")
        expected_size = int(entry.get("size", -1))
        expected_sha = entry.get("sha256")
        if source.stat().st_size != expected_size:
            raise RuntimeError(f"size mismatch for {repo_path}")
        if sha256(source) != expected_sha:
            raise RuntimeError(f"sha256 mismatch for {repo_path}")
        destination = repo_root / pathlib.Path(*pathlib.PurePosixPath(repo_path).parts)
        plan.append((repo_path, source, destination))
    return plan


def apply_plan(plan):
    for _, source, destination in plan:
        destination.parent.mkdir(parents=True, exist_ok=True)
        temporary = destination.with_name(destination.name + ".g1tmp")
        shutil.copyfile(source, temporary)
        temporary.replace(destination)


def main():
    parser = argparse.ArgumentParser(description="Validate and optionally apply Unity G1 generated baseline files")
    parser.add_argument("--artifact-dir", required=True)
    parser.add_argument("--repo-root", default=str(ROOT))
    parser.add_argument("--apply", action="store_true", help="copy verified files into the local workspace")
    args = parser.parse_args()

    artifact_root = pathlib.Path(args.artifact_dir).resolve()
    repo_root = pathlib.Path(args.repo_root).resolve()
    try:
        plan = build_apply_plan(artifact_root, repo_root)
        print(f"UNITY G1 BOOTSTRAP VALIDATED: {len(plan)} files")
        for repo_path, source, _ in plan:
            print(f"- {repo_path} <- {source}")
        if args.apply:
            apply_plan(plan)
            print("UNITY G1 BOOTSTRAP APPLIED TO LOCAL WORKSPACE")
        else:
            print("DRY RUN ONLY: pass --apply to copy verified files")
        return 0
    except Exception as exc:
        print("UNITY G1 BOOTSTRAP APPLY FAILED")
        print(exc)
        return 1


if __name__ == "__main__":
    sys.exit(main())
