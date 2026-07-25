PTDS_AlphaBlend — circular silhouette on square mesh
=====================================================

Purpose
-------
How a square MeshEmitter mesh (e.g. magiccircleblack02) appears as a round
disc in L2 / Unity when DrawStyle=PTDS_AlphaBlend.

This is NOT a UC "make round" flag. Roundness comes from texture alpha +
AlphaBlend. Documented from e_u031_a MeshEmitter0 "DarkMatter" (2026-07-20).

Related files
-------------
  ../L2FxPTDS_DrawStyle.hlsl
    enum L2FX_PTDS_AlphaBlend = 1
    Unity Pass: Blend SrcAlpha OneMinusSrcAlpha

  ../PTDS_DrawStyle_Reference/README.txt
    live D3DDrv Src/Dest table for all PTDS modes

  Effects wrapper (example):
    ../../Effects/it_teleport_v1_ca/MeshEmitter0_DarkMatter.shader

UC contract (DarkMatter)
------------------------
  DrawStyle=PTDS_AlphaBlend
  StaticMesh=magiccircleblack02   (geometry is a square/planar mesh)
  UniformSize=True, StartSize=(0.8,0.8,0.8)
  ColorScale white→white
  ColorMultiplierRange=(0.1, 0.001, 0.001)   // dark red, not pure black
  FadeIn / FadeOut / ForcedFade
  No Texture= in UC — mesh material texture comes from the StaticMesh package
  (Unity: assign the matching atlas, e.g. fx_m_t0054_A / SRGB_Disabled)

Why the mesh looks round
------------------------
1. Mesh vertices stay square (or rectangular plane).
2. Texture RGB may show a circle; the silhouette is in texture **alpha**:
     circle interior  alpha ≈ 1
     corners / outside alpha ≈ 0
3. Pass blend (from L2FxPTDS_DrawStyle):
     Blend SrcAlpha OneMinusSrcAlpha
   GPU:
     out.rgb = src.rgb * src.a + dst.rgb * (1 - src.a)
   Transparent corners disappear → round visual.

Live FF fragment (RenderDoc SPIR-V)
-----------------------------------
Core path (fog / alpha-test optional around it):

  out = sample(t0, uv) * textureFactor

No luma cutout. No geometric circle. Shape = tex.a under AlphaBlend.

Unity HLSL that matches
-----------------------
In the effect wrapper Pass:

  Blend SrcAlpha OneMinusSrcAlpha
  Cull Off          // RenderTwoSided=True
  ZWrite Off

Fragment (same as live FF):

  half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
  return tex * IN.color;   // IN.color = ColorScale * ColorMul * FadeIn/Out

Do NOT use for this path:
  - IgnoreMainTexAlpha (forces A=1 → full square fill)
  - AlphaFromLuma on a dark circle (DarkMatter ColorMul is dark red;
    luma≈0 inside and outside → broken / square / invisible)

Texture import checklist
------------------------
  Alpha Source present (not None)
  Alpha Is Transparency = on
  Preview Alpha channel: corners dark/transparent, disc bright
  If atlas is sRGB OFF (e.g. fx_m_t0054_SRGB_Disabled): enable
    _L2SpriteColorGammaToLinear on the material so ColorMul midtones
    match L2 in a Linear project (otherwise ColorMul 0.1 looks bright red)

Color note (not silhouette)
---------------------------
DarkMatter starts black then becomes dark red because of FadeIn (subtractive)
revealing ColorMultiplier (0.1, 0.001, 0.001). Live runtimeColorA8 ≈ BGRA
(0,0,25,255). ColorScale stays white — it does not cause the red tint.

Live slot identity (hook names may be wrong)
--------------------------------------------
Match by UC fields, not by logged MeshEmitterN name:

  colorMultiplier ≈ (0.1, 0.001, 0.001)
  finalSize ≈ (0.8, 0.8, 0.8)
  locLocal ≈ (0, 0, 0)
  spin = 0

What this folder is not
-----------------------
  Not a reusable HLSL include — blend line stays in the .shader Pass.
  Not a substitute for L2FxPTDS_DrawStyle.hlsl enum/table.
  Not mesh geometry conversion (no "round mesh" setting in UC).
