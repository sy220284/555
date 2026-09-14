#!/usr/bin/env python3
"""Verify the evidence produced by GameCI's GitHub-hosted Unity test runner."""

import pathlib
import sys
import xml.etree.ElementTree as ET

from run_unity_g1 import verify_package_lock, verify_project_version, verify_test_results


def verify_hosted_results(artifacts):
    verify_project_version()
    verify_package_lock()

    candidates = sorted(artifacts.rglob("*.xml")) if artifacts.exists() else []
    if not candidates:
        raise RuntimeError(f"hosted Unity runner produced no XML test results under {artifacts}")

    rejected = []
    for candidate in candidates:
        try:
            verify_test_results(candidate)
            print(f"UNITY HOSTED G1 EVIDENCE VERIFIED: {candidate}")
            return candidate
        except (RuntimeError, ET.ParseError, ValueError) as exc:
            rejected.append(f"{candidate}: {exc}")

    raise RuntimeError(
        "no hosted XML result contains the complete passing G1 EditMode suite: "
        + " | ".join(rejected)
    )


def main(argv=None):
    args = sys.argv[1:] if argv is None else argv
    if len(args) != 1:
        print("usage: verify_hosted_unity_g1.py <artifacts-directory>")
        return 2
    try:
        verify_hosted_results(pathlib.Path(args[0]).resolve())
        return 0
    except Exception as exc:
        print("UNITY HOSTED G1 EVIDENCE FAILED")
        print(exc)
        return 1


if __name__ == "__main__":
    sys.exit(main())
