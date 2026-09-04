Decompile_Common / Essence
==========================

Не меняет проверенные формулы в родительской Decompile_Common.
Только недостающие куски для High Elf / Essence skill 1147
(d_mon_fire2_ca, u_mon_fire1_fl, d_mon_fire_ta).

ASM (приоритет High Elves)
---
Engine_essens_high_elves.dll.asm (~308 MB), 2026-09-03:
  Revolution     UpdateParticles loc_208E7A36
                 gate [emitter+0x1FC] bit4, center slot+0x54, RPS+0x60
                 RotateAngleAxis X,Y,Z * 65535 (dword_20DB5BD8)
  RevolutionScale UpdateParticles loc_208E8B2A
                 gate [emitter+0x1FC] bit5, keys+0x230, repeats+0x23C
                 16-byte {RelativeTime,FVector}; writes multiplier slot+0x6C
  VelocityScale  UpdateParticles loc_208E88EC
                 gate [emitter+0x418] bit0, keys+0x41C, repeats+0x428
                 writes slot+0x9C; Location += Velocity*multiplier*dt
  MaxAbsVelocity UpdateParticles loc_208E9A37, emitter+0x3D4 XYZ
                 per-axis clamp Velocity to [-MaxAbs,+MaxAbs], zero skips
  CoordinateSystem SpawnParticle switch loc_208EAB1E, byte emitter+0x110
                 0 Independent: spawn translation, no owner rotation/follow
                 4 Spray: owner rotation applied once to location/velocity/
                 acceleration/RPS; particle then remains world-space
                 IndependentSprayAccel = emitter+0x40 bit0:
                 acceleration/axis loss remain in world basis
                 (L2FxHE_CoordinateSystem.hlsl, Spray spawn matrix freeze)
  PTDU_Forward   FillVB UseDirectionAs @+0x50C == 3
                 loc_209D3CA9 Dir=Loc-Old; cases 3,4 loc_209D4374
  Atlas random   SpawnParticle loc_208EB70C
                 flags+0x358 bit1, Start+0x368 End+0x36C
                 trunc(frand*(End-Start)+Start) → slot+0xCC
  Polar shape    SpawnParticle loc_208EA217  +0x17C:
                 0=box 1=sphere 2=polar 3=all three
  ProjectionNormal  Initialize SafeNormal(+0x510) → +0x528
  VertMesh       UpdateParticles loc_20A3F100  mesh@+0x548 anim@+0x524
  Warmup         в HE не найден — оставлен UE2 loop

