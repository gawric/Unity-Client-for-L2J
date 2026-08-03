# -*- coding: utf-8 -*-
# IDA Pro debugger script — ANProjectile::Tick dump
#
# Use for Wind Strike OR Power Shot (skill 56):
#   BP ANProjectile::Tick  (stub jmp → body …F51400 / Engine+0x1E1400)
#   Cast skill, stop on first Tick while flying (state+0x34 == 0x11)
#   File → Script file… → this script
#   Paste >>> PASTE THESE <<< into chat — especially Speed+0x4DC and dirMul+0x4E0
#
# Prefer live AutoLogin hook: ANSkillProjectileTick.log (powerShotHint / formulaHint)
#
# this = ECX/ESI = ANProjectile*
#
# Key fields (Interlude):
#   +0x34   state byte (0x11 = flying)
#   +0x1BC  Location
#   +0x1D4  Velocity
#   +0x2C4  FNMover*
#   +0x4DC  Speed          ★
#   +0x4E0  dir multiplier
#   +0x4E4  TargetActor*
#   +0x4E8  TargetPos
#   +0x534  Distance (+1332)
#   +0x504  flags (+1284)  bit2 = arc from PrepareInterpolation
#
# ASLR-safe: live regs + offsets only.

import math
import struct
import idc
import ida_segment

# AActor / ANProjectile
OFF_LOCATION = 0x1BC
OFF_VELOCITY = 0x1D4
OFF_STATE = 0x34
OFF_FNMOVER = 0x2C4
OFF_SPEED = 0x4DC
OFF_DIR_MUL = 0x4E0
OFF_TARGET_ACTOR = 0x4E4
OFF_TARGET_POS = 0x4E8
OFF_FLAGS = 0x504          # +1284
OFF_DIST = 0x534           # +1332 from PrepareInterpolation / Tick path
OFF_COLLISION_RADIUS = 0x2F0
OFF_COLLISION_HEIGHT = 0x2F4

# If accidentally still on SkillEffectShot pawn:
OFF_PAWN_SKILL_ID = 0x4F8
OFF_PAWN_STAGE = 0x59C
OFF_PAWN_TARGET = 0x520


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


def try_vec3(addr):
    x = try_f32(addr)
    y = try_f32(addr + 4)
    z = try_f32(addr + 8)
    if x is None or y is None or z is None:
        return None
    return (x, y, z)


def try_reg_u32(name):
    try:
        return int(idc.get_reg_value(name)) & 0xFFFFFFFF
    except Exception:
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
    try:
        if math.isnan(v) or math.isinf(v):
            return str(v)
    except Exception:
        pass
    return "%.6f" % v


def fmt_u32(v):
    if v is None:
        return "?"
    return "0x%08X (%d)" % (v, v)


def fmt_ptr(v):
    if v is None:
        return "?"
    return "0x%08X" % v


def fmt_vec(v):
    if v is None:
        return "?"
    return "(%.3f, %.3f, %.3f)" % v


def vec_len(v):
    if v is None:
        return None
    return math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])


def dist3(a, b):
    if a is None or b is None:
        return None
    dx = a[0] - b[0]
    dy = a[1] - b[1]
    dz = a[2] - b[2]
    return math.sqrt(dx * dx + dy * dy + dz * dz)


def plausible_ptr(p):
    return p is not None and 0x10000 < p < 0x7FFF0000


def resolve_this():
    ecx = try_reg_u32("ECX") or 0
    esi = try_reg_u32("ESI") or 0
    if plausible_ptr(ecx):
        return ecx
    if plausible_ptr(esi):
        return esi
    return ecx or esi


def looks_like_pawn(obj):
    """SkillEffectShot pawn still has skillId at +0x4F8 and stage at +0x59C."""
    sid = try_u32(obj + OFF_PAWN_SKILL_ID)
    st = try_u32(obj + OFF_PAWN_STAGE)
    if sid is None or st is None:
        return False
    # Wind Strike / common skill ids are small ints; stage 0..4
    return 1 <= sid <= 30000 and 0 <= st <= 8


def dump_pawn_hint(obj):
    print("Looks like APawn (SkillEffectShot), not ANProjectile.")
    print("  skillId +0x4F8 = %s" % fmt_u32(try_u32(obj + OFF_PAWN_SKILL_ID)))
    print("  stage   +0x59C = %s" % fmt_u32(try_u32(obj + OFF_PAWN_STAGE)))
    print("  target  +0x520 = %s" % fmt_ptr(try_u32(obj + OFF_PAWN_TARGET)))
    print("Tip: disable SkillEffectShot BP; BP ANProjectile::Tick instead.")


