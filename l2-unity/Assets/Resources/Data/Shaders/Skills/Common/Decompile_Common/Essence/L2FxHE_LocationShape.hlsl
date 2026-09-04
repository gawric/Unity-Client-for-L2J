#ifndef L2_FX_HE_LOCATION_SHAPE_INCLUDED
#define L2_FX_HE_LOCATION_SHAPE_INCLUDED

// High Elf StartLocationShape — Engine_essens_high_elves.dll
// UParticleEmitter::SpawnParticle loc_208EA217, emitter+0x17C:
//   0 = box GetRand(+0x160)
//   1 = sphere radius GetRand(+0x180)
//   2 = polar GetRand(+0x188)  ← live PTLS_Polar
//   3 = box AND sphere AND polar (do not treat as polar-only)
// Polar Cartesian stays in ../L2FxSpritePolar.hlsl (HE uses GMath sin table,
// same mapping). PolarRange alone does not spawn polar.
//
// 1147 fire_dust_out: shape=2, Polar Z=15. SphereRadiusRange is unused.

#include "../L2FxSpritePolar.hlsl"
#include "../L2FxStartLocationRange.hlsl"

static const float L2FX_HE_SHAPE_BOX = 0.0;
static const float L2FX_HE_SHAPE_SPHERE = 1.0;
static const float L2FX_HE_SHAPE_POLAR = 2.0;

float L2FxHE_LocationShape_IsPolar(float shape)
{
    return (shape > 1.5 && shape < 2.5) ? 1.0 : 0.0;
}

float L2FxHE_LocationShape_IsBox(float shape)
{
    return (shape > -0.5 && shape < 0.5) ? 1.0 : 0.0;
}

// Offset + Polar GetRand. Use when IsPolar(shape).
float3 L2FxHE_LocationShape_PolarStartUe(
    float3 startLocationOffsetUe,
    float2 thetaDegreesMinMax,
    float2 phiDegreesMinMax,
    float2 radiusUuMinMax,
    inout uint appRandState)
{
    return L2Fx_SpritePolar_StartLocationUe(
        startLocationOffsetUe,
        thetaDegreesMinMax,
        phiDegreesMinMax,
        radiusUuMinMax,
        appRandState);
}

// Offset + StartLocationRange box. Use when IsBox(shape).
float3 L2FxHE_LocationShape_BoxStartUe(
    float3 startLocationOffsetUe,
    float2 rangeXUu,
    float2 rangeYUu,
    float2 rangeZUu,
    inout uint appRandState)
{
    return L2Fx_StartLocationRange_ApplyUe(
        startLocationOffsetUe,
        rangeXUu,
        rangeYUu,
        rangeZUu,
        appRandState);
}

#endif
