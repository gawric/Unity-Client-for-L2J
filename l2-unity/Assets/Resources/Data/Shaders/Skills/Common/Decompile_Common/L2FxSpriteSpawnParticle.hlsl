#ifndef L2_FX_SPRITE_SPAWN_PARTICLE_INCLUDED
#define L2_FX_SPRITE_SPAWN_PARTICLE_INCLUDED

//
// UParticleEmitter::SpawnParticle TLS appRand stream for SpriteEmitter when
// StartLocationShape@+0x174 == 0 (box / default): LocRange GetRand fires.
//
// LIVE VERIFIED (2026-07-22): e_u505_c / shot_N_atk SpriteEmitter325 "smog"
//   SpawnSoulShotSmogCapture rngStream draws=28 scopes=12 truncated=0
//
//   [0..2]   StartVelocityRange       +0x3A0  vector Z,Y,X
//   [3..5]   StartLocationRange       +0x158  vector (smog: min=max=0)
//   [6]      Mesh/OtherScalar         +0x2FC  scalar [0,1]
//   [7..9]   UnusedRangeVectorA       +0x1FC  vector (zeros)
//   [10..12] UnusedRangeVectorB       +0x214  vector (zeros)
//   [13..15] ColorMultiplierRange     +0xB8   vector
//   [16]     LifetimeRange            +0x380  scalar
//   [17]     InitialDelayRange        +0x378  scalar
//   [18]     StartVelocityRadialRange +0x198  scalar (often 1..1)
//   [19..21] StartSizeRange           +0x2CC  vector
//   [22..24] StartSpinRange           +0x278  vector  ← ParticleGroup spin state
//   [25..27] SpinsPerSecondRange      +0x260  vector
//   (+ 3 appFrand SpinCCWorCW after SPS; not in FRange rngStream)
//
// Polar@+0x180 is NOT in this stream (shape≠Polar). Do not call L2FxSpritePolar.
// Same mid-stream burn as L2FxMeshSpawnParticle (Wave); this module returns the
// sprite-needed Vel/Loc/ColorMul/Life/Size instead of Z-only mesh samples.
//
// Input state = TLS immediately before StartVelocityRange GetRand.
// Leaves state at StartSpin entry (draw 22) — matches _SpriteSpinRandStateBits.
//

#include "L2FxAppRand.hlsl"

static const int L2FX_SPRITE_SPAWN_DRAWS_BEFORE_START_SPIN = 22;
static const int L2FX_SPRITE_SPAWN_FRANGE_DRAWS_PER_SLOT = 28;

// Full shape-0 spawn sample through StartSize (draws 0..21).
void L2Fx_SpriteSpawnParticle_SampleShape0ThroughSize(
    float2 velocityXRange,
    float2 velocityYRange,
    float2 velocityZRange,
    float2 locationXRange,
    float2 locationYRange,
    float2 locationZRange,
    float2 colorMulXRange,
    float2 colorMulYRange,
    float2 colorMulZRange,
    float2 lifetimeRange,
    float2 initialDelayRange,
    float2 velocityRadialRange,
    float2 sizeXRange,
    float2 sizeYRange,
    float2 sizeZRange,
    inout uint state,
    out float3 velocityUe,
    out float3 locationUe,
    out float3 colorMulRgb,
    out float lifetimeSeconds,
    out float initialDelaySeconds,
    out float velocityRadial,
    out float3 sizeUu)
{
    // [0..2] StartVelocityRange
    velocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        velocityXRange, velocityYRange, velocityZRange, state);

    // [3..5] StartLocationRange (may be all zeros — still advances TLS)
    locationUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        locationXRange, locationYRange, locationZRange, state);

    // [6] +0x2FC scalar [0,1]
    L2Fx_FRange_GetRand(float2(0.0, 1.0), state);

    // [7..12] unused zero vectors +0x1FC / +0x214
    [unroll]
    for (int i = 0; i < 6; ++i)
    {
        L2Fx_AppFrand(state);
    }

    // [13..15] ColorMultiplierRange
    colorMulRgb = L2Fx_FRangeVector_GetRandYawPitchRoll(
        colorMulXRange, colorMulYRange, colorMulZRange, state);

    // [16..18] Lifetime, InitialDelay, VelocityRadial
    lifetimeSeconds = L2Fx_FRange_GetRand(lifetimeRange, state);
    initialDelaySeconds = L2Fx_FRange_GetRand(initialDelayRange, state);
    velocityRadial = L2Fx_FRange_GetRand(velocityRadialRange, state);

    // [19..21] StartSizeRange (UniformSize billboards use .x after all three draws)
    sizeUu = L2Fx_FRangeVector_GetRandYawPitchRoll(
        sizeXRange, sizeYRange, sizeZRange, state);
}

#endif // L2_FX_SPRITE_SPAWN_PARTICLE_INCLUDED
