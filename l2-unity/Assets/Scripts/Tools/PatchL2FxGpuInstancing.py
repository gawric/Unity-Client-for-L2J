#!/usr/bin/env python3
"""Add ParticleGroup GPU instancing hooks to L2 HLSL skill shaders. Idempotent."""
from __future__ import annotations

import os
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Resources" / "Data" / "Shaders" / "Skills"
COMMON_INCLUDE = ROOT / "Common" / "L2FxInstancing.hlsl"
SKIP_NAMES = {"L2RibbonStrip.shader"}


def include_path_for(shader: Path) -> str:
    rel = os.path.relpath(COMMON_INCLUDE, shader.parent)
    return rel.replace("\\", "/")


def patch_subshader_tags(text: str) -> str:
    if "L2FxGpuInstancing" in text:
        return text

    def repl_multiline(match: re.Match[str]) -> str:
        block = match.group(0)
        if "L2FxGpuInstancing" in block:
            return block
        return block[:-1] + '            "L2FxGpuInstancing" = "On"\n        }'

    text2, n = re.subn(
        r"Tags\s*\n\s*\{[^{}]*\}",
        repl_multiline,
        text,
        count=1,
    )
    if n:
        return text2

    def repl_single(match: re.Match[str]) -> str:
        inner = match.group(1).rstrip()
        if "L2FxGpuInstancing" in inner:
            return match.group(0)
        return 'Tags { ' + inner + ' "L2FxGpuInstancing" = "On" }'

    return re.sub(r"Tags\s*\{\s*([^}]*)\}", repl_single, text, count=1)


def patch_pragma(text: str) -> str:
    if "multi_compile_instancing" in text:
        return text
    return re.sub(
        r"(#pragma\s+fragment\s+frag\s*\n)",
        r"\1            #pragma multi_compile_instancing\n",
        text,
        count=1,
    )


def patch_attributes(text: str) -> str:
    if "UNITY_VERTEX_INPUT_INSTANCE_ID" in text:
        return text

    def repl(match: re.Match[str]) -> str:
        body = match.group(1)
        indent = "                "
        if not body.endswith("\n"):
            body += "\n"
        return (
            "struct Attributes\n"
            + match.group(0)[len("struct Attributes"):match.group(0).index("{") + 1]
            + "\n"
            + body
            + indent
            + "UNITY_VERTEX_INPUT_INSTANCE_ID\n"
            + "            };"
        )

    # Safer: insert before the first Attributes closing brace.
    pattern = re.compile(
        r"struct Attributes\s*\{(.*?)^\s*\};",
        re.DOTALL | re.MULTILINE,
    )
    match = pattern.search(text)
    if not match:
        return text
    body = match.group(1).rstrip() + "\n                UNITY_VERTEX_INPUT_INSTANCE_ID\n"
    return text[: match.start(1)] + body + text[match.end(1) :]


def patch_include(text: str, include_rel: str) -> str:
    if "L2FxInstancing.hlsl" in text:
        return text
    needle = "CBUFFER_END"
    idx = text.find(needle)
    if idx < 0:
        return text
    insert_at = idx + len(needle)
    snippet = f'\n\n            #include "{include_rel}"'
    return text[:insert_at] + snippet + text[insert_at:]


def patch_vert(text: str) -> str:
    if "UNITY_SETUP_INSTANCE_ID(" in text:
        return text

    match = re.search(
        r"Varyings\s+vert\s*\(\s*Attributes\s+(\w+)\s*\)\s*\n\s*\{",
        text,
    )
    if not match:
        return text
    param = match.group(1)
    insert_at = match.end()
    return text[:insert_at] + f"\n                UNITY_SETUP_INSTANCE_ID({param});" + text[insert_at:]


def patch_file(path: Path) -> bool:
    original = path.read_text(encoding="utf-8")
    text = original
    text = patch_subshader_tags(text)
    text = patch_pragma(text)
    text = patch_attributes(text)
    text = patch_include(text, include_path_for(path))
    text = patch_vert(text)
    if text == original:
        return False
    path.write_text(text, encoding="utf-8", newline="\n")
    return True


def main() -> None:
    changed = []
    skipped = []
    for path in sorted(ROOT.rglob("*.shader")):
        if path.name in SKIP_NAMES:
            skipped.append(path.name)
            continue
        if patch_file(path):
            changed.append(str(path.relative_to(ROOT)))
        elif "L2FxInstancing.hlsl" in path.read_text(encoding="utf-8"):
            skipped.append(path.name + " (already)")
        else:
            skipped.append(path.name + " (no change)")
    print(f"patched={len(changed)}")
    for name in changed:
        print("  " + name)
    print(f"skipped={len(skipped)}")
    for name in skipped:
        print("  " + name)


if __name__ == "__main__":
    main()
