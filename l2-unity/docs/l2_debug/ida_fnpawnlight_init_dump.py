# -*- coding: utf-8 -*-
# IDA Pro debugger script — FNPawnLight::Init dump (hit flash for shot/soulshot).
#
# BP on Engine.dll entry of FNPawnLight::Init:
#   this session: engine.dll:00EBADB0
#   first insn:   mov al, [esp+arg_0]
#   epilogue:     retn 40h  (0x40 bytes of stack args)
#
# Usage:
#   1) Break on Init entry (before or at mov al, [esp+arg_0])
#   2) File → Script file… → this script
#   3) Paste Output window into chat
#
# Optional filter: only dump when type==0 (Action_Attack hit light).

import struct
import idc
import ida_segment

# Set True to skip Init calls that are not hit-flash (type != 0).
FILTER_TYPE0_ONLY = False

# FNPawnLight field offsets (from Init asm).
OFF_TYPE = 0x08
OFF_FLAG_A9 = 0x09
OFF_LIFE_A10 = 0x0A
OFF_FLAG_A17 = 0x0B
OFF_COLOR_LIT = 0x0C   # FPlane after GetIntensity
OFF_COLOR_BASE = 0x1C  # FPlane before intensity
OFF_POS = 0x2C         # FVector
OFF_DIR = 0x38         # FVector from FRotator::Vector
OFF_PARAM_FROM_A16 = 0x44
OFF_PARAM_A15 = 0x4C

# Owner pawn often still in EDI at entry? Not guaranteed — try [this+0] chain later if needed.
# AActor Location for cross-check if owner known:
OFF_ACTOR_LOCATION = 0x1BC  # float* index 111


def read_bytes(addr, size):
    data = idc.read_dbg_memory(addr, size)
    if data is None or len(data) != size:
        raise RuntimeError("Cannot read 0x%08X (size=%d)" % (addr, size))
    return data


def read_u32(addr):
    return struct.unpack("<I", read_bytes(addr, 4))[0]


def read_u8(addr):
    return struct.unpack("<B", read_bytes(addr, 1))[0]


def read_f32(addr):
    return struct.unpack("<f", read_bytes(addr, 4))[0]


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


def try_f32(addr):
    try:
        return read_f32(addr)
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


def fmt_f32(v):
    if v is None:
        return "?"
    return "%.6f" % v


def fmt_u32(v):
    if v is None:
        return "?"
    return "0x%08X (%d)" % (v, v)


def fmt_vec(x, y, z):
    return "(%s, %s, %s)" % (fmt_f32(x), fmt_f32(y), fmt_f32(z))


def fmt_plane(r, g, b, a):
    return "(%s, %s, %s, %s)" % (fmt_f32(r), fmt_f32(g), fmt_f32(b), fmt_f32(a))


def detect_stack_arg_base(eip, esp, ebp):
    """
    At Init entry: [ESP]=ret, [ESP+4]=arg_0 (type).
    After `push esi` / `push edi` arg base slides +4/+8 — detect via EIP vs known body.
    """
    # Prefer classic entry: first stack slot after ret looks like tiny type (0/1).
    type_at_4 = try_u32(esp + 4)
    if type_at_4 is not None and (type_at_4 & 0xFFFFFF00) == 0 and type_at_4 <= 2:
        return "ESP+4 (entry)", esp + 4

    # After push esi only
    type_at_8 = try_u32(esp + 8)
    if type_at_8 is not None and (type_at_8 & 0xFFFFFF00) == 0 and type_at_8 <= 2:
        return "ESP+8 (after push esi)", esp + 8

    # After push esi + push edi
    type_at_C = try_u32(esp + 0xC)
    if type_at_C is not None and (type_at_C & 0xFFFFFF00) == 0 and type_at_C <= 2:
        return "ESP+C (after push esi/edi)", esp + 0xC

    # Frame already built (rare for this function — no push ebp)
    if ebp and esp < ebp < esp + 0x80:
        t = try_u32(ebp + 8)
        if t is not None and (t & 0xFFFFFF00) == 0 and t <= 2:
            return "EBP+8", ebp + 8

    return "ESP+4 (fallback)", esp + 4


