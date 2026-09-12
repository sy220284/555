#!/usr/bin/env python3
import json
import os
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
EXPECTED_EDITOR_VERSION = "6000.3.24f1"
REQUIRED_EDITMODE_TESTS = (
    "ModernRA.Tests.FixedStepRateTests.Bootstrap_ConfiguresFixedStepGroupToThirtyHertz",
    "ModernRA.Tests.UnityWorldBootstrapSmokeTests.GrayRangeBootstrap_MaterializesRuntimeStateAndAllAnchors",
    "ModernRA.Tests.AnnihilationRuleBridgeSmokeTests.RuleBridge_StartsFromGrayRangeAndMirrorsAuthoritativeState",
)


def run(command, log_path):
    print("RUN:", " ".join(str(part) for part in command))
    result = subprocess.run(command, cwd=ROOT, text=True)
    if result.returncode != 0:
        print(f"command failed with exit code {result.returncode}; see {log_path}")
        return result.returncode
    return 0


def require_editor():
    raw = os.environ.get("UNITY_EDITOR_PATH", "").strip()
    if not raw:
        raise RuntimeError(
            "UNITY_EDITOR_PATH is required and must point to an activated Unity 6000.3.24f1 editor executable"
        )
    editor = pathlib.Path(raw)
    if not editor.exists():
        raise RuntimeError(f"UNITY_EDITOR_PATH does not exist: {editor}")
    if not editor.is_file():
        raise RuntimeError(f"UNITY_EDITOR_PATH is not a file: {editor}")
    return editor


