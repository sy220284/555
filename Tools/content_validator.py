#!/usr/bin/env python3
import json
import pathlib
import re
import sys
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
DATA = ROOT / "Data"
DOCS = ROOT / "docs"
ID_RE = re.compile(r"\b(?:FAC|CTY|UNIT|FORM|BLD|WPN|TECH|STRAT|ABL|STATUS|VET|CHAR|MODE|RULESET|MAP|MIS|SFX|VOICE|MUSIC|UIEV|LOC|MIN|MAT)_[A-Z0-9_]+\b")
LEGACY_RE = re.compile(r"\b(?:FACTION|COUNTRY|BLDG|ABILITY|MISSION)_[A-Z0-9_]+\b")


def documented_ids():
    result = set()
    for path in DOCS.rglob("*.md"):
        result.update(ID_RE.findall(path.read_text(encoding="utf-8", errors="ignore")))
    return result


def walk_ids(value, found):
    if isinstance(value, dict):
        if isinstance(value.get("id"), str):
            found.append(value["id"])
        for child in value.values():
            walk_ids(child, found)
    elif isinstance(value, list):
        for child in value:
            walk_ids(child, found)


def main():
    errors = []
    ids = []
    docs_ids = documented_ids()
    json_files = [p for p in DATA.rglob("*.json") if "Schemas" not in p.parts]

    for path in json_files:
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:
            errors.append(f"{path.relative_to(ROOT)}: invalid json: {exc}")
            continue
        if not isinstance(payload, dict) or not isinstance(payload.get("schema_version"), int):
            errors.append(f"{path.relative_to(ROOT)}: missing integer schema_version")
        walk_ids(payload, ids)
        text = path.read_text(encoding="utf-8")
        for legacy in LEGACY_RE.findall(text):
            errors.append(f"{path.relative_to(ROOT)}: legacy id prefix detected: {legacy}")
        for token in ID_RE.findall(text):
            if token.startswith(("MIN_", "MAT_")):
                continue
            if token not in docs_ids and token not in ids:
                errors.append(f"{path.relative_to(ROOT)}: reference not documented: {token}")

    duplicates = [item for item, count in Counter(ids).items() if count > 1]
    for item in duplicates:
        errors.append(f"duplicate data id: {item}")

    for item in ids:
        if not ID_RE.fullmatch(item):
            errors.append(f"invalid stable id: {item}")

    mineral_file = DATA / "Minerals" / "minerals.json"
    if mineral_file.exists():
        minerals = json.loads(mineral_file.read_text(encoding="utf-8"))["minerals"]
        mineral_ids = {m["id"] for m in minerals}
        required = {"MIN_BASE_METALS", "MIN_RARE_EARTHS", "MIN_BATTERY_METALS", "MIN_HIGH_PERFORMANCE_ALLOYS", "MIN_NUCLEAR_FUEL", "MIN_MIXED_STRATEGIC", "MIN_SALVAGE_FIELD"}
        missing = required - mineral_ids
        if missing:
            errors.append("missing mineral ids: " + ", ".join(sorted(missing)))

    ruleset_file = DATA / "Rulesets" / "rulesets.json"
    if ruleset_file.exists():
        rulesets = json.loads(ruleset_file.read_text(encoding="utf-8"))["rulesets"]
        conquest = next((x for x in rulesets if x["id"] == "RULESET_CONQUEST_STANDARD"), None)
        quick = next((x for x in rulesets if x["id"] == "RULESET_QUICKWAR_STANDARD"), None)
        if not conquest or not conquest.get("material_tech_binding"):
            errors.append("conquest ruleset must enable material tech binding")
        if not quick or not quick.get("all_tech_granted"):
            errors.append("quickwar ruleset must grant all tech")
        if any(x.get("time_driven_progression") for x in rulesets):
            errors.append("standard rulesets cannot enable time driven progression")

    tech_file = DATA / "Technology" / "common_technology.json"
    if tech_file.exists():
        techs = json.loads(tech_file.read_text(encoding="utf-8"))["technologies"]
        bound = [t for t in techs if t.get("material_requirements_by_ruleset", {}).get("RULESET_CONQUEST_STANDARD")]
        by_tier = Counter(t["tier"] for t in bound)
        if by_tier[2] != 0:
            errors.append("conquest material binding must not lock T2 common tech")
        if not (1 <= by_tier[3] <= 2):
            errors.append("conquest T3 common material-bound tech count must be 1..2")
        if not (2 <= by_tier[4] <= 3):
            errors.append("conquest T4 common material-bound tech count must be 2..3")
        if not (1 <= by_tier[5] <= 2):
            errors.append("conquest T5 common material-bound tech count must be 1..2")

    if errors:
        print("CONTENT VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1
    print(f"CONTENT VALIDATION PASSED: {len(json_files)} json files, {len(ids)} data ids")
    return 0

if __name__ == "__main__":
    sys.exit(main())
