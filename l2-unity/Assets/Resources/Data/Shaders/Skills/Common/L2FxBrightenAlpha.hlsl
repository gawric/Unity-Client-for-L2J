#ifndef L2_FX_BRIGHTEN_ALPHA_INCLUDED
#define L2_FX_BRIGHTEN_ALPHA_INCLUDED

// PTDS_Brighten (SrcAlpha One): rebuild contribution when texture RGB is visible but imported alpha is low.
// Include after L2FxMeshFragment.hlsl for L2Fx_MeshFrag_SampleTextureAlpha.

struct L2Fx_BrightenAlphaTuning
{
    float haloInteriorFill;
    float haloLumaMin;
    float haloLumaMax;
    float faintRayFill;
    float faintRayLumaMin;
    float faintRayLumaMax;
    float faintRayRgbLift;
    float historyRgbAlphaFill;
    float historyRgbAlphaPower;
    float historyRgbAlphaMin;
    float historyRgbBoost;
    float alphaPower;
    float rgbAlphaModulate;
};

float L2Fx_BrightenAlphaRaw(
    half4 tex,
    float alphaFromLuma,
    float lumaAlphaFloor,
    float ignoreMainTexAlpha)
{
    if (alphaFromLuma > 0.5)
    {
        return L2Fx_MeshFrag_SampleTextureAlpha(tex, alphaFromLuma, lumaAlphaFloor, ignoreMainTexAlpha);
    }

    return ignoreMainTexAlpha < 0.5 ? tex.a : 1.0;
}

// Returns RGB (after optional faint lift) and alpha blend weight for SrcAlpha One.
void L2Fx_BrightenApplyTextureContribution(
    half4 tex,
    half4 tint,
    L2Fx_BrightenAlphaTuning tuning,
    float alphaRaw,
    inout half3 rgb,
    out float alphaBlend)
{
    float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
    float texPeak = max(max(tex.r, tex.g), tex.b);

    float aFromColor = smoothstep(tuning.haloLumaMin, tuning.haloLumaMax, lum);
    alphaBlend = max(alphaRaw, aFromColor * tuning.haloInteriorFill);

    float historySignal = saturate((texPeak - tuning.historyRgbAlphaMin) / max(1.0 - tuning.historyRgbAlphaMin, 1e-4));
    float historyAlpha = pow(historySignal, max(tuning.historyRgbAlphaPower, 0.0001)) * tuning.historyRgbAlphaFill;
    alphaBlend = max(alphaBlend, historyAlpha);
    rgb *= (half)lerp(1.0, tuning.historyRgbBoost, historyAlpha);

    float faintVisible = smoothstep(tuning.faintRayLumaMin, tuning.faintRayLumaMax, lum);
    float brightSuppress = 1.0 - smoothstep(tuning.faintRayLumaMax, tuning.haloLumaMax, lum);
    float alphaGap = 1.0 - saturate(alphaRaw * 2.0);
    float faintMask = faintVisible * brightSuppress * alphaGap;
    alphaBlend = max(alphaBlend, faintMask * tuning.faintRayFill);

    float tintPeak = max(texPeak, 1e-4);
    half3 faintTint = (half3)(tex.rgb / tintPeak);
    half faintLift = (half)(faintMask * tuning.faintRayRgbLift);
    rgb = max(rgb, faintTint * faintLift * tint.rgb);

    alphaBlend = pow(saturate(alphaBlend), max(tuning.alphaPower, 0.0001));
    rgb *= (half)lerp(1.0, alphaBlend, saturate(tuning.rgbAlphaModulate));
}

half4 L2Fx_BrightenFinalize(
    half3 rgb,
    float alphaBlend,
    half4 tint,
    float opacity,
    float lifeAlpha,
    float viewMask)
{
    half4 col;
    col.rgb = rgb;
    col.a = (half)saturate(alphaBlend * tint.a * opacity * lifeAlpha);
    col.rgb *= (half)viewMask;
    col.a *= (half)viewMask;
    return col;
}

#endif // L2_FX_BRIGHTEN_ALPHA_INCLUDED
