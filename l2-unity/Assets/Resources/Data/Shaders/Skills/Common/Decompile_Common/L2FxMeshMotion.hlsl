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
// Not covered here: random range selection, damping, velocity loss, revolution,
// mesh attachment, and collision/owner tracking.

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

#endif // L2_FX_MESH_MOTION_INCLUDED
