#ifndef L2_FX_EMITTER_SPAWN_INCLUDED
#define L2_FX_EMITTER_SPAWN_INCLUDED

// ═══════════════════════════════════════════════════════════════
// L2_FX_EMITTER_SPAWN — UE3 SpriteEmitter spawn helpers
// Unity-corrected (Y-up + unit conversion).
//
// UNIT CONVERSION: UE3 Unreal Units → Unity meters
//   UE3: 1 UU = 1 cm = 0.01 m
//   Unity: 1 unit = 1 meter
//   Multiply all length/velocity/accel by L2FX_UU_TO_UNITY.
//
// Based on UE3 USpriteEmitter / UParticleEmitter disassembly.
// ═══════════════════════════════════════════════════════════════

#include "L2FxParticleAnim.hlsl"

// ───────────────────────────────────────────────────────────────
// Unit conversion
// ───────────────────────────────────────────────────────────────
#ifndef L2FX_UU_TO_UNITY
#define L2FX_UU_TO_UNITY 0.01
#endif

// ───────────────────────────────────────────────────────────────
// Angles & spin
// ───────────────────────────────────────────────────────────────
static const float L2Fx_DegToRad = 0.01745329252;
#define L2FX_SPIN_TO_RAD 0.000095873799

// =================================================================
// SPAWN: LIFETIME & DELAY
// =================================================================

float L2Fx_RandomLifetime(float2 lifetimeMinMax, float seed, float startTime, float salt)
{
    return max(1e-4, L2Fx_RandomRange(lifetimeMinMax, seed, startTime, salt));
}

float L2Fx_RandomInitialDelay(float2 delayMinMax, float seed, float startTime, float salt)
{
    return max(0.0, L2Fx_RandomRange(delayMinMax, seed, startTime, salt));
}

// =================================================================
// SPAWN: POSITION — BOX
// =================================================================

float3 L2Fx_SpawnOffsetBox(
    float2 rangeX_UE, float2 rangeY_UE, float2 rangeZ_UE,
    float seed, float startTime)
{
    float3 off_UE = float3(
        L2Fx_RandomRange(rangeX_UE, seed, startTime, 83.0),
        L2Fx_RandomRange(rangeY_UE, seed, startTime, 89.0),
        L2Fx_RandomRange(rangeZ_UE, seed, startTime, 97.0));
    return off_UE * L2FX_UU_TO_UNITY;
}

// =================================================================
// SPAWN: POSITION — COMBINE
// =================================================================

float3 L2Fx_CombineSpawnOffsets(
    float3 startLocationOffset_UE,
    float3 polarCartesian,
    float3 boxCartesian)
{
    return startLocationOffset_UE * L2FX_UU_TO_UNITY + polarCartesian + boxCartesian;
}

// =================================================================
// SPAWN: VELOCITY
// =================================================================

float3 L2Fx_VelocityRandomBox(
    float2 vx_UE, float2 vy_UE, float2 vz_UE,
    float seed, float startTime)
{
    float3 vel_UE = float3(
        L2Fx_RandomRange(vx_UE, seed, startTime, 101.0),
        L2Fx_RandomRange(vy_UE, seed, startTime, 103.0),
        L2Fx_RandomRange(vz_UE, seed, startTime, 107.0));
    return vel_UE * L2FX_UU_TO_UNITY;
}

float3 L2Fx_VelocityOutwardFromOwner(
    float3 spawnWorld, float3 ownerWorld,
    float2 speedMinMax_UE,
    float seed, float startTime)
{
    float3 d = spawnWorld - ownerWorld;
    float len = length(d);
    float3 dir = len > 1e-5 ? (d / len) : float3(0, 1, 0);
    float speed = L2Fx_RandomRange(speedMinMax_UE, seed, startTime, 113.0);
    return dir * speed * L2FX_UU_TO_UNITY;
}

float3 L2Fx_VelocityTowardOwner(
    float3 spawnWorld, float3 ownerWorld,
    float2 speedMinMax_UE,
    float seed, float startTime)
{
    float3 d = ownerWorld - spawnWorld;
    float len = length(d);
    float3 dir = len > 1e-5 ? (d / len) : float3(0, 1, 0);
    float speed = L2Fx_RandomRange(speedMinMax_UE, seed, startTime, 109.0);
    return dir * speed * L2FX_UU_TO_UNITY;
}

float3 L2Fx_VelocityAddTowardOwner(
    float3 spawnWorld, float3 ownerWorld,
    float2 addRangeMinMax_UE,
    float seed, float startTime)
{
    float3 d = spawnWorld - ownerWorld;
    float len = length(d);
    float3 dir = len > 1e-5 ? (d / len) : float3(0, 1, 0);
    float add = L2Fx_RandomRange(addRangeMinMax_UE, seed, startTime, 127.0);
    return dir * add * L2FX_UU_TO_UNITY;
}

// =================================================================
// SPAWN: ACCELERATION
// =================================================================

