#ifndef L2_FX_SPRITE_COLOR_FADE_INCLUDED
#define L2_FX_SPRITE_COLOR_FADE_INCLUDED

// ═══════════════════════════════════════════════════════════════════════════
// L2_FX_SPRITE_COLOR_FADE   —   SPRITE EMITTER ONLY
// UE3 USpriteEmitter runtime COLOR + FADE-IN/OUT, verified byte-for-byte against
// live Engine.dll memory (AEmitter::Tick hook).
//   * FADE-OUT verified on effect LineageEffect.e_u031_a (match=yes).
//   * FADE-IN  verified on effect LineageEffect.m_u004_b_2 (SpriteEmitter0,
//     FadeInEndTime=0.09): 92/96 ticks exact, maxDelta +/-1 (time rounding).
//     Layer with FadeInEndTime~0.049: 5/5 exact. Same subtractive mirror as fade-out.
//     Proof: AutoLoginInterlude/L2_Formuls/SpriteFadeIn_Formula.txt
//
// SCOPE: use this ONLY for USpriteEmitter layers (billboards). Mesh emitters
// have a different memory layout and their own helpers (L2FxMeshLifetimeAlpha,
// L2FxMeshEmitter). Do NOT use this file for mesh layers.
//
// This is the exact color pipeline the engine writes into the particle draw
// color slot (UParticle+0xA8, "runtimeColorA8", byte order BGRA). It reproduces:
//   1) per-particle random ColorMultiplier applied to the emitter ColorScale
//   2) SUBTRACTIVE fade-in  (rises from black) applied EQUALLY to R,G,B,A
//   3) SUBTRACTIVE fade-out (additive fade-to-black) applied EQUALLY to R,G,B,A
//
// Reference / proof: AutoLoginInterlude/L2_Formuls/e_u031_a_ColorFade_Formula.txt
// ═══════════════════════════════════════════════════════════════════════════
//
// ─── FOR AI: EXACT CONTRACT (read this before editing or wiring) ─────────────
// PURPOSE: reproduce, in Unity/URP HLSL, the exact draw color a UE3 SpriteEmitter
// writes per frame. Prefer ONE call: L2Fx_SpriteColorFade_FullKeys(...). It runs
// the engine order:  ColorScale -> * ColorMultiplier -> FadeIn/Out -> Opacity.
//
// FUNCTION MAP (which to call):
//   * white ColorScale (e_u031_a):     L2Fx_SpriteColorFade_White(...)
//   * pre-sampled colored ColorScale:  L2Fx_SpriteColorFade_WithScale(...)
//   * whole pipeline from raw uniforms: L2Fx_SpriteColorFade_FullKeys(...)  <-- default
//   * fade only, no color:             L2Fx_SpriteColorFade_Apply(...)
//
// !!! RGB vs BGR — THE #1 MISTAKE, READ CAREFULLY !!!
//   In the ENGINE MEMORY the draw byte (UParticle+0xA8, "runtimeColorA8") is
//   stored as BGRA: byte0=B, byte1=G, byte2=R, byte3=A. The particle field
//   ColorMultiplier (UParticle+0x84) is a float VECTOR laid out as RGB:
//     ColorMultiplier.X = RED, .Y = GREEN, .Z = BLUE.
//   The engine writes crosswise:
//     runtimeColorA8.B(byte0) = 255 * ColorMultiplier.Z (BLUE)
//     runtimeColorA8.G(byte1) = 255 * ColorMultiplier.Y (GREEN)
//     runtimeColorA8.R(byte2) = 255 * ColorMultiplier.X (RED)
//   IN THIS UNITY LIBRARY everything is float RGBA in the 0..1 range. There is
//   NO byte packing here, so DO NOT swap channels. Provide the multiplier range
//   as normal RGB:  _ColorMulMin/_ColorMulMax = (R, G, B).  Example e_u031_a:
//     _ColorMulMin = (0.5, 0.5, 0.8)   // R, G, B
//     _ColorMulMax = (0.7, 0.7, 1.0)   // R, G, B
//   The BGRA order matters ONLY when comparing against the C++ log
//   (runtimeColorA8 prints B,G,R,A). Unity color = float4(R,G,B,A) as usual.
//   Rule of thumb: engine byte = BGRA, engine multiplier vector = RGB (XYZ),
//                  this shader = RGB float4. Keep RGB here; never reorder.
//
// !!! TEXTURE sRGB — THE #2 MISTAKE (core vs soft gel / "lamp without aura") !!!
//   L2 Interlude (D3D9 fixed-function) multiplies atlas texels ~as authored bytes:
//       out = tex * vertexColor   (PTDS_Brighten: Blend One OneMinusSrcColor)
//   Unity URP samples sRGB textures into linear. Dim soft "gel" / plasma around a
//   bright core is crushed (~8–10× darker); only the hot core remains visible.
//   Raising _RgbBoost to ~8 is a false fix — it compensates gamma decode.
//
//   RULE: import L2 particle/effect atlases as Linear (Inspector: sRGB = OFF).
//     Path family: Assets/Resources/Data/SysTextures/LineageEffectsTextures/fx_m_t*
//     Also *_A variants when RGB carries color/plasma (not UI / character albedo).
//   Do NOT disable sRGB on UI icons, skins, or PBR albedo.
//
//   VERIFIED EXAMPLE (2026-07-18):
//     Effect:   it_healing_potion_ta / SpriteEmitter7 Name="kirakira"
//     Texture:  LineageEffectsTextures/fx_m_t0005.png
//               (atlas cells SubdivisionStart..End = 6..8: white core + blue gel)
//     ColorMul: (1.0, 0.553, 0.120) from .uc — formula OK; gel missing was sRGB.
//     Fix:      TextureImporter sRGBTexture = 0  →  gel + core visible at _RgbBoost=1.
//     Same atlas with sRGB=1: only the center "lamp", gel invisible without Boost≈8.
//
// !!! COLORSPACE Linear — THE #3 MISTAKE (peak washes white / "zebra") !!!
//   Even with atlas sRGB OFF, ColorMul/ColorScale midtones (e.g. G=0.553) are
//   still L2 display/gamma numbers. In Unity Linear they read too bright → peak
//   looks white while mid-life looks OK. Do NOT hack Fade or crush with _RgbBoost.
//   After this facade's FullKeys/WithScale/White, optionally apply:
//     #include "L2FxSpriteColorGammaLinear.hlsl"
//     color = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(color, _L2SpriteColorGammaToLinear);
//   Verified: it_healing_potion_ta SpriteEmitter7 kirakira; also MeshEmitter0 Wave
//   + MeshEmitter2 needlelight (mat toggle ON). FX + sRGB tex OFF —
//   see L2FxSpriteColorGammaLinear.hlsl for full contract.
//
// INPUTS ARE 1:1 WITH .uc: every uniform equals one SpriteEmitter field. Copy
// authored .uc values straight into the material; do not convert or rescale.
//
// WHAT THIS DOES NOT DO: mesh emitters (use L2FxMesh* helpers). Opacity is applied
// in ApplyOpacity like UpdateParticles: AlphaBlend→A, Brighten/0→RGB (smog live).
// ═══════════════════════════════════════════════════════════════════════════
//
// ─── HOW TO USE (быстрый старт) ──────────────────────────────────────────────
//
// PRINCIPLE: material uniforms map 1:1 to the .uc SpriteEmitter fields. Copy the
// authored .uc values straight into the material — no conversion in between.
//
// 1) Include order in your shader (after Core.hlsl):
//        #include "L2FxParticleAnim.hlsl"      // timing / random / normalizedAge
//        #include "L2FxEmitterSpawn.hlsl"      // color multiplier + subtractive fade
//        #include "L2FxSpriteColorFade.hlsl"   // this file (high-level facade)
//
// 2) Per material, expose these _uniforms (each = one field from the .uc):
//        float3 _ColorMulMin;   // ColorMultiplierRange min = (R,G,B) e.g. (0.5, 0.5, 0.8)
//        float3 _ColorMulMax;   // ColorMultiplierRange max = (R,G,B) e.g. (0.7, 0.7, 1.0)
//        float  _Lifetime;      // MaxLifetime seconds, e.g. 1.2
//        float  _HasLifetime;   // 1 if the emitter has a finite lifetime, else 0
//        float  _FadeIn;        // .uc FadeIn        (bool -> 0/1)
//        float  _FadeInEnd;     // .uc FadeInEndTime seconds  (e.g. 0.2)
//        float  _FadeOut;       // .uc FadeOut       (bool -> 0/1)
//        float  _FadeOutStart;  // .uc FadeOutStartTime seconds (e.g. 0.3)
//    (ColorScale for e_u031_a is white->white, so it is omitted here. If your
//     effect uses a real ColorScale gradient, sample it first — see NOTE below.)
//
// 3) In the fragment (or vertex) shader, compute the final vertex/particle color:
//
//        float age = L2Fx_AgeSeconds(_Time.y, startTime, initialDelay);
//        float4 col = L2Fx_SpriteColorFade_White(
//            _ColorMulMin, _ColorMulMax,   // random RGB range from .uc
//            age, _Lifetime, _HasLifetime,
//            _FadeIn,  _FadeInEnd,         // fade-in  (from .uc)
//            _FadeOut, _FadeOutStart,      // fade-out (from .uc)
//            seed, startTime);             // seed = per-particle _Seed
//
//        // 'col' is the finished draw color (linear 0..1, straight alpha).
//        // For the L2 additive pillar just output it and use additive blend:
//        //     Blend One One  (or SrcAlpha One)  in the pass.
//        return col * texColor;            // texture = fx_m_t0059 etc.
//
// ─── WHY IT LOOKS LIKE THIS (проверено на памяти движка) ─────────────────────
//   * ColorMultiplier is RGB (X=R, Y=G, Z=B). In the engine byte the channels
//     are stored BGRA, but in Unity we keep everything as float RGBA, so the
//     mapping is natural here (no crosswise swap needed in shader space).
//   * ColorMultiplier is randomized ONCE at spawn and never changes.
//   * Fade-out SUBTRACT verified live on e_u031_a (match=yes, ceil rounding).
//   * Fade-in SUBTRACT verified live on m_u004_b_2 (match=yes, ceil rounding):
//     fadeAmount = ceil(255 * (FadeInEndTime - age) / FadeInEndTime), age<FadeInEndTime.
//     Same subtractive mirror of fade-out, applied equally to R,G,B,A.
//   * e_u031_a specifically has FadeIn=OFF — set _FadeIn=0 for that effect.
//     Enable _FadeIn when .uc has FadeIn=true (now confirmed accurate).
//   * Opacity: UpdateParticles (+836 AlphaBlend). Modes 1/2/4 scale A only;
//     else (Brighten / AlphaBlend=0) scale RGB into runtimeColorA8. Live smog
//     Opacity=0.6: A8.rgb *= 0.6, A from Fade only — see ApplyOpacity.
//     e_u031_a Translucent may leave A=255 at spawn; draw-stage still applies.
//     If you need them, apply them at the material/blend stage, not here.
//
// ─── NOTE: effects WITH a real ColorScale gradient ───────────────────────────
//   e_u031_a uses white->white ColorScale, so color = ColorMultiplier only.
//   For effects with a colored ColorScale, sample it first with
//   L2Fx_SampleColorScale(...) (L2FxEmitterSpawn.hlsl), multiply by the random
//   ColorMultiplier, then feed the result to L2Fx_SpriteColorFade_WithScale(...).
//
// ─── FULL PIPELINE (one call = whole L2 sprite color logic) ──────────────────
//   L2Fx_SpriteColorFade_FullKeys(...) runs the complete engine order:
//       ColorScale gradient -> * ColorMultiplier -> FadeIn/Out -> Opacity
//   Every input maps 1:1 to a .uc SpriteEmitter field. Extra material uniforms:
//        int    _ColorScaleCount;  // number of ColorScale keys (1..4)
//        float  _ColorScaleParam;  // .uc ColorScaleRepeats (0 for e_u031_a)
//        float  _AlphaBlend;       // 1 = keep ColorScale alpha, 0 = force a=1
//        float4 _ColorKey0;        // ColorScale[0] color (relTime is implicitly 0)
//        float  _ColorKey1Time; float4 _ColorKey1;   // ColorScale[1] relTime + color
//        float  _ColorKey2Time; float4 _ColorKey2;   // ColorScale[2] (optional)
//        float  _ColorKey3Time; float4 _ColorKey3;   // ColorScale[3] (optional)
//        float  _Opacity;          // .uc Opacity
//        float  _OpacityRatio;     // .uc OpacityRatio
//   Example (colored ColorScale, full logic in one line):
//        float4 col = L2Fx_SpriteColorFade_FullKeys(
//            _ColorScaleCount, _ColorScaleParam, _AlphaBlend,
//            _ColorKey0, _ColorKey1Time, _ColorKey1,
//            _ColorKey2Time, _ColorKey2, _ColorKey3Time, _ColorKey3,
//            _ColorMulMin, _ColorMulMax,
//            age, _Lifetime, _HasLifetime,
//            _FadeIn, _FadeInEnd, _FadeOut, _FadeOutStart,
//            _Opacity, _OpacityRatio,
//            seed, startTime);
//        return col * texColor;   // additive blend
//   For e_u031_a: _ColorScaleCount=2, both keys white, _FadeIn=0, _FadeOut=1,
//                 _FadeOutStart=0.3, _Lifetime=1.2 (Opacity applied at draw only).
// ═══════════════════════════════════════════════════════════════════════════

