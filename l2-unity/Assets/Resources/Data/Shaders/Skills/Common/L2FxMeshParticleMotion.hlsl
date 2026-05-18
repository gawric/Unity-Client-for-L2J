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

// PTVD_OwnerAndStartPosition: StartVelocityRange X/Y scale UE direction components (not a single radial speed).
// spawnOffsetUnity uses the same UE→Unity remap as StartLocationOffset (x, z, y).
float3 L2Fx_VelocityOwnerAndStartPosition(
    float3 spawnOffsetUnity,
    float2 velocityRangeX_UE,
    float2 velocityRangeY_UE,
    float seed,
    float startTime)
{
    float3 dUE = float3(spawnOffsetUnity.x, spawnOffsetUnity.z, spawnOffsetUnity.y);
    float len = length(dUE);
    float3 nUE = len > 1e-5 ? (dUE / len) : float3(0, 0, 1);
    float sx = L2Fx_RandomRange(velocityRangeX_UE, seed, startTime, 113.0);
    float sy = L2Fx_RandomRange(velocityRangeY_UE, seed, startTime, 115.0);
    float3 velUE = float3(nUE.x * sx, nUE.y * sy, nUE.z * sy);
    return float3(velUE.x, velUE.z, velUE.y);
}

// UE FVector (X,Y,Z) → Unity (X,Z,Y) for Y-up.
float3 L2Fx_UeVectorToUnity(float3 vUe)
{
    return float3(vUe.x, vUe.z, vUe.y);
}

// Spawn_Velocity (row 4) + GetVelocityDirectionFrom Project (row 31):
//   vel = lerp(VelMin, VelMax, rand); dir = normalize(pos - owner); vel = dir * dot(vel, dir)
float3 L2Fx_VelocitySpawnThenProjectOnOwner(
    float3 spawnPosUe,
    float3 ownerPosUe,
    float2 velocityRangeX_UE,
    float2 velocityRangeY_UE,
    float2 velocityRangeZ_UE,
    float seed,
    float startTime)
{
    float3 velUe = float3(
        L2Fx_RandomRange(velocityRangeX_UE, seed, startTime, 101.0),
        L2Fx_RandomRange(velocityRangeY_UE, seed, startTime, 103.0),
        L2Fx_RandomRange(velocityRangeZ_UE, seed, startTime, 107.0));

    float3 dirUe = spawnPosUe - ownerPosUe;
    float len = length(dirUe);
    dirUe = len > 1e-5 ? (dirUe / len) : float3(0, 0, 1);
    return dirUe * dot(velUe, dirUe);
}

// Update_MovePosition + MoveVelocity + VelocityLoss (rows 36–37, 48): v=v0+a*t-loss*t, p=v0*t+½a*t²-½loss*t²
float3 L2Fx_DisplacementLinearVelocityLoss(
    float3 velocity,
    float3 acceleration,
    float3 velocityLossPerSec,
    float ageSeconds)
{
    float t = max(0.0, ageSeconds);
    return velocity * t + 0.5 * acceleration * t * t - 0.5 * velocityLossPerSec * t * t;
}

// Horizontal direction from spawn offset (XZ plane)
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

// Sharp sideways burst (XZ) + soft fall (Y). VelocityRange X = horizontal, Y = initial sink.
float3 L2Fx_VelocityFogSpreadHorizontal(
    float3 spawnOffsetUnity,
    float2 horizontalSpeedRange_UE,
    float2 downwardSpeedRange_UE,
    float downwardScale,
    float horizontalBoost,
    float2 fallbackAzimuthDeg,
    float seed,
    float startTime)
{
    float2 hDir = L2Fx_OutwardDirectionXZ(spawnOffsetUnity, fallbackAzimuthDeg, seed, startTime, 181.0);
    float hs = L2Fx_RandomRange(horizontalSpeedRange_UE, seed, startTime, 113.0) * horizontalBoost;
    float ds = L2Fx_RandomRange(downwardSpeedRange_UE, seed, startTime, 115.0) * downwardScale;
    return float3(hDir.x * hs, -ds, hDir.y * hs);
}

// Horizontal spread slows (VelocityLoss); vertical keeps Acceleration (gravity), no loss on Y.
float3 L2Fx_DisplacementFogFall(
    float3 velocity,
    float3 acceleration,
    float horizontalVelocityLossPerSec,
    float ageSeconds)
{
    float t = max(0.0, ageSeconds);
    float3 velH = float3(velocity.x, 0.0, velocity.z);
    float3 velV = float3(0.0, velocity.y, 0.0);
    float3 lossH = float3(horizontalVelocityLossPerSec, 0.0, horizontalVelocityLossPerSec);
    return L2Fx_DisplacementLinearVelocityLoss(velH, float3(0, 0, 0), lossH, t)
        + L2Fx_DisplacementLinearVelocityLoss(velV, acceleration, float3(0, 0, 0), t);
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

// UMeshEmitter .uc SpinsPerSecondRange (e.g. 0.02) = revolutions per second, not URU.
// StartSpin from .uc is 0..1 rotations. Do NOT use L2FX_SPIN_TO_RAD here.
float L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(
    float startSpinRevolutions,
    float spinsPerSecond,
    float ageSeconds)
{
    return (startSpinRevolutions + spinsPerSecond * ageSeconds) * L2Fx_TwoPi;
}

// Magic circle meshes on ground in Unity: spin around +Y (not UE local Z / XY plane).
void L2Fx_ApplyMeshSpinAroundY(
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
        positionOS.x * c + positionOS.z * s,
        positionOS.y,
        -positionOS.x * s + positionOS.z * c);

    normalOS = float3(
        normalOS.x * c + normalOS.z * s,
        normalOS.y,
        -normalOS.x * s + normalOS.z * c);
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

void L2Fx_ApplySpinCCWorCW_Vector(inout float3 spinsPerSecond, float3 ccwOrCw)
{
    spinsPerSecond.x *= (ccwOrCw.x == 0.0) ? -1.0 : 1.0;
    spinsPerSecond.y *= (ccwOrCw.y == 0.0) ? -1.0 : 1.0;
    spinsPerSecond.z *= (ccwOrCw.z == 0.0) ? -1.0 : 1.0;
}

float L2Fx_ApplySpinCCWorCW_Scalar(float spinsPerSecond, float ccwOrCwX)
{
    return (ccwOrCwX == 0.0) ? -spinsPerSecond : spinsPerSecond;
}

#endif // L2_FX_MESH_PARTICLE_MOTION_INCLUDED