"""
Find Lineage II StaticMesh (.pskx) assets that match a RenderDoc mesh draw call.

Typical workflow:
  1. Capture draw call in RenderDoc (note index count → triangles = indices / 3).
  2. Export VS output to CSV (columns VTX, IDX, out_Texcoord0.x/y, ...).
  3. Run this script against unpacked umodel PSKX folder.

See docs/renderdoc-pskx-mesh-finder.md for full instructions.
"""
from __future__ import annotations

import argparse
import csv
import re
import struct
from collections import defaultdict
from pathlib import Path

UV_TOLERANCE = 0.025


def tri_count(path: Path) -> int | None:
    data = path.read_bytes()
    foff = data.find(b"FACE0000")
    if foff < 0:
        return None
    return struct.unpack_from("<i", data, foff + 28)[0]


def faces_by_material(path: Path) -> dict[int, int]:
    data = path.read_bytes()
    foff = data.find(b"FACE0000")
    if foff < 0:
        return {}
    fn = struct.unpack_from("<i", data, foff + 28)[0]
    base = foff + 32
    mats: dict[int, int] = {}
    for i in range(fn):
        _, _, _, mat, _ = struct.unpack_from("<3HHH", data, base + i * 12)
        mats[mat] = mats.get(mat, 0) + 1
    return mats


def uv_signature(path: Path, material: int | None = None) -> set[tuple[float, float]]:
    data = path.read_bytes()
    woff, foff = data.find(b"VTXW0000"), data.find(b"FACE0000")
    if woff < 0 or foff < 0:
        return set()
    wn = struct.unpack_from("<i", data, woff + 28)[0]
    fn = struct.unpack_from("<i", data, foff + 28)[0]
    wedges: list[tuple[float, float]] = []
    base = woff + 32
    for i in range(wn):
        _, u, v = struct.unpack_from("<Iff", data, base + i * 16)
        wedges.append((round(u, 3), round(v, 3)))
    used: set[int] = set()
    base = foff + 32
    for i in range(fn):
        w0, w1, w2, mat, _ = struct.unpack_from("<3HHH", data, base + i * 12)
        if material is not None and mat != material:
            continue
        used.update([w0, w1, w2])
    return {wedges[w] for w in used}


def _find_column(fieldnames: list[str], needle: str) -> str:
    for col in fieldnames:
        if needle in col.replace(" ", ""):
            return col
    raise ValueError(
        f"CSV missing column containing '{needle}'. "
        f"Export VS output from RenderDoc (out_Texcoord0.x/y). "
        f"Got columns: {fieldnames}"
    )


def load_renderdoc_uv(csv_path: Path) -> tuple[int, set[tuple[float, float]]]:
    rows = list(csv.DictReader(csv_path.open(encoding="utf-8")))
    if not rows:
        raise ValueError(f"CSV is empty: {csv_path}")
    u_key = _find_column(list(rows[0].keys()), "Texcoord0.x")
    v_key = _find_column(list(rows[0].keys()), "Texcoord0.y")
    rd_uv = {
        (round(float(r[u_key]), 3), round(float(r[v_key]), 3))
        for r in rows
    }
    return len(rows), rd_uv


def count_uv_hits(mesh_uv: set[tuple[float, float]], rd_uv: set[tuple[float, float]]) -> int:
    return sum(
        1
        for ru in rd_uv
        if any(
            abs(ru[0] - mu[0]) < UV_TOLERANCE and abs(ru[1] - mu[1]) < UV_TOLERANCE
            for mu in mesh_uv
        )
    )


def scan_meshes(pskx_dir: Path, rd_uv: set[tuple[float, float]], min_hits: int) -> list[dict]:
    results = []
    rd_n = len(rd_uv)
    for f in sorted(pskx_dir.glob("*.pskx")):
        data = f.read_bytes()
        foff = data.find(b"FACE0000")
        if foff < 0:
            continue
        tris = struct.unpack_from("<i", data, foff + 28)[0]
        poff = data.find(b"PNTS0000")
        verts = struct.unpack_from("<i", data, poff + 28)[0] if poff >= 0 else 0

        full_hits = count_uv_hits(uv_signature(f), rd_uv)
        if full_hits < min_hits:
            continue

        mat_breakdown = []
        for mat, mat_tris in sorted(faces_by_material(f).items()):
            mat_hits = count_uv_hits(uv_signature(f, material=mat), rd_uv)
            if mat_hits > 0:
                mat_breakdown.append(
                    {"mat": mat, "tris": mat_tris, "uv_hits": mat_hits}
                )

        results.append(
            {
                "name": f.name,
                "stem": f.stem,
                "verts": verts,
                "tris": tris,
                "uv_hits": full_hits,
                "rd_uv_count": rd_n,
                "materials": mat_breakdown,
            }
        )
    return sorted(results, key=lambda x: (-x["uv_hits"], x["tris"]))


