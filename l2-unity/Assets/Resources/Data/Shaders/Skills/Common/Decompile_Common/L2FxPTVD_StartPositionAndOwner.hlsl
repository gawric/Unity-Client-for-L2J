#ifndef L2_FX_PTVD_START_POSITION_AND_OWNER_INCLUDED
#define L2_FX_PTVD_START_POSITION_AND_OWNER_INCLUDED

// L2FxPTVD_StartPositionAndOwner
//
// Status: VERIFIED ON LIVE CLIENT DATA (m_u004_b / SpriteEmitter0).
//
// SpawnParticleSnapshot captures confirmed, with 0.0 validation error:
//   velocityBeforePtvd = rawVelocity + acceleration * spawnDeltaTime
//   direction = SafeNormal(ptvdInput)
//   velocityAfterPtvd = -velocityBeforePtvd * direction // component-wise
//
// SpriteEmitter0 uses CoordinateSystem=1, where the client passes its local
// spawn position directly to SafeNormal; ownerPositionUe is therefore zero.
// For other coordinate systems, the engine may pass ownerPositionUe -
// spawnPositionUe to SafeNormal instead.
//
// Verified RNG chain for SpriteEmitter0 StartVelocityRange:
//   draw order: Z, Y, X
//   zFraction = 1.0 - appRandZ / 32767.0
//   rawVelocityZ = -18.0 + 19.0 * zFraction
//
// Full spawn RNG stream captured for this exact effect:
//   m_u004_b / SpriteEmitter0 consumes 28 appRand draws per spawn.
//   draws [0..2]   StartVelocityRange       (Z, Y, X)
//   draws [3..5]   StartLocationPolarRange  (radius, phi, theta; Z, Y, X)
//   draws [16]     LifetimeRange            (1.0 .. 1.8)
//   draws [19..21] StartSizeRange
//   draws [22..24] StartSpinRange
//   draws [25..27] SpinsPerSecondRange
// PTVD consumes no additional RNG draw; it operates on the already-spawned
// position and velocity. The remaining spawn draws are captured in
// SpawnParticleSnapshot.log but are intentionally not assigned a shader
// semantic here until their emitter fields are mapped.
//
// SafeNormal's zero-length fallback was not observed in live data.

float3 L2FxPTVD_StartPositionAndOwnerApply(
    float3 velocityBeforePtvdUe,
    float3 directionUe)
{
    return -velocityBeforePtvdUe * directionUe;
}

float3 L2FxPTVD_StartPositionAndOwner(
    float3 velocityBeforePtvdUe,
    float3 spawnPositionUe,
    float3 ownerPositionUe)
{
    float3 directionUe = spawnPositionUe - ownerPositionUe;
    float directionLength = length(directionUe);

    // Keep a safe shader fallback; UE2 SafeNormal's zero-length branch is unverified.
    if (directionLength <= 1e-5)
    {
        return float3(0.0, 0.0, 0.0);
    }

    directionUe /= directionLength;
    return L2FxPTVD_StartPositionAndOwnerApply(velocityBeforePtvdUe, directionUe);
}

#endif // L2_FX_PTVD_START_POSITION_AND_OWNER_INCLUDED
