# -*- coding: utf-8 -*-
# IDA Pro debugger script — SpriteEmitter0 PTVD / StartVelocity spawn dump (m_u004_b).
#
# Place breakpoints on ANY of these Engine.dll addresses (same build as live IDA):
#   +0xFD2CFA  lea ecx,[ebx+3A0h]   PRE_START_VELOCITY
#   +0xFD2D00  call GetRand         ON_START_VELOCITY_CALL
#   +0xFD2D14  mov [edi+20h],eax    AFTER_RAW_VELOCITY  (recommended)
#   +0xFD2D3C  call GetRand         ON_LOCATION_OFFSET  (ebx+158)
#   +0xFD2DC1  call GetRand         ON_POLAR_CALL       (ebx+180, shape==2 only)
#
# Optional Core.dll BP for appRand TLS state (inside appFrand, BEFORE imul 214013):
#   Stop on: mov ecx,[eax+14h]  then run script — ECX = state_before_draw
#
# Registers at Engine BPs:
#   EBX = UParticleEmitter* (this)
#   EDI = particle slot base
#
# No IDA condition required — script prints MATCH / NOT MATCHED for SE0 fingerprint.

import struct
import idc
import ida_segment

URU_SCALE = 65535.0
APP_RAND_MUL = 214013
APP_RAND_ADD = 2531011
APP_FRAND_DIV = 32767.0

# Milestone IDA EAs (Engine.dll, Interlude build — verify if your IDs differ).
ENGINE_MILESTONE_IDA_EA = {
    "PRE_START_VELOCITY": 0xFD2CFA,
    "ON_START_VELOCITY_CALL": 0xFD2D00,
    "AFTER_RAW_VELOCITY": 0xFD2D14,
    "ON_LOCATION_OFFSET": 0xFD2D3C,
    "ON_POLAR_CALL": 0xFD2DC1,
}

EMITTER_START_VELOCITY_RANGE_OFF = 0x3A0
EMITTER_START_LOCATION_OFFSET_OFF = 0x158
EMITTER_START_LOCATION_POLAR_OFF = 0x180
EMITTER_PTVD_MODE_OFF = 0x400
EMITTER_SHAPE_OFF = 0x174
MILESTONE_TOLERANCE = 4

# Core.dll appFrand — same Interlude build as SpriteEmitter2 script.
APP_RAND_STATE_LOAD_EA = 0x1017D352
APP_RAND_IMUL_EA = 0x1017D355

# SpriteEmitter0 fingerprint (m_u004_b UC SpriteEmitter0).
TARGET_REPEATS = 9.0
TARGET_FADE_OUT = 0.954
TARGET_ACCEL_Z = -40.0
TARGET_VEL_X = 60.0
TARGET_VEL_Z_MIN = -18.0
TARGET_VEL_Z_MAX = 1.0
TARGET_PTVD_MODE = 1

# SpriteEmitter2 (for comparison when you hit wrong layer).
SE2_REPEATS = 60.0
SE2_FADE_OUT = 0.154


def read_bytes(addr, size):
    data = idc.read_dbg_memory(addr, size)
    if data is None or len(data) != size:
        raise RuntimeError("Cannot read 0x%08X (size=%d)" % (addr, size))
    return data


def read_f32(addr):
    raw = read_bytes(addr, 4)
    bits = struct.unpack("<I", raw)[0]
    value = struct.unpack("<f", raw)[0]
    return value, bits


def read_u32(addr):
    return struct.unpack("<I", read_bytes(addr, 4))[0]


def read_u8(addr):
    return read_bytes(addr, 1)[0]


def get_seg_base(name_substr):
    name_substr = name_substr.lower()
    qty = ida_segment.get_segm_qty()
    for i in range(qty):
        seg = ida_segment.getnseg(i)
        seg_name = ida_segment.get_segm_name(seg).lower()
        if name_substr in seg_name:
            return seg.start_ea
    return None


def rel_in_module(eip, module_substr):
    base = get_seg_base(module_substr)
    if base is None:
        return None, None
    return base, eip - base


def _match_milestone(offset, table, tolerance):
    for name, target in table.items():
        if abs(offset - target) <= tolerance:
            return name
    return None


