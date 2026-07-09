#ifndef L2_FX_ATLAS_DEBUG_INCLUDED
#define L2_FX_ATLAS_DEBUG_INCLUDED

#include "L2FxParticleAnim.hlsl"

// Scene-view atlas preview: active only while _StartTime ~= 0 (material asset / Scene).
// Play mode sets _StartTime on spawn, so production timing is never overridden in game.
// Preview uses _DebugAtlasPreviewAge (0-1 of lifetime) to scrub visible age; FadeIn/FadeOut
// from material (_FadeInEndTime, _FadeoutStartTime) apply with the same L2Fx_LifetimeAlpha as in play mode.
//
// Optional sprite preview toggles (copy into shader Properties [Header(Debug)] block):
//   [Toggle] _DebugAtlasPreviewRealSize ("Debug Preview Real UC Size (no x8)", Float) = 0
//   [Toggle] _DebugAtlasPreviewMotion ("Debug Preview Motion (spawn+velocity)", Float) = 0
//   _DebugAtlasPreviewSizeScale ("Debug Preview Size Multiplier (ignored if Real Size)", Range(0.5, 32)) = 8
// Multi-slot motion in Scene: enable preview + motion; editor L2FxAtlasPreviewSlotSeedSync assigns per-slot _Seed.
//
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

// Motion preview: spawn offset + velocity integration in Scene (still _StartTime=0).
float L2Fx_AtlasDebug_PreviewMotion(float scenePreview, float debugPreviewMotion)
{
    return (scenePreview > 0.5 && debugPreviewMotion > 0.5) ? 1.0 : 0.0;
}

// Real UC/runtime size curve instead of enlarged scene preview quad.
float L2Fx_AtlasDebug_PreviewRealSize(float scenePreview, float debugPreviewRealSize)
{
    return (scenePreview > 0.5 && debugPreviewRealSize > 0.5) ? 1.0 : 0.0;
}

// Run spawn / velocity / displacement paths (disabled in static atlas-only preview).
float L2Fx_AtlasDebug_UseRuntimeMotion(float scenePreview, float previewMotion)
{
    return (scenePreview < 0.5 || previewMotion > 0.5) ? 1.0 : 0.0;
}

// Spin follows motion rule: static preview keeps quad axis-aligned for atlas inspection.
float L2Fx_AtlasDebug_UseRuntimeSpin(float scenePreview, float previewMotion, float spinParticlesEnabled)
{
    return (spinParticlesEnabled > 0.5 && L2Fx_AtlasDebug_UseRuntimeMotion(scenePreview, previewMotion) > 0.5)
        ? 1.0
        : 0.0;
}

void L2Fx_AtlasDebug_OverrideAgeNorm(
    float scenePreview,
    float debugAtlasPreviewLoop,
    float debugAtlasPreviewAgeNorm,
    float currentTime,
    float lifetime,
    inout float age,
    inout float ageNorm)
{
    if (scenePreview < 0.5)
    {
        return;
    }

    lifetime = max(lifetime, 1e-4);
    age = L2Fx_AtlasDebug_ResolvePreviewAge(
        debugAtlasPreviewLoop,
        debugAtlasPreviewAgeNorm,
        currentTime,
        lifetime);
    ageNorm = saturate(age / lifetime);
}

// baseSizeM = authored start size (before lifetime SizeScale). runtimeSizeScale = play-mode curve (e.g. SizeScale at ageNorm).
// previewMinWidthToHeight: 0 = no artificial width; ~0.12 for thin PTDU_Up streaks in enlarged preview.
float3 L2Fx_AtlasDebug_ResolveSpriteSize(
    float scenePreview,
    float previewRealSize,
    float3 baseSizeM,
    float runtimeSizeScale,
    float previewSizeScale,
    float previewMinWidthToHeight)
{
    if (scenePreview < 0.5 || previewRealSize > 0.5)
    {
        return baseSizeM * runtimeSizeScale;
    }

    float3 sizeM = baseSizeM * max(previewSizeScale, 0.5);
    if (previewMinWidthToHeight > 0.0)
    {
        sizeM.x = max(sizeM.x, sizeM.y * previewMinWidthToHeight);
    }

    return sizeM;
}

// Static preview: pin flipbook to SubdivisionStart; loop or motion use runtime flipbook path.
float L2Fx_AtlasDebug_PinFlipbookToStart(float scenePreview, float debugAtlasPreviewLoop, float previewMotion)
{
    return (scenePreview > 0.5 && debugAtlasPreviewLoop < 0.5 && previewMotion < 0.5) ? 1.0 : 0.0;
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

// One+One additive atlas on black Scene background (luma mask, no dark-blue floor).
half4 L2Fx_AtlasDebug_AdditiveOneOnePreview(half4 texColor, half3 tintRgb, half rgbBoost)
{
    half mask = max(max(texColor.r, texColor.g), texColor.b);
    half3 rgb = texColor.rgb * tintRgb * rgbBoost;
    half3 previewRgb = lerp(half3(0.0, 0.0, 0.0), rgb, saturate(mask));
    return half4(saturate(previewRgb), 1.0);
}

#endif
