#!/usr/bin/env python3
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
EXPECTED_EDITOR_VERSION = "6000.3.24f1"
EXPECTED_EDITOR_REVISION = "4e7b9b5b6244"
EXPECTED_UNITY_LINE = "6000.3"
REQUIRED_UNITY_PACKAGES = {
    "com.unity.addressables": "2.7.6",
    "com.unity.burst": "1.8.26",
    "com.unity.entities": "1.4.3",
    "com.unity.entities.graphics": "1.4.16",
    "com.unity.test-framework.performance": "3.2.0",
    "com.unity.transport": "2.6.0",
}
REQUIRED_G1_ASSEMBLIES = {
    "ModernRA.Core",
    "ModernRA.Rules",
    "ModernRA.Simulation",
    "ModernRA.Combat",
    "ModernRA.Navigation",
    "ModernRA.AI",
    "ModernRA.IntelEW",
    "ModernRA.Robotics",
    "ModernRA.Network",
    "ModernRA.Tests.Editor",
}
FORBIDDEN_VERSION_MARKERS = ("preview", "experimental", "-pre", "-exp")
SIMULATION_FORBIDDEN_API_PATTERNS = (
    ("UnityEngine dependency", re.compile(r"\busing\s+UnityEngine(?:\.|\s*;)|\bUnityEngine\.")),
    ("Camera", re.compile(r"\bCamera\b")),
    ("Material", re.compile(r"\bMaterial\b")),
    ("AudioSource", re.compile(r"\bAudioSource\b")),
    ("Animator", re.compile(r"\bAnimator\b")),
    ("GameObject", re.compile(r"\bGameObject\b")),
    ("MonoBehaviour", re.compile(r"\bMonoBehaviour\b")),
    ("VisualEffect", re.compile(r"\bVisualEffect\b")),
)


def load_json(path: pathlib.Path):
    return json.loads(path.read_text(encoding="utf-8"))


def has_forbidden_version_marker(version: object) -> bool:
    if not isinstance(version, str):
        return True
    lowered = version.lower()
    return any(marker in lowered for marker in FORBIDDEN_VERSION_MARKERS)


def find_forbidden_simulation_api(text: str):
    return [label for label, pattern in SIMULATION_FORBIDDEN_API_PATTERNS if pattern.search(text)]


