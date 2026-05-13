#ifndef L2_FX_MESH_PARTICLE_MOTION_INCLUDED
#define L2_FX_MESH_PARTICLE_MOTION_INCLUDED

// ═══════════════════════════════════════════════════════════════
// L2_FX_MESH_PARTICLE_MOTION — Unity-corrected motion helpers
//
// Based on UE3 FParticle / UMeshEmitter disassembly.
//
// Corrections vs original library:
//   1. Polar: Y-up, pitch from horizontal plane.
//   2. VelocityLoss = LINEAR subtraction, not exponential drag.
//   3. Spin = SINGLE SCALAR (rotation around local Z).
//
// Depends on: L2FxParticleAnim.hlsl, L2_FX_EMITTER_SPAWN_INCLUDED.hlsl
// ═══════════════════════════════════════════════════════════════

#include "L2FxParticleAnim.hlsl"
#include "L2FxEmitterSpawn.hlsl"

// ───────────────────────────────────────────────────────────────
// Displacement: V₀t + ½at² (constant accel, NO drag)
// ───────────────────────────────────────────────────────────────


// Polar spawn offset for Unity's Y-up world: azimuth around +Y, polar angle from +Y.
// Matches UE-style StartLocationPolarRange semantics when converted to Unity axes.
float3 L2Fx_SpawnOffsetPolarYDegrees(
    float2 azimuthDegMinMax,
    float2 polarFromPositiveYDegMinMax,
    float2 radiusMinMax,
    float seed,
    float startTime)
{
    float theta = L2Fx_RandomRange(azimuthDegMinMax, seed, startTime, 71.0) * L2Fx_DegToRad;
    float phi = L2Fx_RandomRange(polarFromPositiveYDegMinMax, seed, startTime, 73.0) * L2Fx_DegToRad;
    float radius = L2Fx_RandomRange(radiusMinMax, seed, startTime, 79.0);
    float sinPhi = sin(phi);
    return float3(
        radius * sinPhi * cos(theta),
        radius * cos(phi),
        radius * sinPhi * sin(theta));
}


// Exponential drag approximation plus constant acceleration.
float3 L2Fx_DampedDisplacement(float3 velocity, float3 acceleration, float velocityLoss, float ageSeconds)
{
    float t = max(0.0, ageSeconds);
    float loss = max(0.0, velocityLoss);
    float3 velocityDisplacement = velocity * t;
    if (loss > 1e-4)
    {
        velocityDisplacement = velocity * ((1.0 - exp(-loss * t)) / loss);
    }

    return velocityDisplacement + (0.5 * acceleration * t * t);
}

float3 L2Fx_DisplacementConstantAccel(
    float3 velocity,
    float3 acceleration,
    float ageSeconds)
{
    float t = max(0.0, ageSeconds);
    return velocity * t + 0.5 * acceleration * t * t;
}

// ───────────────────────────────────────────────────────────────
// Horizontal direction from spawn offset (XZ plane)
// ───────────────────────────────────────────────────────────────

float2 L2Fx_OutwardDirectionXZ(
    float3 spawnOffset,
    float2 fallbackAzimuthDegMinMax,
    float seed, float startTime, float salt)
{
    float2 hDir = spawnOffset.xz;
    float len = length(hDir);

    if (len > 1e-5)
        return hDir / len;

    float fallbackAngle = L2Fx_RandomRange(
        fallbackAzimuthDegMinMax, seed, startTime, salt) * L2Fx_DegToRad;
    return float2(cos(fallbackAngle), sin(fallbackAngle));
}

// ───────────────────────────────────────────────────────────────
// Spin — SCALAR, rotation around local Z
// ───────────────────────────────────────────────────────────────

float L2Fx_ComputeSpinAngleRadians(
    float startSpin,
    float spinsPerSecond,
    float ageSeconds)
{
    float units = startSpin + spinsPerSecond * ageSeconds;
    return units * L2FX_SPIN_TO_RAD;
}

void L2Fx_ApplyMeshScalarSpin(
    inout float3 positionOS,
    inout float3 normalOS,
    bool spinParticles,
    float spinAngleRad)
{
    if (!spinParticles)
        return;

    float c = cos(spinAngleRad);
    float s = sin(spinAngleRad);

    positionOS = float3(
        positionOS.x * c - positionOS.y * s,
        positionOS.x * s + positionOS.y * c,
        positionOS.z);

    normalOS = float3(
        normalOS.x * c - normalOS.y * s,
        normalOS.x * s + normalOS.y * c,
        normalOS.z);
}

void L2Fx_RotatePositionAndNormal(
    inout float3 positionOS,
    inout float3 normalOS,
    float3 angles)
{
    positionOS = L2Fx_RotateX(positionOS, angles.x);
    positionOS = L2Fx_RotateY(positionOS, angles.y);
    positionOS = L2Fx_RotateZ(positionOS, angles.z);
    normalOS = L2Fx_RotateX(normalOS, angles.x);
    normalOS = L2Fx_RotateY(normalOS, angles.y);
    normalOS = L2Fx_RotateZ(normalOS, angles.z);
}

void L2Fx_ApplyMeshParticleSpin(
    inout float3 positionOS,
    inout float3 normalOS,
    float spinParticles,
    float ageSeconds,
    float2 startSpinX,
    float2 startSpinY,
    float2 startSpinZ,
    float2 spinsPerSecondX,
    float2 spinsPerSecondY,
    float2 spinsPerSecondZ,
    float seed,
    float startTime)
{
    if (spinParticles < 0.5)
    {
        return;
    }

    float3 angles = L2Fx_RotationAngles(
        ageSeconds,
        startSpinX,
        startSpinY,
        startSpinZ,
        spinsPerSecondX,
        spinsPerSecondY,
        spinsPerSecondZ,
        seed,
        startTime);

    L2Fx_RotatePositionAndNormal(positionOS, normalOS, angles);
}

#endif // L2_FX_MESH_PARTICLE_MOTION_INCLUDED