def dump_args(arg_base):
    # IDA arg_N layout for Init (matches retn 40h / asm at 00EBADB0).
    type_v = try_u32(arg_base + 0x00)
    pos_x = try_f32(arg_base + 0x04)
    pos_y = try_f32(arg_base + 0x08)
    pos_z = try_f32(arg_base + 0x0C)
    pitch = try_u32(arg_base + 0x10)
    yaw = try_u32(arg_base + 0x14)
    roll = try_u32(arg_base + 0x18)
    a9 = try_u32(arg_base + 0x1C)
    a10 = try_u32(arg_base + 0x20)
    col_r = try_f32(arg_base + 0x24)
    col_g = try_f32(arg_base + 0x28)
    col_b = try_f32(arg_base + 0x2C)
    col_a = try_f32(arg_base + 0x30)
    a15 = try_f32(arg_base + 0x34)
    a16 = try_f32(arg_base + 0x38)
    a17 = try_u32(arg_base + 0x3C)
    return {
        "type": type_v,
        "pos": (pos_x, pos_y, pos_z),
        "rot": (pitch, yaw, roll),
        "a9": a9,
        "a10": a10,
        "color": (col_r, col_g, col_b, col_a),
        "a15": a15,
        "a16": a16,
        "a17": a17,
    }


def dump_this(this_ptr):
    if not this_ptr:
        return
    print("FNPawnLight this fields:")
    print("  +08 type      = %s" % (("0x%02X" % try_u8(this_ptr + OFF_TYPE)) if try_u8(this_ptr + OFF_TYPE) is not None else "?"))
    print("  +09 a9        = %s" % (("0x%02X" % try_u8(this_ptr + OFF_FLAG_A9)) if try_u8(this_ptr + OFF_FLAG_A9) is not None else "?"))
    print("  +0A a10/life  = %s" % (("0x%02X (%d)" % (try_u8(this_ptr + OFF_LIFE_A10), try_u8(this_ptr + OFF_LIFE_A10))) if try_u8(this_ptr + OFF_LIFE_A10) is not None else "?"))
    print("  +0B a17       = %s" % (("0x%02X" % try_u8(this_ptr + OFF_FLAG_A17)) if try_u8(this_ptr + OFF_FLAG_A17) is not None else "?"))
    print("  +0C colorLit  = %s" % fmt_plane(
        try_f32(this_ptr + OFF_COLOR_LIT + 0),
        try_f32(this_ptr + OFF_COLOR_LIT + 4),
        try_f32(this_ptr + OFF_COLOR_LIT + 8),
        try_f32(this_ptr + OFF_COLOR_LIT + 12)))
    print("  +1C colorBase = %s" % fmt_plane(
        try_f32(this_ptr + OFF_COLOR_BASE + 0),
        try_f32(this_ptr + OFF_COLOR_BASE + 4),
        try_f32(this_ptr + OFF_COLOR_BASE + 8),
        try_f32(this_ptr + OFF_COLOR_BASE + 12)))
    print("  +2C pos       = %s" % fmt_vec(
        try_f32(this_ptr + OFF_POS + 0),
        try_f32(this_ptr + OFF_POS + 4),
        try_f32(this_ptr + OFF_POS + 8)))
    print("  +38 dir       = %s" % fmt_vec(
        try_f32(this_ptr + OFF_DIR + 0),
        try_f32(this_ptr + OFF_DIR + 4),
        try_f32(this_ptr + OFF_DIR + 8)))
    print("  +44 from_a16  = %s" % fmt_f32(try_f32(this_ptr + OFF_PARAM_FROM_A16)))
    print("  +4C a15       = %s" % fmt_f32(try_f32(this_ptr + OFF_PARAM_A15)))


