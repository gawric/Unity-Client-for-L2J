# -*- coding: utf-8 -*-
# IDA Pro 9 debugger script — UCanvas::DrawTargetName entry dump
#
# Usage:
#   BP on engine.dll DrawTargetName body:  sub_F1D100  (NOT the jmp stub at CD3A62)
#   Stand at first insn:  push 0FFFFFFFFh
#   File → Script file… → this script
#   Paste >>> PASTE THESE <<< into chat
#
# Signature (__thiscall):
#   void UCanvas::DrawTargetName(
#       FLevelSceneNode*, FRenderInterface*, FVector, ulong,
#       User*, TargetRenderType, L2FontType, ulong)
#
# At ENTRY (before prologue runs further — OK at first push):
#   ECX            = UCanvas*  (this)
#   [ESP+00]       = return address  (caller after call)
#   [ESP+04]       = FLevelSceneNode*
#   [ESP+08]       = FRenderInterface*
#   [ESP+0C..14]   = FVector XYZ     ★ world nameplate anchor
#   [ESP+18]       = ulong
#   [ESP+1C]       = User*
#   [ESP+20]       = TargetRenderType
#   [ESP+24]       = L2FontType
#   [ESP+28]       = ulong
#
# DrawTargetName only Projects the passed FVector — height formula is in the CALLER.
# This dump compares FVector.Z vs Actor.Location.Z + CollisionHeight candidates.

import math
import struct
import idc
import ida_segment

# AActor (same as your projectile dump)
OFF_LOCATION = 0x1BC
OFF_COLLISION_RADIUS = 0x2F0
OFF_COLLISION_HEIGHT = 0x2F4

# User* fields seen in DrawTargetName
OFF_USER_OBJ_204 = 0x204   # [User+0x204] used heavily in DrawTargetName
OFF_USER_FLOAT_284 = 0x284 # drawn as float param (not height)
OFF_USER_FLAGS_294 = 0x294


def read_bytes(addr, size):
    data = idc.read_dbg_memory(addr, size)
    if data is None or len(data) != size:
        raise RuntimeError("Cannot read 0x%08X (size=%d)" % (addr, size))
    return data


def read_u32(addr):
    return struct.unpack("<I", read_bytes(addr, 4))[0]


def read_f32(addr):
    return struct.unpack("<f", read_bytes(addr, 4))[0]


def try_u32(addr):
    try:
        return read_u32(addr)
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


def plausible_ptr(p):
    return p is not None and 0x10000 < p < 0x7FFF0000


def plausible_uu_vec(v):
    """Rough L2 world coords (not screen pixels, not denormals)."""
    if v is None:
        return False
    for c in v:
        if abs(c) > 500000.0:
            return False
        if abs(c) > 0.001 and abs(c) < 0.01:
            return False
    # Z often -something..+something large in Interlude
    return abs(v[2]) < 500000.0


def looks_like_actor(obj):
    loc = try_vec3(obj + OFF_LOCATION)
    ch = try_f32(obj + OFF_COLLISION_HEIGHT)
    cr = try_f32(obj + OFF_COLLISION_RADIUS)
    if not plausible_uu_vec(loc):
        return False
    if ch is None or ch < 1.0 or ch > 5000.0:
        return False
    if cr is None or cr < 0.0 or cr > 5000.0:
        return False
    return True


def dump_height_candidates(label, actor, name_pos):
    if actor is None or name_pos is None:
        return
    loc = try_vec3(actor + OFF_LOCATION)
    ch = try_f32(actor + OFF_COLLISION_HEIGHT)
    if loc is None or ch is None:
        return
    z = name_pos[2]
    print("  -- height vs %s (Actor*=%s) --" % (label, fmt_ptr(actor)))
    print("     Location +0x1BC     = %s" % fmt_vec(loc))
    print("     CollRadius +0x2F0   = %s" % fmt_f32(try_f32(actor + OFF_COLLISION_RADIUS)))
    print("     CollHeight +0x2F4   = %s" % fmt_f32(ch))
    cands = [
        ("Loc.Z", loc[2]),
        ("Loc.Z + CH", loc[2] + ch),
        ("Loc.Z + CH*2", loc[2] + ch * 2.0),
        ("Loc.Z + CH*2.1", loc[2] + ch * 2.1),
        ("Loc.Z + CH + 8", loc[2] + ch + 8.0),
        ("Loc.Z + CH*2 + 8", loc[2] + ch * 2.0 + 8.0),
    ]
    best = None
    for name, vz in cands:
        dz = z - vz
        print("     FVec.Z - (%-16s) = %s   (candZ=%s)" % (name, fmt_f32(dz), fmt_f32(vz)))
        if best is None or abs(dz) < abs(best[1]):
            best = (name, dz)
    if best is not None:
        print("     BEST match: %s  (dZ=%s)" % (best[0], fmt_f32(best[1])))
    # XY drift vs Location
    dx = name_pos[0] - loc[0]
    dy = name_pos[1] - loc[1]
    print("     dXY vs Location     = (%.3f, %.3f)  |d|=%.3f" % (
        dx, dy, math.sqrt(dx * dx + dy * dy)))


