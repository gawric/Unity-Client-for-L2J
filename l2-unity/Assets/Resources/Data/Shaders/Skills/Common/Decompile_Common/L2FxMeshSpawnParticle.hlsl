#ifndef L2_FX_MESH_SPAWN_PARTICLE_INCLUDED
#define L2_FX_MESH_SPAWN_PARTICLE_INCLUDED

//
// UMeshEmitter::SpawnParticle TLS appRand stream (shared by MeshEmitter slots).
//
// VERIFIED ONLY (so far): it_healing_potion_ta, Name="Wave"
//   (Unity MeshEmitter0 / L2 MeshEmitter4), live SpawnParticleSnapshot 2026-07-17.
//   Same Core GetRand order as the MeshEmitter3 StartSpin chain (31 draws/slot).
//   Do not assume other MeshEmitters match this property walk until checked.
//
// Formula:
//   Vel FRangeVector → Loc FRangeVector → scalar[0,1] → 2×zeroVec (6 frands)
//   → ColorMultiplier FRangeVector → Lifetime → InitialDelay → scalar1
//   → Size FRangeVector → StartSpin → SPS → 3×appFrand (SpinCCW)
//   = 28 logged GetRand draws + 3 trailing appFrand = 31 draws / slot.
//
// Input state = TLS appRand immediately before StartVelocityRange GetRand.
// All ranges come from the effect material / UC — this module does not bake
// one effect's constants. Callers that need a different property walk add a
// separate Decompile_Common path; do not fork effect-local RNG.
//

#include "L2FxAppRand.hlsl"

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

#endif // L2_FX_MESH_SPAWN_PARTICLE_INCLUDED