def detect_milestone(eip):
    if eip == APP_RAND_STATE_LOAD_EA:
        return "APP_RAND_STATE_LOAD", None, eip
    if eip == APP_RAND_IMUL_EA:
        return "APP_RAND_BEFORE_IMUL", None, eip

    engine_base, rel = rel_in_module(eip, "engine")
    if engine_base is not None:
        hit = _match_milestone(rel, {k: v - engine_base for k, v in ENGINE_MILESTONE_IDA_EA.items()}, MILESTONE_TOLERANCE)
        if hit:
            return hit, engine_base, rel

    hit = _match_milestone(eip, ENGINE_MILESTONE_IDA_EA, MILESTONE_TOLERANCE)
    if hit:
        return hit, engine_base, rel if engine_base is not None else 0

    if engine_base is not None and 0x2D000 <= rel <= 0x2E000:
        return "ENGINE_SPAWN_EARLY", engine_base, rel

    core_base, core_rel = rel_in_module(eip, "core")
    if core_base is not None and 0x1010ED00 <= core_rel <= 0x1010EF00:
        return "CORE_GETRAND_BODY", core_base, core_rel

    return "UNKNOWN", engine_base, rel if engine_base else 0


def try_read_tls_rand_state(milestone, slot):
    engine_milestones = {
        "PRE_START_VELOCITY", "ON_START_VELOCITY_CALL", "AFTER_RAW_VELOCITY",
        "ON_LOCATION_OFFSET", "ON_POLAR_CALL", "ENGINE_SPAWN_EARLY", "UNKNOWN",
    }

    if milestone in engine_milestones:
        return None, (
            "not readable at Engine BP (ECX is FRange* or GetRand result). "
            "Stop inside Core.dll appFrand on 'mov ecx,[eax+14h]' before imul."
        )

    hints = []
    ecx = idc.get_reg_value("ECX") & 0xFFFFFFFF
    eax = idc.get_reg_value("EAX") & 0xFFFFFFFF

    if milestone == "APP_RAND_STATE_LOAD":
        try:
            return read_u32(eax + 0x14), "dword_[EAX+14h]_before_mov"
        except RuntimeError:
            return None, "cannot read TLS state at EAX+14h"
    if milestone == "APP_RAND_BEFORE_IMUL":
        if 0x1000 < ecx < 0x7F000000:
            return ecx, "ECX_before_imul"
        try:
            return read_u32(eax + 0x14), "dword_[EAX+14h]_before_store"
        except RuntimeError:
            return None, "cannot read TLS state at EAX+14h"

    try:
        raw_z_bits = read_u32(slot + 0x20)
        if ecx == raw_z_bits:
            ecx = 0
    except RuntimeError:
        pass

    if milestone.startswith("CORE_") and 0x1000 < ecx < 0x7F000000:
        hints.append(("ECX_register", ecx))

    if 0x10000 < eax < 0x7F000000:
        try:
            state = read_u32(eax + 0x14)
            if state != 0:
                hints.append(("dword_[EAX+14h]", state))
        except RuntimeError:
            pass

    if not hints:
        return None, (
            "state not auto-read. Stop inside appFrand on "
            "'mov ecx,[eax+14h]' (before imul 214013) and re-run."
        )

    for label, value in hints:
        if label == "dword_[EAX+14h]":
            return value, label
    return hints[0][1], hints[0][0]


def app_rand_step(state):
    state = (state * APP_RAND_MUL + APP_RAND_ADD) & 0xFFFFFFFF
    app_rand = (state >> 16) & 0x7FFF
    return state, app_rand / APP_FRAND_DIV


def frange_sample(min_val, max_val, random_fraction):
    return random_fraction * (min_val - max_val) + max_val


def frange_vector_get_rand_z_y_x(x_range, y_range, z_range, state):
    state, fraction_z = app_rand_step(state)
    value_z = frange_sample(z_range[0], z_range[1], fraction_z)
    state, fraction_y = app_rand_step(state)
    value_y = frange_sample(y_range[0], y_range[1], fraction_y)
    state, fraction_x = app_rand_step(state)
    value_x = frange_sample(x_range[0], x_range[1], fraction_x)
    return (value_x, value_y, value_z), state


def read_emitter_frange(emitter, offset):
    min_v, _ = read_f32(emitter + offset)
    max_v, _ = read_f32(emitter + offset + 4)
    return min_v, max_v


def read_frange_vector(emitter, offset):
    x = read_emitter_frange(emitter, offset)
    y = read_emitter_frange(emitter, offset + 8)
    z = read_emitter_frange(emitter, offset + 16)
    return x, y, z


