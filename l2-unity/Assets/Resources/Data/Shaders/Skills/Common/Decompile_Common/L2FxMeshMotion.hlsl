#ifndef L2_FX_MESH_MOTION_INCLUDED
#define L2_FX_MESH_MOTION_INCLUDED

// UMeshEmitter base motion in original UE coordinates (UU).
//
// CONFIRMED from UParticleEmitter::UpdateParticles decompile and live L2 memory
// for LineageEffect.m_u004_b_2 / MeshEmitter5:
//   StartVelocity.Z = -23, Acceleration.Z = -11, age ~= 2.0 s
//   predicted local Z = -23 * 2 + 0.5 * -11 * 2^2 = -68 UU
//   live particle locLocal.Z ~= -68 UU.
//
// UNITY VALIDATION PENDING:
// the UE-space integration is confirmed, but no active Unity mesh effect yet
// exercises StartVelocityRange + Acceleration. Do not treat UU -> Unity meters
// velocity/acceleration conversion as verified until a Unity/L2 trajectory
// comparison is captured at matching particle ages.
//
// This library intentionally remains in UE-space. Apply UE -> Unity axis/world
// calibration separately after motion evaluation; that bridge is project-specific.
//
// Not covered here: random range selection, revolution, mesh attachment,
// and collision/owner tracking.
//
// VelocityLoss: same UParticleEmitter proportional drag as L2FxSpriteMotion
// (live BlueDust / UpdateParticles). Per-axis VelocityLossRange units are 1/s.
//
// MaxAbsVelocity (UParticleEmitter::UpdateParticles decompile):
//   Per-axis clamp of particle Velocity to [-MaxAbs, +MaxAbs].
//   MaxAbs==0 → that axis is NOT clamped.
//   Tick order in the same function: integrate Location from Velocity, then
//   MaxAbsVelocity clamp on Velocity, then VelocityLoss.
//
//   *** NOT LIVE-VERIFIED ***
//   Taken from IDA/decompile of Lineage2 UParticleEmitter::UpdateParticles only.
//   No ParticleSnapshot / hook compare vs original client logs yet.
//   Do not treat closed-form EvaluatePosition* as including MaxAbs — those
//   helpers stay ballistic/drag only; call ClampVelocityMaxAbsUe on discrete
//   velocity when an effect needs UC MaxAbsVelocity parity.

float3 L2Fx_MeshMotion_EvaluatePositionUe(
    float3 startLocationOffsetUe,
    float3 startVelocityUe,
    float3 accelerationUe,
    float ageSeconds)
{
    float t = max(ageSeconds, 0.0);
    return startLocationOffsetUe
        + startVelocityUe * t
        + 0.5 * accelerationUe * t * t;
}

float3 L2Fx_MeshMotion_EvaluateDisplacementUe(
    float3 startVelocityUe,
    float3 accelerationUe,
    float ageSeconds)
{
    float t = max(ageSeconds, 0.0);
    return startVelocityUe * t + 0.5 * accelerationUe * t * t;
}

// Per-axis: p(t) = (a/loss)*t + (v0 - a/loss)*(1 - exp(-loss*t))/loss
// loss<=0 falls back to ballistic v0*t + 0.5*a*t^2.
float L2Fx_MeshMotion_DisplacementComponentWithDrag(
    float v0, float a, float loss, float t)
{
    if (loss <= 1e-6)
        return v0 * t + 0.5 * a * t * t;
    float vInf = a / loss;
    return vInf * t + (v0 - vInf) * (1.0 - exp(-loss * t)) / loss;
}

float3 L2Fx_MeshMotion_EvaluateDisplacementUeWithDrag(
    float3 startVelocityUe,
    float3 accelerationUe,
    float3 velocityLossPerSecUe,
    float ageSeconds)
{
    float t = max(ageSeconds, 0.0);
    float3 disp;
    disp.x = L2Fx_MeshMotion_DisplacementComponentWithDrag(
        startVelocityUe.x, accelerationUe.x, velocityLossPerSecUe.x, t);
    disp.y = L2Fx_MeshMotion_DisplacementComponentWithDrag(
        startVelocityUe.y, accelerationUe.y, velocityLossPerSecUe.y, t);
    disp.z = L2Fx_MeshMotion_DisplacementComponentWithDrag(
        startVelocityUe.z, accelerationUe.z, velocityLossPerSecUe.z, t);
    return disp;
}

float3 L2Fx_MeshMotion_EvaluatePositionUeWithDrag(
    float3 startLocationOffsetUe,
    float3 startVelocityUe,
    float3 accelerationUe,
    float3 velocityLossPerSecUe,
    float ageSeconds)
{
    return startLocationOffsetUe + L2Fx_MeshMotion_EvaluateDisplacementUeWithDrag(
        startVelocityUe,
        accelerationUe,
        velocityLossPerSecUe,
        ageSeconds);
}

// ---------------------------------------------------------------------------
// MaxAbsVelocity — DECOMPILE ONLY (not verified vs Lineage2 live logs)
// Source: UParticleEmitter::UpdateParticles
//   if (MaxAbs != 0) Velocity = clamp(Velocity, -MaxAbs, +MaxAbs)  // per axis
// Emitter MaxAbs floats: this+240/241/242 (X/Y/Z). Particle vel: +24/+28/+32.
// ---------------------------------------------------------------------------
float L2Fx_MeshMotion_ClampAxisMaxAbsUe(float velocityUe, float maxAbsUe)
{
    // Decompile: MaxAbs == 0 skips the clamp for that axis.
    if (maxAbsUe == 0.0)
        return velocityUe;
    return clamp(velocityUe, -maxAbsUe, maxAbsUe);
}

float3 L2Fx_MeshMotion_ClampVelocityMaxAbsUe(
    float3 velocityUe,
    float3 maxAbsVelocityUe)
{
    return float3(
        L2Fx_MeshMotion_ClampAxisMaxAbsUe(velocityUe.x, maxAbsVelocityUe.x),
        L2Fx_MeshMotion_ClampAxisMaxAbsUe(velocityUe.y, maxAbsVelocityUe.y),
        L2Fx_MeshMotion_ClampAxisMaxAbsUe(velocityUe.z, maxAbsVelocityUe.z));
}

#endif // L2_FX_MESH_MOTION_INCLUDED