def print_uc_refs(uc_root: Path | None, stems: set[str]) -> None:
    if uc_root is None or not uc_root.is_dir():
        return
    pat = re.compile(r"StaticMesh'([^']+)'")
    refs: dict[str, list[str]] = defaultdict(list)
    for uc in uc_root.rglob("*.uc"):
        text = uc.read_text(encoding="utf-8", errors="ignore")
        for m in pat.finditer(text):
            base = m.group(1).split(".")[-1]
            if base in stems:
                refs[base].append(uc.relative_to(uc_root).as_posix())
    if not refs:
        return
    print("\nUC scripts referencing matched mesh names:")
    for base in sorted(refs.keys()):
        print(f"  {base} — {len(refs[base])} UC file(s)")
        for r in refs[base][:3]:
            print(f"    {r}")
        if len(refs[base]) > 3:
            print(f"    ... +{len(refs[base]) - 3} more")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Match RenderDoc VS CSV UV fingerprint to unpacked .pskx meshes."
    )
    parser.add_argument(
        "--csv",
        type=Path,
        required=True,
        help="RenderDoc mesh VS export CSV (out_Texcoord0.x/y columns).",
    )
    parser.add_argument(
        "--pskx-dir",
        type=Path,
        required=True,
        help="Folder with umodel-exported LineageEffectsStaticmeshes *.pskx files.",
    )
    parser.add_argument(
        "--uc-root",
        type=Path,
        default=None,
        help="Optional: UnrealScript root to list .uc files referencing matched mesh names.",
    )
    parser.add_argument(
        "--min-hits",
        type=int,
        default=8,
        help="Minimum UV corner matches (of unique RenderDoc UVs) to report. Default: 8.",
    )
    parser.add_argument(
        "--expected-tris",
        type=int,
        default=None,
        help="Optional: highlight meshes (or material slots) with this triangle count.",
    )
    args = parser.parse_args()

    if not args.csv.is_file():
        raise SystemExit(f"CSV not found: {args.csv}")
    if not args.pskx_dir.is_dir():
        raise SystemExit(f"PSKX directory not found: {args.pskx_dir}")

    vs_rows, rd_uv = load_renderdoc_uv(args.csv)
    rd_tris = vs_rows // 3
    print(f"RenderDoc CSV: {args.csv.name}")
    print(f"  VS rows: {vs_rows}  →  ~{rd_tris} triangles (rows / 3)")
    print(f"  Unique UV corners: {len(rd_uv)}")
    if rd_uv:
        us = [u for u, _ in rd_uv]
        vs = [v for _, v in rd_uv]
        print(f"  UV box: U {min(us):.3f}–{max(us):.3f}, V {min(vs):.3f}–{max(vs):.3f}")

    matches = scan_meshes(args.pskx_dir, rd_uv, args.min_hits)
    if not matches:
        print(f"\nNo meshes with >={args.min_hits}/{len(rd_uv)} UV hits.")
        print("Try lowering --min-hits or re-export CSV from the correct draw call.")
        return

    print(f"\nMatches (>={args.min_hits}/{len(rd_uv)} UV hits):")
    for m in matches:
        flag = ""
        if args.expected_tris is not None and m["tris"] == args.expected_tris:
            flag = "  [total tris match]"
        print(
            f"  {m['uv_hits']:2d}/{len(rd_uv)} UV  tris={m['tris']:4d}  verts={m['verts']:4d}  "
            f"{m['name']}{flag}"
        )
        for slot in m["materials"]:
            slot_flag = ""
            if args.expected_tris is not None and slot["tris"] == args.expected_tris:
                slot_flag = "  ← material slot matches expected tris"
            if slot["uv_hits"] >= args.min_hits or slot["tris"] == args.expected_tris:
                print(
                    f"      mat {slot['mat']}: {slot['tris']} tris, "
                    f"{slot['uv_hits']}/{len(rd_uv)} UV hits{slot_flag}"
                )

    best = matches[0]
    print("\nBest candidate:")
    print(f"  File: {best['name']}")
    print(f"  UE path: LineageEffectsStaticmeshes.*.{best['stem']}")
    perfect_slots = [s for s in best["materials"] if s["uv_hits"] == len(rd_uv)]
    if perfect_slots:
        s = perfect_slots[0]
        print(
            f"  Use material slot {s['mat']} ({s['tris']} tris) if full mesh has more triangles."
        )

    print_uc_refs(args.uc_root, {m["stem"] for m in matches})


if __name__ == "__main__":
    main()