// Per-particle random ColorMultiplier (RGB) as authored in ColorMultiplierRange.
// Returns the spawn color (alpha = 1). Deterministic per (seed, startTime).
float4 L2Fx_SpriteColorFade_SpawnColor(
    float3 colorMulMin,   // ColorMultiplierRange.min = (R,G,B)
    float3 colorMulMax,   // ColorMultiplierRange.max = (R,G,B)
    float seed,
    float startTime)
{
    // White ColorScale base (1,1,1) * random multiplier — matches e_u031_a.
    float3 rgb = L2Fx_ApplyColorMultiplier(
        float3(1.0, 1.0, 1.0),
        colorMulMin,
        colorMulMax,
        seed,
        startTime);
    return float4(rgb, 1.0);
}

// Subtractive fade-in AND fade-out.
// Brighten / additive (alphaBlend=0): subtract the same amount from RGB+A
// (fade-from-black). AlphaBlend: fade A only so RGB stays the authored milk
// color. BlendBetween steals vertex A for the frame lerp; if RGB also goes
// to black while texture a=1, a new puff is an opaque black quad.
float4 L2Fx_SpriteColorFade_Apply(
    float4 color,
    float ageSeconds,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStart,
    float alphaBlend)
{
    if (hasLifetime < 0.5)
    {
        return color;
    }

    float lt = max(1e-4, lifetime);

    // Fully dead outside [0, lifetime].
    if (ageSeconds <= 0.0 || ageSeconds >= lt)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float fadeAmount = 0.0;
    if (fadeIn >= 0.5 && fadeInEndTime > 0.0 && ageSeconds < fadeInEndTime)
    {
        fadeAmount += saturate((fadeInEndTime - ageSeconds) / max(1e-4, fadeInEndTime));
    }

    if (fadeOut >= 0.5)
    {
        float start = clamp(fadeOutStart, 0.0, lt);
        if (ageSeconds > start)
        {
            fadeAmount += saturate((ageSeconds - start) / max(1e-4, lt - start));
        }
    }

    fadeAmount = saturate(fadeAmount);
    if (alphaBlend >= 0.5)
    {
        color.a -= fadeAmount;
    }
    else
    {
        color -= fadeAmount;
    }

    return max(color, 0.0);
}