UC: Essens_LineaegEffect/Classes/*.uc
Include путь из шейдера эффекта:
  #include "Essence/L2FxHE_....hlsl"
  (из папки Decompile_Common)

Что уже есть в Common (не дублировать)
--------------------------------------
Box spawn          L2FxSpriteSpawnParticle / L2FxMeshSpawnParticle / L2FxStartLocationRange
Polar Cartesian    L2FxSpritePolar  (вызывать ТОЛЬКО если shape Polar)
Motion + VelLoss   L2FxSpriteMotion / L2FxMeshMotion
PTVD 1 и 2         L2FxPTVD_StartPositionAndOwner / L2FxPTVD_OwnerAndStartPosition
Spin / SpinCCW     L2FxSpriteSpin / L2FxMeshSpin
ColorScale+Fade    L2FxSpriteColorFade / L2FxMeshColorFade
SizeScale          L2FxSpriteSizeScale / L2FxMeshSizeScale
PTDS Darken/Bright L2FxPTDS_DrawStyle
PTDU_Up / Normal   L2FxPTDU_Up / L2FxPTDU_Normal
PTRS_Actor         L2FxPTRS_Actor
BlendBetween FF    L2FxD3d9FixedFunction

Что добавлено здесь
-------------------
L2FxHE_LocationShape.hlsl     HE Polar live=2 (UC=3) → ../L2FxSpritePolar
L2FxHE_AtlasSubdivision.hlsl  U/V, Start/End, Blend/Random; UV из ../../L2FxFlipbook
L2FxHE_Revolution.hlsl        UseRevolution орбита (формула НЕ live)
L2FxHE_PTDU_Forward.hlsl      smorke Forward (FillVB НЕ live; оси как PTDU_Up)
L2FxHE_Warmup.hlsl            Flame WarmupTicks/RelativeWarmup (НЕ live)
L2FxHE_VertMesh.hlsl          include Mesh spawn/spin/size/fade/motion; mesh=sh2
L2FxHE_ProjectionNormal.hlsl  UC ProjectionNormal UE→Unity для PTDU_Normal
L2FxHE_VectorScale.hlsl       Velocity/Revolution curves + MaxAbsVelocity
                              GPU age reconstruction integrates 16/32 midpoints
L2FxHE_CoordinateSystem.hlsl  PTCS native ids + IndependentSprayAccel world axes

Слои 1147
---------
d_mon_fire2_ca (5)
  Aura     box, ColorRepeats=100, SizeScale, 4x4 Blend, PTVD=1, SpinCCW
           polar в UC не активен (shape не Polar)
  Sprite   UseRevolution RPS.Z±0.3, Random End=3 1x1, Offset Z=2
  dipan10  Darken, accel Z, SizeScale, PTDU_Normal + ProjectionNormal (0,0,90)
  Main     Mesh guardnaiaCenter, SizeScale, Spin
  smorke   PTDU_Forward, Offset Z=15, 2x2 Random End=3
           RPS.Z=1 без UseRevolution → орбиту не включать

u_mon_fire1_fl (4)
  Core     Brighten, 4x4 Random 8..10, SpinCCW, SizeScale
  Flame    Brighten, polar в UC не активен, vel X=-100 * DrawScale
           Warmup 2 / 0.2, non-uniform size, Random End=2
  fireRoll Mesh, SizeScale keys но UseSizeScale=0
  CoreRound Brighten, Blend End=16 4x4

d_mon_fire_ta (6)
  black    Darken, VelLoss=5, PTVD=2, polar не активен (Box), SizeScale
  center   Brighten, Blend, Accel (10,10,10), SizeScale from 0
  sp       Mesh cross_poison, SPS Z=0.5, StartSpin ±1, PTVD=2
  dust     Polar shape=2, Polar Z=15 (не SphereRadius), UseRevolution, PTVD=2
  VertMesh sh2, size 0.12, life 0.7
  Cb       Brighten, Blend 2x4 End=3, VelLoss=5, ColorRepeats=17

DrawScale: CA ~0.583, FL ~0.680, TA 1.0.
Offset / times / IPS / spin / color / polar angles не масштабировать.
Spatial size / range / vel / accel / polar radius / maxAbsVel — да.

Не библиотеки (не писать сюда)
------------------------------
WeatherSoundCheck, bSunAffect, UseMeshBlendMode / RenderTwoSided,
FadeIn bool offset (времена уже в ColorFade).

L2EffectGenerator
-----------------
UC → материал unified Sprite/Mesh (не трогает L2FxFlipbook / L2FxCoreGeometryTest):
  UseRevolution / RPS / RevolutionCenterOffset
  UseRevolutionScale / RevolutionScale / Repeats (до 5 ключей в UC)
  UseVelocityScale / VelocityScale / Repeats (все найденные до 7 ключей)
  MaxAbsVelocity (UC default 10000 по каждой оси)
  CoordinateSystem Relative / Independent / Spray:
    GPU per-slot object matrix snapshot; GO fallback detaches and restores slots
    _CoordinateSystem native 0..7 on Sprite/Mesh materials (UC default 1)
    IndependentSprayAccel keeps Accel/VelocityLoss on world axes for Spray
    AddLocationFromOtherEmitter → AddLocationFromOtherEmitterProvider on prefab root
  PTDU_Forward → _OrientationMode=3
  PTLS_Polar → _FullTlsShape + _HeLocationShape=2
  PTLS_Sphere + SphereRadiusRange
  VertMeshEmitter + VertexMesh= → тот же MeshEmitter путь
  ProjectionNormal SafeNormal + UE→Unity (только PTDU_Normal)
ParticleGroupV2: Logs/ParticleGroupV2Compare.txt (формат HE EffectLog).
