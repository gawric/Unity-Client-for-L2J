Lineage 2 Interlude BeamEmitter — verified reference port
========================================================

Статус
------
Проверено на оригинальном клиенте Lineage 2 Interlude:

  - Engine.dll x86: UBeamEmitter::SpawnParticle / UpdateParticles;
  - live-эффект: LineageEffect.wh_heal_ta, слой BeamEmitter0;
  - дата live-проверки: 2026-08-23;
  - геометрия, время жизни и runtimeColorA8 снимались из памяти клиента.

Это изолированный reference-модуль. Он пока НЕ подключён к загрузчику эффектов,
EffectPart, ParticleGroup или конкретным скилам.


Файлы
-----
../L2FxBeamSegment.hlsl
  PTEP_Offset, ширина StartSizeRange.X и camera-facing разворот полосы.

../L2FxBeamColor.hlsl
  Проверенный путь цвета BeamEmitter:
  ColorScale -> ColorMultiplier -> FadeIn/FadeOut -> Opacity.

../L2BeamStrip.shader
  Общий URP strip shader с reference-параметрами wh_heal_ta. Его можно
  назначить материалу вручную, но production skill assets его не используют.

Assets/Scripts/Rendering/Effects/L2BeamEmitterStripBuilder.cs
  Создаёт unit strip mesh. Скрипт находится рядом с L2RibbonEmitter.cs.


Live-результаты wh_heal_ta
-------------------------
Runtime клиент отличается от authored UC dump. Для точного совпадения с
запущенным Interlude использовались именно runtime-значения:

  DetermineEndPointBy       = PTEP_Offset (2)
  LowFrequencyPoints       = 3
  HighFrequencyPoints      = 10
  BeamEndPoints[0].Offset  = (0, 0, -190) UU
  StartLocationOffset      = (0, 0, 150) UU
  StartLocationRange       = X/Y -9.167..9.167, Z -25..25 UU
  StartSizeRange.X         = 5..8.333 UU
  Lifetime                 = 2..3 seconds
  FadeInEndTime            = 0.5 seconds
  FadeOutStartTime         = 1.0 seconds
  ColorScaleRepeats        = 10
  Opacity / OpacityRatio   = 0.33 / 1.0
  DrawStyle                = PTDS_Translucent = Blend One One
  MaxParticles             = 20
  observed spawn interval  = approximately 0.2 seconds (approximately 5 PPS)

HF geometry:

  HF[i].t   = i / (HighFrequencyPoints - 1)
  HF[i].Loc = lerp(start, end, HF[i].t)

LF geometry:

  LF = start, midpoint, end

При BeamNoiseRange=0 точки и Location не двигаются после spawn.


Как создать mesh
----------------
Пример будущей интеграции:

  Mesh beamMesh = L2BeamEmitterStripBuilder.Build(
      highFrequencyPoints: 10,
      beamTextureUScale: 1f,
      beamTextureVScale: 1f);

  meshFilter.sharedMesh = beamMesh;

Для authored UC можно передать HighFrequencyPoints из файла. Для визуального
совпадения с проверенным runtime wh_heal_ta следует передать 10.

Контракт вершин:

  position.z = 0..1 вдоль луча;
  position.x = -0.5/+0.5 поперёк полосы;
  UV.x       = координата вдоль луча;
  UV.y       = 0/1 по краям.

Шейдер сам вычисляет start/end, ширину и camera-facing направление.


Как подключить HLSL позже
-------------------------
В ShaderLab/HLSL pass:

  #include "Decompile_Common/L2FxBeamSegment.hlsl"
  #include "Decompile_Common/L2FxBeamColor.hlsl"

Геометрия:

  float3 endUe = L2Fx_Beam_PtepOffsetEndUe(
      startUe, offsetX, offsetY, offsetZ, spawnState);

  float widthMeters =
      L2Fx_Beam_HalfWidthMeters(sizeUU, worldCalibration) * 2.0;

  float3 positionWS = L2Fx_Beam_BillboardPointWS(
      startWS, endWS, along, across, widthMeters, cameraWS);

Цвет:

  float4 runtimeColor = L2Fx_Beam_RuntimeColorKeys(...);

Для проверенного PTDS_Translucent:

  Blend One One

После L2Fx_Beam_RuntimeColorKeys НЕ применять
L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled.

Live доказательство для slot 0:

  age=0.2899, lifetime=2.5396
  ColorMultiplier=(0.6356,0.6409,0.6362)
  runtimeColorA8 BGRA=(15,15,15,147)

Raw HLSL formula даёт эти значения. Дополнительный gamma/pow превращает
15/255 примерно в 1/255 и вызывает визуальное резкое мигание.


Зависимости
-----------
Уже существующие файлы:

  L2FxAppRand.hlsl
  L2FxSpriteColorFade.hlsl
  L2FxSpritePolar.hlsl
  L2FxStartLocationRange.hlsl
  ../L2FxEmitterSpawn.hlsl
  ../L2FxCoreGeometryTest.hlsl
  URP Core.hlsl

Для beam-текстуры рекомендуется TextureWrapMode.Repeat.
Cull Off, ZWrite Off, ZTest LEqual соответствуют reference shader.


Что пока не реализовано / не подтверждено
-----------------------------------------
  - BeamNoiseRange и LF/HF noise;
  - branching;
  - PTEP_Actor;
  - PTEP_TraceOffset;
  - PTEP_Accumulative;
  - RotatingSheets / несколько camera-facing sheets;
  - collision/trace endpoint;
  - production lifecycle и подключение к ParticleGroup;
  - автоматический парсинг BeamEndPoints из UC в рабочем проекте.

PTEP_Distance и PTEP_OffsetAsAbsolute присутствуют в reference shader только
как подготовленные MVP-ветки; live-проверка wh_heal_ta покрывает PTEP_Offset.


Интеграционный порядок (на будущее)
-----------------------------------
1. Добавить BeamEmitter в parser/model EffectSettings.
2. Создать один mesh через L2BeamEmitterStripBuilder и переиспользовать его.
3. На каждый particle slot передавать отдельные _StartTime и appRand state.
4. Настроить material properties из UC/runtime.
5. Для PTDS_Translucent оставить raw Beam runtime color и Blend One One.
6. Уничтожать slot после его собственного MaxLifetime.
7. Только после A/B проверки подключить component к skill prefab/loader.
