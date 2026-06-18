#ifndef L2_FX_ATLAS_DEBUG_INCLUDED
#define L2_FX_ATLAS_DEBUG_INCLUDED

// Scene-view atlas preview: active only while _StartTime ~= 0 (material asset / Scene).
// Play mode sets _StartTime on spawn, so production timing is never overridden in game.
// Atlas preview still uses real spawn offset + motion; only age/size/flipbook are overridden for tuning.
// Per-slot _Seed in edit mode: L2FxAtlasPreviewSlotSeedSync (MaterialPropertyBlock on each particle slot).
// Spawn-region wireframe: enable _DebugSpawnRegion on material + L2FxSpriteSpawnRegionDebugDrawer (Editor).
float L2Fx_AtlasDebug_IsScenePreviewActive(float debugAtlasPreview, float startTime)
{
    return (debugAtlasPreview > 0.5 && startTime <= 1e-4) ? 1.0 : 0.0;
}

float L2Fx_AtlasDebug_ResolvePreviewAge(
    float debugAtlasPreviewLoop,
    float debugAtlasPreviewAge,
    float currentTime,
    float lifetime)
{
    lifetime = max(lifetime, 1e-4);

    if (debugAtlasPreviewLoop > 0.5)
    {
        return fmod(max(0.0, currentTime), lifetime);
    }

    return max(0.0, debugAtlasPreviewAge);
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
