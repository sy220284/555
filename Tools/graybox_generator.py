#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
from typing import Any, Iterable

GENERATOR_VERSION = 1
DEFAULT_CELL_SIZE_M = 50


def point_in_polygon(point: tuple[float, float], polygon: list[list[float]]) -> bool:
    x, y = point
    inside = False
    j = len(polygon) - 1
    for i in range(len(polygon)):
        xi, yi = polygon[i]
        xj, yj = polygon[j]
        intersects = ((yi > y) != (yj > y)) and (
            x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-12) + xi
        )
        if intersects:
            inside = not inside
        j = i
    return inside


def euclidean(a: Iterable[float], b: Iterable[float]) -> float:
    ax, ay = a
    bx, by = b
    return math.hypot(ax - bx, ay - by)


def canonical_bytes(data: Any) -> bytes:
    return json.dumps(data, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def sha256_json(data: Any) -> str:
    return hashlib.sha256(canonical_bytes(data)).hexdigest()


def enumerate_simple_paths(graph: dict[str, list[str]], start: str, goal: str, max_depth: int = 8) -> list[list[str]]:
    found: list[list[str]] = []
    stack: list[tuple[str, list[str]]] = [(start, [start])]
    while stack:
        node, path = stack.pop()
        if len(path) > max_depth:
            continue
        if node == goal:
            found.append(path)
            continue
        for nxt in sorted(graph.get(node, []), reverse=True):
            if nxt not in path:
                stack.append((nxt, path + [nxt]))
    return sorted(found, key=lambda p: (len(p), p))


def validate_region_graph(control_regions: list[dict[str, Any]]) -> tuple[dict[str, list[str]], list[str]]:
    graph = {r["region_id"]: list(r["neighbors"]) for r in control_regions}
    errors: list[str] = []
    for region_id, neighbors in graph.items():
        for neighbor in neighbors:
            if neighbor not in graph:
                errors.append(f"region {region_id} references missing neighbor {neighbor}")
                continue
            if region_id not in graph[neighbor]:
                errors.append(f"region graph is not bidirectional: {region_id} -> {neighbor}")
    return graph, errors


def sample_grid_cells(size_m: list[int], cell_size_m: int, polygons: list[dict[str, Any]], polygon_key: str) -> dict[str, list[list[int]]]:
    width, height = size_m
    cells_by_id: dict[str, list[list[int]]] = {p[polygon_key]: [] for p in polygons}
    for gy in range(math.ceil(height / cell_size_m)):
        cy = gy * cell_size_m + cell_size_m / 2
        for gx in range(math.ceil(width / cell_size_m)):
            cx = gx * cell_size_m + cell_size_m / 2
            for item in polygons:
                if point_in_polygon((cx, cy), item["polygon"]):
                    cells_by_id[item[polygon_key]].append([gx, gy])
    return cells_by_id


def compress_cells(cells: list[list[int]]) -> list[list[int]]:
    """Encode sorted grid cells as [row_y, start_x, end_x] inclusive spans."""
    if not cells:
        return []
    rows: dict[int, list[int]] = {}
    for gx, gy in cells:
        rows.setdefault(gy, []).append(gx)
    spans: list[list[int]] = []
    for gy in sorted(rows):
        xs = sorted(set(rows[gy]))
        start = prev = xs[0]
        for x in xs[1:]:
            if x == prev + 1:
                prev = x
                continue
            spans.append([gy, start, prev])
            start = prev = x
        spans.append([gy, start, prev])
    return spans


def sample_polyline(points: list[list[float]], spacing_m: float) -> list[list[int]]:
    samples: list[list[int]] = []
    for index in range(len(points) - 1):
        x1, y1 = points[index]
        x2, y2 = points[index + 1]
        length = euclidean((x1, y1), (x2, y2))
        steps = max(1, math.ceil(length / spacing_m))
        for step in range(steps):
            t = step / steps
            samples.append([round(x1 + (x2 - x1) * t), round(y1 + (y2 - y1) * t)])
    samples.append([round(points[-1][0]), round(points[-1][1])])
    deduped: list[list[int]] = []
    for point in samples:
        if not deduped or point != deduped[-1]:
            deduped.append(point)
    return deduped


def choose_initial_anchors(spawn: dict[str, Any]) -> list[dict[str, Any]]:
    cx, cy = spawn["center"]
    team = spawn["team_slot"]
    facing = math.radians(spawn["facing_deg"])
    forward = (math.cos(facing), math.sin(facing))
    right = (-forward[1], forward[0])
    layout = [
        ("COMMAND_CORE", 0, 0),
        ("POWER", -160, -120),
        ("REFINERY", 170, -120),
        ("GROUND_PRODUCTION", 0, 190),
        ("HARVESTER_RALLY", -230, 210),
        ("COMBAT_RALLY", 230, 210),
    ]
    anchors = []
    for role, rx, fy in layout:
        x = cx + right[0] * rx + forward[0] * fy
        y = cy + right[1] * rx + forward[1] * fy
        anchors.append({"role": role, "team_slot": team, "position": [round(x), round(y)]})
    return anchors


def nearest_distance(origin: list[float], positions: list[list[float]]) -> float:
    return min(euclidean(origin, p) for p in positions)


def fairness_metrics(map_data: dict[str, Any]) -> dict[str, Any]:
    spawns = {s["team_slot"]: s["center"] for s in map_data["spawn_sectors"]}
    industrial = {
        team: [n["position"] for n in map_data["resource_nodes"] if n["resource_class"] == "INDUSTRIAL" and n["control_region_id"] == f"REGION_HOME_{'A' if team == 1 else 'B'}"]
        for team in (1, 2)
    }
    strategic = {
        team: [n["position"] for n in map_data["resource_nodes"] if n["resource_class"] == "STRATEGIC" and n["control_region_id"] == f"REGION_HOME_{'A' if team == 1 else 'B'}"]
        for team in (1, 2)
    }
    center_sites = [s["position"] for s in map_data["strategic_sites"] if s["control_region_id"] == "REGION_CENTER"]
    metrics: dict[str, Any] = {"teams": {}}
    for team in (1, 2):
        metrics["teams"][str(team)] = {
            "nearest_home_industrial_m": round(nearest_distance(spawns[team], industrial[team]), 3),
            "nearest_home_strategic_m": round(nearest_distance(spawns[team], strategic[team]), 3),
            "center_site_m": round(nearest_distance(spawns[team], center_sites), 3),
        }
    diffs = {}
    for key in metrics["teams"]["1"]:
        a = metrics["teams"]["1"][key]
        b = metrics["teams"]["2"][key]
        denom = max(a, b, 1.0)
        diffs[key] = round(abs(a - b) / denom, 6)
    metrics["relative_differences"] = diffs
    metrics["max_relative_difference"] = max(diffs.values()) if diffs else 0.0
    return metrics


def validate_map(map_data: dict[str, Any], cell_size_m: int) -> list[str]:
    errors: list[str] = []
    if map_data.get("map_id") != "MAP_GRAY_RANGE":
        errors.append("this generator profile currently expects MAP_GRAY_RANGE")
    width, height = map_data["size_m"]
    if width % cell_size_m or height % cell_size_m:
        errors.append("map dimensions must be divisible by cell size")
    if len(map_data["spawn_sectors"]) != 2:
        errors.append("gray range requires exactly two spawn sectors")

    graph, graph_errors = validate_region_graph(map_data["control_regions"])
    errors.extend(graph_errors)
    home_by_team = {x["team_slot"]: x["region_id"] for x in map_data["home_core_regions"]}
    if set(home_by_team) != {1, 2}:
        errors.append("home core regions must define team slots 1 and 2")
    else:
        paths = enumerate_simple_paths(graph, home_by_team[1], home_by_team[2])
        if len(paths) < 2:
            errors.append("map requires at least two independent strategic region paths")

    road_ids = {r["road_id"] for r in map_data["roads"]}
    if len(road_ids) < 2:
        errors.append("map requires at least two road corridors")
    for node in map_data["resource_nodes"]:
        for road in node.get("logistics_links", []):
            if road not in road_ids:
                errors.append(f"resource {node['node_id']} references missing road {road}")

    build_by_team = {b["team_slot"]: b for b in map_data["buildable_polygons"]}
    for spawn in map_data["spawn_sectors"]:
        team = spawn["team_slot"]
        if team not in build_by_team:
            errors.append(f"team {team} missing buildable polygon")
            continue
        if not point_in_polygon(tuple(spawn["center"]), build_by_team[team]["polygon"]):
            errors.append(f"team {team} spawn center is outside buildable polygon")
        for anchor in choose_initial_anchors(spawn):
            if not point_in_polygon(tuple(anchor["position"]), spawn["initial_build_polygon"]):
                errors.append(f"team {team} initial anchor {anchor['role']} is outside initial build polygon")

    fairness = fairness_metrics(map_data)
    if fairness["max_relative_difference"] > 0.10:
        errors.append(f"spawn fairness exceeds 10%: {fairness['max_relative_difference']:.3f}")
    return errors


def generate_manifest(map_data: dict[str, Any], cell_size_m: int) -> dict[str, Any]:
    errors = validate_map(map_data, cell_size_m)
    if errors:
        raise ValueError("; ".join(errors))

    width, height = map_data["size_m"]
    build_cells = sample_grid_cells(map_data["size_m"], cell_size_m, map_data["buildable_polygons"], "build_id")
    region_cells = sample_grid_cells(map_data["size_m"], cell_size_m, map_data["control_regions"], "region_id")
    graph, _ = validate_region_graph(map_data["control_regions"])
    home_by_team = {x["team_slot"]: x["region_id"] for x in map_data["home_core_regions"]}
    strategic_paths = enumerate_simple_paths(graph, home_by_team[1], home_by_team[2])

    road_manifest = []
    for road in sorted(map_data["roads"], key=lambda r: r["road_id"]):
        road_manifest.append({
            "road_id": road["road_id"],
            "width_m": road["width_m"],
            "terrain_cost_multiplier": road["terrain_cost_multiplier"],
            "sampled_centerline": sample_polyline(road["spline_points"], cell_size_m),
        })

    initial_anchors = []
    for spawn in sorted(map_data["spawn_sectors"], key=lambda s: s["team_slot"]):
        initial_anchors.extend(choose_initial_anchors(spawn))

    manifest = {
        "graybox_schema_version": 1,
        "generator_version": GENERATOR_VERSION,
        "source_map_id": map_data["map_id"],
        "source_map_sha256": sha256_json(map_data),
        "cell_size_m": cell_size_m,
        "grid": {
            "width_cells": width // cell_size_m,
            "height_cells": height // cell_size_m,
            "playable_cell_count": (width // cell_size_m) * (height // cell_size_m),
        },
        "buildable": [
            {"build_id": build_id, "cell_count": len(cells), "cell_spans": compress_cells(cells)}
            for build_id, cells in sorted(build_cells.items())
        ],
        "control_regions": [
            {
                "region_id": region_id,
                "cell_count": len(cells),
                "neighbors": sorted(graph[region_id]),
                "cell_spans": compress_cells(cells),
            }
            for region_id, cells in sorted(region_cells.items())
        ],
        "strategic_paths": strategic_paths,
        "roads": road_manifest,
        "resource_anchors": [
            {"node_id": n["node_id"], "position": n["position"], "resource_class": n["resource_class"], "mineral_id": n["mineral_id"]}
            for n in sorted(map_data["resource_nodes"], key=lambda n: n["node_id"])
        ],
        "strategic_site_anchors": [
            {"site_id": s["site_id"], "site_type": s["site_type"], "position": s["position"]}
            for s in sorted(map_data["strategic_sites"], key=lambda s: s["site_id"])
        ],
        "initial_spawn_anchors": initial_anchors,
        "fairness": fairness_metrics(map_data),
    }
    manifest["manifest_sha256"] = sha256_json(manifest)
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate deterministic graybox manifest from a ModernRA map sidecar")
    parser.add_argument("--map", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--cell-size", type=int, default=DEFAULT_CELL_SIZE_M)
    args = parser.parse_args()

    map_data = json.loads(args.map.read_text(encoding="utf-8"))
    manifest = generate_manifest(map_data, args.cell_size)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print("GRAYBOX GENERATION PASSED")
    print(f"map={manifest['source_map_id']} grid={manifest['grid']['width_cells']}x{manifest['grid']['height_cells']} cell={manifest['cell_size_m']}m")
    print(f"paths={len(manifest['strategic_paths'])} roads={len(manifest['roads'])} anchors={len(manifest['initial_spawn_anchors'])}")
    print(f"fairness_max_relative_difference={manifest['fairness']['max_relative_difference']:.6f}")
    print(f"manifest_sha256={manifest['manifest_sha256']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
