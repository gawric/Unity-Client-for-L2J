#ifndef L2_FX_SPAWN_REGION_DEBUG_INCLUDED
#define L2_FX_SPAWN_REGION_DEBUG_INCLUDED

// Spawn-region debug contract (Scene tuning).
// GPU: include from sprite shaders; Editor gizmo: L2FxSpriteSpawnRegionDebugDrawer.cs
// must use the same UE polar + box + offset composition as below.

#include "L2FxAtlasDebug.hlsl"
#include "L2FxEmitterSpawn.hlsl"

float L2Fx_SpawnRegionDebug_IsActive(float debugSpawnRegion, float startTime)
{
    return (debugSpawnRegion > 0.5 && startTime <= 1e-4) ? 1.0 : 0.0;
}

// Single polar sample in UE FVector space (matches L2Fx_SpawnOffsetPolarDegrees).
float3 L2Fx_SpawnRegionPolarOffsetUe(float thetaDeg, float phiDeg, float radius)
{
    return L2Fx_PolarCartesianUe(thetaDeg, phiDeg, radius);
}

// Random point on a sphere surface in UE FVector space (for SphereRadiusRange).
float3 L2Fx_SpawnRegionRandomOnSphereUe(float seed, float startTime, float radiusUe, float saltBase)
{
    float u = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, saltBase);
    float v = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, saltBase + 1.0);
    float theta = L2Fx_TwoPi * u;
    float z = 1.0 - 2.0 * v;
    float r = sqrt(max(0.0, 1.0 - z * z));
    return float3(r * cos(theta), r * sin(theta), z) * radiusUe;
}

// Full spawn offset in UE space: polar cap + box jitter + offset (.uc StartLocation*).
float3 L2Fx_SpawnRegionOffsetUe(
    float2 azimuthDegMinMax,
    float2 polarFromPositiveZDegMinMax,
    float2 radiusMinMax,
    float3 startLocationOffsetUe,
    float3 startLocationRangeMinUe,
    float3 startLocationRangeMaxUe,
    float seed,
    float startTime)
{
    float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
        azimuthDegMinMax,
        polarFromPositiveZDegMinMax,
        radiusMinMax,
        seed,
        startTime);

    posUe += float3(
        L2Fx_RandomRange(float2(startLocationRangeMinUe.x, startLocationRangeMaxUe.x), seed, startTime, 83.0),
        L2Fx_RandomRange(float2(startLocationRangeMinUe.y, startLocationRangeMaxUe.y), seed, startTime, 89.0),
        L2Fx_RandomRange(float2(startLocationRangeMinUe.z, startLocationRangeMaxUe.z), seed, startTime, 97.0));

    posUe += startLocationOffsetUe;
    return posUe;
}

#endif