// One-call facade for white-ColorScale sprites (e_u031_a and siblings):
// random spawn color from ColorMultiplierRange + subtractive fade-in/out.
float4 L2Fx_SpriteColorFade_White(
    float3 colorMulMin,
    float3 colorMulMax,
    float ageSeconds,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStart,
    float seed,
    float startTime)
{
    float4 spawn = L2Fx_SpriteColorFade_SpawnColor(
        colorMulMin,
        colorMulMax,
        seed,
        startTime);

    return L2Fx_SpriteColorFade_Apply(
        spawn,
        ageSeconds,
        lifetime,
        hasLifetime,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStart,
        0.0);
}

// Variant for effects that DO use a colored ColorScale gradient. Pass the
// already-sampled gradient color (from L2Fx_SampleColorScale) as scaleColor;
// it is multiplied by the random ColorMultiplier, then faded.
float4 L2Fx_SpriteColorFade_WithScale(
    float4 scaleColor,
    float3 colorMulMin,
    float3 colorMulMax,
    float ageSeconds,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStart,
    float seed,
    float startTime,
    float alphaBlend)
{
    float3 rgb = L2Fx_ApplyColorMultiplier(
        scaleColor.rgb,
        colorMulMin,
        colorMulMax,
        seed,
        startTime);

    return L2Fx_SpriteColorFade_Apply(
        float4(rgb, scaleColor.a),
        ageSeconds,
        lifetime,
        hasLifetime,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStart,
        alphaBlend);
}

