#ifndef L2_FX_MESH_BRIGHTEN_D3D9_INCLUDED
#define L2_FX_MESH_BRIGHTEN_D3D9_INCLUDED

// D3D9 fixed-function brighten for ribbon/mesh emitters (supportenchant00, fx_m_t0005).
//
// Original L2 FS: out = sample(t0, uv) * textureFactor; blend SrcAlpha One.
// Ribbon UV layout: low luma = tail gradient, high luma = bright head (separate mesh strips).
//
// Tail lift: additive RGB on soft band only (not multiplicative split — avoids head blowout).
// FadeIn/FadeOut: multiply final RGB by lifeAlpha; alpha = textureFactor * tex.a (no lifeAlpha²).
//
// Texture: use authored alpha (fx_m_t*_A.png), not From Gray Scale on blue VFX atlases.
// Include after L2FxMeshFragment.hlsl.

// Weight for tail band: smoothstep(softLum) excluding bright head (lineLum).
float L2Fx_MeshBrighten_SoftTailWeight(
    float3 texRgb,
    float softLumMin,
    float softLumMax,
    float lineLumMin,
    float lineLumMax)
{
    float lum = dot(texRgb, float3(0.299, 0.587, 0.114));
    float lineW = (lineLumMax > lineLumMin + 1e-5)
        ? smoothstep(lineLumMin, lineLumMax, lum)
        : lum;
    float softW = 0.0;
    if (softLumMax > softLumMin + 1e-5)
    {
        softW = smoothstep(softLumMin, softLumMax, lum) * (1.0 - lineW);
    }
    return softW;
}

half3 L2Fx_MeshBrighten_TexHueTint(half3 texRgb)
{
    half texPeak = max(max(texRgb.r, texRgb.g), texRgb.b);
    return texPeak > 0.02 ? texRgb / texPeak : half3(1.0, 1.0, 1.0);
}

// PTDS_Brighten mesh path: tex * factor + tail hue lift; lifeAlpha gates RGB only.
half4 L2Fx_MeshBrighten_D3d9TexFactor(
    half4 texColor,
    half4 factor,
    half lifeAlpha,
    float tailLift,
    float softLumMin,
    float softLumMax,
    float lineLumMin,
    float lineLumMax,
    float rgbBoost,
    float alphaBoost,
    float ignoreMainTexAlpha,
    float fadeAlphaWithLife)
{
    half la = lifeAlpha;
    half3 rgb = texColor.rgb * factor.rgb * (half)rgbBoost;

    float softW = L2Fx_MeshBrighten_SoftTailWeight(
        texColor.rgb, softLumMin, softLumMax, lineLumMin, lineLumMax);
    half3 hueTint = L2Fx_MeshBrighten_TexHueTint(texColor.rgb);
    rgb += hueTint * factor.rgb * (half)softW * (half)tailLift;
    rgb *= la;

    half alpha = factor.a * (half)alphaBoost;
    if (ignoreMainTexAlpha < 0.5)
    {
        alpha *= texColor.a;
    }
    if (fadeAlphaWithLife >= 0.5)
    {
        alpha *= la;
    }

    return half4(saturate(rgb), saturate(alpha));
}

#endif // L2_FX_MESH_BRIGHTEN_D3D9_INCLUDED
