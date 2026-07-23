# -*- coding: utf-8 -*-
# IDA Pro debugger script — APawn::AssociateAttackedNotify dump.
#
# BP on Engine.dll:
#   entry: AssociateAttackedNotify / sub_FF7310 (push ebp)
#   spawn: FF75A8 (+6E8), FF760B (StaticLoadClass), FF767A (table SpawnActor)
#
# Run: File → Script file… while stopped. Paste Output window into chat.

import struct
import idc
import ida_segment

ASSOCIATE_BODY_EA_HINT = 0x00FF7310
ASSOCIATE_THUNK_EA_HINT = 0x00CE0BC2

# RVAs for Engine.dll (base 0xCD0000 this session → entry 0xFF7310).
REL_ASSOCIATE_ENTRY = 0x327310
REL_SPAWN_6E8 = 0x3275A8
REL_STATIC_LOAD_TABLE = 0x32760B
REL_SPAWN_TABLE = 0x32767A
REL_HARDCODE_P_U004 = 0x327811
MILESTONE_TOL = 2

OFF_LOCATION = 0x1BC
OFF_EFFECT_CLASS = 0x6E8
OFF_ATTACKER_FLAGS = 0x1820

# Interlude UObject (AutoLogin EngineSDK):
#   Class @ +0x0C, Name(FName) @ +0x10, Outer @ +0x18
OFF_UOBJECT_CLASS = 0x0C
OFF_UOBJECT_NAME = 0x10
OFF_UOBJECT_OUTER = 0x18
NAME_PROBES = (0x10, 0x2C, 0x20)

# GNames TArray<FNameEntry*> at core.dll + 0x227AE0
CORE_GNAMES_RVA = 0x227AE0
# FNameEntry: Flags(+0), Index(+4), NextHash(+8), Name wchar(+0xC)
FNAME_ENTRY_NAME_OFF = 0x0C


def read_bytes(addr, size):
    data = idc.read_dbg_memory(addr, size)
    if data is None or len(data) != size:
        raise RuntimeError("Cannot read 0x%08X (size=%d)" % (addr, size))
    return data


def read_u32(addr):
    return struct.unpack("<I", read_bytes(addr, 4))[0]


def read_u8(addr):
    return struct.unpack("<B", read_bytes(addr, 1))[0]


def try_u32(addr):
    try:
        return read_u32(addr)
    except RuntimeError:
        return None


def try_u8(addr):
    try:
        return read_u8(addr)
    except RuntimeError:
        return None


def get_seg_base(name_substr):
    name_substr = name_substr.lower()
    qty = ida_segment.get_segm_qty()
    for i in range(qty):
        seg = ida_segment.getnseg(i)
        seg_name = ida_segment.get_segm_name(seg).lower()
        if name_substr in seg_name:
            return seg.start_ea
    return None


def read_wchar_z(addr, max_chars=128):
    if not addr or addr == 0xFFFFFFFF:
        return None
    chars = []
    for i in range(max_chars):
        try:
            raw = read_bytes(addr + i * 2, 2)
        except RuntimeError:
            break
        code = struct.unpack("<H", raw)[0]
        if code == 0:
            break
        if 32 <= code < 127:
            chars.append(chr(code))
        else:
            chars.append("?")
    return "".join(chars) if chars else None


def gnames_lookup(name_index):
    if name_index is None or name_index <= 0 or name_index >= 0x100000:
        return None
    core_base = get_seg_base("core")
    if core_base is None:
        return None
    names_array = core_base + CORE_GNAMES_RVA
    data = try_u32(names_array)
    count = try_u32(names_array + 4)
    if not data:
        return None
    ceiling = count if (count and 0 < count < 0x400000) else 0x100000
    if name_index >= ceiling:
        return None
    entry = try_u32(data + name_index * 4)
    if not entry:
        return None
    for off in (FNAME_ENTRY_NAME_OFF, 0x8, 0x10):
        s = read_wchar_z(entry + off)
        if s and len(s) >= 2:
            return s
    return None


def resolve_uobject_name(obj):
    if not obj or obj == 0xFFFFFFFF:
        return None, None
    for off in NAME_PROBES:
        idx = try_u32(obj + off)
        name = gnames_lookup(idx)
        if name:
            return name, idx
    return None, try_u32(obj + OFF_UOBJECT_NAME)


def uobject_name(obj):
    if not obj or obj == 0xFFFFFFFF:
        return "<null>"
    name_str, name_idx = resolve_uobject_name(obj)
    cls = try_u32(obj + OFF_UOBJECT_CLASS)
    outer = try_u32(obj + OFF_UOBJECT_OUTER)
    outer_name, _ = resolve_uobject_name(outer) if outer else (None, None)
    return "obj=0x%08X name=%s nameIdx=%s class=%s outer=%s" % (
        obj,
        ("'%s'" % name_str) if name_str else "?",
        ("0x%X" % name_idx) if name_idx is not None else "?",
        ("0x%08X" % cls) if cls is not None else "?",
        ("'%s'" % outer_name) if outer_name else (
            ("0x%08X" % outer) if outer else "?"),
    )


