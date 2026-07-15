#ifndef L2_FX_SPRITE_MOTION_INCLUDED
#define L2_FX_SPRITE_MOTION_INCLUDED

// L2_FX_SPRITE_MOTION - SPRITE EMITTER ONLY - Decompile_Common
//
// SpriteEmitter2 / m_u004_b StartVelocityRange status: NOT VERIFIED.
// Its authored velocity is only X/Y=[-0.001,0.001], Z=[0.004,0.004] UU/s,
// which is too weak to validate reliably from the live visual result. The
// StartVelocity appRand state/order and slot path must be captured on another
// effect with a larger StartVelocityRange before treating the helper below as
// byte-exact for SpriteEmitter2. Do not confuse the verified SpriteEmitter2
// StartSpin/SPS trace with StartVelocity: they are separate spawn paths.
//
// SOURCE: UParticleEmitter::UpdateParticles (Engine.dll x86, IDA decomp).
// VERIFIED byte-exact on live log via AEmitter::Tick hook (velocity/accel/velMul logged):
//   Motion: m_u004_a (accel Z=+100), m_u004_b_2 (accel Z=-40) -> v+=a*dt & pos ~100%
//   Drag:   BlueDust (VelocityLossRange=2, accel=0) -> proportional loss=2.000/s, std 0.007
//   Proof: AutoLoginInterlude/L2_Formuls/SpriteMotion_Formula.txt
//   Scripts: docs/_verify_motion_direct.py, docs/_verify_velocityloss.py
//
// Engine per-tick order (particle slot, stride 0xE0), VERIFIED:
//   Velocity     += Acceleration * dt                   // +0x18 += +0xD4
//   OldLocation   = Location                            // +0x0C  = +0x00
//   Location     += Velocity * VelocityMultiplier * dt  // +0x00, +0x90 (uses PRE-loss velocity)
//   Velocity     -= Velocity * VelocityLoss * dt        // proportional drag, applied LAST
//                = Velocity * (1 - VelocityLoss * dt)
//
// Closed form (constant a, VelocityMultiplier=1):
//   No drag:            v(t) = v0 + a*t ;  p(t) = v0*t + 0.5*a*t^2
//   Proportional drag:  dv/dt = a - loss*v
//                       v(t) = a/loss + (v0 - a/loss)*exp(-loss*t)
//                       p(t) = (a/loss)*t + (v0 - a/loss)*(1 - exp(-loss*t))/loss
//   VelocityLoss is per-axis (VelocityLossRange X,Y,Z); loss=0 -> ballistic fallback.
//
// IMPORTANT — this shader helper is continuous-time, whereas native L2 updates
// particles discretely once per emitter tick (acceleration first, then location).
// For m_u004_b / SpriteEmitter0 the spawn path also pre-applies acceleration over
// SpawnDeltaTime before PTVD. Replaying the same L2 state therefore shows a small
// vertical velocity offset (about Acceleration * SpawnDeltaTime) immediately after
// spawn. Keep this helper for stable GPU motion; do not interpret that offset as
// VelocityLoss/drag or add artificial damping to compensate.
//
// NOTE: this matches Common/L2FxMeshParticleMotion.hlsl :: L2Fx_DisplacementVelocityLossExp
//   (same exponential model). The earlier linear "-0.5*loss*t*t" WithLoss form was WRONG
//   and has been replaced by the exp WithDrag form below.
//
// AXIS: L2/UE Z-up -> Unity Y-up: unity = float3(ue.x, ue.z, ue.y)
// Unit scale: UE UU (cm) -> Unity meters via L2FX_UU_TO_UNITY (0.01).

#include "../L2FxEmitterSpawn.hlsl"
#include "../L2FxCoreGeometryTest.hlsl"
#include "L2FxAppRand.hlsl"

float3 L2Fx_SpriteMotion_UeVectorToUnity(float3 vUe)
{
    return float3(vUe.x, vUe.z, vUe.y);
}

// StartVelocityRange is a UE FRangeVector. Its appRand draw order is Z/Y/X,
// matching the particle slot's X/Y/Z velocity components after reconstruction.
float3 L2Fx_SpriteMotion_StartVelocityUeFromAppRandState(
    float2 velocityXRangeUe,
    float2 velocityYRangeUe,
    float2 velocityZRangeUe,
    uint startVelocityRandState)
{
    uint state = startVelocityRandState;
    return L2Fx_FRangeVector_GetRandYawPitchRoll(
        velocityXRangeUe,
        velocityYRangeUe,
        velocityZRangeUe,
        state);
}

// Ballistic displacement in UE/L2 units (Z-up). No drag.
float3 L2Fx_SpriteMotion_DisplacementUe(
    float3 velocity0Ue,
    float3 accelerationUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float t = max(0.0, ageSeconds);
    float vm = max(0.0, velocityMultiplier);
    return vm * (velocity0Ue * t + 0.5 * accelerationUe * t * t);
}

