#!/usr/bin/env python3
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = ROOT / "Tools" / "run_unity_g1.py"
TEST_ROOT = ROOT / "Packages" / "com.modernra.tests" / "Tests" / "Editor"

REQUIRED_TEST_CONTRACTS = (
    (
        "FixedStepRateTests.cs",
        "FixedStepRateTests",
        "Bootstrap_ConfiguresFixedStepGroupToThirtyHertz",
    ),
    (
        "UnityWorldBootstrapSmokeTests.cs",
        "UnityWorldBootstrapSmokeTests",
        "GrayRangeBootstrap_MaterializesRuntimeStateAndAllAnchors",
    ),
    (
        "AnnihilationRuleBridgeSmokeTests.cs",
        "AnnihilationRuleBridgeSmokeTests",
        "RuleBridge_StartsFromGrayRangeAndMirrorsAuthoritativeState",
    ),
)


def expected_full_name(class_name, method_name):
    return f"ModernRA.Tests.{class_name}.{method_name}"


def validate_contract(root=ROOT):
    errors = []
    runner = root / "Tools" / "run_unity_g1.py"
    test_root = root / "Packages" / "com.modernra.tests" / "Tests" / "Editor"

    if not runner.exists():
        return ["Tools/run_unity_g1.py missing"]
    runner_text = runner.read_text(encoding="utf-8", errors="ignore")

    for filename, class_name, method_name in REQUIRED_TEST_CONTRACTS:
        source = test_root / filename
        full_name = expected_full_name(class_name, method_name)
        if not source.exists():
            errors.append(f"required G1 EditMode source missing: {source.relative_to(root)}")
            continue
        text = source.read_text(encoding="utf-8", errors="ignore")
        class_pattern = re.compile(rf"\bclass\s+{re.escape(class_name)}\b")
        method_pattern = re.compile(
            rf"\[Test\]\s*(?:\[[^\]]+\]\s*)*public\s+void\s+{re.escape(method_name)}\s*\(",
            re.MULTILINE,
        )
        if not class_pattern.search(text):
            errors.append(f"{source.relative_to(root)} missing class {class_name}")
        if not method_pattern.search(text):
            errors.append(f"{source.relative_to(root)} missing [Test] method {method_name}")
        if full_name not in runner_text:
            errors.append(f"run_unity_g1.py does not require EditMode test {full_name}")

    return errors


def main():
    errors = validate_contract(ROOT)
    if errors:
        print("G1 TEST CONTRACT VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1
    print(f"G1 TEST CONTRACT VALIDATION PASSED: {len(REQUIRED_TEST_CONTRACTS)} required EditMode tests")
    return 0


if __name__ == "__main__":
    sys.exit(main())
