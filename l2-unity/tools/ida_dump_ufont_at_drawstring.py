# IDA Pro 9 — dump helpers for UCanvas::Draw3DCoordText hit (lobby names)
#
# BP on:
#   UCanvas::Draw3DCoordText(FVector, ulong, ushort*, UTexture*, int, int, L2FontType, int)
# Prefer break at ENTRY (push ebp) OR after prologue at first real insn.
#
# This function does NOT take UFont* — it takes L2FontType.
# Glyph table lives in UFont resolved inside DrawTextToCanvas.
#
# Workflow:
#   A) Run this script on Draw3DCoordText hit → confirms nickname text
#   B) BP UFont::RemapChar → run script again in RemapChar mode → dumps UFont glyphs

import json
import os

import idaapi
import ida_bytes
import ida_dbg
import ida_idd
import ida_kernwin
import ida_name
import idc

UFONT_GLYPHS_PTR = 13
UFONT_GLYPH_COUNT = 14
UFONT_TEXTURES = 16
UFONT_PAGE_COUNT = 17
GLYPH_STRIDE = 20


def read_u32(ea):
    return ida_bytes.get_dword(ea) & 0xFFFFFFFF


def read_u8(ea):
    return ida_bytes.get_byte(ea) & 0xFF


def read_wchar_z(ea, max_chars=256):
    if not ea:
        return ""
    out = []
    for i in range(max_chars):
        c = ida_bytes.get_word(ea + i * 2) & 0xFFFF
        if c == 0:
            break
        out.append(chr(c))
    return "".join(out)


def dbg_ok(ea):
    try:
        ida_bytes.get_dword(ea)
        return ea >= 0x10000
    except Exception:
        return False


def find_name_ea(substrs):
    for i in range(ida_name.get_nlist_size()):
        name = ida_name.get_nlist_name(i) or ""
        if all(s in name for s in substrs):
            return ida_name.get_nlist_ea(i), name
    return idaapi.BADADDR, None


def dump_font(font_ea):
    glyphs_ptr = read_u32(font_ea + UFONT_GLYPHS_PTR * 4)
    glyph_count = read_u32(font_ea + UFONT_GLYPH_COUNT * 4)
    textures_ptr = read_u32(font_ea + UFONT_TEXTURES * 4)
    page_count = read_u32(font_ea + UFONT_PAGE_COUNT * 4)
    if glyph_count == 0 or glyph_count > 200000:
        raise RuntimeError("bad glyph_count=%d (wrong UFont* layout?)" % glyph_count)

    pages = []
    if textures_ptr and 0 < page_count < 64:
        for p in range(page_count):
            pages.append(read_u32(textures_ptr + 4 * p))

    glyphs = []
    for i in range(glyph_count):
        g = glyphs_ptr + i * GLYPH_STRIDE
        glyphs.append({
            "index": i,
            "startU": read_u32(g + 0),
            "startV": read_u32(g + 4),
            "uSize": read_u32(g + 8),
            "vSize": read_u32(g + 12),
            "page": read_u8(g + 16),
        })
    return {
        "font_ea": font_ea,
        "glyphs_ptr": glyphs_ptr,
        "glyph_count": glyph_count,
        "textures_ptr": textures_ptr,
        "page_count": page_count,
        "pages": pages,
        "glyphs": glyphs,
    }


def try_remap(font_ea, ch, remap_ea):
    if remap_ea == idaapi.BADADDR:
        return None
    try:
        tif = idaapi.tinfo_t()
        idaapi.parse_decl(
            tif, None,
            "unsigned short __thiscall f(void *font, unsigned short ch);", 0)
        return int(ida_idd.Appcall[remap_ea](font_ea, ch)) & 0xFFFF
    except Exception as e:
        print("Appcall RemapChar failed:", e)
        return None


def read_draw3d_args_at_entry():
    """__thiscall Draw3DCoordText — at ENTRY before push ebp."""
    esp = ida_dbg.get_reg_val("ESP")
    # [esp]=ret, +4+8+C=FVector, +10=color, +14=text*, +18=tex, +1C,+20 pad?, +24 fonttype...
    # retn 28h => 10 dwords args
    return {
        "x": read_u32(esp + 0x04),
        "y": read_u32(esp + 0x08),
        "z_bits": read_u32(esp + 0x0C),
        "color": read_u32(esp + 0x10),
        "text_ea": read_u32(esp + 0x14),
        "tex": read_u32(esp + 0x18),
        "a8": read_u32(esp + 0x1C),
        "a9": read_u32(esp + 0x20),
        "font_type": read_u32(esp + 0x24),
        "a11": read_u32(esp + 0x28),
        "layout": "entry_ESP",
    }


