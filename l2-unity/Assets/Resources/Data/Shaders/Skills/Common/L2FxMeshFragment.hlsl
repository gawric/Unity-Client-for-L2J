#ifndef L2_FX_MESH_FRAGMENT_INCLUDED
#define L2_FX_MESH_FRAGMENT_INCLUDED

// Fragment helpers for URP mesh emitters (curse / magic circle).

float L2Fx_MeshFrag_AlphaFeather(float texAlpha, float alphaEdgeFeather)
{
    if (alphaEdgeFeather < 1e-4)
    {
        return texAlpha;
    }

    return smoothstep(0.0, alphaEdgeFeather, texAlpha);
}

float L2Fx_MeshFrag_SampleTextureAlpha(
    float4 texColor,
    float alphaFromLuma,
    float lumaAlphaFloor,
    float ignoreMainTexAlpha)
{
    if (alphaFromLuma > 0.5)
    {
        float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
        float mask = saturate((lum - lumaAlphaFloor) / max(1.0 - lumaAlphaFloor, 1e-4));
        if (ignoreMainTexAlpha < 0.5)
        {
            return mask * texColor.a;
        }

        return mask;
    }

    if (ignoreMainTexAlpha < 0.5)
    {
        return texColor.a;
    }

    return 1.0;
}

float L2Fx_MeshFrag_ApplyAlphaPowerStrength(
    float mask,
    float alphaFromLuma,
    float alphaPower,
    float alphaStrength)
{
    if (alphaFromLuma > 0.5)
    {
        return pow(saturate(mask), max(alphaPower, 0.0001)) * alphaStrength;
    }

    return mask;
}

void L2Fx_MeshFrag_ApplyGroundShadow(
    inout float4 color,
    float4 texColor,
    float4 groundShadowColor,
    float groundShadowLumaFloor)
{
    float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
    float maskRaw = lum * texColor.a;
    float mask = saturate((maskRaw - groundShadowLumaFloor) / max(1.0 - groundShadowLumaFloor, 1e-4));
    color.rgb = groundShadowColor.rgb * mask;
    color.a = saturate(color.a * mask);
}

// MagicCircleBrighten: split soft ribbon vs sharp lines by luma; optional UV layer.
float3 L2Fx_MeshFrag_MagicCircleLumaUvSplit(
    float3 tintedRgb,
    float4 texColor,
    float2 uvMesh,
    float splitRibbonByLum,
    float softLumMin,
    float softLumMax,
    float lineLumMin,
    float lineLumMax,
    float softOpacityMul,
    float lineOpacityMul,
    float softRgbBoost,
    float lineRgbBoost,
    float splitByUvLayer,
    float4 uvLayerCenter,
    float uvLayerDistMin,
    float uvLayerDistMax,
    float outerSoftOpacityMul,
    float outerLineOpacityMul,
    float outerSoftRgbBoost,
    float outerLineRgbBoost)
{
    if (splitRibbonByLum < 0.5)
    {
        return tintedRgb;
    }

    float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
    float lineW = (lineLumMax > lineLumMin + 1e-5)
        ? smoothstep(lineLumMin, lineLumMax, lum)
        : lum;
    float softW = 0.0;
    if (softLumMax > softLumMin + 1e-5)
    {
        softW = smoothstep(softLumMin, softLumMax, lum) * (1.0 - lineW);
    }

    float fInner = softW * softOpacityMul * softRgbBoost + lineW * lineOpacityMul * lineRgbBoost;
    float f = fInner;

    if (splitByUvLayer > 0.5)
    {
        float2 c = uvLayerCenter.xy;
        float dist = abs(uvMesh.x - c.x) + abs(uvMesh.y - c.y);
        float lo = min(uvLayerDistMin, uvLayerDistMax);
        float hi = max(uvLayerDistMin, uvLayerDistMax);
        float outerW = 1.0 - smoothstep(lo, max(hi, lo + 1e-5), dist);
        float fOuter = softW * outerSoftOpacityMul * outerSoftRgbBoost
            + lineW * outerLineOpacityMul * outerLineRgbBoost;
        f = lerp(fInner, fOuter, outerW);
    }

    return tintedRgb * f;
}

// PTDS_Darken: white = no change, dark tint = darken (min-blend or DstColor multiply).
float3 L2Fx_MeshFrag_DarkenMinSource(float3 tintedRgb, float mask)
{
    float m = saturate(mask);
    return lerp(float3(1.0, 1.0, 1.0), saturate(tintedRgb), m);
}

#endif // L2_FX_MESH_FRAGMENT_INCLUDED