def validate_project(root: pathlib.Path = ROOT):
    errors = []
    assets_root = root / "Assets"
    packages_root = root / "Packages"
    manifest_path = packages_root / "manifest.json"
    project_version_path = root / "ProjectSettings" / "ProjectVersion.txt"

    if not assets_root.is_dir():
        errors.append("Assets directory missing; Unity cannot open this checkout as a project")

    if not project_version_path.exists():
        errors.append("ProjectSettings/ProjectVersion.txt missing")
    else:
        project_version = project_version_path.read_text(encoding="utf-8")
        editor_match = re.search(r"^m_EditorVersion:\s*(\S+)", project_version, re.MULTILINE)
        revision_match = re.search(r"^m_EditorVersionWithRevision:\s*\S+\s*\(([^)]+)\)", project_version, re.MULTILINE)
        editor_version = editor_match.group(1) if editor_match else None
        editor_revision = revision_match.group(1) if revision_match else None
        if editor_version != EXPECTED_EDITOR_VERSION:
            errors.append(
                f"ProjectVersion editor {editor_version!r} != frozen {EXPECTED_EDITOR_VERSION!r}"
            )
        if editor_revision != EXPECTED_EDITOR_REVISION:
            errors.append(
                f"ProjectVersion revision {editor_revision!r} != frozen {EXPECTED_EDITOR_REVISION!r}"
            )

    if not manifest_path.exists():
        errors.append("Packages/manifest.json missing")
        root_manifest = {}
    else:
        try:
            root_manifest = load_json(manifest_path).get("dependencies", {})
        except Exception as exc:
            errors.append(f"Packages/manifest.json invalid json: {exc}")
            root_manifest = {}

    if not isinstance(root_manifest, dict):
        errors.append("Packages/manifest.json dependencies must be an object")
        root_manifest = {}

    for package_name, expected_version in REQUIRED_UNITY_PACKAGES.items():
        actual = root_manifest.get(package_name)
        if actual != expected_version:
            errors.append(
                f"root manifest {package_name}={actual!r} != frozen {expected_version!r}"
            )

    for package_name, version in sorted(root_manifest.items()):
        if has_forbidden_version_marker(version):
            errors.append(f"root manifest {package_name} uses preview/experimental version {version!r}")

    packages = {}
    package_paths = {}
    for package_json in sorted(packages_root.glob("com.modernra.*/package.json")):
        try:
            payload = load_json(package_json)
        except Exception as exc:
            errors.append(f"{package_json.relative_to(root)} invalid json: {exc}")
            continue
        name = payload.get("name")
        version = payload.get("version")
        expected_name = package_json.parent.name
        if name != expected_name:
            errors.append(f"{package_json.relative_to(root)} name {name!r} != folder {expected_name!r}")
            continue
        if not isinstance(version, str) or not version:
            errors.append(f"{package_json.relative_to(root)} missing version")
            continue
        if name in packages:
            errors.append(f"duplicate package name: {name}")
        packages[name] = payload
        package_paths[name] = package_json.parent
        if payload.get("unity") != EXPECTED_UNITY_LINE:
            errors.append(
                f"{name}: unity baseline {payload.get('unity')!r} != {EXPECTED_UNITY_LINE!r}"
            )

    for name, payload in packages.items():
        dependencies = payload.get("dependencies", {})
        if not isinstance(dependencies, dict):
            errors.append(f"{name}: dependencies must be an object")
            continue
        for dep, requested in dependencies.items():
            if has_forbidden_version_marker(requested):
                errors.append(f"{name}: {dep} uses preview/experimental version {requested!r}")
            if dep.startswith("com.modernra."):
                target = packages.get(dep)
                if target is None:
                    errors.append(f"{name}: missing embedded dependency {dep}")
                elif target.get("version") != requested:
                    errors.append(
                        f"{name}: {dep} requests {requested}, embedded version is {target.get('version')}"
                    )
            elif dep.startswith("com.unity."):
                root_version = root_manifest.get(dep)
                if root_version is not None and root_version != requested:
                    errors.append(f"{name}: {dep} requests {requested}, root manifest pins {root_version}")

    graph = {
        name: [
            dep
            for dep in payload.get("dependencies", {})
            if dep.startswith("com.modernra.") and dep in packages
        ]
        for name, payload in packages.items()
    }
    visiting = set()
    visited = set()

    def visit(node, stack):
        if node in visiting:
            start = stack.index(node) if node in stack else 0
            errors.append("package dependency cycle: " + " -> ".join(stack[start:] + [node]))
            return
        if node in visited:
            return
        visiting.add(node)
        stack.append(node)
        for dep in graph.get(node, []):
            visit(dep, stack)
        stack.pop()
        visiting.remove(node)
        visited.add(node)

    for package in sorted(graph):
        visit(package, [])

    assembly_to_package = {}
    asmdef_payloads = {}
    asmdef_files = sorted(packages_root.glob("com.modernra.*/**/*.asmdef"))
    for asmdef in asmdef_files:
        try:
            payload = load_json(asmdef)
        except Exception as exc:
            errors.append(f"{asmdef.relative_to(root)} invalid json: {exc}")
            continue
        name = payload.get("name")
        if not isinstance(name, str) or not name:
            errors.append(f"{asmdef.relative_to(root)} missing assembly name")
            continue
        if name in assembly_to_package:
            errors.append(f"duplicate assembly name: {name}")
        owner_package = asmdef.relative_to(packages_root).parts[0]
        assembly_to_package[name] = owner_package
        asmdef_payloads[asmdef] = payload

    missing_g1 = sorted(REQUIRED_G1_ASSEMBLIES - set(assembly_to_package))
    if missing_g1:
        errors.append("missing G1 assemblies: " + ", ".join(missing_g1))

    modern_assemblies = set(assembly_to_package)
    for asmdef, payload in asmdef_payloads.items():
        owner = payload.get("name", str(asmdef))
        owner_package = assembly_to_package.get(owner)
        owner_dependencies = packages.get(owner_package, {}).get("dependencies", {}) if owner_package else {}
        for reference in payload.get("references", []):
            if not isinstance(reference, str) or not reference.startswith("ModernRA."):
                continue
            if reference not in modern_assemblies:
                errors.append(f"{owner}: missing assembly reference {reference}")
                continue
            target_package = assembly_to_package[reference]
            if owner_package and target_package != owner_package and target_package not in owner_dependencies:
                errors.append(
                    f"{owner}: references {reference} from {target_package} without package dependency"
                )

    test_asmdef = next(
        (payload for payload in asmdef_payloads.values() if payload.get("name") == "ModernRA.Tests.Editor"),
        None,
    )
    if test_asmdef is not None:
        if "Editor" not in test_asmdef.get("includePlatforms", []):
            errors.append("ModernRA.Tests.Editor must be restricted to Editor")
        if "TestAssemblies" not in test_asmdef.get("optionalUnityReferences", []):
            errors.append("ModernRA.Tests.Editor must declare TestAssemblies")
        if test_asmdef.get("autoReferenced") is not False:
            errors.append("ModernRA.Tests.Editor must set autoReferenced=false")

    rules_dir = packages_root / "com.modernra.rules"
    if rules_dir.exists():
        rules_package = packages.get("com.modernra.rules", {})
        if rules_package.get("dependencies"):
            errors.append("com.modernra.rules must remain dependency-free")
        forbidden = re.compile(r"\b(?:using\s+Unity(?:Engine|\.)|UnityEngine\.)")
        for source in rules_dir.rglob("*.cs"):
            text = source.read_text(encoding="utf-8", errors="ignore")
            if forbidden.search(text):
                errors.append(
                    f"{source.relative_to(root)}: deterministic rules kernel must not reference Unity APIs"
                )

    simulation_runtime = packages_root / "com.modernra.simulation" / "Runtime"
    if simulation_runtime.exists():
        for source in sorted(simulation_runtime.rglob("*.cs")):
            text = source.read_text(encoding="utf-8", errors="ignore")
            violations = find_forbidden_simulation_api(text)
            if violations:
                errors.append(
                    f"{source.relative_to(root)}: authoritative simulation boundary uses forbidden presentation API(s): "
                    + ", ".join(violations)
                )

    simulation_rate = root / "Packages/com.modernra.simulation/Runtime/SimulationRate.cs"
    fixed_step_bootstrap = root / "Packages/com.modernra.simulation/Runtime/FixedStepRateBootstrapSystem.cs"
    if not simulation_rate.exists():
        errors.append("SimulationRate.cs missing")
    else:
        text = simulation_rate.read_text(encoding="utf-8")
        if not re.search(r"TicksPerSecond\s*=\s*30\s*;", text):
            errors.append("SimulationRate.TicksPerSecond must remain 30")
        if "SecondsPerTick = 1f / TicksPerSecond" not in text:
            errors.append("SimulationRate.SecondsPerTick must derive from TicksPerSecond")

    if not fixed_step_bootstrap.exists():
        errors.append("FixedStepRateBootstrapSystem.cs missing")
    else:
        text = fixed_step_bootstrap.read_text(encoding="utf-8")
        if "FixedStepSimulationSystemGroup" not in text or "SimulationRate.SecondsPerTick" not in text:
            errors.append("FixedStepRateBootstrapSystem must explicitly set ECS fixed step to SimulationRate.SecondsPerTick")

    return errors, len(packages), len(modern_assemblies)


def main() -> int:
    errors, package_count, assembly_count = validate_project(ROOT)
    if errors:
        print("PROJECT STRUCTURE VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1

    print(
        "PROJECT STRUCTURE VALIDATION PASSED: "
        f"Unity {EXPECTED_EDITOR_VERSION}, {package_count} embedded packages, {assembly_count} assemblies"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