def classify_emitter(emitter):
    repeats, _ = read_f32(emitter + 0xB4)
    fade_out, _ = read_f32(emitter + 0xE8)
    _, accel_z_bits = read_f32(emitter + 0x3C)
    vel_x_min, _ = read_f32(emitter + EMITTER_START_VELOCITY_RANGE_OFF)
    vel_z_min, _ = read_f32(emitter + EMITTER_START_VELOCITY_RANGE_OFF + 16)
    ptvd = read_u8(emitter + EMITTER_PTVD_MODE_OFF)

    is_se0 = (
        abs(repeats - TARGET_REPEATS) < 0.01 and
        abs(fade_out - TARGET_FADE_OUT) < 0.01 and
        accel_z_bits == struct.unpack("<I", struct.pack("<f", TARGET_ACCEL_Z))[0] and
        abs(vel_x_min - TARGET_VEL_X) < 0.01 and
        abs(vel_z_min - TARGET_VEL_Z_MIN) < 0.01 and
        ptvd == TARGET_PTVD_MODE
    )

    is_se2 = (
        abs(repeats - SE2_REPEATS) < 0.01 and
        abs(fade_out - SE2_FADE_OUT) < 0.01
    )

    if is_se0:
        return "SpriteEmitter0 MATCH", True
    if is_se2:
        return "SpriteEmitter2 (wrong layer for SE0 dump)", False
    return "NOT MATCHED (other emitter)", False


def line_f32(label, addr, slot_base):
    value, bits = read_f32(addr)
    off = addr - slot_base
    print("%-34s +0x%02X = % .9f  bits=0x%08X" % (label, off, value, bits))


def ptvd_component_predict(raw_vel, spawn_pos, owner_pos=(0.0, 0.0, 0.0)):
    dx = spawn_pos[0] - owner_pos[0]
    dy = spawn_pos[1] - owner_pos[1]
    dz = spawn_pos[2] - owner_pos[2]
    length = (dx * dx + dy * dy + dz * dz) ** 0.5
    if length <= 1e-5:
        return (0.0, 0.0, 0.0)
    dir_x, dir_y, dir_z = dx / length, dy / length, dz / length
    return (
        -raw_vel[0] * dir_x,
        -raw_vel[1] * dir_y,
        -raw_vel[2] * dir_z,
    )


