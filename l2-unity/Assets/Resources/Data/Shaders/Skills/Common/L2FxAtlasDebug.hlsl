#ifndef L2_FX_ATLAS_DEBUG_INCLUDED
#define L2_FX_ATLAS_DEBUG_INCLUDED

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
