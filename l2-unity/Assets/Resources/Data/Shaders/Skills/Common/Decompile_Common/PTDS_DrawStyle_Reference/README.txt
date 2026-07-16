PTDS / UParticleMaterial.ParticleBlending → D3D9 blend
=======================================================

Purpose
-------
Live-proven mapping from emitter DrawStyle (UC EParticleDrawStyle) through
UParticleMaterial+0x578 into FD3DRenderInterface stage Src/Dest blend.

Reject eyeballed helpers that claim PTDS_Brighten = SrcAlpha/One:
  ../../L2FxBrightenAlpha.hlsl
  ../../L2FxMeshBrightenD3d9.hlsl

Unity Pass Blend (write this in .shader Pass, not in HLSL)
----------------------------------------------------------
Enum (L2FxPTDS_DrawStyle.hlsl)              Unity
-----------------------------------------  --------------------------------
L2FX_PTDS_Regular (0)                      Blend One Zero
L2FX_PTDS_AlphaBlend (1)                   Blend SrcAlpha OneMinusSrcAlpha
L2FX_PTDS_Modulated (2)                    Blend DstColor SrcColor
L2FX_PTDS_Translucent (3)                  Blend One One
L2FX_PTDS_AlphaModulate_MightNotFog... (4) Blend One OneMinusSrcAlpha
L2FX_PTDS_Darken (5)                       Blend Zero OneMinusSrcColor
L2FX_PTDS_Brighten (6)                     Blend One OneMinusSrcColor

Client path
-----------
UMeshEmitter::RenderParticles (UseMeshBlendMode bit0 == 0):

  DrawStyle BYTE @ emitter+0x344
  -> dword store UParticleMaterial+0x578  (ParticleBlending)
  -> RI::SetMaterial(UParticleMaterial*)
  -> D3DDrv SetMaterial dispatcher Try F92AB40
  -> apply F90D4F0  (this file's switch)

UParticleMaterial native (after URenderedMaterial):

  +0x578  ParticleBlending / DrawStyle   DWORD 0..6
  +0x57C  BlendBetweenSubdivisions
  +0x580..  other ints / BitmapMaterial*
  +0x590 / +0x594  floats used later in F90D4F0
  +0x5AC  bit0
  +0x5B0  NumProjectors-like
  +0x5B4 + i*0x4C  Projectors[8]

Apply function (this session ASLR):

  D3DDrv unk_F90D4F0
  switch ([mat+578h]) cases 0..6
  writes stage object at [RI_device+538h]:

    stage+0x04  AlphaBlend enable (0/1)
    stage+0x24  D3DRS_SRCBLEND  (D3DBLEND_*)
    stage+0x28  D3DRS_DESTBLEND
    stage+0x2C  extra flag (often 1 with TFactor path)
    stage+0x30  TFactor FColor

D3DBLEND (D3D9)
---------------
  1 ZERO
  2 ONE
  3 SRCCOLOR
  4 INVSRCCOLOR
  5 SRCALPHA
  6 INVSRCALPHA
  9 DESTCOLOR

Switch table (F90D4F0) — proven
-------------------------------
At switch entry: esi=0, edi=1 (ZERO), edx=2 (ONE), ecx=6 (INVSRCALPHA).

Case  UC name (EParticleDrawStyle)     Src (+24)     Dest (+28)    +4  Notes
----  -------------------------------  ------------  ------------  --  -----
0     PTDS_Regular                     ONE (2)       ZERO (1)      0   opaque
1     PTDS_AlphaBlend                  SRCALPHA (5)  INVSRCALPHA(6) 1
2     PTDS_Modulated                   DESTCOLOR(9)  SRCCOLOR (3)  1   + TFactor gray 7F
3     PTDS_Translucent                 ONE (2)       ONE (2)       1   additive; +2C=1; TFactor 0
4     PTDS_AlphaModulate_...           ONE (2)       INVSRCALPHA(6) 1   +2C=1; TFactor 0
5     PTDS_Darken                      ZERO (1)      INVSRCCOLOR(4) 1   +2C=1; TFactor 0
6     PTDS_Brighten                    ONE (2)       INVSRCCOLOR(4) 1   +2C=1; TFactor 0

PTDS_Brighten (case 6) — Unity
------------------------------
  Blend One OneMinusSrcColor

GPU:
  out = src * ONE + dst * INVSRCCOLOR
      = src + dst * (1 - src)

Not SrcAlpha One. Not One One (that is Translucent / case 3).

Healing potion MeshEmitter needlelight/Wave:
  DrawStyle=PTDS_Brighten (6), UseMeshBlendMode=False
  live ParticleBlending @ proxy+578 == 6

Live proof session
------------------
  SetMaterial entry D3DDrv 0F928E70
  dispatcher      0F929120
  Try ParticleMat  0F92AB40  (EAX = UParticleMaterial*)
  Apply            0F90D4F0  switch on [mat+578h]