// ─── Opacity (UpdateParticles → runtimeColorA8) ──────────────────────────────
// LIVE (e_u505_c smog, PTDS_Brighten, Opacity=0.6): after ColorScale*Mul*Fade,
// UpdateParticles scales RGB by Opacity*OpacityRatio when emitter AlphaBlend
// byte (+836) is NOT in {1,2,4}. Alpha stays from fade only.
//   Tick3 age=0.0403: fade → A=85; RGB after *0.6 → A8=(17,24,7,85) PASS.
// AlphaBlend modes 1/2/4 (typical AlphaBlend draw): scale A only; RGB untouched.
// e_u031_a Translucent Opacity note (A stays 255 at spawn) still holds for
// AlphaBlend-style paths; Brighten smog bakes Opacity into A8.rgb.
float4 L2Fx_SpriteColorFade_ApplyOpacity(
    float4 color,
    float opacity,
    float opacityRatio,
    float alphaBlend)
{
    float o = saturate(opacity) * saturate(opacityRatio);
    if (o >= 0.9999)
    {
        return color;
    }

    // Hex-Rays MeshSpawning_* block: +836 in {1,2,4} → A; else → RGB.
    // Callers pass 0/1 toggles; treat >=0.5 as alpha-scaling path.
    if (alphaBlend >= 0.5)
    {
        color.a *= o;
    }
    else
    {
        color.rgb *= o;
    }

    return color;
}

