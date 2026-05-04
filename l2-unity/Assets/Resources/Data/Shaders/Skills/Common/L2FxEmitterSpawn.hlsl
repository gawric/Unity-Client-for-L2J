#ifndef L2_FX_EMITTER_SPAWN_INCLUDED
#define L2_FX_EMITTER_SPAWN_INCLUDED

// Unreal Engine 1 / Lineage 2 Interlude–style emitter helpers: polar + box spawn offset,
// constant velocity drift, radial velocity toward owner, random lifetime/delay,
// two-key size scale and color over normalized age. Flipbook: see L2FxFlipbook.hlsl.
//
// Include after Core.hlsl if you use float3 with matrices; otherwise standalone.
// Depends on L2FxParticleAnim for L2Fx_RandomRange / hashing.
#include "L2FxParticleAnim.hlsl"

static const float L2Fx_DegToRad = 0.01745329252;

// Random lifetime in [min, max] (Unreal LifetimeRange).
float L2Fx_RandomLifetime(float2 lifetimeMinMax, float seed, float startTime, float salt)
{
    return max(1e-4, L2Fx_RandomRange(lifetimeMinMax, seed, startTime, salt));
}

// Random initial delay in [min, max] (Unreal InitialDelayRange).
float L2Fx_RandomInitialDelay(float2 delayMinMax, float seed, float startTime, float salt)
{
    return max(0.0, L2Fx_RandomRange(delayMinMax, seed, startTime, salt));
}

// Polar spawn offset matching common UE1-style StartLocationPolarRange:
//   X = azimuth around Z (degrees, 0..360)
//   Y = angle from +Z axis (degrees; 0 = on +Z, 90 = in XY plane)
//   Z = radius (world units)
// Maps to Cartesian: ring near horizontal for Y ~ 85..95, r ~ const.
float3 L2Fx_SpawnOffsetPolarDegrees(
    float2 azimuthDegMinMax,
    float2 polarFromPositiveZDegMinMax,
    float2 radiusMinMax,
    float seed,
    float startTime)
{
    float thetaDeg = L2Fx_RandomRange(azimuthDegMinMax, seed, startTime, 71.0);
    float phiDeg = L2Fx_RandomRange(polarFromPositiveZDegMinMax, seed, startTime, 73.0);
    float r = L2Fx_RandomRange(radiusMinMax, seed, startTime, 79.0);

    float theta = thetaDeg * L2Fx_DegToRad;
    float phi = phiDeg * L2Fx_DegToRad;
    float sinPhi = sin(phi);
    float x = r * sinPhi * cos(theta);
    float y = r * sinPhi * sin(theta);
    float z = r * cos(phi);
    return float3(x, y, z);
}

// Axis-aligned box random offset (Unreal StartLocationRange per axis).
float3 L2Fx_SpawnOffsetBox(float2 rangeX, float2 rangeY, float2 rangeZ, float seed, float startTime)
{
    float x = L2Fx_RandomRange(rangeX, seed, startTime, 83.0);
    float y = L2Fx_RandomRange(rangeY, seed, startTime, 89.0);
    float z = L2Fx_RandomRange(rangeZ, seed, startTime, 97.0);
    return float3(x, y, z);
}

// Full spawn offset: fixed emitter offset + polar + box (typical additive combination).
float3 L2Fx_CombineSpawnOffsets(float3 startLocationOffset, float3 polarCartesian, float3 boxCartesian)
{
    return startLocationOffset + polarCartesian + boxCartesian;
}

// Constant velocity displacement: p += v * t (no drag).
float3 L2Fx_DisplacementFromVelocity(float3 velocity, float ageSeconds)
{
    return velocity * max(0.0, ageSeconds);
}

// Random velocity per axis (Unreal StartVelocityRange when direction is axis-aligned / local).
float3 L2Fx_VelocityRandomBox(float2 vx, float2 vy, float2 vz, float seed, float startTime)
{
    return float3(
        L2Fx_RandomRange(vx, seed, startTime, 101.0),
        L2Fx_RandomRange(vy, seed, startTime, 103.0),
        L2Fx_RandomRange(vz, seed, startTime, 107.0));
}

// PTVD_StartPositionAndOwner: direction from spawn toward owner, magnitude in [speedMin, speedMax].
// Use when steam should move along (owner - spawn) in world space.
float3 L2Fx_VelocityTowardOwner(float3 spawnWorld, float3 ownerWorld, float2 speedMinMax, float seed, float startTime)
{
    float3 d = ownerWorld - spawnWorld;
    float len = length(d);
    float3 dir = len > 1e-5 ? (d / len) : float3(0, 0, 1);
    float speed = L2Fx_RandomRange(speedMinMax, seed, startTime, 109.0);
    return dir * speed;
}

// Outward from owner through spawn: direction normalize(spawn - owner).
float3 L2Fx_VelocityOutwardFromOwner(float3 spawnWorld, float3 ownerWorld, float2 speedMinMax, float seed, float startTime)
{
    float3 d = spawnWorld - ownerWorld;
    float len = length(d);
    float3 dir = len > 1e-5 ? (d / len) : float3(0, 1, 0);
    float speed = L2Fx_RandomRange(speedMinMax, seed, startTime, 113.0);
    return dir * speed;
}

// UseRegularSizeScale=False with a single SizeScale key at end of life:
// implicit (RelativeTime=0, RelativeSize=1) -> (tEnd, sEnd). Same as Interlude Steam entry.
float L2Fx_SizeScaleImplicitStartOneKey(float normalizedAge, float useSizeScale, float relativeTimeEnd, float relativeSizeEnd)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float te = max(1e-4, saturate(relativeTimeEnd));
    float u = saturate(normalizedAge / te);
    return lerp(1.0, relativeSizeEnd, u);
}

// ColorScale with two stops: c0 at age 0, c1 at normalized time t1 (Unreal RelativeTime on second key).
float4 L2Fx_ColorScaleTwoKeys(float normalizedAge, float4 color0, float4 color1, float relativeTime1)
{
    float t = max(1e-4, saturate(relativeTime1));
    float u = saturate(normalizedAge / t);
    return lerp(color0, color1, u);
}

#endif