float3 L2Fx_AccelerationRandom(
    float3 min_UE, float3 max_UE,
    float seed, float startTime)
{
    float3 acc_UE = float3(
        L2Fx_RandomRange(float2(min_UE.x, max_UE.x), seed, startTime, 137.0),
        L2Fx_RandomRange(float2(min_UE.y, max_UE.y), seed, startTime, 139.0),
        L2Fx_RandomRange(float2(min_UE.z, max_UE.z), seed, startTime, 149.0));
    return acc_UE * L2FX_UU_TO_UNITY;
}

// =================================================================
// SPAWN: SIZE
// =================================================================

float3 L2Fx_StartSize(
    float3 sizeMin_UE, float3 sizeMax_UE,
    bool uniformSize,
    float seed, float startTime)
{
    float3 sz;
    if (uniformSize)
    {
        float s = L2Fx_RandomRange(float2(sizeMin_UE.x, sizeMax_UE.x), seed, startTime, 151.0);
        sz = float3(s, s, s);
    }
    else
    {
        sz = float3(
            L2Fx_RandomRange(float2(sizeMin_UE.x, sizeMax_UE.x), seed, startTime, 151.0),
            L2Fx_RandomRange(float2(sizeMin_UE.y, sizeMax_UE.y), seed, startTime, 157.0),
            L2Fx_RandomRange(float2(sizeMin_UE.z, sizeMax_UE.z), seed, startTime, 163.0));
    }
    return sz * L2FX_UU_TO_UNITY;
}

// =================================================================
// SPAWN: SPIN (scalar)
// =================================================================

float L2Fx_StartSpin(float2 spinRange, float seed, float startTime)
{
    return L2Fx_RandomRange(spinRange, seed, startTime, 167.0);
}

float L2Fx_SpinsPerSecond(float2 spsRange, float seed, float startTime)
{
    return L2Fx_RandomRange(spsRange, seed, startTime, 173.0);
}

// =================================================================
// SPAWN: SUBIMAGE
// =================================================================

uint L2Fx_SubImageIndex(float2 subRange, uint cols, uint rows, float seed, float startTime)
{
    uint count = cols * rows;
    if (count == 0)
        return 0;
    float idx = L2Fx_RandomRange(subRange, seed, startTime, 193.0);
    return (uint) idx % count;
}

// =================================================================
// UPDATE: COLORS
// =================================================================

float4 L2Fx_SampleColorScale(
    float normalizedAge,
    float colorScaleParam,
    uint colorScaleCount,
    float colorScaleTimes[8],
    float4 colorScaleColors[8],
    bool bAlphaBlend)
{
    if (colorScaleCount == 0)
        return float4(1, 1, 1, 1);

    float sp = frac((colorScaleParam + 1.0) * normalizedAge);

    uint idx = 0;
    while (idx < colorScaleCount && colorScaleTimes[idx] < sp)
        idx++;

    float4 prevCol;
    float prevT;
    float4 nextCol;
    float nextT;

    if (idx == 0)
    {
        prevCol = float4(1, 1, 1, 1);
        prevT = 0.0;
        nextCol = colorScaleColors[0];
        nextT = colorScaleTimes[0];
    }
    else
    {
        prevCol = colorScaleColors[idx - 1];
        prevT = colorScaleTimes[idx - 1];
        nextCol = (idx < colorScaleCount) ? colorScaleColors[idx] : prevCol;
        nextT = (idx < colorScaleCount) ? colorScaleTimes[idx] : prevT + 1e-4;
    }

    float ts = (sp - prevT) / max(nextT - prevT, 1e-4);
    float4 col = lerp(prevCol, nextCol, ts);
    col.a = bAlphaBlend ? col.a : 1.0;
    return col;
}

// =================================================================
// UPDATE: SIZES
// =================================================================

float3 L2Fx_SampleSizeScale(
    float normalizedAge,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float sizeScaleTimes[8],
    float3 sizeScaleValues[8],
    bool bUseRegularSizeScale)
{
    if (sizeScaleCount == 0)
        return float3(1, 1, 1);

    float sp = frac((sizeScaleParam + sizeScaleRepeats) * normalizedAge);

    uint idx = 0;
    while (idx < sizeScaleCount && sizeScaleTimes[idx] < sp)
        idx++;

    float3 prevS;
    float prevT;
    float3 nextS;
    float nextT;

    if (idx == 0)
    {
        prevS = float3(1, 1, 1);
        prevT = 0.0;
        nextS = sizeScaleValues[0];
        nextT = sizeScaleTimes[0];
    }
    else
    {
        prevS = sizeScaleValues[idx - 1];
        prevT = sizeScaleTimes[idx - 1];
        nextS = (idx < sizeScaleCount) ? sizeScaleValues[idx] : prevS;
        nextT = (idx < sizeScaleCount) ? sizeScaleTimes[idx] : prevT + 1e-4;
    }

    if (abs(nextT - prevT) < 1e-4)
        return prevS;

    float ts = (sp - prevT) / (nextT - prevT);

    if (bUseRegularSizeScale)
        return lerp(prevS, nextS, ts);
    else
        return lerp(prevS.x, nextS.x, ts).xxx;
}

