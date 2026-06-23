#ifndef L2_FX_ATLAS_DEBUG_INCLUDED
#define L2_FX_ATLAS_DEBUG_INCLUDED

#include "L2FxParticleAnim.hlsl"

// Scene-view atlas preview: active only while _StartTime ~= 0 (material asset / Scene).
// Play mode sets _StartTime on spawn, so production timing is never overridden in game.
// Preview uses _DebugAtlasPreviewAge (0-1 of lifetime) to scrub visible age; FadeIn/FadeOut
// from material (_FadeInEndTime, _FadeoutStartTime) apply with the same L2Fx_LifetimeAlpha as in play mode.
float L2Fx_AtlasDebug_IsScenePreviewActive(float debugAtlasPreview, float startTime)
{
    return (debugAtlasPreview > 0.5 && startTime <= 1e-4) ? 1.0 : 0.0;
}

float L2Fx_AtlasDebug_ResolvePreviewAge(
    float debugAtlasPreviewLoop,
    float debugAtlasPreviewAgeNorm,
    float currentTime,
    float lifetime)
{
    lifetime = max(lifetime, 1e-4);

    if (debugAtlasPreviewLoop > 0.5)
    {
        return fmod(max(0.0, currentTime), lifetime);
    }

    return saturate(debugAtlasPreviewAgeNorm) * lifetime;
}

// Visible age for L2Fx_LifetimeAlpha in preview: timeY=startTime+delay+age, with startTime=delay=0.
float L2Fx_AtlasDebug_PreviewLifeAlpha(
    float debugAtlasPreviewLoop,
    float debugAtlasPreviewAgeNorm,
    float currentTime,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOutEnabled,
    float fadeoutStartTime,
    float fadeOutPower)
{
    float visibleAge = L2Fx_AtlasDebug_ResolvePreviewAge(
        debugAtlasPreviewLoop,
        debugAtlasPreviewAgeNorm,
        currentTime,
        lifetime);
    float lifeAlpha = L2Fx_LifetimeAlpha(
        visibleAge,
        hasLifetime,
        0.0,
        0.0,
        lifetime,
        fadeIn,
        fadeInEndTime,
        fadeOutEnabled,
        fadeoutStartTime);
    if (fadeOutEnabled > 0.5)
    {
        lifeAlpha = pow(saturate(lifeAlpha), max(fadeOutPower, 0.0001));
    }

    return lifeAlpha;
}

// Shared helper for material/scene preview of the currently selected atlas cell.
// The caller is responsible for sampling the already-selected cell UV and providing
// the alpha/mask that best matches that shader's runtime alpha logic.
half4 L2Fx_AtlasDebugPreviewColor(
    half4 texColor,
    float alphaMask,
    float previewAlpha,
    float rgbBoost,
    float4 backgroundColor)
{
    half3 previewRgb = lerp(
        (half3)backgroundColor.rgb,
        texColor.rgb * (half)rgbBoost,
        (half)saturate(alphaMask));
    return half4(previewRgb, (half)previewAlpha);
}

#endif