def main():
    eip = int(idc.get_reg_value("EIP")) & 0xFFFFFFFF
    emitter = int(idc.get_reg_value("EBX")) & 0xFFFFFFFF
    slot = int(idc.get_reg_value("EDI")) & 0xFFFFFFFF
    eax = int(idc.get_reg_value("EAX")) & 0xFFFFFFFF

    milestone, engine_base, rel = detect_milestone(eip)
    state, state_source = try_read_tls_rand_state(milestone, slot)

    repeats, repeats_bits = read_f32(emitter + 0xB4)
    fade_out, fade_out_bits = read_f32(emitter + 0xE8)
    accel_z, accel_z_bits = read_f32(emitter + 0x3C)
    shape = read_u8(emitter + EMITTER_SHAPE_OFF)
    ptvd = read_u8(emitter + EMITTER_PTVD_MODE_OFF)

    label, is_target = classify_emitter(emitter)

    vel_ranges = read_frange_vector(emitter, EMITTER_START_VELOCITY_RANGE_OFF)
    loc_ranges = read_frange_vector(emitter, EMITTER_START_LOCATION_OFFSET_OFF)

    print("=" * 78)
    print("SPRITE EMITTER0 PTVD / START VELOCITY SPAWN DUMP")
    print("EIP=0x%08X  milestone=%s" % (eip, milestone))
    if engine_base:
        print("Engine.dll base=0x%08X  rel=+0x%X" % (engine_base, rel))
    print("emitter(EBX)=0x%08X  slot(EDI)=0x%08X  EAX=0x%08X" % (emitter, slot, eax))
    print("Fingerprint: %s" % label)
    print("Emitter: ColorScaleRepeats=%.6f FadeOutStart=%.6f AccelZ=%.6f" %
          (repeats, fade_out, accel_z))
    print("  repeats_bits=0x%08X fade_bits=0x%08X accelZ_bits=0x%08X" %
          (repeats_bits, fade_out_bits, accel_z_bits))
    print("StartLocationShape@+0x174=%u (UC PTLS_Polar=3)  PTVD@+0x400=%u (expect 1)" %
          (shape, ptvd))
    print("StartVelocityRange@+0x3A0:")
    print("  X=[%.3f,%.3f] Y=[%.3f,%.3f] Z=[%.3f,%.3f]" %
          (vel_ranges[0][0], vel_ranges[0][1],
           vel_ranges[1][0], vel_ranges[1][1],
           vel_ranges[2][0], vel_ranges[2][1]))
    print("StartLocationOffset@+0x158:")
    print("  X=[%.3f,%.3f] Y=[%.3f,%.3f] Z=[%.3f,%.3f]" %
          (loc_ranges[0][0], loc_ranges[0][1],
           loc_ranges[1][0], loc_ranges[1][1],
           loc_ranges[2][0], loc_ranges[2][1]))

    if not is_target:
        print("\n  !! Not SE0 — F9 and continue, or use healing potion for more hits.")
        print("  SE0 expects: repeats=9 fade=0.954 accelZ=-40 vel@3A0=60,60,-18,1 ptvd=1")
        print("  SE2 looks like: repeats=60 fade=0.154 tiny StartVelocityRange")

    if state is not None and is_target and milestone in (
        "PRE_START_VELOCITY", "ON_START_VELOCITY_CALL", "AFTER_RAW_VELOCITY",
    ):
        print("\nappRand state_before_StartVelocity_GetRand:")
        print("  source=%s  state=0x%08X" % (state_source, state))
        raw_mirror, st = frange_vector_get_rand_z_y_x(vel_ranges[0], vel_ranges[1], vel_ranges[2], state)
        print("  CPU mirror rawVelocity=(%.6f, %.6f, %.6f)" % raw_mirror)
        print("  CPU mirror after 3 draws state=0x%08X" % (st & 0xFFFFFFFF))
        print("  -> Unity StartVelocity rand state = 0x%08X" % (state & 0xFFFFFFFF))
    else:
        print("\nappRand state: %s" % state_source)

    print("\nSlot location (building at +0x00):")
    line_f32("Location X", slot + 0x00, slot)
    line_f32("Location Y", slot + 0x04, slot)
    line_f32("Location Z", slot + 0x08, slot)

    print("\nSlot raw velocity (GetRand ebx+3A0 at FD2D00 — NOT slot+0x24):")
    line_f32("rawVel X", slot + 0x18, slot)
    line_f32("rawVel Y", slot + 0x1C, slot)
    line_f32("rawVel Z", slot + 0x20, slot)

    if milestone == "AFTER_RAW_VELOCITY":
        print("\n  (EAX = rawVel.Z just written to [edi+20h])")

    if is_target and milestone == "AFTER_RAW_VELOCITY":
        raw_x, _ = read_f32(slot + 0x18)
        raw_y, _ = read_f32(slot + 0x1C)
        raw_z, _ = read_f32(slot + 0x20)
        loc_x, _ = read_f32(slot + 0x00)
        loc_y, _ = read_f32(slot + 0x04)
        loc_z, _ = read_f32(slot + 0x08)
        v_spawn = ptvd_component_predict((raw_x, raw_y, raw_z), (loc_x, loc_y, loc_z))
        print("\nPTVD component preview (owner=0,0,0; spawn may still change before PTVD block):")
        print("  vSpawn_preview=(%.6f, %.6f, %.6f)" % v_spawn)
        print("  (final PTVD runs later when [ebx+400]==1 — search cmp byte ptr [ebx+400h])")

    print("\nNote: slot+0x24 is StartSize later, NOT raw velocity at this milestone.")

    print("\nBreakpoint guide (this build):")
    for name, ea in ENGINE_MILESTONE_IDA_EA.items():
        if engine_base:
            print("  %s: Engine.dll+0x%X  (abs ~0x%08X)" %
                  (name, ea - engine_base if ea > engine_base else ea, ea))
        else:
            print("  %s: EIP ~0x%08X" % (name, ea))
    print("  appRand TLS: Core.dll appFrand BP on mov ecx,[eax+14h] before imul")
    print("  Manual SE0 check: [EBX+3A0]==60.0f  [EBX+0B4]==9.0f  byte[EBX+400]==1")
    print("=" * 78)


main()
