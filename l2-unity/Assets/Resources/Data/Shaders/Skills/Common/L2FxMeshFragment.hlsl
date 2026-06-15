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

// Fade alpha near mesh UV edges so the quad boundary is not visible (square falloff).
float L2Fx_QuadEdgeSoftMask(float2 uv01, float edgeSoftness)
{
    if (edgeSoftness <= 1e-4)
    {
        return 1.0;
    }

    float2 edgeDist = min(saturate(uv01), saturate(1.0 - uv01));
    float edgeMin = min(edgeDist.x, edgeDist.y);
    return smoothstep(0.0, edgeSoftness, edgeMin);
}

// Per-axis quad edge fade: weak on X (wide ring sides), stronger on Y (top/bottom margin).
float L2Fx_QuadEdgeSoftMaskSelective(float2 uv01, float2 edgeSoftness)
{
    float2 edgeDist = min(saturate(uv01), saturate(1.0 - uv01));

    float maskX = (edgeSoftness.x <= 1e-4) ? 1.0 : smoothstep(0.0, edgeSoftness.x, edgeDist.x);
    float maskY = (edgeSoftness.y <= 1e-4) ? 1.0 : smoothstep(0.0, edgeSoftness.y, edgeDist.y);
    return maskX * maskY;
}

// Circular fade: matches round sprite alpha; softness is UV radius band before inscribed circle edge.
float L2Fx_RadialUvSoftMask(float2 uv01, float edgeSoftness)
{
    if (edgeSoftness <= 1e-4)
    {
        return 1.0;
    }

    float radial = length(uv01 - 0.5) * 2.0;
    return 1.0 - smoothstep(1.0 - edgeSoftness, 1.0, radial);
}

half4 L2Fx_AlphaDilatedSample(
    TEXTURE2D_PARAM(mainTex, mainTexSampler),
    float2 uv,
    float2 texelSize,
    float dilateTexels)
{
    half4 tex = SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv);
    if (dilateTexels <= 0.001)
    {
        return tex;
    }

    float2 s = texelSize * dilateTexels;
    float2 sx = float2(s.x, 0.0);
    float2 sy = float2(0.0, s.y);
    float2 sd = s * 0.7071;

    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sx * 0.35));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sx * 0.35));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sy * 0.35));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sy * 0.35));

    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sx * 0.7));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sx * 0.7));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sy * 0.7));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sy * 0.7));

    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sx));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sx));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sy));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sy));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + sd));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv - sd));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + float2(sd.x, -sd.y)));
    tex = max(tex, SAMPLE_TEXTURE2D(mainTex, mainTexSampler, uv + float2(-sd.x, sd.y)));
    return tex;
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

// BC2/DXT3-style sprites: black RGB + shape in alpha. Use tint as particle color unless tex carries RGB.
float3 L2Fx_MeshFrag_SpriteTintRgb(float3 texRgb, float3 tintRgb)
{
    float rgbPeak = max(max(texRgb.r, texRgb.g), texRgb.b);
    return rgbPeak > 0.02 ? texRgb * tintRgb : tintRgb;
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
