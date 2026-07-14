#ifndef L2_FX_MESH_SPIN_INCLUDED
#define L2_FX_MESH_SPIN_INCLUDED

// UMeshEmitter SpinParticles: UE2.5 FRotator helpers.
//
// CONFIRMED by UMeshEmitter::RenderParticles + live slots:
//   runtimeRotatorURU = trunc(startRotationURU + spinVelocityURUPerSecond * age).
//   FRotator trig wraps one turn at 65536 URU; radians = URU * 2*pi / 65536.
//   UC normalized StartSpin/SpinsPerSecond values are converted with 65535.
//
// Validation:
// - LineageEffect.m_u004_b_2 / MeshEmitter6 (the current healing-potion mesh):
//   StartSize=0.065, Lifetime=1.5, two slots; three StartSpin axes are random
//   in [0, 65535], and all spin velocities are zero. This path is confirmed.
// - LineageEffect.m_u008_b_2 / MeshEmitter6:
//   StartSpinRange.X=0.255 -> Yaw=16711.4238 (=0.255*65535);
//   SpinsPerSecondRange.X=0.1 -> Yaw speed=+6553.5 (=0.1*65535).
//   Thus UC X maps to runtime Yaw and SpinCCWorCW.X=0 yields a positive rate.
//
// Exact StartSpin RNG path (verified 2026-07-13):
// - UParticleEmitter::SpawnParticle calls FRangeVector::GetRand;
// - GetRand uses appFrand(), not appSRand();
// - appRand is the MSVC TLS LCG: state=state*214013+2531011;
// - FRangeVector draws Roll(Z), Pitch(Y), Yaw(X), then stores Yaw/Pitch/Roll;
// - FRange::GetRand is Max + appFrand() * (Min - Max).
//
// Still pending before declaring every mesh setting universal:
// - SpinCCWorCW axis value 1 direction;
// - UC Y/Z -> runtime Pitch/Roll mapping;
// - replaying the full CRT TLS stream before every emitter spawn.

#include "../L2FxEmitterSpawn.hlsl"
#include "L2FxAppRand.hlsl"

static const float L2FX_MESH_SPIN_UC_TO_URU = 65535.0;
static const float L2FX_MESH_SPIN_URU_PER_TURN = 65536.0;
static const float L2FX_MESH_SPIN_URU_TO_RAD = 6.28318530718 / L2FX_MESH_SPIN_URU_PER_TURN;

// Inputs are already in runtime order (Yaw, Pitch, Roll). This deliberately
// does not assert a universal UC Y/Z mapping; the caller provides it.
float3 L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
    float2 yawRangeUc,
    float2 pitchRangeUc,
    float2 rollRangeUc,
    float seed,
    float startTime)
{
    return float3(
        L2Fx_RandomRange(yawRangeUc, seed, startTime, 401.0),
        L2Fx_RandomRange(pitchRangeUc, seed, startTime, 409.0),
        L2Fx_RandomRange(rollRangeUc, seed, startTime, 419.0))
        * L2FX_MESH_SPIN_UC_TO_URU;
}

// Exact UE2.5 SpawnParticle StartSpin path. startSpinRandState must equal the
// appRand TLS state immediately before FRangeVector::GetRand's Roll draw.
// Use a uint supplied by CPU/replay data: a float seed loses 32-bit precision.
float3 L2Fx_MeshSpin_StartYawPitchRollUruFromAppRandState(
    float2 yawRangeUc,
    float2 pitchRangeUc,
    float2 rollRangeUc,
    uint startSpinRandState)
{
    uint state = startSpinRandState;
    float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        yawRangeUc,
        pitchRangeUc,
        rollRangeUc,
        state);
    return startSpinUc * L2FX_MESH_SPIN_UC_TO_URU;
}

// Inputs are already in runtime order (Yaw, Pitch, Roll). directionSign must
// be derived from verified SpinCCWorCW semantics; +1 is confirmed for UC X=0.
float3 L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
    float3 spinsPerSecond,
    float3 directionSign)
{
    return spinsPerSecond * directionSign * L2FX_MESH_SPIN_UC_TO_URU;
}

// Matches the int cast in UMeshEmitter::RenderParticles. The return order is
// (Yaw, Pitch, Roll), matching the raw particle-slot fields +0x3C/+0x40/+0x44.
float3 L2Fx_MeshSpin_EvaluateYawPitchRollUru(
    float3 startYawPitchRollUru,
    float3 velocityYawPitchRollUruPerSecond,
    float ageSeconds)
{
    return trunc(
        startYawPitchRollUru
        + velocityYawPitchRollUruPerSecond * max(ageSeconds, 0.0));
}

float3 L2Fx_MeshSpin_YawPitchRollUruToRadians(float3 yawPitchRollUru)
{
    return yawPitchRollUru * L2FX_MESH_SPIN_URU_TO_RAD;
}

// FRotator constructor order in the original renderer is (Pitch, Yaw, Roll).
float3 L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(float3 yawPitchRoll)
{
    return float3(yawPitchRoll.y, yawPitchRoll.x, yawPitchRoll.z);
}

// Exact UE2.5 FRotator-to-matrix coefficients from sub_1BC63F0. HLSL's
// mul(vector, matrix) uses the same row-vector convention as FVector * FMatrix.
float3 L2Fx_MeshSpin_RotateUeLocalPositionPitchYawRoll(
    float3 ueLocalPosition,
    float3 pitchYawRollRadians)
{
    float sinPitch;
    float cosPitch;
    float sinYaw;
    float cosYaw;
    float sinRoll;
    float cosRoll;
    sincos(pitchYawRollRadians.x, sinPitch, cosPitch);
    sincos(pitchYawRollRadians.y, sinYaw, cosYaw);
    sincos(pitchYawRollRadians.z, sinRoll, cosRoll);

    float3x3 rotationRows = float3x3(
        cosPitch * cosYaw,
        cosPitch * sinYaw,
        sinPitch,
        sinRoll * sinPitch * cosYaw - cosRoll * sinYaw,
        sinRoll * sinPitch * sinYaw + cosRoll * cosYaw,
        -sinRoll * cosPitch,
        -(cosRoll * sinPitch * cosYaw + sinRoll * sinYaw),
        cosYaw * sinRoll - cosRoll * sinPitch * sinYaw,
        cosRoll * cosPitch);

    return mul(ueLocalPosition, rotationRows);
}

// Mesh vertex coordinates imported from UE follow the same basis bridge as
// positional data: Unity(X,Y,Z) = UE(X,Z,Y). Conjugating the UE FRotator by
// that basis preserves the original rotation in Unity object coordinates.
float3 L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
    float3 unityLocalPosition,
    float3 pitchYawRollRadians)
{
    float3 ueLocalPosition = float3(
        unityLocalPosition.x,
        unityLocalPosition.z,
        unityLocalPosition.y);
    float3 ueRotatedPosition = L2Fx_MeshSpin_RotateUeLocalPositionPitchYawRoll(
        ueLocalPosition,
        pitchYawRollRadians);
    return float3(
        ueRotatedPosition.x,
        ueRotatedPosition.z,
        ueRotatedPosition.y);
}

#endif // L2_FX_MESH_SPIN_INCLUDED
