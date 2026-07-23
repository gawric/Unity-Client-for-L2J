#ifndef L2_FX_START_LOCATION_RANGE_INCLUDED
#define L2_FX_START_LOCATION_RANGE_INCLUDED

// L2FxStartLocationRange
//
// Status: LIVE — e_u505_c / shot_N_atk "Particle" SpawnSoulShotParticleCapture 2026-07-22
//
//   Range field IS populated at +0x158 (X+/-1 YZ+/-5), but SpawnParticle does
//   NOT call FRangeVector::GetRand(+0x158) for this emitter.
//   Live StartLocationShape@+0x174 = 2 (UC file says PTLS_Polar=3).
//   RNG scopes: Vel(+0x3A0) then Polar(+0x180) — no LocRange scope.
//   Therefore L2Fx_StartLocationRange_* must NOT be applied for this Particle
//   layer; use Offset(+0x14C)=(2,0,0) + Polar only.
//
//   When to use this module: emitters whose SpawnParticle snapshot shows a
//   scope at emitter+0x158 (shape 0 or live-3 box path). Verify with
//   "L2FxStartLocationRange mirror: result=PASS" before marking an effect PASS.
//
// SOURCE: Lineage II Interlude Engine.dll, UParticleEmitter::SpawnParticle.
// IDA often misnames this field (seen as UseSkeletalLocationAs_90). The real
// StartLocationRange_47 symbol later in the same function multiplies
// ColorMultiplier (+0x84) — that is ColorMultiplierRange, NOT this box path.
//
// UC: StartLocationRange=(X=(Min,Max), Y=(Min,Max), Z=(Min,Max)) in UU.
//
// SpawnParticle (provisional + LIVE SoulShot Particle):
//   Location is first written from StartLocationOffset (+0x14C).
//   LIVE e_u505 Particle: shape@+0x174 == 2 → Polar GetRand(+0x180) only;
//     StartLocationRange(+0x158) is present in memory but NOT sampled.
//   IDA also shows a box path when shape is 0 or 3:
//     Location += FRangeVector::GetRand(StartLocationRange)
//   Always confirm with SpawnParticleSnapshot scopes before using this add.
//
// FRangeVector::GetRand draw order is Z, then Y, then X (same as AppRand helper).
// Returned float3 is UE axis order (X, Y, Z).

#include "L2FxAppRand.hlsl"

// Box jitter in UE/L2 UU from .uc StartLocationRange Min/Max per axis.
float3 L2Fx_StartLocationRange_GetRandUe(
    float2 rangeXUu,
    float2 rangeYUu,
    float2 rangeZUu,
    inout uint appRandState)
{
    return L2Fx_FRangeVector_GetRandYawPitchRoll(
        rangeXUu,
        rangeYUu,
        rangeZUu,
        appRandState);
}

// SpawnParticle: Location += GetRand(StartLocationRange).
float3 L2Fx_StartLocationRange_ApplyUe(
    float3 locationUe,
    float2 rangeXUu,
    float2 rangeYUu,
    float2 rangeZUu,
    inout uint appRandState)
{
    return locationUe + L2Fx_StartLocationRange_GetRandUe(
        rangeXUu,
        rangeYUu,
        rangeZUu,
        appRandState);
}

#endif // L2_FX_START_LOCATION_RANGE_INCLUDED
