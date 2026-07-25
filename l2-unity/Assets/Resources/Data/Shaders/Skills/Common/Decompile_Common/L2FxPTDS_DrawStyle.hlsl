#ifndef L2_FX_PTDS_DRAWSTYLE_INCLUDED
#define L2_FX_PTDS_DRAWSTYLE_INCLUDED

// Live-proven: D3DDrv F90D4F0 switch on UParticleMaterial+0x578 (ParticleBlending).
// See PTDS_DrawStyle_Reference/README.txt
//
// Reject ../../L2FxBrightenAlpha.hlsl and ../../L2FxMeshBrightenD3d9.hlsl
// (they assumed SrcAlpha/One for Brighten — wrong).

#define L2FX_PTDS_Regular                                  0
#define L2FX_PTDS_AlphaBlend                               1
#define L2FX_PTDS_Modulated                                2
#define L2FX_PTDS_Translucent                              3
#define L2FX_PTDS_AlphaModulate_MightNotFogCorrectly       4
#define L2FX_PTDS_Darken                                   5
#define L2FX_PTDS_Brighten                                 6

// Unity Pass Blend lines (Dest = OneMinusSrcColor == D3DBLEND_INVSRCCOLOR):
//
//   Regular:     Blend One Zero
//   AlphaBlend:  Blend SrcAlpha OneMinusSrcAlpha
//   Modulated:   Blend DstColor SrcColor
//   Translucent: Blend One One
//   AlphaMod:    Blend One OneMinusSrcAlpha
//   Darken:      Blend Zero OneMinusSrcColor
//   Brighten:    Blend One OneMinusSrcColor
//
// Opacity + Brighten (smog / shot_N_atk SpriteEmitter325, live e_u505_c):
//   Blend ignores vertex A for coverage. UpdateParticles with AlphaBlend=0
//   multiplies runtimeColorA8.RGB by Opacity (not A).
//   In L2Fx_SpriteColorFade_FullKeys pass alphaBlend=0 so ApplyOpacity does
//   color.rgb *= Opacity. Do NOT use alphaBlend=1 (A-only) with this blend —
//   smoke stays too dense. See L2FxSpriteColorFade.hlsl ApplyOpacity.

#endif
