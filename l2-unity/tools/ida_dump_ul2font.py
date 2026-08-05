# IDA Pro 9 — final UL2Font ASCII glyph dump
#
# Lookup (sub_F8D7A50):
#   first = page[1], count = page[2], glyphs = page[3]
#   if first <= ch < first+count:
#       return glyphs + 16 * (ch - first)
#
# Glyph (16 bytes) — likely:
#   +0 StartU, +4 USize(w), +8 StartV, +0C VSize(h)
#   (GetGlyph requires +4>0 and +0C>0)
#
# Hit E2E180 EDI=UL2Font*, run this. Or set FONT_EA.

import json
import os
import struct

import ida_bytes
import ida_dbg
import ida_kernwin
import idc

FONT_EA = 0  # 0 = read EDI


def ru32(ea):
    return ida_bytes.get_dword(ea) & 0xFFFFFFFF


def ri32(ea):
    return struct.unpack("<i", struct.pack("<I", ru32(ea)))[0]


def main():
    if not ida_dbg.is_debugger_on():
        ida_kernwin.warning("Debugger not active")
        return

    font = FONT_EA or ida_dbg.get_reg_val("EDI")
    print("UL2Font*=%08X" % font)

    aw, ah = ru32(font + 0x40), ru32(font + 0x44)
    pages = ru32(font + 0x4C)
    page_count = ru32(font + 0x48)
    print("atlas=%dx%d pages=%08X page_count=%d" % (aw, ah, pages, page_count))

    if not pages or page_count < 1 or page_count > 64:
        ida_kernwin.warning("bad page list")
        return

    all_rows = []
    pages_out = []

    for pi in range(page_count):
        page = pages + pi * 0x10
        d0, first, count, glyphs = ru32(page), ru32(page + 4), ru32(page + 8), ru32(page + 12)
        print("page[%d] @%08X d0=%08X first=%d count=%d glyphs=%08X" % (
            pi, page, d0, first, count, glyphs))
        pages_out.append({
            "index": pi, "ea": page, "d0": d0,
            "first": first, "count": count, "glyphs": glyphs,
        })

        if not glyphs or count <= 0 or count > 0x10000:
            continue

        # dump all glyphs in this page range that fall in printable ASCII interest
        # also dump full page table for Unity
        for i in range(count):
            ch = first + i
            ge = glyphs + 16 * i
            u, w, v, h = ri32(ge), ri32(ge + 4), ri32(ge + 8), ri32(ge + 12)
            row = {
                "page": pi,
                "char": chr(ch) if 32 <= ch < 127 else "",
                "code": ch,
                "u": u, "v": v, "w": w, "h": h,
                "ok": w > 0 and h > 0,
            }
            all_rows.append(row)

    # ASCII subset summary
    ascii_ok = [r for r in all_rows if 32 <= r["code"] < 127 and r["ok"]]
    print("total glyphs=%d ascii_ok=%d" % (len(all_rows), len(ascii_ok)))
    for r in ascii_ok[0:10]:
        print("  '%s' U=%d V=%d W=%d H=%d" % (r["char"], r["u"], r["v"], r["w"], r["h"]))

    out_dir = ida_kernwin.ask_str(
        os.path.join(os.path.dirname(idc.get_idb_path() or "."), "ufont_dump"),
        0, "Output folder")
    if not out_dir:
        return
    os.makedirs(out_dir, exist_ok=True)

    payload = {
        "font": font,
        "atlas_w": aw,
        "atlas_h": ah,
        "pages": pages_out,
        "glyphs": all_rows,
        "layout": "StartU,USize,StartV,VSize (16 bytes)",
    }
    jp = os.path.join(out_dir, "ul2font_glyphs_final.json")
    with open(jp, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2, ensure_ascii=False)

    cp = os.path.join(out_dir, "ul2font_ascii.csv")
    with open(cp, "w", encoding="utf-8") as f:
        f.write("char,code,u,v,w,h,page\n")
        for r in all_rows:
            if 32 <= r["code"] < 127:
                f.write("%(char)s,%(code)d,%(u)d,%(v)d,%(w)d,%(h)d,%(page)d\n" % r)

    fp = os.path.join(out_dir, "ul2font_all.csv")
    with open(fp, "w", encoding="utf-8") as f:
        f.write("code,char,u,v,w,h,page,ok\n")
        for r in all_rows:
            f.write("%(code)d,%(char)s,%(u)d,%(v)d,%(w)d,%(h)d,%(page)d,%(ok)s\n" % r)

    print("wrote", jp)
    print("wrote", cp)
    print("wrote", fp)
    print("Also export 1024x128 atlas PNG from RenderDoc.")
    ida_kernwin.info("Glyph dump OK\nascii_ok=%d\n%s" % (len(ascii_ok), cp))


if __name__ == "__main__":
    main()