// =================================================================
// UPDATE: FADE IN / OUT (subtractive)
// =================================================================

float4 L2Fx_ApplyFadeInOut(
    float4 color,
    float normalizedAge,
    float fadeInTime, float4 fadeInColor,
    float fadeOutTime, float4 fadeOutColor,
    bool forcedFade)
{
    if (normalizedAge > fadeOutTime && fadeOutTime < 1.0)
    {
        float fo = (normalizedAge - fadeOutTime) / max(1.0 - fadeOutTime, 1e-4);
        color -= fadeOutColor * saturate(fo);
    }

    if (normalizedAge < fadeInTime && fadeInTime > 0.0)
    {
        float fi = (fadeInTime - normalizedAge) / max(fadeInTime, 1e-4);
        color -= fadeInColor * saturate(fi);
    }

    if (forcedFade && normalizedAge > fadeOutTime && fadeOutTime < 1.0)
    {
        float ft = (normalizedAge - fadeOutTime) / max(1.0 - fadeOutTime, 1e-4);
        color *= 1.0 - saturate(ft);
    }

    return max(color, 0.0);
}

// Polar spawn offset matching common UE1-style StartLocationPolarRange:
//   X = azimuth around Z (degrees, 0..360)
//   Y = angle from +Z axis (degrees; 0 = on +Z, 90 = in XY plane)
//   Z = radius (world units)
// Maps to Cartesian: ring near horizontal for Y ~ 85..95, r ~ const.
float3 L2Fx_PolarCartesianUe(float thetaDeg, float phiDeg, float radius)
{
    float theta = thetaDeg * L2Fx_DegToRad;
    float phi = phiDeg * L2Fx_DegToRad;
    float sinPhi = sin(phi);
    return float3(
        radius * sinPhi * cos(theta),
        radius * sinPhi * sin(theta),
        radius * cos(phi));
}

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
    return L2Fx_PolarCartesianUe(thetaDeg, phiDeg, r);
}

// Constant velocity displacement: p += v * t (no drag).
float3 L2Fx_DisplacementFromVelocity(float3 velocity, float ageSeconds)
{
    return velocity * max(0.0, ageSeconds);
}

// ColorScale with two stops: c0 at age 0, c1 at normalized time t1 (Unreal RelativeTime on second key).
float4 L2Fx_ColorScaleTwoKeys(float normalizedAge, float4 color0, float4 color1, float relativeTime1)
{
    float t = max(1e-4, saturate(relativeTime1));
    float u = saturate(normalizedAge / t);
    return lerp(color0, color1, u);
}

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

// =================================================================
// UPDATE: MOVEMENT, DAMPING, VELOCITY LOSS
// =================================================================

float3 L2Fx_ApplyDamping(float3 vel, float3 damping, float dt)
{
    return vel * (1.0 - damping * dt);
}

float3 L2Fx_ApplyVelocityLoss(float3 vel, float3 loss_UE, float dt)
{
    return vel - loss_UE * L2FX_UU_TO_UNITY * dt;
}

// =================================================================
// UPDATE: COLOR MULTIPLIER
// =================================================================

float3 L2Fx_ApplyColorMultiplier(float3 color, float3 multMin, float3 multMax, float seed, float startTime)
{
    float3 m = float3(
        L2Fx_RandomRange(float2(multMin.x, multMax.x), seed, startTime, 197.0),
        L2Fx_RandomRange(float2(multMin.y, multMax.y), seed + 1, startTime, 199.0),
        L2Fx_RandomRange(float2(multMin.z, multMax.z), seed + 2, startTime, 211.0));
    return color * m;
}

// ─── Абсолютная версия FadeInOut ───
float4 L2Fx_ApplyFadeInOutAbsolute(
    float4 color,
    float ageSeconds,
    float lifetime,
    float fadeInEndTime,
    float4 fadeInColor,
    float fadeOutStartTime,
    float4 fadeOutColor,
    bool forcedFade)
{
    float invLt = 1.0 / max(lifetime, 1e-4);
    float fadeInTime = fadeInEndTime * invLt;
    float fadeOutTime = fadeOutStartTime * invLt;
    float normalizedAge = saturate(ageSeconds * invLt);
    return L2Fx_ApplyFadeInOut(color, normalizedAge, fadeInTime, fadeInColor,
                               fadeOutTime, fadeOutColor, forcedFade);
}

// ─── Lifetime для MeshEmitter (ForcedFade derivation) ───
float L2Fx_MeshDeriveLifetime(float fadeOutStartTime, bool forcedFade, float explicitLifetime)
{
    if (forcedFade && explicitLifetime <= 0.0)
        return max(fadeOutStartTime, 0.001) + 0.001;
    return max(explicitLifetime, 0.001);
}

#endif // L2_FX_EMITTER_SPAWN_INCLUDED