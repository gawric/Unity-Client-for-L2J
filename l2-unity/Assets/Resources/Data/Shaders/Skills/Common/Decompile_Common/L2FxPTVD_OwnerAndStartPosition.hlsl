#ifndef L2_FX_PTVD_OWNER_AND_START_POSITION_INCLUDED
#define L2_FX_PTVD_OWNER_AND_START_POSITION_INCLUDED

// L2FxPTVD_OwnerAndStartPosition
//
// Status: LIVE PASS — e_u505_c / shot_N_atk SpriteEmitter324 "Particle"
//   (UpdateParticles 2026-07-22): after spawn, velocity ≈ rawVel * normalize(spawn)
//   and decays with VelocityLoss (e.g. VelLoss=6). Direction-only module; drag is
//   L2FxSpriteMotion_DisplacementUeWithDrag.
//
// UC: GetVelocityDirectionFrom=PTVD_OwnerAndStartPosition  (enum value 2)
//
// Formula (same SafeNormal axis as mode 1, but NO negate):
//   velocityBeforePtvd = rawVelocity + acceleration * spawnDeltaTime
//   direction = SafeNormal(spawnPosition - ownerPosition)   // CS=1: owner often zero
//   velocityAfterPtvd = +velocityBeforePtvd * direction     // component-wise
//
// Contrast with L2FxPTVD_StartPositionAndOwner (mode 1, verified on SE0):
//   velocityAfterPtvd = -velocityBeforePtvd * direction

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
