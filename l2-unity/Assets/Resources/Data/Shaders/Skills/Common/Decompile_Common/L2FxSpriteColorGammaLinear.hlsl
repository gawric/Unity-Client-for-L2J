#ifndef L2_FX_SPRITE_COLOR_GAMMA_LINEAR_INCLUDED
#define L2_FX_SPRITE_COLOR_GAMMA_LINEAR_INCLUDED

// ═══════════════════════════════════════════════════════════════════════════
// L2_FX_SPRITE_COLOR_GAMMA_LINEAR
//   Particle ColorScale / ColorMultiplier: L2 display-gamma → Unity Linear
//
// Converts L2-authored particle ColorScale / ColorMultiplier RGB from
// display/gamma numbers into Unity Linear before tex * color.
// Does NOT change FadeIn/FadeOut math (keep L2FxSpriteColorFade / L2FxMeshColorFade).
// Does NOT change atlas import settings (sRGB ON/OFF is separate).
//
// Filename keeps "Sprite" from the first proof; same helpers are used on MeshEmitters.
//
// VERIFIED (2026-07-19) — Sprite:
//   Effect:   it_healing_potion_ta / SpriteEmitter7 Name="kirakira"
//   Shader:   L2/Effects/it_healing_potion_ta/SpriteEmitter7_Kirakira
//   Mat:      .../it_healing_potion_ta/SpriteEmitter7.mat
//   Texture:  fx_m_t0005 (sRGB = OFF)
//   Symptom:  peak washed white; mid-life OK
//   ColorMul: (1, 0.553, 0.12)
//
// VERIFIED (2026-07-19) — Mesh (same pipeline gap after sRGB OFF):
//   Effect:   it_healing_potion_ta / MeshEmitter0 "Wave", MeshEmitter2 "needlelight"
//   Shaders:  MeshEmitter0_Wave, MeshEmitter2_Needlelight
//   Mats:     MeshEmitter0.mat, MeshEmitter2.mat
//   Symptom:  after atlas sRGB OFF → soft midtones return, but too bright
//   ColorMul: Wave (1, 0.774, 0.6); needlelight (1, 0.412, 0.023)
//
// SCOPE:
//   * USpriteEmitter and UMeshEmitter particle FX (Brighten / tex * particleColor)
//   * Project Color Space = Linear (no-op when UNITY_COLORSPACE_GAMMA)
//   * Atlas already imported Linear (Inspector sRGB = OFF) — see L2FxSpriteColorFade
//     "TEXTURE sRGB — THE #2 MISTAKE". This module is NOT a substitute for that.
//   * Do NOT use for UI, skins, PBR albedo
//
// WHEN TO ENABLE (_L2SpriteColorGammaToLinear = 1 on the material):
//   1) Sprite or Mesh emitter + Brighten/similar tex * particleColor
//   2) Atlas sRGB OFF (gel/core already correct at _RgbBoost≈1)
//   3) CPU/A8 color matches L2 but Unity still looks too bright / white at peak
//   4) ColorMul has midtone channels that should stay tinted, not bleach
//   5) PRACTICAL (eye A/B, 2026-07-19 healing potion): prefer ON for bright /
//      light-core atlases (hot center, pale gel) where Linear ColorMul washes white.
//      Examples that liked ON: kirakira fx_m_t0005, needlelight bright cores.
//
// WHEN TO LEAVE OFF:
//   * Texture still sRGB ON (fix import first)
//   * Gamma project color space
//   * A/B: toggle off vs raw ColorFade output
//   * PRACTICAL: dark / soft / large soft-mesh layers — pow(ColorMul,2.2) * dark
//      texels compounds and reads slightly too dark, especially when the mesh is
//      large on screen. Prefer OFF there; keep sRGB OFF on the atlas either way.
//   * PRACTICAL (needlelight 2026-07-19): ColorMul with LARGE channel spread /
//      very dark channels — e.g. (1, 0.412, 0.023). pow is NOT hue-safe:
//        G: 0.412 → ~0.14,  B: 0.023 → ~0.0002  → green-yellow collapses to dark yellow.
//      Wave (1, 0.774, 0.6) stays balanced enough that ON matched L2.
//      Heuristic: bright/light FX + balanced ColorMul → ON;
//                 dark soft FX OR unbalanced ColorMul (one channel << others) → OFF.
//      Decide per material by eye (same effect can mix both kinds of layers).
//
// HOW TO WIRE (other shaders / other AIs):
//   1) Include after ColorFade:
//        #include ".../Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
//   2) Properties:
//        [Toggle] _L2SpriteColorGammaToLinear
//            ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
//   3) CBUFFER: float _L2SpriteColorGammaToLinear;
//   4) AFTER FullKeys / ResolveColor ColorFade, BEFORE tex * color and _RgbBoost:
//        color = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
//            color, _L2SpriteColorGammaToLinear);
//      Mesh: wrap inside ResolveColor() return, or right after ResolveColor(...)
//   5) Material: set toggle per layer (bright atlas ON / dark soft OFF).
//      Examples that used ON: SpriteEmitter7.mat, MeshEmitter0/2 (re-check by eye
//      if a layer goes too dark when large).
//
// DO NOT:
//   * Reimplement pow in effect shaders — call this library
//   * Put this inside ColorFade facades (fade must stay engine-accurate)
//   * Use _RgbBoost crush as a substitute (false soft-knee)
// ═══════════════════════════════════════════════════════════════════════════

// Approximate display-gamma → linear.
// A/B 2026-07-19 kirakira: 2.2 too orange; 1.7 hue OK but too bright; 2.0 current eye match.
#ifndef L2FX_SPRITE_COLOR_GAMMA_EXP
#define L2FX_SPRITE_COLOR_GAMMA_EXP 2.0
#endif

float3 L2Fx_SpriteColor_GammaToLinearRgb(float3 gammaRgb)
{
#ifndef UNITY_COLORSPACE_GAMMA
    return pow(max(gammaRgb, 0.0), L2FX_SPRITE_COLOR_GAMMA_EXP);
#else
    return gammaRgb;
#endif
}

float4 L2Fx_SpriteColor_GammaToLinear(float4 color)
{
    color.rgb = L2Fx_SpriteColor_GammaToLinearRgb(color.rgb);
    return color;
}

// Material toggle: enable > 0.5 → convert; else pass-through.
float4 L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(float4 color, float enable)
{
    if (enable > 0.5)
        return L2Fx_SpriteColor_GammaToLinear(color);
    return color;
}

#endif
