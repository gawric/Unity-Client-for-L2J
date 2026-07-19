#ifndef L2_FX_PTVD_OWNER_AND_START_POSITION_INCLUDED
#define L2_FX_PTVD_OWNER_AND_START_POSITION_INCLUDED

// L2FxPTVD_OwnerAndStartPosition
//
// Status: UNVERIFIED — provisional / under test. Do not treat as live-client PASS.
//
// UC: GetVelocityDirectionFrom=PTVD_OwnerAndStartPosition  (enum value 2)
//
// Current working guess (same SafeNormal axis as mode 1, but NO negate):
//   velocityBeforePtvd = rawVelocity + acceleration * spawnDeltaTime
//   direction = SafeNormal(spawnPosition - ownerPosition)   // CS=1: owner often zero
//   velocityAfterPtvd = +velocityBeforePtvd * direction     // component-wise
//
// Contrast with L2FxPTVD_StartPositionAndOwner (mode 1, verified on SE0):
//   velocityAfterPtvd = -velocityBeforePtvd * direction
//
// Direction sign alone is not enough: on it_healing_potion_ta / SpriteEmitter7
// "kirakira", L2 does more than a constant radial push — particles appear to
// speed up early then slow as they move away (end-of-life speed is clearly
// non-linear). Likely VelocityLoss / tick integration / another spawn field,
// not covered here. Debug later with L2 ParticleSnapshot velocity over life;
// leave this module as direction-only until that is captured.

float3 L2FxPTVD_OwnerAndStartPositionApply(
    float3 velocityBeforePtvdUe,
    float3 directionUe)
{
    return velocityBeforePtvdUe * directionUe;
}

float3 L2FxPTVD_OwnerAndStartPosition(
    float3 velocityBeforePtvdUe,
    float3 spawnPositionUe,
    float3 ownerPositionUe)
{
    float3 directionUe = spawnPositionUe - ownerPositionUe;
    float directionLength = length(directionUe);

    if (directionLength <= 1e-5)
    {
        return float3(0.0, 0.0, 0.0);
    }

    directionUe /= directionLength;
    return L2FxPTVD_OwnerAndStartPositionApply(velocityBeforePtvdUe, directionUe);
}

#endif // L2_FX_PTVD_OWNER_AND_START_POSITION_INCLUDED
