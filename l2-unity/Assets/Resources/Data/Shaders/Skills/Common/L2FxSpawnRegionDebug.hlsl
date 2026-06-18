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
    float theta = thetaDeg * L2Fx_DegToRad;
    float phi = phiDeg * L2Fx_DegToRad;
    float sinPhi = sin(phi);
    return float3(
        radius * sinPhi * cos(theta),
        radius * sinPhi * sin(theta),
        radius * cos(phi));
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
