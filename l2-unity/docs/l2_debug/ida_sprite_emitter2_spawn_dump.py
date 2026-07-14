# -*- coding: utf-8 -*-
# IDA Pro debugger script — SpriteEmitter2 spawn dump (m_u004_b).
#
# Place breakpoints on ANY of these Engine.dll RVAs (relative to Engine.dll base):
#   +0xFD3D1B  before StartSpin GetRand (lea ecx,[ebx+278h])
#   +0xFD3D25  on  StartSpin GetRand call
#   +0xFD3D39  after StartSpin written to [EDI+3C]
#   +0xFD3D5A  after SPS written to [EDI+30]
#   +0xFD3CC?  after StartVelocity written to [EDI+24]  (search: lea ecx,[ebx+2CCh])
#
# Optional Core.dll BP for appRand state (inside appFrand, BEFORE imul 214013):
#   Stop on: mov ecx,[eax+14h]  then run script — ECX = state_before_draw
#
# Registers at Engine BPs:
#   EBX = UParticleEmitter* (this)
#   EDI = particle slot base (v7)

import struct
import idc
import ida_segment

URU_SCALE = 65535.0
APP_RAND_MUL = 214013
APP_RAND_ADD = 2531011
APP_FRAND_DIV = 32767.0

# Milestone offsets inside Engine.dll spawn tail (sub_FD2C60).
# IDA often labels these as 0xFD3Dxx absolute EAs; when Engine loads at 0x00CD0000
# the segment-relative offset is 0x303Dxx (EIP 0x00FD3D39 -> rel +0x303D39).
ENGINE_MILESTONE_REL = {
    "AFTER_SIZE_TO_SLOT24": 0x3037D5,
    "PRE_START_SPIN": 0x303D1B,
    "ON_START_SPIN_CALL": 0x303D25,
    "AFTER_START_SPIN": 0x303D39,
    "AFTER_SPS": 0x303D5A,
}
# Flat IDA EA suffixes (same build, alternate addressing).
ENGINE_MILESTONE_IDA_EA = {
    "AFTER_SIZE_TO_SLOT24": 0xFD37D5,
    "PRE_START_SPIN": 0xFD3D1B,
    "ON_START_SPIN_CALL": 0xFD3D25,
    "AFTER_START_SPIN": 0xFD3D39,
    "AFTER_SPS": 0xFD3D5A,
}
# Emitter this+0x2CC: GetRand source for slot+0x24 (runtime reads as StartSizeRange for SE2).
EMITTER_SIZE_RANGE_OFF = 0x2CC
MILESTONE_TOLERANCE = 3

# Core.dll appFrand implementation in this Interlude build:
#   1017D34D call TLS accessor
#   1017D352 mov ecx,[eax+14h]  <-- state is still unmodified
#   1017D355 imul ecx,343FDh
#   1017D35B add  ecx,269EC3h
#   1017D361 mov [eax+14h],ecx
#
# Run this script at 1017D352 (or 1017D355) to capture one appRand state.
APP_RAND_STATE_LOAD_EA = 0x1017D352
APP_RAND_IMUL_EA = 0x1017D355

# SpriteEmitter2 fingerprint (m_u004_b).
TARGET_REPEATS = 60.0
TARGET_FADE_OUT = 0.154
TARGET_MAX_PARTICLES = 1


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


def read_i32(addr):
    return struct.unpack("<i", read_bytes(addr, 4))[0]


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
    # Recognize this before the module-relative checks: Core.dll's IDA EA
    # differs from its loaded segment-relative address.
    if eip == APP_RAND_STATE_LOAD_EA:
        return "APP_RAND_STATE_LOAD", None, eip
    if eip == APP_RAND_IMUL_EA:
        return "APP_RAND_BEFORE_IMUL", None, eip

    engine_base, rel = rel_in_module(eip, "engine")
    if engine_base is not None:
        hit = _match_milestone(rel, ENGINE_MILESTONE_REL, MILESTONE_TOLERANCE)
        if hit:
            return hit, engine_base, rel
        if 0x303D00 <= rel <= 0x303E00:
            return "ENGINE_SPAWN_TAIL", engine_base, rel
    hit = _match_milestone(eip, ENGINE_MILESTONE_IDA_EA, MILESTONE_TOLERANCE)
    if hit:
        return hit, engine_base, rel if engine_base is not None else eip
    core_base, core_rel = rel_in_module(eip, "core")
    if core_base is not None:
        if core_rel == 0x1010EDE0 or core_rel == 0x1010EDE6:
            return "CORE_GETRAND_STARTSPIN_Z", core_base, core_rel
        if 0x1010EDE0 <= core_rel <= 0x1010EE40:
            return "CORE_GETRAND_BODY", core_base, core_rel
    return "UNKNOWN", engine_base, rel if engine_base else 0


