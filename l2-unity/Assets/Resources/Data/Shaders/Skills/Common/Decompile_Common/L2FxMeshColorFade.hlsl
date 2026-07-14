#ifndef L2_FX_MESH_COLOR_FADE_INCLUDED
#define L2_FX_MESH_COLOR_FADE_INCLUDED

// UMeshEmitter runtime ColorScale + FadeOut.
//
// CONFIRMED for LineageEffect.m_u004_b_2 / MeshEmitter6:
// decompile control flow, live L2 particle memory, and the Unity CPU mirror
// agree at matched lifeNorm samples. Example:
//   L2   lifeNorm=0.8933 size=0.3372 runtimeColorA8=(0,0,0,27)
//   Unity lifeNorm=0.8924 size=0.3371 runtimeColorA8=(0,0,0,27)
//
// Verified behavior for that emitter:
//   - ColorScale is sampled linearly by normalized particle lifetime.
//   - ColorScaleRepeats uses phase = frac((repeats + 1) * lifeNorm).
//   - ColorMultiplier is applied to RGB.
//   - FadeOut subtracts the same normalized fade value from RGB and alpha.
//   - With FadeOut=True and omitted FadeOutStartTime, start time is 0:
//       runtimeColorA8.A = floor(255 * (1 - lifeNorm)).
//
// This is byte-validated for the configuration above, not a claim that every
// MeshEmitter variant shares identical FadeIn, alpha-mode, or blend behavior.
// The engine quantizes channels to bytes after its arithmetic. Unity rendering
// should retain floats; use L2Fx_MeshColorFade_ToByte only for hook comparisons.

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

float4 L2Fx_MeshColorFade_Apply(
    float4 colorScale,
    float3 colorMultiplier,
    float ageSeconds,
    float lifetimeSeconds,
    float fadeOut,
    float fadeOutStartTime)
{
    float4 color = float4(colorScale.rgb * colorMultiplier, colorScale.a);
    if (fadeOut < 0.5)
    {
        return saturate(color);
    }

    float lifetime = max(lifetimeSeconds, 1e-4);
    float start = clamp(fadeOutStartTime, 0.0, lifetime);
    float fade = ageSeconds > start
        ? saturate((ageSeconds - start) / max(lifetime - start, 1e-4))
        : 0.0;

    // UE applies the fade subtractively to every runtime BGRA channel.
    return max(color - fade.xxxx, 0.0);
}

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
        fadeOut,
        fadeOutStartTime);
}

uint4 L2Fx_MeshColorFade_ToByte(float4 color)
{
    return (uint4)floor(saturate(color) * 255.0);
}

#endif // L2_FX_MESH_COLOR_FADE_INCLUDED
