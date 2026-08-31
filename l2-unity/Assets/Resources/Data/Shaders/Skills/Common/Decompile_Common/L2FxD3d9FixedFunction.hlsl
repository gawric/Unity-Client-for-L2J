#ifndef L2_FX_D3D9_FIXED_FUNCTION_INCLUDED
#define L2_FX_D3D9_FIXED_FUNCTION_INCLUDED

// D3D9 fixed-function FS combiners from RenderDoc SPIR-V.
// Both bind t0 + t1. The ColorOp is not the same — do not reuse one for the other.
//
// 1) MODULATE2X — two distinct textures (mesh material, e.g. MeshEmitter410
//    circle_flow_01 = circleflow_1 * circleflow_2).
//      t0 = ImageSample(t0, texcoord0)
//      t1 = ImageSample(t1, texcoord1)
//      rgb = saturate(t0 * t1 * 2)
//      rgb = saturate(rgb * textureFactor * 2)
//      a   = t0.a * textureFactor.a
//    Framebuffer: Blend One One. Not two additive draws.
//
// 2) BLENDDIFFUSEALPHA — same atlas twice, different UVs (sprite
//    BlendBetweenSubdivisions, e.g. SpriteEmitter49 / fx_m_t0085).
//    RenderDoc shows two texture slots; UC has one Texture=.
//      t0 = ImageSample(t0, texcoord0)   // frame A
//      t1 = ImageSample(t1, texcoord1)   // frame B
//      mixed = lerp(t0, t1, in_Color0.a) // vertex A = subdiv blend, not opacity
//      rgb = in_Color0.rgb * mixed.rgb
//      a   = mixed.a                     // combiner; do not * Opacity
//    FadeIn coverage is applied after via fade-only (not Opacity).
//    consts.textureFactor is read and unused. Reconstruct in_Color0.a from the
//    flipbook blend (L2 overwrites particle alpha with that factor at draw).

half4 L2Fx_D3d9_Modulate2xTwoTexTFactor(half4 t0, half4 t1, half4 textureFactor)
{
    half4 combined = saturate(t0 * t1 * (half)2.0);
    combined.a = t0.a;
    half3 rgb = saturate(combined.rgb * textureFactor.rgb * (half)2.0);
    half a = combined.a * textureFactor.a;
    return half4(rgb, a);
}

half4 L2Fx_D3d9_BlendDiffuseAlphaTwoTex(half4 t0, half4 t1, half4 vertexColor)
{
    half4 mixed = lerp(t0, t1, vertexColor.a);
    return half4(vertexColor.rgb * mixed.rgb, mixed.a);
}

// Coverage after BlendDiffuseAlpha.
// SPIR-V combiner a = mixed.a — do not multiply UC Opacity (that made milk
// steam invisible). FadeIn/Out still scale coverage: vertex A is the frame
// lerp, so fade lives in colorFade.a. For AlphaBlend, Opacity is already
// folded into colorFade.a; divide it back out to keep milk density.
half L2Fx_D3d9_BlendBetweenParticleCoverage(
    half textureAlpha,
    float fadeAlpha,
    float opacity,
    float alphaBlend)
{
    float fadeOnly = saturate(fadeAlpha);
    if (alphaBlend >= 0.5 && opacity > 1e-4)
    {
        fadeOnly = saturate(fadeAlpha / opacity);
    }

    return textureAlpha * (half)fadeOnly;
}

#endif