def try_read_tls_rand_state(milestone, slot):
    """Best-effort MSVC CRT TLS rand (PTD+0x14) on Win32 x86."""
    engine_milestones = {
        "PRE_START_SPIN", "ON_START_SPIN_CALL", "AFTER_START_SPIN",
        "AFTER_SPS", "ENGINE_SPAWN_TAIL", "UNKNOWN",
    }

    # At Engine spawn BPs, ECX/ESI often hold FRangeVector* (e.g. ebx+278h),
    # or the just-written StartSpin float bits — never TLS state.
    if milestone in engine_milestones:
        return None, (
            "not readable at Engine BP (ECX is FRange* or GetRand result). "
            "Stop inside Core.dll appFrand on 'mov ecx,[eax+14h]' before imul."
        )

    hints = []
    ecx = idc.get_reg_value("ECX") & 0xFFFFFFFF
    eax = idc.get_reg_value("EAX") & 0xFFFFFFFF

    # At 1017D352 the TLS pointer is already in EAX, but ECX has not yet
    # received its state. At 1017D355 both [EAX+14] and ECX are the state
    # immediately before the LCG update.
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

    # Reject ECX if it matches a slot field we just wrote (common false positive).
    try:
        spin_bits = read_u32(slot + 0x3C)
        if ecx == spin_bits:
            ecx = 0
    except RuntimeError:
        pass

    # Inside appFrand after mov ecx,[eax+14h], ECX holds TLS state.
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
    """FRange::GetRand: appFrand() * (Min - Max) + Max."""
    return random_fraction * (min_val - max_val) + max_val


def frange_vector_get_rand_z_y_x(x_range, y_range, z_range, state):
    """FRangeVector::GetRand draw order is Z, then Y, then X."""
    state, fraction_z = app_rand_step(state)
    value_z = frange_sample(z_range[0], z_range[1], fraction_z)
    state, fraction_y = app_rand_step(state)
    value_y = frange_sample(y_range[0], y_range[1], fraction_y)
    state, fraction_x = app_rand_step(state)
    value_x = frange_sample(x_range[0], x_range[1], fraction_x)
    return (value_x, value_y, value_z), state


def read_emitter_frange(emitter, offset):
    """FRange at emitter+offset: Min at +0, Max at +4 per axis block."""
    min_v, _ = read_f32(emitter + offset)
    max_v, _ = read_f32(emitter + offset + 4)
    return min_v, max_v


def line_f32(label, addr, slot_base):
    value, bits = read_f32(addr)
    off = addr - slot_base
    print("%-34s +0x%02X = % .9f  bits=0x%08X  x65535=% .6f" %
          (label, off, value, bits, value * URU_SCALE))


