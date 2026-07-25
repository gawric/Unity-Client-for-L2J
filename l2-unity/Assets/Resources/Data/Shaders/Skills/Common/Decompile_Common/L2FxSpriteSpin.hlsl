#ifndef L2_FX_SPRITE_SPIN_INCLUDED
#define L2_FX_SPRITE_SPIN_INCLUDED

// UE2.5 / Lineage II USpriteEmitter::FillVertexBuffer spin replication.
//
// Confirmed directly in Engine.dll:
//   SpawnParticle:
//     StartSpin = FRangeVector::GetRand(StartSpinRange) * 65535
//     SPS       = FRangeVector::GetRand(SpinsPerSecondRange) * 65535
//   FillVertexBuffer (Pitch / first slot component only):
//     SPS >= 0: angle = uint16(appRound(SPS * Time + StartSpin))
//     SPS <  0: angle = 0xFFFF - uint16(appRound(StartSpin - SPS * Time))
//
// VERIFIED AND PORTED — m_u004_b / SpriteEmitter2 SpawnParticle:
//   Live IDA trace captured the TLS state immediately before StartSpin:
//     state = 0xA1CC1994
//   The exact L2 slot writes for that same spawn were:
//     StartSpin.X (slot +0x3C) = 206.834915161
//     SPS.X       (slot +0x30) =   0.069389932
//   This library reproduces them from that state as 206.834925 and
//   0.069389935 respectively; the remaining difference is float32 rounding.
//
// Verified spawn rules:
//   - appRand LCG: state * 214013 + 2531011, wrapping uint32;
//   - appFrand: ((state >> 16) & 0x7FFF) / 32767;
//   - FRange: Max + appFrand * (Min - Max);
//   - FRangeVector draw order: Z, then Y, then X;
//   - StartSpinRange at emitter +0x278 -> slot +0x3C;
//   - SpinsPerSecondRange at +0x260 -> slot +0x30.
//
// The following renderer-stage behavior is implemented from decompilation but
// has not yet been independently captured in FillVertexBuffer:
//   slot values multiplied by 65535, appRound, uint16 truncation, and final
//   SpinCCWorCW sign handling.
//
// The caller supplies the 32-bit appRand state immediately before the
// StartSpin FRangeVector::GetRand call. SpawnParticle then consumes exactly:
//   StartSpin FRangeVector Z/Y/X, SPS FRangeVector Z/Y/X, SPS X/Y/Z modifiers.

#include "../L2FxEmitterSpawn.hlsl"
#include "L2FxAppRand.hlsl"

static const float L2FX_SPRITE_SPIN_UC_TO_SLOT = 65535.0;
static const float L2FX_SPRITE_SPIN_URU_PER_TURN = 65536.0;
static const float L2FX_SPRITE_SPIN_URU_TO_RAD =
    6.28318530718 / L2FX_SPRITE_SPIN_URU_PER_TURN;

float L2Fx_SpriteSpin_AppRound(float inputValue)
{
    // The captured positive values are non-negative. HLSL round is avoided so
    // shader targets use the same explicit half-up behavior as the L2 path.
    return floor(inputValue + 0.5);
}

float L2Fx_SpriteSpin_Uint16(float roundedValue)
{
    // Keep this float-only: Unity's legacy Cg parser used by target 3.0 does
    // not accept the uint bitwise expression that would normally implement
    // the uint16 cast. All sprite spin inputs here are non-negative.
    return fmod(max(roundedValue, 0.0), L2FX_SPRITE_SPIN_URU_PER_TURN);
}

float L2Fx_SpriteSpin_StartSlotFloatFromUc(float startSpinUc)
{
    return startSpinUc * L2FX_SPRITE_SPIN_UC_TO_SLOT;
}

float L2Fx_SpriteSpin_SpsSlotFloatFromUc(float spinsPerSecondUc, float directionSign)
{
    return spinsPerSecondUc * directionSign * L2FX_SPRITE_SPIN_UC_TO_SLOT;
}

// StartSpin-only variant for callers that need the spawn orientation but not
// SPS/SpinCCWorCW. Mirrors the first FRangeVector draw in SpawnParticle.
float L2Fx_SpriteSpin_StartSlotFloatFromAppRandState(
    float2 startSpinRangeUc,
    uint startSpinRandState)
{
    uint state = startSpinRandState;
    float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        startSpinRangeUc,
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        state);
    return L2Fx_SpriteSpin_StartSlotFloatFromUc(startSpinUc.x);
}

float L2Fx_SpriteSpin_EvaluateAngleUru(
    float startSpinSlotFloat,
    float spinsPerSecondSlotFloat,
    float ageSeconds)
{
    float age = max(ageSeconds, 0.0);

    if (spinsPerSecondSlotFloat >= 0.0)
    {
        return L2Fx_SpriteSpin_Uint16(
            L2Fx_SpriteSpin_AppRound(
                spinsPerSecondSlotFloat * age + startSpinSlotFloat));
    }

    float alternate = L2Fx_SpriteSpin_Uint16(
        L2Fx_SpriteSpin_AppRound(
            startSpinSlotFloat - spinsPerSecondSlotFloat * age));
    return 65535.0 - alternate;
}

float L2Fx_SpriteSpin_EvaluateRadians(
    float startSpinSlotFloat,
    float spinsPerSecondSlotFloat,
    float ageSeconds)
{
    return L2Fx_SpriteSpin_EvaluateAngleUru(
        startSpinSlotFloat,
        spinsPerSecondSlotFloat,
        ageSeconds) * L2FX_SPRITE_SPIN_URU_TO_RAD;
}

// UE's sprite plane and Unity's camera-right/camera-up billboard plane have
// opposite winding. Negate only while mapping the verified L2 angle to Unity.
float2 L2Fx_SpriteSpin_RotateBillboardOffset(float2 quadXY, float angleRadians)
{
    float sine;
    float cosine;
    sincos(-angleRadians, sine, cosine);
    return float2(
        quadXY.x * cosine - quadXY.y * sine,
        quadXY.x * sine + quadXY.y * cosine);
}

// Exact target SpriteEmitter2 SpawnParticle path:
// StartSpinRange at emitter +0x278 writes slot +0x3C first, then
// SpinsPerSecondRange at +0x260 writes slot +0x30. Both FRangeVectors draw
// Z/Y/X. Three following appFrand calls conditionally multiply SPS X/Y/Z by
// -1 according to SpinCCWorCW. The target has X=0, Y=1, Z=1.
void L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
    float2 startSpinRangeUc,
    float2 spinsPerSecondRangeUc,
    float3 spinCcwOrCw,
    uint startSpinRandState,
    out float startSpinSlotFloat,
    out float spinsPerSecondSlotFloat)
{
    uint state = startSpinRandState;
    float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        startSpinRangeUc,
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        state);
    float3 spinsPerSecondUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        spinsPerSecondRangeUc,
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        state);

    if (L2Fx_AppFrand(state) < spinCcwOrCw.x) { spinsPerSecondUc.x *= -1.0; }
    if (L2Fx_AppFrand(state) < spinCcwOrCw.y) { spinsPerSecondUc.y *= -1.0; }
    if (L2Fx_AppFrand(state) < spinCcwOrCw.z) { spinsPerSecondUc.z *= -1.0; }

    startSpinSlotFloat = L2Fx_SpriteSpin_StartSlotFloatFromUc(startSpinUc.x);
    spinsPerSecondSlotFloat = L2Fx_SpriteSpin_SpsSlotFloatFromUc(
        spinsPerSecondUc.x,
        1.0);
}

#endif // L2_FX_SPRITE_SPIN_INCLUDED
