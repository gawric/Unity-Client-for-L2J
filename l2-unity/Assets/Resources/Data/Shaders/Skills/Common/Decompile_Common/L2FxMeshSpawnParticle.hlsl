#ifndef L2_FX_MESH_SPAWN_PARTICLE_INCLUDED
#define L2_FX_MESH_SPAWN_PARTICLE_INCLUDED

//
// UMeshEmitter::SpawnParticle TLS appRand stream (shared by MeshEmitter slots).
//
// LIVE VERIFIED: it_healing_potion_ta, Name="Wave"
//   (Unity MeshEmitter0 / L2 MeshEmitter4), SpawnParticleSnapshot 2026-07-17.
//   Same Core GetRand order as the MeshEmitter3 StartSpin chain (31 draws/slot).
// LIVE VERIFIED: shot_N_atk / e_u505 MeshEmitter225 "Spirit"
//   via L2Fx_MeshSpawnParticle_SampleLocVelSize (full XYZ),
//   SpawnSoulShotSpiritCapture 2026-07-22 draws=28 scopes=12 truncated=0.
//
// Formula:
//   Vel FRangeVector → Loc FRangeVector → scalar[0,1] → 2×zeroVec (6 frands)
//   → ColorMultiplier FRangeVector → Lifetime → InitialDelay → scalar1
//   → Size FRangeVector → StartSpin → SPS → 3×appFrand (SpinCCW)
//   = 28 logged GetRand draws + 3 trailing appFrand = 31 draws / slot.
//
// Same FRange walk also LIVE on SpriteEmitter325 "smog" (shape=0); sprite
// callers use L2FxSpriteSpawnParticle.hlsl for full Vel/Loc/Life/Size outs.
//
// Input state = TLS appRand immediately before StartVelocityRange GetRand.
// All ranges come from the effect material / UC — this module does not bake
// one effect's constants. Callers that need a different property walk add a
// separate Decompile_Common path; do not fork effect-local RNG.
//

#include "L2FxAppRand.hlsl"
#include "L2FxSpritePolar.hlsl"

static const int L2FX_MESH_SPAWN_DRAWS_BEFORE_START_SPIN = 22;
static const int L2FX_MESH_SPAWN_SLOT_TO_SLOT_DRAW_COUNT = 31;

// Mid-stream draws between Loc and Size that do not change LocZ/VelZ/SizeZ
// values for typical UC (fixed ColorMultiplier / Lifetime / Delay), but must
// advance TLS exactly as L2 SpawnParticle.
void L2Fx_MeshSpawnParticle_BurnDrawsBeforeSize(
    float2 colorMulXRange,
    float2 colorMulYRange,
    float2 colorMulZRange,
    float2 lifetimeRange,
    float2 initialDelayRange,
    float2 trailingScalarRange,
    inout uint state)
{
    // Mesh scalar @+0x2FC typically [0,1]
    L2Fx_FRange_GetRand(float2(0.0, 1.0), state);
    // Unused zero vectors (+0x1FC, +0x214): 6 appFrand
    [unroll]
    for (int i = 0; i < 6; ++i)
    {
        L2Fx_AppFrand(state);
    }
    // ColorMultiplierRange (+0xB8), Z→Y→X
    L2Fx_FRangeVector_GetRandYawPitchRoll(
        colorMulXRange,
        colorMulYRange,
        colorMulZRange,
        state);
    L2Fx_FRange_GetRand(lifetimeRange, state);
    L2Fx_FRange_GetRand(initialDelayRange, state);
    L2Fx_FRange_GetRand(trailingScalarRange, state);
}

// Samples StartVelocity.Z, StartLocation.Z, StartSize.Z in L2 draw order.
// Leaves state at StartSpin FRangeVector entry (draw 22).
void L2Fx_MeshSpawnParticle_SampleLocVelSizeZ(
    float2 locationZRange,
    float2 velocityZRange,
    float2 sizeZRange,
    float2 sizeXYRange,
    float2 colorMulXRange,
    float2 colorMulYRange,
    float2 colorMulZRange,
    float2 lifetimeRange,
    float2 initialDelayRange,
    float2 trailingScalarRange,
    inout uint state,
    out float locationZ,
    out float velocityZ,
    out float sizeZ)
{
    float3 velocity = L2Fx_FRangeVector_GetRandYawPitchRoll(
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        velocityZRange,
        state);
    velocityZ = velocity.z;

    float3 location = L2Fx_FRangeVector_GetRandYawPitchRoll(
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        locationZRange,
        state);
    locationZ = location.z;

    L2Fx_MeshSpawnParticle_BurnDrawsBeforeSize(
        colorMulXRange,
        colorMulYRange,
        colorMulZRange,
        lifetimeRange,
        initialDelayRange,
        trailingScalarRange,
        state);

    float3 size = L2Fx_FRangeVector_GetRandYawPitchRoll(
        sizeXYRange,
        sizeXYRange,
        sizeZRange,
        state);
    sizeZ = size.z;
}