def resolve_arg_base(eip, esp, ebp):
    if ebp and esp < ebp < esp + 0x200:
        if try_u32(ebp + 4) is not None:
            return "EBP+8 (frame ready)", ebp + 8
    if (eip & 0xFFFFFFFF) in (ASSOCIATE_BODY_EA_HINT, ASSOCIATE_THUNK_EA_HINT):
        return "ESP+4 (entry)", esp + 4
    ret = try_u32(esp)
    engine_base = get_seg_base("engine")
    if ret is not None and engine_base is not None:
        if engine_base <= ret < engine_base + 0x800000:
            a0 = try_u32(esp + 4)
            if a0 and a0 > 0x10000:
                return "ESP+4 (ret on stack)", esp + 4
            a0b = try_u32(esp + 8)
            if a0b and a0b > 0x10000:
                return "ESP+8 (after push ebp)", esp + 8
    return "ESP+4 (fallback)", esp + 4


def detect_milestone(eip, engine_base):
    if engine_base is None:
        return "UNKNOWN", None
    rel = (eip - engine_base) & 0xFFFFFFFF
    table = {
        "ENTRY": REL_ASSOCIATE_ENTRY,
        "SPAWN_6E8": REL_SPAWN_6E8,
        "STATIC_LOAD_TABLE": REL_STATIC_LOAD_TABLE,
        "SPAWN_TABLE": REL_SPAWN_TABLE,
        "HARDCODE_P_U004": REL_HARDCODE_P_U004,
    }
    for name, target in table.items():
        if abs(rel - target) <= MILESTONE_TOL:
            return name, rel
    if abs(rel - REL_ASSOCIATE_ENTRY) < 0x700:
        return "ASSOCIATE_BODY", rel
    return "UNKNOWN", rel


def predict_branch(arg_14, arg_18, arg_1C, arg_20, arg_24, arg_28,
                   effect_class, attacker_flags):
    lines = []
    if arg_1C != 0 and arg_28 <= 5:
        lines.append(
            "BRANCH: weapon FX TABLE  (arg_1C != 0, index=arg_28=%d)" % arg_28
        )
        if effect_class and arg_20 == 0 and arg_24 != 0:
            lines.append(
                "  -> FIRST spawn [this+6E8]=0x%08X, then table" % effect_class
            )
    elif arg_18 != 0 and arg_14 == 0 and arg_24 != 0:
        lines.append("BRANCH: crit path -> may spawn Lineageeffect.p_u004_a")
    elif arg_24 != 0:
        lines.append("BRANCH: default hit (loc_FF7884)")
        if attacker_flags is not None and (attacker_flags & 4):
            lines.append("  -> it_zariche_ta")
        elif effect_class:
            lines.append("  -> SpawnActor [this+6E8]=0x%08X" % effect_class)
    else:
        lines.append("BRANCH: arg_24 == 0 -> many spawn paths skipped")
    return lines


def dump_spawn_call(milestone, eip, esp, ebp, ecx, edx, esi, eax, engine_base, rel):
    print("=" * 78)
    print("ASSOCIATE SPAWN-SITE DUMP  milestone=%s" % milestone)
    print("EIP=0x%08X  Engine base=0x%08X  rel=+0x%X" % (eip, engine_base or 0, rel or 0))
    print("EAX(fn)=0x%08X  ECX(Level)=0x%08X  EDX=0x%08X  ESI(target)=0x%08X" %
          (eax, ecx, edx, esi))
    print("ESP=0x%08X EBP=0x%08X" % (esp, ebp))
    print("-" * 78)

    cls_esp = try_u32(esp)
    cls_esi = try_u32(esi + OFF_EFFECT_CLASS) if esi else None
    print("[ESP]     SpawnActor Class* = %s" %
          (("0x%08X" % cls_esp) if cls_esp is not None else "?"))
    if cls_esp:
        print("  class:  %s" % uobject_name(cls_esp))
        # Extra: dump NameIndex at +0x10 raw for debugging GNames
        ni = try_u32(cls_esp + OFF_UOBJECT_NAME)
        print("  raw NameIndex[+0x10]=%s  GNames('%s')" % (
            ("0x%X" % ni) if ni is not None else "?",
            gnames_lookup(ni) or "?"))
    print("[ESI+6E8] pawn effect Class* = %s" %
          (("0x%08X" % cls_esi) if cls_esi is not None else "?"))
    if cls_esi and cls_esi != 0xFFFFFFFF:
        print("  class:  %s" % uobject_name(cls_esi))
    print("target pawn (ESI): %s" % uobject_name(esi))
    attacker = try_u32(ebp + 8) if ebp else None
    print("attacker [EBP+8]:  %s" % uobject_name(attacker if attacker else 0))

    print("-" * 78)
    print("Stack peek [ESP+0 .. +0x40]:")
    for off in range(0, 0x44, 4):
        v = try_u32(esp + off)
        print("  [ESP+0x%02X] = %s" % (
            off, ("0x%08X" % v) if v is not None else "?"))

    if milestone == "STATIC_LOAD_TABLE":
        print("-" * 78)
        print("StaticLoadClass: scanning ESP for wchar paths...")
        for off in range(0, 0x28, 4):
            p = try_u32(esp + off)
            s = read_wchar_z(p) if p else None
            if s and ("Lineage" in s or "effect" in s.lower() or "_" in s):
                print("  [ESP+0x%02X] -> '%s'" % (off, s))

    if milestone == "HARDCODE_P_U004":
        print("HARDCODE path: Lineageeffect.p_u004_a")

    print("-" * 78)
    print("Paste this entire dump into chat.")
    print("=" * 78)