def main():
    eip = int(idc.get_reg_value("EIP")) & 0xFFFFFFFF
    esp = int(idc.get_reg_value("ESP")) & 0xFFFFFFFF
    ebp = int(idc.get_reg_value("EBP")) & 0xFFFFFFFF
    ecx = int(idc.get_reg_value("ECX")) & 0xFFFFFFFF
    edx = int(idc.get_reg_value("EDX")) & 0xFFFFFFFF
    esi = int(idc.get_reg_value("ESI")) & 0xFFFFFFFF
    edi = int(idc.get_reg_value("EDI")) & 0xFFFFFFFF
    eax = int(idc.get_reg_value("EAX")) & 0xFFFFFFFF

    engine_base = get_seg_base("engine")
    label, arg_base = detect_stack_arg_base(eip, esp, ebp)
    args = dump_args(arg_base)

    if FILTER_TYPE0_ONLY and args["type"] not in (0, None):
        print("SKIP: type=%s (FILTER_TYPE0_ONLY=True)" % args["type"])
        return

    # this: prefer ECX; if already past `mov esi, ecx`, ESI is this.
    this_ptr = ecx
    if this_ptr < 0x10000 and esi > 0x10000:
        this_ptr = esi

    print("=" * 78)
    print("FNPawnLight::Init DUMP")
    print("EIP=0x%08X" % eip)
    if engine_base is not None:
        print("Engine.dll base=0x%08X  rel=+0x%X" % (engine_base, (eip - engine_base) & 0xFFFFFFFF))
    print("EAX=0x%08X ECX(this)=0x%08X EDX=0x%08X ESI=0x%08X EDI=0x%08X" %
          (eax, ecx, edx, esi, edi))
    print("ESP=0x%08X EBP=0x%08X" % (esp, ebp))
    print("Arg base: %s @ 0x%08X" % (label, arg_base))
    print("this used: 0x%08X" % this_ptr)
    print("-" * 78)
    print("STACK ARGS (for Unity hit flash):")
    print("  type (a2)     = %s   # expect 0 for Action_Attack hit" % fmt_u32(args["type"]))
    print("  pos           = %s" % fmt_vec(*args["pos"]))
    print("  rot PitchYawRoll = (%s, %s, %s)" % (
        fmt_u32(args["rot"][0]), fmt_u32(args["rot"][1]), fmt_u32(args["rot"][2])))
    print("  a9            = %s   # expect 1" % fmt_u32(args["a9"]))
    print("  a10 life/style= %s   # expect 12" % fmt_u32(args["a10"]))
    print("  color FPlane  = %s   # expect ~1,1,1,1" % fmt_plane(*args["color"]))
    print("  a15           = %s   # -> this+4C  ★" % fmt_f32(args["a15"]))
    print("  a16           = %s   # -> (a16+c)*k at +44  ★" % fmt_f32(args["a16"]))
    print("  a17           = %s   # expect 0" % fmt_u32(args["a17"]))
    print("-" * 78)
    print("Raw dwords [arg_base .. +0x40]:")
    for off in range(0, 0x44, 4):
        u = try_u32(arg_base + off)
        f = try_f32(arg_base + off)
        print("  [+0x%02X] u32=%s  f32=%s" % (
            off,
            ("0x%08X" % u) if u is not None else "?",
            fmt_f32(f)))
    print("-" * 78)
    dump_this(this_ptr)
    print("-" * 78)
    if edi > 0x10000:
        # Often owner APawn still in EDI when called from AddPawnLight after ctor.
        print("EDI (maybe owner APawn)=0x%08X" % edi)
        print("  EDI Location@+0x1BC = %s" % fmt_vec(
            try_f32(edi + OFF_ACTOR_LOCATION + 0),
            try_f32(edi + OFF_ACTOR_LOCATION + 4),
            try_f32(edi + OFF_ACTOR_LOCATION + 8)))
        print("  EDI CollisionRadius@+0x2F0 (float idx 188) = %s" %
              fmt_f32(try_f32(edi + 0x2F0)))
    print("-" * 78)
    print("Paste this entire dump into chat.")
    print("=" * 78)


main()
