# IDA Pro 9 — find Draw3DCoordText callers in open nwindow.dll IDB
#
# File -> Script file... (nwindow.dll already open)
# No debugger addresses required for the xref path.

import idaapi
import ida_funcs
import ida_kernwin
import ida_name
import ida_nalt
import ida_bytes
import ida_ua
import idc

NEEDLES = (
    "Draw3DCoordText",
    "Draw3DCoord",
)


def imagebase():
    return idaapi.get_imagebase()


def iter_names_matching():
    for i in range(ida_name.get_nlist_size()):
        name = ida_name.get_nlist_name(i)
        if not name:
            continue
        for needle in NEEDLES:
            if needle in name:
                yield name, ida_name.get_nlist_ea(i)
                break


def collect_xrefs_to(ea):
    out = []
    xb = idaapi.xrefblk_t()
    ok = xb.first_to(ea, 0)
    while ok:
        out.append(xb.frm)
        ok = xb.next_to()
    return out


def func_of(ea):
    f = ida_funcs.get_func(ea)
    if not f:
        return None
    return f.start_ea, ida_funcs.get_func_name(f.start_ea) or "<no name>"


def find_import_plt_stubs():
    """Also scan imports segment for Draw3DCoordText."""
    hits = []
    nimps = ida_nalt.get_import_module_qty()
    for m in range(nimps):
        mod = ida_nalt.get_import_module_name(m) or ""

        def cb(ea, name, ord_):
            if name and any(n in name for n in NEEDLES):
                hits.append((mod, name, ea))
            return True

        ida_nalt.enum_import_names(m, cb)
    return hits


def ask_hex(prompt, default=None):
    default_s = "%X" % default if default is not None else ""
    s = ida_kernwin.ask_str(default_s, 0, prompt)
    if not s:
        return None
    s = s.strip().lower().replace("0x", "").replace("h", "")
    return int(s, 16)


def optional_va_convert():
    """Only if you still have debugger numbers."""
    if not ida_kernwin.ask_yn(idaapi.ASKBTN_NO,
                             "Also convert a debugger return VA?\n"
                             "(No = only use IDB xrefs — recommended)"):
        return

    # IDA imagebase is NOT runtime ASLR base.
    print("NOTE: IDA imagebase=%08X is IDB preferred base, NOT process ASLR base."
          % imagebase())
    rb = ask_hex("Runtime nwindow.dll BASE from debugger Modules")
    if rb is None:
        return
    rv = ask_hex("Runtime RETURN VA ([ESP] at Draw3DCoordText)")
    if rv is None:
        return
    rva = rv - rb
    ea = imagebase() + rva
    print("VA convert: runtime %08X -> RVA %08X -> IDA %08X" % (rv, rva, ea))
    fi = func_of(ea)
    if fi:
        print("  function %s @ %08X" % (fi[1], fi[0]))
        idc.jumpto(fi[0])
    else:
        idc.jumpto(ea)


def main():
    print("=" * 60)
    print("nwindow Draw3DCoordText caller finder")
    print("file       = %s" % ida_nalt.get_root_filename())
    print("imagebase  = %08X  (IDB base; fine for xrefs)" % imagebase())
    print("=" * 60)

    callers = {}  # func_start -> (name, [xref_eas])

    # 1) named symbols
    for name, ea in iter_names_matching():
        print("symbol: %s @ %08X" % (name, ea))
        for frm in collect_xrefs_to(ea):
            fi = func_of(frm)
            key = fi[0] if fi else frm
            nm = fi[1] if fi else "<no func>"
            callers.setdefault(key, (nm, []))
            callers[key][1].append(frm)

    # 2) imports
    imps = find_import_plt_stubs()
    if imps:
        print("--- imports ---")
        for mod, name, ea in imps:
            print("import: %s!%s @ %08X" % (mod, name, ea))
            for frm in collect_xrefs_to(ea):
                fi = func_of(frm)
                key = fi[0] if fi else frm
                nm = fi[1] if fi else "<no func>"
                callers.setdefault(key, (nm, []))
                callers[key][1].append(frm)
    else:
        print("(no import name matched — binary may use ordinal/indirect call)")

    print("")
    print("=== CALLERS (decompile these) ===")
    if not callers:
        print("None found via name/import.")
        print("Fallback: use debugger return VA conversion (dialog next),")
        print("or search for: call near ptr .*Draw3DCoordText")
    else:
        for start, (nm, xrefs) in sorted(callers.items(), key=lambda x: x[0]):
            uniq = sorted(set(xrefs))
            print("FUNC %08X  %s" % (start, nm))
            for x in uniq:
                print("     call site %08X" % x)
        # jump to first caller
        first = sorted(callers.keys())[0]
        idc.jumpto(first)
        print("")
        print(">>> paste into chat <<<")
        for start, (nm, _) in sorted(callers.items(), key=lambda x: x[0]):
            print("CALLER_FUNC=%08X NAME=%s" % (start, nm))
        print("Open first caller and press F5, send decompile.")

    optional_va_convert()


if __name__ == "__main__":
    main()
