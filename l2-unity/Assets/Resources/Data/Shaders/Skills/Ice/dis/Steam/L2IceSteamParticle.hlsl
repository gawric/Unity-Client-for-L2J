#ifndef L2_ICE_STEAM_PARTICLE_INCLUDED
#define L2_ICE_STEAM_PARTICLE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../../../Common/L2FxFlipbook.hlsl"

// --- Logic distilled from steam.txt (SPIR-V) fragment main ---
//   texA = sample(t0, in_Texcoord1.xy)
//   texB = sample(t1, in_Texcoord2.xy)
//   mixed = lerp(texA, texB, in_Color0.aaaa)
//   tmp   = in_Color0 * mixed
//   out   = float4(tmp.rgb, mixed.a)   // alpha from blend, rgb vertex-colored
//
// Optional fog block (spec_state bit + mode) omitted here; use _UseFog + simple exp on view Z if needed.
//
// Atlas: use material _UseAtlasTimeFlipbook=1 (or rely on empty TEXCOORD1) so flipbook runs even when
// the particle system fills TEXCOORD1 with non-UV data.
//
// Flipbook grid: L2Fx_FlipbookAtlasUV / L2Fx_FlipbookFrameIndex / L2Fx_FlipbookAtlasUVBlend (Common/L2FxFlipbook.hlsl).
#define L2FlipbookCellUV L2Fx_FlipbookAtlasUV
#define L2FlipbookFrame L2Fx_FlipbookFrameIndex

float L2SteamLifetimeFade(float age, float life, float fadeInEnd, float fadeOutStart)
{
    const float fi = fadeInEnd > 1e-4 ? saturate(age / fadeInEnd) : 1.0;
    float fo = 1.0;
    if (age >= fadeOutStart && life > fadeOutStart + 1e-4)
        fo = saturate((life - age) / max(life - fadeOutStart, 1e-4));
    else if (age >= fadeOutStart)
        fo = 0.0;
    return fi * fo;
}

float4 L2SteamCompositeColor(
    float4 vertexColor,
    float4 texA,
    float4 texB,
    float opacity,
    float fade)
{
    const float4 mixed = lerp(texA, texB, vertexColor.a);
    const float3 rgb = vertexColor.rgb * mixed.rgb;
    const float a = mixed.a * opacity * fade;
    return float4(rgb, a);
}

#endif
