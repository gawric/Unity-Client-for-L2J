#ifndef L2_FX_SPRITE_POLAR_INCLUDED
#define L2_FX_SPRITE_POLAR_INCLUDED

// L2_FX_SPRITE_POLAR — UParticleEmitter PTLS_Polar spawn replication.
//
// SOURCE: Lineage II Interlude Engine.dll, UParticleEmitter::SpawnParticle.
// VERIFIED: live AEmitter::Tick snapshots, m_u004_b / SpriteEmitter0:
//   StartLocationOffset=(Z=7)
//   StartLocationShape=PTLS_Polar
//   StartLocationPolarRange=(X=(Max=360), Y=(Min=10,Max=105),
//                            Z=(Min=2.4,Max=2.4))
// Seven captured particle StartLocation values reconstructed with:
//   radius=2.4 UU, phi inside [10,105] degrees, and distinct theta values.
//
// IMPORTANT:
// - This library applies only when StartLocationShape == PTLS_Polar.
// - StartLocationPolarRange alone does not activate polar spawning.
//   m_u004_b / SpriteEmitter2 supplies that range but does NOT set
//   StartLocationShape=PTLS_Polar; L2 spawns it only at StartLocationOffset.
// - Input ranges map directly from .uc:
//   X = theta / azimuth degrees, Y = phi from +UE-Z degrees, Z = radius UU.
//
// SpawnParticle calls FRangeVector::GetRand for the range vector. Its draw
// order is Z, then Y, then X, so the appRand sequence is:
//   radius -> phi -> theta.

#include "L2FxAppRand.hlsl"

#ifndef L2FX_UU_TO_UNITY
#define L2FX_UU_TO_UNITY 0.01
#endif

static const float L2FX_SPRITE_POLAR_DEG_TO_RAD = 0.01745329251994329577;

// UE/L2 Cartesian offset in Unreal Units. UE is Z-up.
float3 L2Fx_SpritePolar_CartesianUe(float thetaDegrees, float phiDegrees, float radiusUu)
{
    float thetaRadians = thetaDegrees * L2FX_SPRITE_POLAR_DEG_TO_RAD;
    float phiRadians = phiDegrees * L2FX_SPRITE_POLAR_DEG_TO_RAD;
    float sinPhi = sin(phiRadians);

    return float3(
        radiusUu * sinPhi * cos(thetaRadians),
        radiusUu * sinPhi * sin(thetaRadians),
        radiusUu * cos(phiRadians));
}

// Exact FRangeVector::GetRand order for StartLocationPolarRange:
// rangeX=theta, rangeY=phi, rangeZ=radius; draws occur Z/Y/X.
float3 L2Fx_SpritePolar_GetRandUe(
    float2 thetaDegreesMinMax,
    float2 phiDegreesMinMax,
    float2 radiusUuMinMax,
    inout uint appRandState)
{
    float radiusUu = L2Fx_FRange_GetRand(radiusUuMinMax, appRandState);
    float phiDegrees = L2Fx_FRange_GetRand(phiDegreesMinMax, appRandState);
    float thetaDegrees = L2Fx_FRange_GetRand(thetaDegreesMinMax, appRandState);

    return L2Fx_SpritePolar_CartesianUe(thetaDegrees, phiDegrees, radiusUu);
}

// Final UE slot StartLocation contribution for a PTLS_Polar emitter.
float3 L2Fx_SpritePolar_StartLocationUe(
    float3 startLocationOffsetUe,
    float2 thetaDegreesMinMax,
    float2 phiDegreesMinMax,
    float2 radiusUuMinMax,
    inout uint appRandState)
{
    return startLocationOffsetUe + L2Fx_SpritePolar_GetRandUe(
        thetaDegreesMinMax,
        phiDegreesMinMax,
        radiusUuMinMax,
        appRandState);
}

// UE X,Y,Z (Z-up) -> Unity X,Y,Z (Y-up), including UU-to-meter conversion.
float3 L2Fx_SpritePolar_UeToUnity(float3 uePositionUu)
{
    return float3(uePositionUu.x, uePositionUu.z, uePositionUu.y) * L2FX_UU_TO_UNITY;
}

float3 L2Fx_SpritePolar_StartLocationUnity(
    float3 startLocationOffsetUe,
    float2 thetaDegreesMinMax,
    float2 phiDegreesMinMax,
    float2 radiusUuMinMax,
    inout uint appRandState)
{
    return L2Fx_SpritePolar_UeToUnity(L2Fx_SpritePolar_StartLocationUe(
        startLocationOffsetUe,
        thetaDegreesMinMax,
        phiDegreesMinMax,
        radiusUuMinMax,
        appRandState));
}

#endif // L2_FX_SPRITE_POLAR_INCLUDED