def verify_editor_binary(editor):
    result = subprocess.run(
        [str(editor), "-version"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        timeout=30,
    )
    output = "\n".join(part for part in (result.stdout, result.stderr) if part).strip()
    if result.returncode != 0:
        raise RuntimeError(
            f"Unity -version failed with exit code {result.returncode}: {output or '<no output>'}"
        )
    if EXPECTED_EDITOR_VERSION not in output:
        raise RuntimeError(
            f"UNITY_EDITOR_PATH resolves to wrong editor; expected {EXPECTED_EDITOR_VERSION}, got: {output or '<no output>'}"
        )
    print(f"UNITY EDITOR VERIFIED: {EXPECTED_EDITOR_VERSION}")


def verify_project_version():
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    text = version_file.read_text(encoding="utf-8")
    expected = f"m_EditorVersion: {EXPECTED_EDITOR_VERSION}"
    if expected not in text:
        raise RuntimeError(f"project editor version is not {EXPECTED_EDITOR_VERSION}")


def verify_package_lock(root=ROOT):
    manifest_path = root / "Packages" / "manifest.json"
    lock_path = root / "Packages" / "packages-lock.json"
    if not lock_path.exists():
        raise RuntimeError(
            "Unity Package Manager did not produce Packages/packages-lock.json; "
            "G1 requires the resolved dependency graph before it can be closed"
        )

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    lock = json.loads(lock_path.read_text(encoding="utf-8"))
    requested = manifest.get("dependencies", {})
    resolved = lock.get("dependencies", {})
    if not isinstance(resolved, dict):
        raise RuntimeError("Packages/packages-lock.json dependencies must be an object")

    problems = []
    for package_name, requested_version in sorted(requested.items()):
        entry = resolved.get(package_name)
        if not isinstance(entry, dict):
            problems.append(f"missing direct dependency {package_name}")
            continue
        resolved_version = entry.get("version")
        if resolved_version != requested_version:
            problems.append(
                f"{package_name}: manifest={requested_version!r} lock={resolved_version!r}"
            )
        depth = entry.get("depth")
        if depth != 0:
            problems.append(f"{package_name}: direct dependency depth must be 0, got {depth!r}")

    if problems:
        raise RuntimeError("package lock mismatch: " + "; ".join(problems))

    print(
        "UNITY PACKAGE LOCK VERIFIED: "
        f"{len(requested)} direct dependencies, {len(resolved)} total resolved packages"
    )


def _test_case_full_name(case):
    for key in ("fullname", "full-name", "fullName"):
        value = case.attrib.get(key)
        if value:
            return value
    classname = case.attrib.get("classname") or case.attrib.get("class-name") or ""
    method = case.attrib.get("methodname") or case.attrib.get("method-name") or case.attrib.get("name") or ""
    if classname and method:
        return f"{classname}.{method}"
    return case.attrib.get("name", "")


def verify_test_results(path, required_tests=REQUIRED_EDITMODE_TESTS):
    if not path.exists():
        raise RuntimeError(f"Unity did not produce EditMode results: {path}")

    root = ET.parse(path).getroot()
    failed = int(root.attrib.get("failed", "0") or "0")
    result = root.attrib.get("result", "")
    cases = list(root.iter("test-case"))
    declared_total = root.attrib.get("total") or root.attrib.get("testcasecount")
    total = int(declared_total) if declared_total not in (None, "") else len(cases)

    if total <= 0 or not cases:
        raise RuntimeError("EditMode test run executed zero test cases")
    if failed != 0 or result.lower() == "failed":
        raise RuntimeError(f"EditMode tests failed: failed={failed} result={result}")

    executed = {}
    for case in cases:
        full_name = _test_case_full_name(case)
        if full_name:
            executed[full_name] = case.attrib.get("result", "")

    missing = []
    not_passed = []
    for required in required_tests:
        matches = [(name, case_result) for name, case_result in executed.items() if name == required or name.endswith(required)]
        if not matches:
            missing.append(required)
            continue
        if not any(case_result.lower() == "passed" for _, case_result in matches):
            not_passed.append(required)

    if missing:
        raise RuntimeError("EditMode required tests were not executed: " + "; ".join(missing))
    if not_passed:
        raise RuntimeError("EditMode required tests did not pass: " + "; ".join(not_passed))

    print(
        "UNITY EDITMODE RESULTS VERIFIED: "
        f"total={total}, required={len(required_tests)}, all required tests passed"
    )


def remove_stale_artifact(path):
    if path.exists():
        path.unlink()


def print_log_tail(path, line_count=200):
    if not path.exists():
        print(f"LOG MISSING: {path}")
        return
    lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    tail = lines[-line_count:]
    print(f"--- {path.name} tail ({len(tail)}/{len(lines)} lines) ---")
    for line in tail:
        print(line)
    print(f"--- end {path.name} ---")


def base_editor_command(editor):
    return [
        str(editor),
        "-batchmode",
        "-nographics",
        "-accept-apiupdate",
        "-timestamps",
        "-projectPath",
        str(ROOT),
    ]


def main():
    artifacts = ROOT / "Artifacts" / "unity-g1"
    compile_log = artifacts / "compile.log"
    test_log = artifacts / "editmode.log"
    test_results = artifacts / "editmode-results.xml"
    try:
        verify_project_version()
        editor = require_editor()
        verify_editor_binary(editor)

        artifacts.mkdir(parents=True, exist_ok=True)
        for artifact in (compile_log, test_log, test_results):
            remove_stale_artifact(artifact)

        compile_command = base_editor_command(editor) + [
            "-quit",
            "-logFile",
            str(compile_log),
        ]
        code = run(compile_command, compile_log)
        if code != 0:
            print_log_tail(compile_log)
            return code

        compile_text = compile_log.read_text(encoding="utf-8", errors="ignore") if compile_log.exists() else ""
        if "error CS" in compile_text or "Scripts have compiler errors" in compile_text:
            print("Unity compile log contains compiler errors")
            print_log_tail(compile_log)
            return 2

        verify_package_lock()

        test_command = base_editor_command(editor) + [
            "-runTests",
            "-testPlatform",
            "EditMode",
            "-testResults",
            str(test_results),
            "-logFile",
            str(test_log),
        ]
        code = run(test_command, test_log)
        if code != 0:
            print_log_tail(test_log)
            return code
        try:
            verify_test_results(test_results)
        except Exception:
            print_log_tail(test_log)
            raise
        print(f"UNITY G1 PASSED: {EXPECTED_EDITOR_VERSION} compile + EditMode")
        return 0
    except Exception as exc:
        print("UNITY G1 FAILED")
        print(exc)
        return 1


if __name__ == "__main__":
    sys.exit(main())