// Per-axis: v(t) = a/loss + (v0 - a/loss)*exp(-loss*t)
float L2Fx_SpriteMotion_VelocityComponentWithDrag(
    float v0, float a, float loss, float t)
{
    if (loss <= 1e-6)
        return v0 + a * t;
    float vInf = a / loss;
    return vInf + (v0 - vInf) * exp(-loss * t);
}

// Per-axis: p(t) = (a/loss)*t + (v0 - a/loss)*(1 - exp(-loss*t))/loss
float L2Fx_SpriteMotion_DisplacementComponentWithDrag(
    float v0, float a, float loss, float t)
{
    if (loss <= 1e-6)
        return v0 * t + 0.5 * a * t * t;
    float vInf = a / loss;
    return vInf * t + (v0 - vInf) * (1.0 - exp(-loss * t)) / loss;
}

// Proportional drag displacement in UE/L2 units (Z-up).
// velocityLossPerSecUe = per-axis VelocityLoss from .uc VelocityLossRange (units 1/s).
float3 L2Fx_SpriteMotion_DisplacementUeWithDrag(
    float3 velocity0Ue,
    float3 accelerationUe,
    float3 velocityLossPerSecUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float t = max(0.0, ageSeconds);
    float vm = max(0.0, velocityMultiplier);
    float3 disp;
    disp.x = L2Fx_SpriteMotion_DisplacementComponentWithDrag(
        velocity0Ue.x, accelerationUe.x, velocityLossPerSecUe.x, t);
    disp.y = L2Fx_SpriteMotion_DisplacementComponentWithDrag(
        velocity0Ue.y, accelerationUe.y, velocityLossPerSecUe.y, t);
    disp.z = L2Fx_SpriteMotion_DisplacementComponentWithDrag(
        velocity0Ue.z, accelerationUe.z, velocityLossPerSecUe.z, t);
    return vm * disp;
}

float3 L2Fx_SpriteMotion_DisplacementUnity(
    float3 velocity0Ue,
    float3 accelerationUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float3 dispUe = L2Fx_SpriteMotion_DisplacementUe(
        velocity0Ue, accelerationUe, ageSeconds, velocityMultiplier);
    return L2Fx_SpriteMotion_UeVectorToUnity(dispUe) * L2FX_UU_TO_UNITY;
}

// Use this variant for effects calibrated by L2FxCoreGeometryTest. It keeps
// particle motion in the same UU-to-world scale as StartLocationOffset.
float3 L2Fx_SpriteMotion_DisplacementUnityCalibrated(
    float3 velocity0Ue,
    float3 accelerationUe,
    float ageSeconds,
    float velocityMultiplier,
    float worldCalibration)
{
    float3 displacementUe = L2Fx_SpriteMotion_DisplacementUe(
        velocity0Ue, accelerationUe, ageSeconds, velocityMultiplier);
    return L2Fx_UcPositionToUnityMeters(displacementUe, worldCalibration);
}

float3 L2Fx_SpriteMotion_DisplacementUnityWithDrag(
    float3 velocity0Ue,
    float3 accelerationUe,
    float3 velocityLossPerSecUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float3 dispUe = L2Fx_SpriteMotion_DisplacementUeWithDrag(
        velocity0Ue, accelerationUe, velocityLossPerSecUe,
        ageSeconds, velocityMultiplier);
    return L2Fx_SpriteMotion_UeVectorToUnity(dispUe) * L2FX_UU_TO_UNITY;
}

float3 L2Fx_SpriteMotion_PositionUnity(
    float3 spawnOffsetUe,
    float3 velocity0Ue,
    float3 accelerationUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float3 spawnUnity = L2Fx_SpriteMotion_UeVectorToUnity(spawnOffsetUe) * L2FX_UU_TO_UNITY;
    float3 motionUnity = L2Fx_SpriteMotion_DisplacementUnity(
        velocity0Ue, accelerationUe, ageSeconds, velocityMultiplier);
    return spawnUnity + motionUnity;
}

float3 L2Fx_SpriteMotion_PositionUnityWithDrag(
    float3 spawnOffsetUe,
    float3 velocity0Ue,
    float3 accelerationUe,
    float3 velocityLossPerSecUe,
    float ageSeconds,
    float velocityMultiplier)
{
    float3 spawnUnity = L2Fx_SpriteMotion_UeVectorToUnity(spawnOffsetUe) * L2FX_UU_TO_UNITY;
    float3 motionUnity = L2Fx_SpriteMotion_DisplacementUnityWithDrag(
        velocity0Ue, accelerationUe, velocityLossPerSecUe,
        ageSeconds, velocityMultiplier);
    return spawnUnity + motionUnity;
}

#endif // L2_FX_SPRITE_MOTION_INCLUDED