def read_draw3d_args_after_prologue():
    """After lea ebp,[esp-40h] frame used in this build: [ebp+40h+arg_N]."""
    ebp = ida_dbg.get_reg_val("EBP")
    # From listing: arg_10 = text, arg_C = color, arg_0/4/8 = vector, arg_14=tex,
    # arg_18, arg_20, arg_24 = font flags
    base = ebp + 0x40
    return {
        "x": read_u32(base + 0x00),
        "y": read_u32(base + 0x04),
        "z_bits": read_u32(base + 0x08),
        "color": read_u32(base + 0x0C),
        "text_ea": read_u32(base + 0x10),
        "tex": read_u32(base + 0x14),
        "a8": read_u32(base + 0x18),
        "a9": read_u32(base + 0x1C),
        "font_type": read_u32(base + 0x20),
        "a11": read_u32(base + 0x24),
        "layout": "ebp+40",
    }


def mode_draw3d():
    eip = ida_dbg.get_reg_val("EIP")
    print("EIP=%08X" % eip)

    args = read_draw3d_args_at_entry()
    text = read_wchar_z(args["text_ea"])
    if not text:
        args = read_draw3d_args_after_prologue()
        text = read_wchar_z(args["text_ea"])

    print("layout=%s text_ea=%08X text='%s'" % (args["layout"], args["text_ea"], text))
    print("color=%08X tex=%08X font_type=%d a8=%d" % (
        args["color"], args["tex"], args["font_type"], args["a8"]))

    if not text:
        ida_kernwin.warning(
            "Could not read nickname string.\n"
            "Break at ENTRY (push ebp) of Draw3DCoordText and re-run.")
        return

    out_dir = ida_kernwin.ask_str(
        os.path.join(os.path.dirname(idc.get_idb_path() or "."), "ufont_dump"),
        0, "Output folder")
    if not out_dir:
        return
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "draw3d_hit.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"text": text, "args": args, "eip": eip}, f, indent=2, ensure_ascii=False)
    print("wrote", path)
    print("")
    print("NEXT: set BP on UFont::RemapChar, continue until hit while this name draws,")
    print("      then run this script again and choose RemapChar mode.")
    ida_kernwin.info("Draw3D hit OK\ntext='%s'\n%s" % (text, path))


def mode_remapchar():
    # __thiscall RemapChar(this=UFont*, ch)
    font_ea = ida_dbg.get_reg_val("ECX")
    # ch may be stack [esp+4] at entry
    esp = ida_dbg.get_reg_val("ESP")
    ch = read_u32(esp + 4) & 0xFFFF

    print("RemapChar: UFont*=%08X ch=%d ('%s')" % (
        font_ea, ch, chr(ch) if 32 <= ch < 127 else "?"))

    if not dbg_ok(font_ea):
        ida_kernwin.warning("ECX does not look like UFont*")
        return

    info = dump_font(font_ea)
    remap_ea, remap_name = find_name_ea(["RemapChar"])
    print("glyph_count=%d pages=%s remap=%s" % (
        info["glyph_count"],
        ["%08X" % p for p in info["pages"]],
        remap_name or "None"))

    ascii_rows = []
    for c in range(32, 127):
        idx = try_remap(font_ea, c, remap_ea)
        row = {"char": chr(c), "code": c, "glyphIndex": idx}
        if idx is not None and 0 <= idx < len(info["glyphs"]):
            g = info["glyphs"][idx]
            row.update(g)
        ascii_rows.append(row)

    out_dir = ida_kernwin.ask_str(
        os.path.join(os.path.dirname(idc.get_idb_path() or "."), "ufont_dump"),
        0, "Output folder")
    if not out_dir:
        return
    os.makedirs(out_dir, exist_ok=True)

    payload = {
        "font": {
            "ea": info["font_ea"],
            "glyph_count": info["glyph_count"],
            "page_count": info["page_count"],
            "pages": ["0x%08X" % p for p in info["pages"]],
        },
        "glyphs": info["glyphs"],
        "ascii": ascii_rows,
    }
    json_path = os.path.join(out_dir, "ufont_glyphs.json")
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2, ensure_ascii=False)

    csv_path = os.path.join(out_dir, "ufont_ascii.csv")
    with open(csv_path, "w", encoding="utf-8") as f:
        f.write("char,code,glyphIndex,startU,startV,uSize,vSize,page\n")
        for r in ascii_rows:
            f.write("%s,%s,%s,%s,%s,%s,%s,%s\n" % (
                r.get("char", ""),
                r.get("code", ""),
                "" if r.get("glyphIndex") is None else r["glyphIndex"],
                r.get("startU", ""),
                r.get("startV", ""),
                r.get("uSize", ""),
                r.get("vSize", ""),
                r.get("page", ""),
            ))

    print("wrote", json_path)
    print("wrote", csv_path)
    print("Export atlas from RenderDoc for page texture; send csv/json here.")
    ida_kernwin.info("UFont dump OK\n%d glyphs\n%s" % (info["glyph_count"], json_path))


def main():
    if not ida_dbg.is_debugger_on():
        ida_kernwin.warning("Debugger not active")
        return

    choice = ida_kernwin.ask_buttons(
        "Draw3DCoordText",
        "RemapChar / UFont",
        "Cancel",
        1,
        "Where are you stopped?")
    if choice == 1:
        mode_draw3d()
    elif choice == 0:
        mode_remapchar()


if __name__ == "__main__":
    main()