def main():
    eip = int(idc.get_reg_value("EIP")) & 0xFFFFFFFF
    emitter = int(idc.get_reg_value("EBX")) & 0xFFFFFFFF
    slot = int(idc.get_reg_value("EDI")) & 0xFFFFFFFF

    milestone, engine_base, rel = detect_milestone(eip)
    state, state_source = try_read_tls_rand_state(milestone, slot)

    repeats, _ = read_f32(emitter + 0xB4)
    fade_out, _ = read_f32(emitter + 0xE8)
    max_particles = read_i32(emitter + 0x108)

    is_target = (
        abs(repeats - TARGET_REPEATS) < 0.001 and
        abs(fade_out - TARGET_FADE_OUT) < 0.001 and
        max_particles == TARGET_MAX_PARTICLES
    )

    print("=" * 78)
    print("SPRITE EMITTER2 SPAWN DUMP")
    print("EIP=0x%08X  milestone=%s" % (eip, milestone))
    if engine_base:
        print("Engine.dll base=0x%08X  rel=+0x%X" % (engine_base, rel))
    print("emitter(EBX)=0x%08X  slot(EDI)=0x%08X" % (emitter, slot))
    print("Emitter: ColorScaleRepeats=%.6f FadeOutStart=%.6f MaxParticles=%d" %
          (repeats, fade_out, max_particles))
    print("Target SpriteEmitter2 fingerprint: %s" %
          ("MATCH" if is_target else "NOT MATCHED"))
    if not is_target:
        print("  !! Wrong emitter — use conditional BP, e.g.:")
        print("     dword [EBX+0xB4] == 0x42700000  (ColorScaleRepeats=60)")
        print("     dword [EBX+0xE8] == 0x3E1D70A4  (FadeOutStart=0.154)")
        print("     dword [EBX+0x108] == 1          (MaxParticles=1)")

    ss_min, ss_max = read_emitter_frange(emitter, 0x278)
    sps_min, sps_max = read_emitter_frange(emitter, 0x260)
    size_min_x, size_max_x = read_emitter_frange(emitter, EMITTER_SIZE_RANGE_OFF)
    size_min_y, size_max_y = read_emitter_frange(emitter, EMITTER_SIZE_RANGE_OFF + 8)
    size_min_z, size_max_z = read_emitter_frange(emitter, EMITTER_SIZE_RANGE_OFF + 16)
    print("Emitter ranges: StartSpin.X=[%.3f,%.3f]  SPS.X=[%.3f,%.3f]" %
          (ss_min, ss_max, sps_min, sps_max))
    print("Emitter +0x2CC FRangeVector (GetRand at FD37C2): X=[%.3f,%.3f] Y=[%.3f,%.3f] Z=[%.3f,%.3f]" %
          (size_min_x, size_max_x, size_min_y, size_max_y, size_min_z, size_max_z))
    if abs(size_min_x - 5.5) < 0.01 and abs(size_max_x - 5.5) < 0.01:
        print("  (X matches SE2 StartSizeRange=5.5 — slot+0x24 here is SIZE sample, not StartVelocity)")

    if state is not None and is_target:
        print("\nappRand state_before_StartSpin_GetRand:")
        print("  source=%s  state=0x%08X" % (state_source, state))
        # Each FRangeVector consumes exactly three appFrand calls: Z, Y, X.
        # This is the state just before the StartSpin vector's first (Z) draw.
        ss_y = read_emitter_frange(emitter, 0x278 + 8)
        ss_z = read_emitter_frange(emitter, 0x278 + 16)
        sps_y = read_emitter_frange(emitter, 0x260 + 8)
        sps_z = read_emitter_frange(emitter, 0x260 + 16)
        st = state
        start_spin, st = frange_vector_get_rand_z_y_x(
            (ss_min, ss_max), ss_y, ss_z, st)
        sps, st = frange_vector_get_rand_z_y_x(
            (sps_min, sps_max), sps_y, sps_z, st)
        print("  CPU mirror StartSpin=(%.6f, %.6f, %.6f) uc" % start_spin)
        print("  CPU mirror SPS=(%.6f, %.6f, %.6f) uc" % sps)
        print("  CPU mirror after StartSpin+SPS (6 draws) state=0x%08X" %
              (st & 0xFFFFFFFF))
        print("  -> Unity _SpriteSpinRandStateBits = 0x%08X" % (state & 0xFFFFFFFF))
    else:
        print("\nappRand state: %s" % state_source)

    spin_x, spin_bits = read_f32(slot + 0x3C)
    if milestone == "AFTER_START_SPIN" and is_target:
        if not (ss_min - 0.01 <= spin_x <= ss_max + 0.01):
            print("\n  !! StartSpin.X=%.6f outside emitter range [%.3f,%.3f]" %
                  (spin_x, ss_min, ss_max))
    if milestone == "AFTER_START_SPIN":
        print("\n(SPS +0x30 not written yet at AFTER_START_SPIN — value below may be stale)")

    if milestone == "AFTER_SIZE_TO_SLOT24":
        print("\n(slot +0x24..2C just written; UniformSize copy at FD37E1 may not have run yet)")
    print("\nSlot +0x24..2C (GetRand ebx+2CCh at FD37C2 — SIZE for SE2, not .uc StartVelocity):")
    line_f32("Velocity X (UC)", slot + 0x24, slot)
    line_f32("Velocity Y (UC)", slot + 0x28, slot)
    line_f32("Velocity Z (UC)", slot + 0x2C, slot)

    print("\nSpinsPerSecond (before *65535):")
    line_f32("SPS X (UC)", slot + 0x30, slot)
    line_f32("SPS Y",        slot + 0x34, slot)
    line_f32("SPS Z",        slot + 0x38, slot)

    print("\nStartSpin (before *65535):")
    line_f32("StartSpin X (UC)", slot + 0x3C, slot)
    line_f32("StartSpin Y",        slot + 0x40, slot)
    line_f32("StartSpin Z",        slot + 0x44, slot)

    time_value, time_bits = read_f32(slot + 0xAC)
    print("\nTime +0xAC = %.9f  bits=0x%08X" % (time_value, time_bits))

    print("\nBreakpoint guide (this session):")
    if engine_base:
        for name, rel_off in ENGINE_MILESTONE_REL.items():
            print("  %s: Engine.dll+0x%X  (abs EIP ~0x%08X)" %
                  (name, rel_off, engine_base + rel_off))
    else:
        for name, ea in ENGINE_MILESTONE_IDA_EA.items():
            print("  %s: EIP ~0x%08X" % (name, ea))
    print("  appRand state: Core.dll appFrand, BP on mov ecx,[eax+14h] before imul 214013")
    print("  Filter SE2:     [EBX+0xB4]==60.0f && [EBX+0xE8]==0.154f && [EBX+0x108]==1")
    print("=" * 78)


main()
