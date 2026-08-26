#ifndef L2_FX_BEAM_COLOR_INCLUDED
#define L2_FX_BEAM_COLOR_INCLUDED

// UBeamEmitter runtime color — VERIFIED on the original Lineage 2 Interlude
// client (Engine.dll, wh_heal_ta UParticle runtimeColorA8 capture, 2026-08-23).
//
// Exact live match:
//   ColorScale(age, Repeats + 1)
//   * per-particle ColorMultiplier
//   -> subtractive FadeIn / FadeOut on RGBA
//   -> PTDS_Translucent Opacity scales RGB, not A
//
// Slot 0 proof at age=0.2899, lifetime=2.5396:
//   ColorMultiplier=(0.6356,0.6409,0.6362)
//   runtimeColorA8 BGRA=(15,15,15,147)
// The existing L2Fx_SpriteColorFade_FullKeys path reproduces these bytes.
//
// IMPORTANT: return this raw color directly for PTDS_Translucent (Blend One One).
// Do not run L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled afterwards: 15/255
// becomes approximately 1/255 and makes the beam appear to blink.

#include "L2FxSpriteColorFade.hlsl"

float4 L2Fx_Beam_RuntimeColorKeys(
    uint colorScaleCount,
    float colorScaleRepeats,
    float4 colorKey0,
    float colorKey1Time,
    float4 colorKey1,
    float colorKey2Time,
    float4 colorKey2,
    float3 colorMultiplierMin,
    float3 colorMultiplierMax,
    float ageSeconds,
    float lifetimeSeconds,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStartTime,
    float opacity,
    float opacityRatio,
    float seed,
    float startTime)
{
    // alphaBlend=0 is the verified PTDS_Translucent path: Opacity scales RGB.
    return L2Fx_SpriteColorFade_FullKeys(
        colorScaleCount,
        colorScaleRepeats,
        0.0,
        colorKey0,
        colorKey1Time,
        colorKey1,
        colorKey2Time,
        colorKey2,
        1.0,
        colorKey2,
        colorMultiplierMin,
        colorMultiplierMax,
        ageSeconds,
        lifetimeSeconds,
        1.0,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStartTime,
        opacity,
        opacityRatio,
        seed,
        startTime);
}

#endif // L2_FX_BEAM_COLOR_INCLUDED
