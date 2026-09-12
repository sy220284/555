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


def load_json(path, errors):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"{path.relative_to(ROOT)}: invalid json: {exc}")
        return None


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


def validate_map(path, payload, mineral_ids, ruleset_ids, errors):
    required_top = {
        "schema_version", "map_id", "size_m", "playable_bounds", "spawn_sectors",
        "resource_nodes", "strategic_sites", "control_regions", "roads", "bridges",
        "buildable_polygons", "terrain_regions", "ai_zones", "weather_profile",
        "ruleset_compatibility"
    }
    missing = required_top - set(payload)
    if missing:
        errors.append(f"{path.relative_to(ROOT)}: missing map fields: {', '.join(sorted(missing))}")
        return
    if payload["schema_version"] != 4:
        errors.append(f"{path.relative_to(ROOT)}: map schema_version must be 4")
    if path.name != f'{payload["map_id"]}.map.json':
        errors.append(f"{path.relative_to(ROOT)}: filename/map_id mismatch")
    size = payload.get("size_m", [])
    if len(size) != 2 or any(not isinstance(v, (int, float)) or v <= 0 for v in size):
        errors.append(f"{path.relative_to(ROOT)}: invalid size_m")

    spawns = payload.get("spawn_sectors", [])
    slots = [s.get("team_slot") for s in spawns]
    if len(spawns) < 2 or len(slots) != len(set(slots)):
        errors.append(f"{path.relative_to(ROOT)}: requires unique team spawn sectors")
    for spawn in spawns:
        if spawn.get("safe_radius_m", 0) < 600:
            errors.append(f"{path.relative_to(ROOT)}: {spawn.get('spawn_id')} safe radius below 600m")

    regions = payload.get("control_regions", [])
    region_ids = [r.get("region_id") for r in regions]
    if None in region_ids or len(region_ids) != len(set(region_ids)):
        errors.append(f"{path.relative_to(ROOT)}: duplicate/invalid control region ids")
    region_set = set(region_ids)
    for region in regions:
        unknown_neighbors = set(region.get("neighbors", [])) - region_set
        if unknown_neighbors:
            errors.append(f"{path.relative_to(ROOT)}: {region.get('region_id')} unknown neighbors {sorted(unknown_neighbors)}")

    node_ids = set()
    for node in payload.get("resource_nodes", []):
        node_id = node.get("node_id")
        if not node_id or node_id in node_ids:
            errors.append(f"{path.relative_to(ROOT)}: duplicate/invalid resource node {node_id}")
        node_ids.add(node_id)
        mineral = node.get("mineral_id")
        if mineral is not None and mineral not in mineral_ids:
            errors.append(f"{path.relative_to(ROOT)}: {node_id} unknown mineral {mineral}")
        if node.get("control_region_id") not in region_set:
            errors.append(f"{path.relative_to(ROOT)}: {node_id} unknown control region")
        if node.get("capacity", -1) < 0 or node.get("base_income_rate", -1) < 0:
            errors.append(f"{path.relative_to(ROOT)}: {node_id} negative resource values")

    site_ids = set()
    for site in payload.get("strategic_sites", []):
        site_id = site.get("site_id")
        if not site_id or site_id in site_ids:
            errors.append(f"{path.relative_to(ROOT)}: duplicate/invalid strategic site {site_id}")
        site_ids.add(site_id)
        if site.get("control_region_id") not in region_set:
            errors.append(f"{path.relative_to(ROOT)}: {site_id} unknown control region")

    for region in regions:
        missing_nodes = set(region.get("contained_resource_nodes", [])) - node_ids
        missing_sites = set(region.get("contained_strategic_sites", [])) - site_ids
        if missing_nodes:
            errors.append(f"{path.relative_to(ROOT)}: {region.get('region_id')} missing resource refs {sorted(missing_nodes)}")
        if missing_sites:
            errors.append(f"{path.relative_to(ROOT)}: {region.get('region_id')} missing site refs {sorted(missing_sites)}")

    unknown_rulesets = set(payload.get("ruleset_compatibility", [])) - ruleset_ids
    if unknown_rulesets:
        errors.append(f"{path.relative_to(ROOT)}: unknown rulesets {sorted(unknown_rulesets)}")

    if payload.get("map_id") == "MAP_GRAY_RANGE":
        by_region_class = Counter((n.get("control_region_id"), n.get("resource_class")) for n in payload.get("resource_nodes", []))
        if by_region_class[("REGION_HOME_A", "INDUSTRIAL")] != by_region_class[("REGION_HOME_B", "INDUSTRIAL")]:
            errors.append("MAP_GRAY_RANGE: home industrial node count is asymmetric")
        if by_region_class[("REGION_HOME_A", "STRATEGIC")] != by_region_class[("REGION_HOME_B", "STRATEGIC")]:
            errors.append("MAP_GRAY_RANGE: home strategic node count is asymmetric")


def main():
    errors = []
    ids = []
    docs_ids = documented_ids()
    json_files = [p for p in DATA.rglob("*.json") if "Schemas" not in p.parts]

    payloads = {}
    for path in json_files:
        payload = load_json(path, errors)
        if payload is None:
            continue
        payloads[path] = payload
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

    mineral_ids = set()
    mineral_file = DATA / "Minerals" / "minerals.json"
    if mineral_file in payloads:
        minerals = payloads[mineral_file]["minerals"]
        mineral_ids = {m["id"] for m in minerals}
        required = {"MIN_BASE_METALS", "MIN_RARE_EARTHS", "MIN_BATTERY_METALS", "MIN_HIGH_PERFORMANCE_ALLOYS", "MIN_NUCLEAR_FUEL", "MIN_MIXED_STRATEGIC", "MIN_SALVAGE_FIELD"}
        missing = required - mineral_ids
        if missing:
            errors.append("missing mineral ids: " + ", ".join(sorted(missing)))

    ruleset_ids = set()
    ruleset_file = DATA / "Rulesets" / "rulesets.json"
    if ruleset_file in payloads:
        rulesets = payloads[ruleset_file]["rulesets"]
        ruleset_ids = {x["id"] for x in rulesets}
        conquest = next((x for x in rulesets if x["id"] == "RULESET_CONQUEST_STANDARD"), None)
        quick = next((x for x in rulesets if x["id"] == "RULESET_QUICKWAR_STANDARD"), None)
        if not conquest or not conquest.get("material_tech_binding"):
            errors.append("conquest ruleset must enable material tech binding")
        if not quick or not quick.get("all_tech_granted"):
            errors.append("quickwar ruleset must grant all tech")
        if any(x.get("time_driven_progression") for x in rulesets):
            errors.append("standard rulesets cannot enable time driven progression")

    tech_file = DATA / "Technology" / "common_technology.json"
    if tech_file in payloads:
        techs = payloads[tech_file]["technologies"]
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

    for path, payload in payloads.items():
        if path.parent.name == "Maps" and path.name.endswith(".map.json"):
            validate_map(path, payload, mineral_ids, ruleset_ids, errors)

    if errors:
        print("CONTENT VALIDATION FAILED")
        for error in errors:
            print("-", error)
        return 1
    print(f"CONTENT VALIDATION PASSED: {len(json_files)} json files, {len(ids)} data ids")
    return 0

if __name__ == "__main__":
    sys.exit(main())