def find_actor_near_user(user, name_pos):
    """Heuristic: scan User dword fields for AActor-like objects near name FVector."""
    hits = []
    if not plausible_ptr(user):
        return hits
    for off in range(0, 0x300, 4):
        p = try_u32(user + off)
        if not plausible_ptr(p):
            continue
        if not looks_like_actor(p):
            continue
        loc = try_vec3(p + OFF_LOCATION)
        if loc is None or name_pos is None:
            continue
        dxy = math.sqrt((loc[0] - name_pos[0]) ** 2 + (loc[1] - name_pos[1]) ** 2)
        dz = abs(loc[2] - name_pos[2])
        # nameplate should be near actor XY; Z within a few CollisionHeights
        if dxy < 200.0 and dz < 1000.0:
            hits.append((off, p, dxy, dz))
    hits.sort(key=lambda t: (t[2], t[3]))
    return hits


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

    print("=" * 78)
    print("UCanvas::DrawTargetName ENTRY DUMP")
    print("EIP=0x%08X" % eip)
    if engine_base is not None:
        print("Engine.dll base=0x%08X  EIP rel=+0x%X" % (
            engine_base, (eip - engine_base) & 0xFFFFFFFF))
        # Expected body entry relative (ASLR): compare to your static IDB
        print("  (static IDB body was ~0xF1D100 — match EIP rel if rebased)")
    print("EAX=0x%08X EBX=0x%08X ECX=0x%08X EDX=0x%08X" % (eax, ebx, ecx, edx))
    print("ESI=0x%08X EDI=0x%08X EBP=0x%08X ESP=0x%08X" % (esi, edi, ebp, esp))
    print("-" * 78)

    if not esp:
        print("FAIL: ESP=0")
        print("=" * 78)
        return

    ret = try_u32(esp + 0x00)
    scene = try_u32(esp + 0x04)
    ri = try_u32(esp + 0x08)
    name_pos = try_vec3(esp + 0x0C)
    arg_ulong0 = try_u32(esp + 0x18)
    user = try_u32(esp + 0x1C)
    target_render = try_u32(esp + 0x20)
    font_type = try_u32(esp + 0x24)
    arg_ulong1 = try_u32(esp + 0x28)

    canvas = ecx if plausible_ptr(ecx) else None

    print(">>> PASTE THESE FOR NAMEPLATE ANCHOR <<<")
    print("  UCanvas* this(ECX) = %s" % fmt_ptr(canvas))
    print("  return [ESP+00]    = %s" % fmt_ptr(ret))
    if engine_base is not None and ret is not None:
        print("  return rel engine = +0x%X  ★ jump here, scroll UP for FVector build" % (
            (ret - engine_base) & 0xFFFFFFFF))
    print("  SceneNode [ESP+04]= %s" % fmt_ptr(scene))
    print("  RenderIface[+08]  = %s" % fmt_ptr(ri))
    print("  FVector   [ESP+0C]= %s  ★★ nameplate world pos" % fmt_vec(name_pos))
    if name_pos is not None:
        print("    raw hex X/Y/Z   = %s %s %s" % (
            fmt_ptr(try_u32(esp + 0x0C)),
            fmt_ptr(try_u32(esp + 0x10)),
            fmt_ptr(try_u32(esp + 0x14))))
    print("  ulong     [ESP+18]= %s" % fmt_u32(arg_ulong0))
    print("  User*     [ESP+1C]= %s" % fmt_ptr(user))
    print("  TargetRT  [ESP+20]= %s" % fmt_u32(target_render))
    print("  L2FontType[+24]   = %s" % fmt_u32(font_type))
    print("  ulong     [ESP+28]= %s" % fmt_u32(arg_ulong1))

    if plausible_ptr(user):
        print("-" * 78)
        print("  User+0x204 obj     = %s" % fmt_ptr(try_u32(user + OFF_USER_OBJ_204)))
        print("  User+0x284 float   = %s" % fmt_f32(try_f32(user + OFF_USER_FLOAT_284)))
        print("  User+0x294 flags   = %s" % fmt_u32(try_u32(user + OFF_USER_FLAGS_294)))

        # Prefer object at +0x204 if it is an actor; else scan
        obj204 = try_u32(user + OFF_USER_OBJ_204)
        actor = None
        if plausible_ptr(obj204) and looks_like_actor(obj204):
            actor = obj204
            dump_height_candidates("User+0x204", actor, name_pos)
        else:
            print("  User+0x204 is not AActor-like; scanning User+0..0x2FC for Actor near FVector…")
            hits = find_actor_near_user(user, name_pos)
            if not hits:
                print("  no Actor-like ptr near name FVector — dump caller asm next")
            else:
                for i, (off, p, dxy, dz) in enumerate(hits[:5]):
                    print("  hit[%d] User+0x%03X → Actor*=%s  dXY=%.1f dZ=%.1f" % (
                        i, off, fmt_ptr(p), dxy, dz))
                actor = hits[0][1]
                dump_height_candidates("User+0x%03X" % hits[0][0], actor, name_pos)

    print("-" * 78)
    print("Next:")
    print("  1) Paste this block into chat")
    print("  2) In IDA: G → return address, scroll UP — find who built FVector")
    print("  3) Optional 2nd BP on return addr; when hit AFTER DrawTargetName,")
    print("     you are past the call — formula is still ABOVE that addr")
    print("=" * 78)


main()
