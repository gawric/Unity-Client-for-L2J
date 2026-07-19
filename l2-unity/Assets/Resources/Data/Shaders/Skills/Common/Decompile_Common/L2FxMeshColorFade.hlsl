#ifndef L2_FX_MESH_COLOR_FADE_INCLUDED
#define L2_FX_MESH_COLOR_FADE_INCLUDED

// UMeshEmitter runtime ColorScale + FadeIn/FadeOut + Opacity.
//
// CONFIRMED (FadeOut / ColorScale / ColorMultiplier):
//   LineageEffect.m_u004_b_2 / MeshEmitter6 — decompile + live memory + Unity mirror.
//   Example: L2 lifeNorm=0.8933 runtimeColorA8=(0,0,0,27) matches Unity.
//
// CONFIRMED (FadeIn + Opacity, 2026-07-17):
//   it_healing_potion_ta MeshEmitter needlelight + Wave UpdateParticles logs.
//   - FadeIn: age < FadeInEnd -> subtract (FadeInEnd - age) / FadeInEnd from RGBA
//   - FadeOut: age > FadeOutStart -> subtract (age - start) / (life - start) from RGBA
//   - Both subtractive, same as Sprite (L2FxSpriteColorFade_Apply)
//   - Opacity (.uc) multiplies RGB ONLY after fades; runtimeColorA8.A is NOT scaled
//   - Wave Opacity=0.5: t=0.0482 -> BGRA(51,73,102,204); post-FadeIn -> (76,98,127,255)
//   - needlelight FadeInEnd=0.03 / FadeOutStart=0.28: Tick1/58 byte-exact match
//
// Pipeline:
//   ColorScale * ColorMultiplier
//   -> FadeIn / FadeOut (subtractive on all channels)
//   -> max(0)
//   -> RGB *= Opacity
//   -> floor(*255) for hook compare (L2Fx_MeshColorFade_ToByte)
//
// Unity rendering should retain floats; ToByte is for live hook comparisons only.
//
// After this facade (Linear project + atlas sRGB OFF), particle ColorMul midtones
// can look too bright — optional L2FxSpriteColorGammaLinear.hlsl on the material
// toggle _L2SpriteColorGammaToLinear. Verified Wave + needlelight 2026-07-19.
// Do not bake that into this file (fade/A8 must stay engine-accurate).

#include "../L2FxEmitterSpawn.hlsl"

void L2Fx_MeshColorFade_BuildKeys6(
    float4 color0,
    float time1, float4 color1,
    float time2, float4 color2,
    float time3, float4 color3,
    float time4, float4 color4,
    float time5, float4 color5,
    out float times[8],
    out float4 colors[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        colors[i] = float4(1, 1, 1, 1);
    }

    times[0] = 0.0;
    colors[0] = color0;
    times[1] = time1;
    colors[1] = color1;
    times[2] = time2;
    colors[2] = color2;
    times[3] = time3;
    colors[3] = color3;
    times[4] = time4;
    colors[4] = color4;
    times[5] = time5;
    colors[5] = color5;
}

// Full mesh path: FadeIn + FadeOut + Opacity (RGB only).
float4 L2Fx_MeshColorFade_Apply(
    float4 colorScale,
    float3 colorMultiplier,
    float ageSeconds,
    float lifetimeSeconds,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStartTime,
    float opacity)
{
    float4 color = float4(colorScale.rgb * colorMultiplier, colorScale.a);
    float lifetime = max(lifetimeSeconds, 1e-4);

    // Fade-in: particle rises from black over [0, FadeInEndTime].
    if (fadeIn >= 0.5 && fadeInEndTime > 0.0 && ageSeconds < fadeInEndTime)
    {
        float fi = (fadeInEndTime - ageSeconds) / max(1e-4, fadeInEndTime);
        color -= saturate(fi);
    }

    // Fade-out: particle sinks to black over [FadeOutStart, lifetime].
    if (fadeOut >= 0.5)
    {
        float start = clamp(fadeOutStartTime, 0.0, lifetime);
        if (ageSeconds > start)
        {
            float fo = (ageSeconds - start) / max(1e-4, lifetime - start);
            color -= saturate(fo);
        }
    }

    color = max(color, 0.0);

    // CONFIRMED: Opacity scales source RGB only; A stays post-fade.
    color.rgb *= saturate(opacity);
    return color;
}

// Backward-compatible FadeOut-only path (no FadeIn, Opacity=1).
float4 L2Fx_MeshColorFade_Apply(
    float4 colorScale,
    float3 colorMultiplier,
    float ageSeconds,
    float lifetimeSeconds,
    float fadeOut,
    float fadeOutStartTime)
{
    return L2Fx_MeshColorFade_Apply(
        colorScale,
        colorMultiplier,
        ageSeconds,
        lifetimeSeconds,
        0.0,
        0.0,
        fadeOut,
        fadeOutStartTime,
        1.0);
}

float4 L2Fx_MeshColorFade_FullKeys6(
    float ageSeconds,
    float lifetimeSeconds,
    float colorScaleRepeats,
    float3 colorMultiplier,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStartTime,
    float opacity,
    float4 color0,
    float time1, float4 color1,
    float time2, float4 color2,
    float time3, float4 color3,
    float time4, float4 color4,
    float time5, float4 color5)
{
    float times[8];
    float4 colors[8];
    L2Fx_MeshColorFade_BuildKeys6(
        color0,
        time1, color1,
        time2, color2,
        time3, color3,
        time4, color4,
        time5, color5,
        times,
        colors);

    float lifeNorm = saturate(ageSeconds / max(lifetimeSeconds, 1e-4));
    float4 colorScale = L2Fx_SampleColorScale(
        lifeNorm,
        colorScaleRepeats,
        6,
        times,
        colors,
        true);
    return L2Fx_MeshColorFade_Apply(
        colorScale,
        colorMultiplier,
        ageSeconds,
        lifetimeSeconds,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStartTime,
        opacity);
}

// Backward-compatible FullKeys6 (no FadeIn, Opacity=1).
float4 L2Fx_MeshColorFade_FullKeys6(
    float ageSeconds,
    float lifetimeSeconds,
    float colorScaleRepeats,
    float3 colorMultiplier,
    float fadeOut,
    float fadeOutStartTime,
    float4 color0,
    float time1, float4 color1,
    float time2, float4 color2,
    float time3, float4 color3,
    float time4, float4 color4,
    float time5, float4 color5)
{
    return L2Fx_MeshColorFade_FullKeys6(
        ageSeconds,
        lifetimeSeconds,
        colorScaleRepeats,
        colorMultiplier,
        0.0,
        0.0,
        fadeOut,
        fadeOutStartTime,
        1.0,
        color0,
        time1, color1,
        time2, color2,
        time3, color3,
        time4, color4,
        time5, color5);
}

uint4 L2Fx_MeshColorFade_ToByte(float4 color)
{
    return (uint4)floor(saturate(color) * 255.0);
}

#endif // L2_FX_MESH_COLOR_FADE_INCLUDED