def main():
    eip = int(idc.get_reg_value("EIP")) & 0xFFFFFFFF
    esp = int(idc.get_reg_value("ESP")) & 0xFFFFFFFF
    ebp = int(idc.get_reg_value("EBP")) & 0xFFFFFFFF
    ecx = int(idc.get_reg_value("ECX")) & 0xFFFFFFFF
    edx = int(idc.get_reg_value("EDX")) & 0xFFFFFFFF
    esi = int(idc.get_reg_value("ESI")) & 0xFFFFFFFF
    eax = int(idc.get_reg_value("EAX")) & 0xFFFFFFFF

    engine_base = get_seg_base("engine")
    milestone, rel = detect_milestone(eip, engine_base)

    if milestone in (
            "SPAWN_6E8", "SPAWN_TABLE", "STATIC_LOAD_TABLE", "HARDCODE_P_U004"):
        dump_spawn_call(
            milestone, eip, esp, ebp, ecx, edx, esi, eax, engine_base, rel)
        return

    label, arg_base = resolve_arg_base(eip, esp, ebp)
    names = [
        "arg_0  otherPawn/attacker",
        "arg_4  relatedActor",
        "arg_8",
        "arg_C  (Action a3 / hit info)",
        "arg_10",
        "arg_14 (Action a5)",
        "arg_18 (crit path if !=0)",
        "arg_1C (TABLE path if !=0)",
        "arg_20 (skip +6E8 spawn if !=0)",
        "arg_24 (spawn gate; 0=skip)",
        "arg_28 (table index 0..5)",
    ]
    args = [try_u32(arg_base + i * 4) for i in range(11)]

    target = ecx
    attacker = args[0] if args[0] else edx
    effect_class = try_u32(target + OFF_EFFECT_CLASS) if target else None
    attacker_flags = try_u8(attacker + OFF_ATTACKER_FLAGS) if attacker else None
    target_flags = try_u8(target + OFF_ATTACKER_FLAGS) if target else None

    arg_14 = args[5] or 0
    arg_18 = args[6] or 0
    arg_1C = args[7] or 0
    arg_20 = args[8] or 0
    arg_24 = args[9] or 0
    arg_28 = args[10] or 0

    print("=" * 78)
    print("ASSOCIATE_ATTACKED_NOTIFY DUMP  milestone=%s" % milestone)
    print("EIP=0x%08X" % eip)
    if engine_base is not None:
        print("Engine.dll base=0x%08X  rel=+0x%X" % (engine_base, eip - engine_base))
    print("EAX=0x%08X ECX(this/target)=0x%08X EDX=0x%08X ESI=0x%08X" %
          (eax, ecx, edx, esi))
    print("ESP=0x%08X EBP=0x%08X" % (esp, ebp))
    print("Arg base: %s @ 0x%08X" % (label, arg_base))
    print("-" * 78)
    for i, name in enumerate(names):
        val = args[i]
        print("  [%2d] %-36s = %s" % (
            i, name, ("0x%08X (%d)" % (val, val)) if val is not None else "<unreadable>"))
    print("-" * 78)
    print("target  (ECX):     %s" % uobject_name(target))
    print("attacker(arg0):    %s" % uobject_name(attacker if attacker else 0))
    print("[target+0x6E8] effect UClass* = %s" %
          (("0x%08X" % effect_class) if effect_class is not None else "?"))
    if effect_class:
        print("  effect class:    %s" % uobject_name(effect_class))
    print("[attacker+0x1820] flags = %s%s" % (
        ("0x%02X" % attacker_flags) if attacker_flags is not None else "?",
        ("  bit2=%d" % (1 if (attacker_flags & 4) else 0))
        if attacker_flags is not None else ""))
    print("[target+0x1820]   flags = %s" %
          (("0x%02X" % target_flags) if target_flags is not None else "?"))
    print("-" * 78)
    for line in predict_branch(
            arg_14, arg_18, arg_1C, arg_20, arg_24, arg_28,
            effect_class, attacker_flags):
        print(line)
    print("-" * 78)
    print("Paste this entire dump into chat.")
    print("=" * 78)


main()