// Build up to 4 ColorScale keys into the [8] arrays consumed by L2Fx_SampleColorScale.
// Key 0 relTime is implicitly 0; keys 1..3 carry their own relTime (from .uc RelativeTime).
void L2Fx_SpriteColorFade_BuildKeys(
    uint count,
    float4 c0,
    float t1, float4 c1,
    float t2, float4 c2,
    float t3, float4 c3,
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
    colors[0] = c0;
    if (count >= 2) { times[1] = t1; colors[1] = c1; }
    if (count >= 3) { times[2] = t2; colors[2] = c2; }
    if (count >= 4) { times[3] = t3; colors[3] = c3; }
}

// FULL PIPELINE (prebuilt arrays): ColorScale -> ColorMultiplier -> Fade -> Opacity.
// This is the complete UE3 USpriteEmitter draw-color logic in engine order.
float4 L2Fx_SpriteColorFade_Full(
    float normalizedAge,
    float colorScaleParam,
    uint colorScaleCount,
    float colorScaleTimes[8],
    float4 colorScaleColors[8],
    float alphaBlend,
    float3 colorMulMin,
    float3 colorMulMax,
    float ageSeconds,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStart,
    float opacity,
    float opacityRatio,
    float seed,
    float startTime)
{
    // 1. ColorScale gradient by normalized age (white for e_u031_a).
    float4 scaleCol = L2Fx_SampleColorScale(
        normalizedAge,
        colorScaleParam,
        colorScaleCount,
        colorScaleTimes,
        colorScaleColors,
        alphaBlend >= 0.5);

    // 2. * ColorMultiplier (random per particle)   3. FadeIn/Out (subtractive).
    float4 col = L2Fx_SpriteColorFade_WithScale(
        scaleCol,
        colorMulMin,
        colorMulMax,
        ageSeconds,
        lifetime,
        hasLifetime,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStart,
        seed,
        startTime,
        alphaBlend);

    // 4. Opacity (UpdateParticles): AlphaBlend→A, else→RGB (Brighten smog).
    return L2Fx_SpriteColorFade_ApplyOpacity(col, opacity, opacityRatio, alphaBlend);
}

// FULL PIPELINE (individual key uniforms): the recommended one-call entry point.
// Feed raw .uc SpriteEmitter values straight from material uniforms — no conversion.
float4 L2Fx_SpriteColorFade_FullKeys(
    uint colorScaleCount,
    float colorScaleParam,
    float alphaBlend,
    float4 colorKey0,
    float colorKey1Time, float4 colorKey1,
    float colorKey2Time, float4 colorKey2,
    float colorKey3Time, float4 colorKey3,
    float3 colorMulMin,
    float3 colorMulMax,
    float ageSeconds,
    float lifetime,
    float hasLifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOut,
    float fadeOutStart,
    float opacity,
    float opacityRatio,
    float seed,
    float startTime)
{
    float times[8];
    float4 colors[8];
    L2Fx_SpriteColorFade_BuildKeys(
        colorScaleCount,
        colorKey0,
        colorKey1Time, colorKey1,
        colorKey2Time, colorKey2,
        colorKey3Time, colorKey3,
        times,
        colors);

    float normalizedAge = saturate(ageSeconds / max(1e-4, lifetime));

    return L2Fx_SpriteColorFade_Full(
        normalizedAge,
        colorScaleParam,
        colorScaleCount,
        times,
        colors,
        alphaBlend,
        colorMulMin,
        colorMulMax,
        ageSeconds,
        lifetime,
        hasLifetime,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStart,
        opacity,
        opacityRatio,
        seed,
        startTime);
}

#endif // L2_FX_SPRITE_COLOR_FADE_INCLUDED
