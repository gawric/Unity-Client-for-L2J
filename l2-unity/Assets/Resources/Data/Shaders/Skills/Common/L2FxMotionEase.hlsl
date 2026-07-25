#ifndef L2_FX_MOTION_EASE_INCLUDED
#define L2_FX_MOTION_EASE_INCLUDED

// ═══════════════════════════════════════════════════════════════
// L2_FX_MOTION_EASE — ease-in path motion (slow start, fast finish)
//
// Use for funnel / charge-up particles: zero speed at spawn, rising
// speed toward a target. Works in any space (pass spawn/target in OS or WS).
// ═══════════════════════════════════════════════════════════════

// Normalized lifetime [0,1] → path progress with ease-in.
// easePower > 1: slow start; = 1: linear.
float L2Fx_EaseInPathProgress(float ageNorm, float easePower)
{
    return pow(saturate(ageNorm), max(easePower, 1.0));
}

// d(progress)/d(ageNorm); zero at birth, peaks near end when easePower > 1.
float L2Fx_EaseInSpeedFactor(float ageNorm, float easePower)
{
    float u = saturate(ageNorm);
    float p = max(easePower, 1.0);
    return p * pow(max(u, 1e-8), p - 1.0);
}

// Eased lerp spawn → target. Returns path progress via out for arc/arrival helpers.
float3 L2Fx_EaseInPathPosition(
    float3 spawnPos,
    float3 targetPos,
    float ageNorm,
    float easePower,
    out float pathProgress)
{
    pathProgress = L2Fx_EaseInPathProgress(ageNorm, easePower);
    return lerp(spawnPos, targetPos, pathProgress);
}

// Overload when path progress is not needed.
float3 L2Fx_EaseInPathPosition(
    float3 spawnPos,
    float3 targetPos,
    float ageNorm,
    float easePower)
{
    float pathProgress;
    return L2Fx_EaseInPathPosition(spawnPos, targetPos, ageNorm, easePower, pathProgress);
}

// Optional arc bend from constant acceleration; strongest mid-flight, zero at endpoints.
float3 L2Fx_EaseInPathArcOffset(
    float3 acceleration,
    float ageSeconds,
    float pathProgress,
    float arcScale)
{
    float arcWeight = (1.0 - pathProgress) * (1.0 - pathProgress);
    float t = max(0.0, ageSeconds);
    return acceleration * (0.5 * t * t) * arcWeight * arcScale;
}

// Clamp at target and hide when path completes or within stop radius.
void L2Fx_FocalArrivalClamp(
    float3 targetPos,
    float pathProgress,
    float pathCompleteThreshold,
    float stopDistanceWorld,
    inout float3 centerPos,
    out float visibility)
{
    visibility = 1.0;
    if (pathProgress >= pathCompleteThreshold)
    {
        centerPos = targetPos;
        visibility = 0.0;
        return;
    }

    if (length(centerPos - targetPos) <= stopDistanceWorld)
    {
        centerPos = targetPos;
        visibility = 0.0;
    }
}

// Pull scattered arc paths toward a shared focal in the final lifetime segment.
float3 L2Fx_EndFocalConverge(
    float3 centerPos,
    float3 focalPos,
    float ageNorm,
    float convergeStartNorm,
    float convergePower)
{
    if (convergeStartNorm >= 0.999)
    {
        return centerPos;
    }

    float u = saturate((ageNorm - convergeStartNorm) / max(1.0 - convergeStartNorm, 1e-4));
    if (u <= 0.0)
    {
        return centerPos;
    }

    float pull = pow(u, max(convergePower, 0.25));
    return lerp(centerPos, focalPos, pull);
}

#endif