def main():
    eip = try_reg_u32("EIP") or 0
    eax = try_reg_u32("EAX") or 0
    ebx = try_reg_u32("EBX") or 0
    ecx = try_reg_u32("ECX") or 0
    edx = try_reg_u32("EDX") or 0
    esi = try_reg_u32("ESI") or 0
    edi = try_reg_u32("EDI") or 0
    ebp = try_reg_u32("EBP") or 0
    esp = try_reg_u32("ESP") or 0

    engine_base = get_seg_base("engine")
    proj = resolve_this()

    print("=" * 78)
    print("ANProjectile::Tick DUMP")
    print("EIP=0x%08X" % eip)
    if engine_base is not None:
        print("Engine.dll base=0x%08X  rel=+0x%X" % (
            engine_base, (eip - engine_base) & 0xFFFFFFFF))
    print("EAX=0x%08X EBX=0x%08X ECX=0x%08X EDX=0x%08X" % (eax, ebx, ecx, edx))
    print("ESI=0x%08X EDI=0x%08X EBP=0x%08X ESP=0x%08X" % (esi, edi, ebp, esp))
    print("-" * 78)

    if not plausible_ptr(proj):
        print("FAIL: no plausible this in ECX/ESI")
        print("=" * 78)
        return

    # Tick args after push ebp; mov ebp,esp
    dt = try_f32(ebp + 8) if ebp else None
    tick_type = try_u32(ebp + 0xC) if ebp else None

    state = try_u8(proj + OFF_STATE)
    speed = try_f32(proj + OFF_SPEED)
    dir_mul = try_f32(proj + OFF_DIR_MUL)
    mover = try_u32(proj + OFF_FNMOVER)
    tgt_actor = try_u32(proj + OFF_TARGET_ACTOR)
    loc = try_vec3(proj + OFF_LOCATION)
    vel = try_vec3(proj + OFF_VELOCITY)
    tgt_pos = try_vec3(proj + OFF_TARGET_POS)
    dist_field = try_f32(proj + OFF_DIST)
    flags = try_u32(proj + OFF_FLAGS)
    vel_len = vec_len(vel)
    dist_live = dist3(loc, tgt_pos)

    # Mis-hit SkillEffectShot?
    if looks_like_pawn(proj) and (speed is None or abs(speed) < 1e-3) and state != 0x11:
        dump_pawn_hint(proj)
        print("=" * 78)
        return

    print(">>> PASTE THESE FOR FORMULA <<<")
    print("  ANProjectile*     = %s" % fmt_ptr(proj))
    print("  state +0x34       = %s  (want 0x11 flying)" % (
        ("0x%02X" % state) if state is not None else "?"))
    print("  DeltaTime [ebp+8] = %s" % fmt_f32(dt))
    print("  ELevelTick[+0C]   = %s" % fmt_u32(tick_type))
    print("  Speed   +0x4DC    = %s  ★★ UU/s" % fmt_f32(speed))
    print("  dirMul  +0x4E0    = %s" % fmt_f32(dir_mul))
    print("  |Velocity|        = %s  (should ≈ Speed)" % fmt_f32(vel_len))
    print("  Velocity +0x1D4   = %s" % fmt_vec(vel))
    print("  Location +0x1BC   = %s" % fmt_vec(loc))
    print("  TargetPos +0x4E8  = %s" % fmt_vec(tgt_pos))
    print("  TargetActor +0x4E4= %s" % fmt_ptr(tgt_actor))
    print("  FNMover* +0x2C4   = %s" % fmt_ptr(mover))
    print("  Dist field +0x534 = %s UU" % fmt_f32(dist_field))
    print("  Dist live Loc→Tgt = %s UU" % fmt_f32(dist_live))
    print("  flags +0x504      = %s  (bit2=arc)" % fmt_u32(flags))

    d = dist_field if (dist_field is not None and dist_field > 1.0) else dist_live
    sp = speed if (speed is not None and abs(speed) > 1.0) else vel_len
    if d is not None and sp is not None and sp > 1.0:
        fly = d / sp
        print("  flySec ≈ Dist/Speed = %s s  ★" % fmt_f32(fly))
        print("  compare bow: Dist/1500 = %s s" % fmt_f32(d / 1500.0))

    if state is not None and state != 0x11:
        print("  WARN: state!=0x11 — may be pre-shot / already done; continue or re-cast")
    if not plausible_ptr(mover):
        print("  NOTE: FNMover=null — Speed may come from Velocity alone")
    if speed is not None and abs(speed) < 1e-3:
        print("  NOTE: Speed=0 at entry — F4 to after fstp [esi+4DCh] (~Tick+0x123)")

    print("-" * 78)
    print("Unity map:")
    print("  Speed UU/s  →  Unity = Speed / 52.5  (same scale as arrow)")
    print("  flySec      →  DistUU / SpeedUU   (linear, like bow)")
    print("Tip: 2–3 casts at different Dist; if Speed≈const → fixed-speed formula.")
    print("Prior SkillEffectShot Dist≈590 UU — expect fly≈590/Speed.")
    print("=" * 78)


main()