uint L2Fx_MeshSpawnParticle_StateBeforeVelocity(uint burstBaseState, int slotIndex)
{
    uint state = burstBaseState;
    [loop]
    for (int i = 0; i < slotIndex * L2FX_MESH_SPAWN_SLOT_TO_SLOT_DRAW_COUNT; ++i)
    {
        state = state * L2FX_APP_RAND_MULTIPLIER + L2FX_APP_RAND_INCREMENT;
    }
    return state;
}

// Full Loc/Vel/Size XYZ through draw 21 (same stream as Sprite shape-0 /
// L2FxSpriteSpawnParticle). Leaves state at StartSpin entry (draw 22).
// Use for anisotropic mesh emitters (e.g. shot_N_atk MeshEmitter225 Spirit).
void L2Fx_MeshSpawnParticle_SampleLocVelSize(
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
    float2 trailingScalarRange,
    float2 sizeXRange,
    float2 sizeYRange,
    float2 sizeZRange,
    inout uint state,
    out float3 velocityUe,
    out float3 locationUe,
    out float3 colorMulRgb,
    out float lifetimeSeconds,
    out float initialDelaySeconds,
    out float3 sizeUe)
{
    velocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        velocityXRange, velocityYRange, velocityZRange, state);

    locationUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        locationXRange, locationYRange, locationZRange, state);

    // +0x2FC scalar [0,1]
    L2Fx_FRange_GetRand(float2(0.0, 1.0), state);
    [unroll]
    for (int i = 0; i < 6; ++i)
    {
        L2Fx_AppFrand(state);
    }

    colorMulRgb = L2Fx_FRangeVector_GetRandYawPitchRoll(
        colorMulXRange, colorMulYRange, colorMulZRange, state);
    lifetimeSeconds = L2Fx_FRange_GetRand(lifetimeRange, state);
    initialDelaySeconds = L2Fx_FRange_GetRand(initialDelayRange, state);
    L2Fx_FRange_GetRand(trailingScalarRange, state);

    sizeUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        sizeXRange, sizeYRange, sizeZRange, state);
}

// Same 31-draw mesh spawn stream as SampleLocVelSize, but Loc FRangeVector
// is StartLocationPolarRange (radius→phi→theta) when StartLocationShape=PTLS_Polar.
void L2Fx_MeshSpawnParticle_SampleVelPolarSize(
    float2 velocityXRange,
    float2 velocityYRange,
    float2 velocityZRange,
    float2 polarThetaDegreesMinMax,
    float2 polarPhiDegreesMinMax,
    float2 polarRadiusUuMinMax,
    float2 colorMulXRange,
    float2 colorMulYRange,
    float2 colorMulZRange,
    float2 lifetimeRange,
    float2 initialDelayRange,
    float2 trailingScalarRange,
    float2 sizeXRange,
    float2 sizeYRange,
    float2 sizeZRange,
    inout uint state,
    out float3 velocityUe,
    out float3 polarOffsetUe,
    out float3 colorMulRgb,
    out float lifetimeSeconds,
    out float initialDelaySeconds,
    out float3 sizeUe)
{
    velocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        velocityXRange, velocityYRange, velocityZRange, state);

    polarOffsetUe = L2Fx_SpritePolar_GetRandUe(
        polarThetaDegreesMinMax,
        polarPhiDegreesMinMax,
        polarRadiusUuMinMax,
        state);

    L2Fx_FRange_GetRand(float2(0.0, 1.0), state);
    [unroll]
    for (int i = 0; i < 6; ++i)
    {
        L2Fx_AppFrand(state);
    }

    colorMulRgb = L2Fx_FRangeVector_GetRandYawPitchRoll(
        colorMulXRange, colorMulYRange, colorMulZRange, state);
    lifetimeSeconds = L2Fx_FRange_GetRand(lifetimeRange, state);
    initialDelaySeconds = L2Fx_FRange_GetRand(initialDelayRange, state);
    L2Fx_FRange_GetRand(trailingScalarRange, state);

    sizeUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
        sizeXRange, sizeYRange, sizeZRange, state);
}

// UParticleEmitter UniformSize: StartSizeRange still GetRand's X/Y/Z for TLS,
// then particle Size uses X on all axes. Y/Z in UC are leftover and must not
// scale the mesh. Call after SampleLocVelSize / any XYZ size sample.
float3 L2Fx_MeshSize_ApplyUniformSize(float3 sampledSizeUe)
{
    return sampledSizeUe.xxx;
}

#endif // L2_FX_MESH_SPAWN_PARTICLE_INCLUDED
