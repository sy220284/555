#!/usr/bin/env python3
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PACKAGES = ROOT / "Packages"
MANIFEST = PACKAGES / "manifest.json"


def load_json(path: pathlib.Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    errors = []
    root_manifest = load_json(MANIFEST).get("dependencies", {})

    packages = {}
    package_paths = {}
    for package_json in sorted(PACKAGES.glob("com.modernra.*/package.json")):
        try:
            payload = load_json(package_json)
        except Exception as exc:
            errors.append(f"{package_json.relative_to(ROOT)} invalid json: {exc}")
            continue
        name = payload.get("name")
        version = payload.get("version")
        expected_name = package_json.parent.name
        if name != expected_name:
            errors.append(f"{package_json.relative_to(ROOT)} name {name!r} != folder {expected_name!r}")
            continue
        if not isinstance(version, str) or not version:
            errors.append(f"{package_json.relative_to(ROOT)} missing version")
            continue
        if name in packages:
            errors.append(f"duplicate package name: {name}")
        packages[name] = payload
        package_paths[name] = package_json.parent

    for name, payload in packages.items():
        dependencies = payload.get("dependencies", {})
        if not isinstance(dependencies, dict):
            errors.append(f"{name}: dependencies must be an object")
            continue
        for dep, requested in dependencies.items():
            if dep.startswith("com.modernra."):
                target = packages.get(dep)
                if target is None:
                    errors.append(f"{name}: missing embedded dependency {dep}")
                elif target.get("version") != requested:
                    errors.append(f"{name}: {dep} requests {requested}, embedded version is {target.get('version')}")
            elif dep.startswith("com.unity."):
                root_version = root_manifest.get(dep)
                if root_version is not None and root_version != requested:
                    errors.append(f"{name}: {dep} requests {requested}, root manifest pins {root_version}")

    graph = {
        name: [dep for dep in payload.get("dependencies", {}) if dep.startswith("com.modernra.") and dep in packages]
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
    asmdef_files = sorted(PACKAGES.glob("com.modernra.*/**/*.asmdef"))
    for asmdef in asmdef_files:
        try:
            payload = load_json(asmdef)
        except Exception as exc:
            errors.append(f"{asmdef.relative_to(ROOT)} invalid json: {exc}")
            continue
        name = payload.get("name")
        if not isinstance(name, str) or not name:
            errors.append(f"{asmdef.relative_to(ROOT)} missing assembly name")
            continue
        if name in assembly_to_package:
            errors.append(f"duplicate assembly name: {name}")
        assembly_to_package[name] = asmdef.parents[1].name if asmdef.parent.name == "Runtime" else asmdef.parts[-4]

    modern_assemblies = set(assembly_to_package)
    for asmdef in asmdef_files:
        payload = load_json(asmdef)
        owner = payload.get("name", str(asmdef))
        for reference in payload.get("references", []):
            if isinstance(reference, str) and reference.startswith("ModernRA.") and reference not in modern_assemblies:
                errors.append(f"{owner}: missing assembly reference {reference}")

    rules_dir = PACKAGES / "com.modernra.rules"
    if rules_dir.exists():
        rules_package = packages.get("com.modernra.rules", {})
        if rules_package.get("dependencies"):
            errors.append("com.modernra.rules must remain dependency-free")
        forbidden = re.compile(r"\b(?:using\s+Unity(?:Engine|\.)|UnityEngine\.)")
        for source in rules_dir.rglob("*.cs"):
            text = source.read_text(encoding="utf-8", errors="ignore")
            if forbidden.search(text):
                errors.append(f"{source.relative_to(ROOT)}: deterministic rules kernel must not reference Unity APIs")

    if errors:
        print("PROJECT STRUCTURE VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1

    print(f"PROJECT STRUCTURE VALIDATION PASSED: {len(packages)} embedded packages, {len(modern_assemblies)} assemblies")
    return 0


if __name__ == "__main__":
    sys.exit(main())
