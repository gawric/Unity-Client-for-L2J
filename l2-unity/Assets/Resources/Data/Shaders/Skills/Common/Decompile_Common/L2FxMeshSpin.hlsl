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
//   SpinsPerSecondRange.X=0.1 -> Yaw speed magnitude 0.1*65535.
//   UC X maps to runtime slot c0 / Yaw.
//
// Exact StartSpin RNG path (verified 2026-07-13):
// - UParticleEmitter::SpawnParticle calls FRangeVector::GetRand;
// - GetRand uses appFrand(), not appSRand();
// - appRand is the MSVC TLS LCG: state=state*214013+2531011;
// - FRangeVector draws Roll(Z), Pitch(Y), Yaw(X), then stores Yaw/Pitch/Roll;
// - FRange::GetRand is Max + appFrand() * (Min - Max).
//
// SpinCCWorCW (verified 2026-08-24, e_u056_a Coin / MeshEmitter7, 16 slots):
//   After StartSpin + SPS FRangeVector draws, SpawnParticle consumes 3 appFrand
//   calls and multiplies each SPS axis by -1 when frand < SpinCCWorCW.axis.
//   UE2 default is (0.5, 0.5, 0.5). UC omit keeps 0.5 on every axis; a partial
//   write such as (X=1) is (1, 0.5, 0.5). Explicit (0,0,0) never flips.
//
// Still pending before declaring every mesh setting universal:
// - Non-uniform StartSize (thin Z) + Unity-remapped mesh: Wave looks flat with
//   S only, but tips with R — fix inside this library / basis conjugation, do
//   not bypass with yaw-only helpers in effect shaders.
// - L2FxPTRS_Actor is for UseRotationFrom=PTRS_Actor WorldMat (owner TRS), not
//   a substitute for this spin module.

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

// UC-order spinsPerSecond (Yaw, Pitch, Roll) before URU conversion.
void L2Fx_MeshSpin_ApplySpinCCWorCW_Uc(
    inout float3 spinsPerSecondUc,
    float3 spinCcwOrCw,
    inout uint appRandState)
{
    if (L2Fx_AppFrand(appRandState) < spinCcwOrCw.x)
        spinsPerSecondUc.x *= -1.0;
    if (L2Fx_AppFrand(appRandState) < spinCcwOrCw.y)
        spinsPerSecondUc.y *= -1.0;
    if (L2Fx_AppFrand(appRandState) < spinCcwOrCw.z)
        spinsPerSecondUc.z *= -1.0;
}

float3 L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(float3 spinsPerSecondUc)
{
    return spinsPerSecondUc * L2FX_MESH_SPIN_UC_TO_URU;
}

// Preview-only when CPU did not supply an appRand TLS state. Same comparison as
// ApplySpinCCWorCW_Uc, but the three draws come from the hashed spawn helper.
float3 L2Fx_MeshSpin_ApplySpinCCWorCW_Hashed(
    float3 spinsPerSecondUc,
    float3 spinCcwOrCw,
    float seed,
    float startTime)
{
    if (L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, 53.0) < spinCcwOrCw.x)
        spinsPerSecondUc.x *= -1.0;
    if (L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, 59.0) < spinCcwOrCw.y)
        spinsPerSecondUc.y *= -1.0;
    if (L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, 61.0) < spinCcwOrCw.z)
        spinsPerSecondUc.z *= -1.0;
    return spinsPerSecondUc;
}

// Exact UE2.5 SpawnParticle spin path: StartSpin, SPS, then 3x SpinCCWorCW.
void L2Fx_MeshSpin_SpawnYawPitchRollUruFromAppRandState(
    float2 yawRangeUc,
    float2 pitchRangeUc,
    float2 rollRangeUc,
    float2 spsYawRangeUc,
    float2 spsPitchRangeUc,
    float2 spsRollRangeUc,
    float3 spinCcwOrCw,
    uint startSpinRandState,
    out float3 startYawPitchRollUru,
    out float3 spinRateYawPitchRollUruPerSecond)
{
    uint state = startSpinRandState;
    float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        yawRangeUc,
        pitchRangeUc,
        rollRangeUc,
        state);
    float3 spinsPerSecondUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
        spsYawRangeUc,
        spsPitchRangeUc,
        spsRollRangeUc,
        state);
    L2Fx_MeshSpin_ApplySpinCCWorCW_Uc(spinsPerSecondUc, spinCcwOrCw, state);
    startYawPitchRollUru = startSpinUc * L2FX_MESH_SPIN_UC_TO_URU;
    spinRateYawPitchRollUruPerSecond =
        L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(spinsPerSecondUc);
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
//
// The Y↔Z swap is improper (det=-1). Slot URU / SpinCCWorCW still match L2
// ParticleSnapshot numerically, but visual CW sense on the Unity mesh flips —
// same class of port fix as L2Fx_SpriteSpin_RotateBillboardOffset (negate only
// at Unity apply). Do not invert .uc SpinCCWorCW on materials to compensate.
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
        -pitchYawRollRadians);
    return float3(
        ueRotatedPosition.x,
        ueRotatedPosition.z,
        ueRotatedPosition.y);
}

#endif // L2_FX_MESH_SPIN_INCLUDED
