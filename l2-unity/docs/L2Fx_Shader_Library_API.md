# L2Fx Shader Library API

Справочник публичных функций HLSL-библиотеки эффектов L2.  
**Для AI-агентов:** перед чтением целых `.shader` файлов проверь этот документ — переиспользуй готовые хелперы из `Assets/Resources/Data/Shaders/Skills/Common/`.

**Onboarding:** [L2_EFFECT_PORT_AI_PROMPT.md](L2_EFFECT_PORT_AI_PROMPT.md) — стартовый промпт, правило GUID в `.mat`, порядок чтения docs.

Связанные документы:
- [L2_Shader_Property_Catalog.md](metrics/L2_Shader_Property_Catalog.md) — свойства материалов по шейдерам
- [L2_UC_TO_UNITY_AI_GUIDE.md](metrics/L2_UC_TO_UNITY_AI_GUIDE.md) — перенос `.uc` → Unity

---

## Подключение

```hlsl
#include "../Common/L2FxEmitterSpawn.hlsl"   // базовый spawn + random
#include "../Common/L2FxMotionEase.hlsl"    // ease-in воронка (без зависимостей)
```

Типичный sprite-шейдер:
```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../Common/L2FxEmitterSpawn.hlsl"
#include "../Common/L2FxFlipbook.hlsl"
#include "../Common/L2FxMotionEase.hlsl"
#include "../Common/L2FxMeshParticleMotion.hlsl"
#include "../Common/L2FxSpriteEmitterVertex.hlsl"
#include "../Common/L2FxMeshFragment.hlsl"
```

**Путь:** `Assets/Resources/Data/Shaders/Skills/Common/`

---

## Константы и макросы

| Имя | Файл | Значение / смысл |
|-----|------|------------------|
| `L2FX_UU_TO_UNITY` | `L2FxEmitterSpawn.hlsl` | `0.01` — UE Unreal Units → Unity meters |
| `L2FX_SPIN_TO_RAD` | `L2FxEmitterSpawn.hlsl` | конверсия UE spin units → радианы |
| `L2Fx_DegToRad` | `L2FxEmitterSpawn.hlsl` | `π/180` |
| `L2Fx_TwoPi` | `L2FxParticleAnim.hlsl` | `2π` |

**Оси:** UE `(X,Y,Z)` → Unity `(X,Z,Y)` через `L2Fx_UeVectorToUnity`.

---

## L2FxParticleAnim.hlsl

Базовая анимация частиц: random, age, size, rotation, lifetime alpha.

| Функция | Описание |
|---------|----------|
| `L2Fx_Hash11(n)` | Детерминированный hash float→float для random. |
| `L2Fx_RandomRange(minMax, seed, startTime, salt)` | Случайное значение в `[min,max]` (стабильно per-particle). |
| `L2Fx_NormalizedAge(timeY, hasLifetime, startTime, delay, lifetime)` | `age/lifetime` с учётом delay и `_HasLifetime`. |
| `L2Fx_AgeSeconds(timeY, startTime, delay)` | Возраст частицы в секундах после delay. |
| `L2Fx_SizeScale(ageNorm, useSizeScale, scale0..2)` | Legacy 3-key SizeScale multiplier. |
| `L2Fx_StartSize(rangeX/Y/Z, uniformSize, seed, startTime)` | Стартовый размер (uniform или per-axis random). |
| `L2Fx_RotateX/Y/Z(p, angle)` | Поворот точки вокруг оси. |
| `L2Fx_RotationAngles(...)` | Случайные углы spin для mesh (3 оси). |
| `L2Fx_LifetimeAlpha(...)` | Alpha от FadeIn/FadeOut + lifetime (универсальная). |

---

## L2FxEmitterSpawn.hlsl

Spawn, velocity, acceleration, ColorScale/SizeScale, fade — порт UE3 SpriteEmitter.

| Функция | Описание |
|---------|----------|
| `L2Fx_RandomLifetime(minMax, seed, startTime, salt)` | Случайный lifetime per particle. |
| `L2Fx_RandomInitialDelay(minMax, seed, startTime, salt)` | Случайная задержка spawn. |
| `L2Fx_SpawnOffsetPolarDegrees(...)` | Polar spawn (углы в градусах, UE Z-up semantics). |
| `L2Fx_SpawnOffsetBox(rangeMin, rangeMax, seed, startTime)` | Box jitter ± range per axis. |
| `L2Fx_CombineSpawnOffsets(polar, box, offset)` | Сложение polar + box + offset. |
| `L2Fx_VelocityRandomBox(rangeX/Y/Z, seed, startTime)` | Случайная скорость по осям (UE). |
| `L2Fx_VelocityOutwardFromOwner(spawnWS, ownerWS, speed, seed, startTime)` | Скорость **от owner** наружу. |
| `L2Fx_VelocityTowardOwner(spawnWS, ownerWS, speed, seed, startTime)` | Скорость **к owner** (homing burst). |
| `L2Fx_VelocityAddTowardOwner(...)` | Добавка скорости вдоль spawn→owner. |
| `L2Fx_AccelerationRandom(min, max, seed, startTime)` | Случайное ускорение по осям. |
| `L2Fx_StartSize(...)` | Дубликат start size (spawn-focused API). |
| `L2Fx_StartSpin(spinRange, seed, startTime)` | Начальный spin (revolutions). |
| `L2Fx_SpinsPerSecond(spsRange, seed, startTime)` | Скорость вращения rev/s. |
| `L2Fx_SubImageIndex(subRange, cols, rows, seed, startTime)` | Индекс sub-image в атласе. |
| `L2Fx_SampleColorScale(ageNorm, param, count, times[], colors[], alphaBlend)` | UE ColorScale curve → float4 tint. |
| `L2Fx_SampleSizeScale(...)` | UE SizeScale curve → float3 multiplier. |
| `L2Fx_ApplyFadeInOut(age, fadeIn, fadeInEnd, fadeOut, fadeOutStart, lifetime)` | Fade multiplier [0,1] normalized. |
| `L2Fx_ApplyFadeInOutAbsolute(...)` | Fade по абсолютному времени (сек). |
| `L2Fx_DisplacementFromVelocity(velocity, age)` | `velocity × age`. |
| `L2Fx_ColorScaleTwoKeys(...)` | 2-key ColorScale shortcut. |
| `L2Fx_SizeScaleImplicitStartOneKey(...)` | 1-key SizeScale от 1.0 к end value. |
| `L2Fx_ApplyDamping(vel, damping, dt)` | Экспоненциальный damping скорости. |
| `L2Fx_ApplyVelocityLoss(vel, loss_UE, dt)` | UE VelocityLoss (linear subtract). |
| `L2Fx_ApplyColorMultiplier(...)` | Random RGB multiplier. |
| `L2Fx_MeshDeriveLifetime(fadeOutStart, forcedFade, explicitLifetime)` | Lifetime из FadeOutStartTime. |

CCW/CW spin: `L2Fx_ApplySpinCCWorCW_Scalar` / `L2Fx_ApplySpinCCWorCW_Vector` в `L2FxMeshParticleMotion.hlsl`.

---

## L2FxMotionEase.hlsl

Ease-in движение: медленный старт, разгон к цели. Используется в `MightTaSprite`.

| Функция | Описание |
|---------|----------|
| `L2Fx_EaseInPathProgress(ageNorm, easePower)` | Прогресс пути `pow(ageNorm, power)`; power>1 → медленный старт. |
| `L2Fx_EaseInSpeedFactor(ageNorm, easePower)` | Множитель скорости `d(progress)/d(ageNorm)`; 0 в начале. |
| `L2Fx_EaseInPathPosition(spawn, target, ageNorm, power)` | Eased lerp spawn→target. |
| `L2Fx_EaseInPathPosition(..., out pathProgress)` | То же + возвращает progress для arc/arrival. |
| `L2Fx_EaseInPathArcOffset(accel, age, pathProgress, arcScale)` | Дуга от ускорения; `(1-p)²` fade на концах. |
| `L2Fx_FocalArrivalClamp(target, pathProgress, threshold, stopDist, inout pos, out visibility)` | Стоп в цели; `visibility=0` для исчезновения. |

**Пример (воронка к focal):**
```hlsl
float pathProgress;
centerWS = L2Fx_EaseInPathPosition(spawnWS, focalWS, ageNorm, _FunnelEasePower, pathProgress);
centerWS += L2Fx_EaseInPathArcOffset(accWS, age, pathProgress, _FunnelArcScale);
L2Fx_FocalArrivalClamp(focalWS, pathProgress, 0.985, stopDist, centerWS, visibility);
```

---

## L2FxMeshParticleMotion.hlsl

Motion helpers для mesh/sprite: UE velocity modes, displacement, spin.

| Функция | Описание |
|---------|----------|
| `L2Fx_SpawnOffsetPolarYDegrees(...)` | Polar spawn Y-up (pitch от горизонта). |
| `L2Fx_VelocityOwnerAndStartPosition(...)` | PTVD_OwnerAndStartPosition — radial от owner через spawn offset. |
| `L2Fx_UeVectorToUnity(vUe)` | `(X,Y,Z)_UE → (X,Z,Y)_Unity`. |
| `L2Fx_VelocitySpawnThenProjectOnOwner(...)` | PTVD_StartPositionAndOwner — проекция vel на ось spawn−owner. |
| `L2Fx_DisplacementLinearVelocityLoss(v, a, loss, age)` | `v₀t + ½at² − ½loss·t²` (UE linear drag). |
| `L2Fx_DisplacementLinearHorizontalVelocityLoss(v, a, hLoss, age)` | Drag только XZ; Y с accel без loss. |
| `L2Fx_OutwardDirectionXZ(spawnOffset, fallbackAzimuth, seed, ...)` | Нормализованное направление наружу в XZ. |
| `L2Fx_VelocityFogSpreadHorizontal(...)` | PoisonCloud-style: burst XZ + sink Y. |
| `L2Fx_DisplacementFogFall(v, a, hLoss, age)` | Fog fall: horizontal drag + vertical gravity. |
| `L2Fx_DampedDisplacement(v, a, loss, age)` | Displacement с экспоненциальным drag. |
| `L2Fx_DisplacementConstantAccel(v, a, age)` | Классика `v₀t + ½at²`. |
| `L2Fx_ComputeSpinAngleRadians(...)` | Scalar spin angle для sprite mesh. |
| `L2Fx_ApplyMeshScalarSpin(inout pos, normal, useSpin, angle)` | Применить scalar spin к quad vertex. |
| `L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(...)` | Spin angle (rev/s units). |
| `L2Fx_ApplyMeshSpinAroundY(...)` | Spin вокруг local Y. |
| `L2Fx_RotatePositionAndNormal(...)` | Поворот pos + normal. |
| `L2Fx_ApplyMeshParticleSpin(...)` | Полный 3-axis mesh spin. |
| `L2Fx_ApplySpinCCWorCW_Vector(inout sps, ccwOrCw)` | CCW/CW для 3-axis spin. |
| `L2Fx_ApplySpinCCWorCW_Scalar(sps, ccwOrCwX)` | CCW/CW для scalar spin. |

---

## L2FxFlipbook.hlsl

Атлас / flipbook UV для sprite sheet.

| Функция | Описание |
|---------|----------|
| `L2Fx_FlipbookCellCount(uSub, vSub)` | Число кадров в атласе. |
| `L2Fx_FlipbookAtlasUV(uv, cellIndex, uSub, vSub)` | UV для ячейки атласа. |
| `L2Fx_FlipbookFrameIndex(ageNorm, subStart, subEnd)` | Индекс кадра по normalized age. |
| `L2Fx_FlipbookFrameFloat(...)` | Frame index как float (для blend). |
| `L2Fx_FlipbookBlendFrames(ageNorm, s0, s1, out fa, out fb, out blend)` | Два соседних кадра + blend weight. |
| `L2Fx_FlipbookAtlasUVBlend(...)` | UV blend между двумя кадрами. |
| `L2Fx_FlipbookSubDivisionRandomFrame(seed, startTime, s0, s1, salt)` | Random subdivision (UseRandomSubdivision). |
| `L2Fx_FlipbookSubDivisionAtlasUV(...)` | Atlas UV для subdivision mode. |
| `L2Fx_FlipbookSubDivisionUV_Random(...)` | Random sub-UV variant. |

---

## L2FxSpriteEmitterVertex.hlsl

Billboard, seed, camera-facing, ColorScale arrays.

| Функция | Описание |
|---------|----------|
| `L2Fx_SpriteMaterialSeed(globalSeed)` | Per-particle seed (`0` → use `_Seed`). |
| `L2Fx_ObjectWorldScale()` | Scale объекта из `unity_ObjectToWorld`. |
| `L2Fx_MotionCompensationForManualBillboardScale(manualScale)` | Компенсация motion при `_BillboardScale`. |
| `L2Fx_CameraHorizontalBasis(out rightH, out forwardH)` | Camera right/forward projected on XZ. |
| `L2Fx_CameraHorizontalUnitDirection(azimuthDeg)` | Unit vector в camera-horizontal plane. |
| `L2Fx_WorldVelocityToObject(velWS)` | Velocity WS → OS (для object-space integration). |
| `L2Fx_CameraBillboardPositionWS(centerWS, quadOS, billboardScale, zOffset)` | Camera-facing quad corner WS. |
| `L2Fx_ResolveParticleNormalWS(surfaceNormalMaterial)` | Normal для PTDU billboard. |
| `L2Fx_PtduNormalPositionWS(...)` | PTDU_Lit normal-aligned billboard position. |
| `L2Fx_BuildColorScaleArrays5(...)` | Pack до 5 ColorScale keys в arrays для `L2Fx_SampleColorScale`. |

---

## L2FxSpriteMultiSheet.hlsl

Multi-sheet / view-dependent sprite positioning.

| Функция | Описание |
|---------|----------|
| `L2Fx_SpriteYawRadiansFromObjectMatrix()` | Yaw объекта из matrix. |
| `L2Fx_SpriteCameraForwardXZ(centerWS)` | Camera forward на XZ plane. |
| `L2Fx_PtduUpMultiSheetPositionWS(...)` | PTDU_Up multi-sheet vertex WS. |
| `L2Fx_SpriteViewOffsetAndMaskRadius(...)` | View-space offset + mask radius для soft edge. |
| `L2Fx_SpriteViewSoftMask(viewOffset, maskRadius, edgeSoftness)` | Radial soft mask в view space. |

---

## L2FxSpawnRegionDebug.hlsl

Контракт spawn region (GPU + Editor wireframe). Editor: `Assets/Editor/L2FxSpriteSpawnRegionDebugDrawer.cs`.

| Функция | Описание |
|---------|----------|
| `L2Fx_SpawnRegionDebug_IsActive(debugSpawnRegion, startTime)` | Wireframe активен в edit mode. |
| `L2Fx_SpawnRegionPolarOffsetUe(theta, phi, radius)` | Одна polar-точка в UE space. |
| `L2Fx_SpawnRegionOffsetUe(...)` | **Полный spawn:** polar + box jitter + offset (как `.uc`). |

**Формула spawn:** `polar(θ,φ,r) + boxJitter(±range) + StartLocationOffset`

---

## L2FxAtlasDebug.hlsl

Edit-mode atlas preview. Editor sync: `Assets/Editor/L2FxAtlasPreviewSlotSeedSync.cs` (per-slot `_Seed`).

| Функция | Описание |
|---------|----------|
| `L2Fx_AtlasDebug_IsScenePreviewActive(debugAtlasPreview, startTime)` | Preview mode в Scene view. |
| `L2Fx_AtlasDebug_ResolvePreviewAge(loop, previewAge, timeY, lifetime)` | Age для preview scrubber. |
| `L2Fx_AtlasDebugPreviewColor(tex, mask, alpha, boost, bgColor)` | Debug preview fragment color. |

---

## L2FxMeshDebug.hlsl

Mesh preview timing / lifetime alpha в edit mode.

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshDebug_IsPreviewActive(debugMeshPreview, startTime)` | Mesh preview on. |
| `L2Fx_MeshDebug_ResolvePreviewAge(...)` | Preview age (sec). |
| `L2Fx_MeshDebug_ComputeTiming(...)` | Pack delay/lifetime/age/ageNorm для debug. |
| `L2Fx_MeshDebug_LifetimeAlphaAtAge(...)` | Lifetime alpha при фиксированном age. |
| `L2Fx_MeshDebug_LifetimeAlpha(...)` | Lifetime alpha в preview mode. |

---

## L2FxHold.hlsl

Hold/release для mesh эффектов (`_Hold` от `ParticleSingle`).

| Функция | Описание |
|---------|----------|
| `L2Fx_HoldCapSeconds(lifetime, hold)` | Hold cap в секундах. |
| `L2Fx_HoldReleaseT(hold, holdSizeReference)` | 0=full hold, 1=released. |
| `L2Fx_HoldMotionAge(elapsed, lifetime, hold)` | Motion age с cap на hold. |
| `L2Fx_HoldMotionAgeStable(...)` | Motion age с плавным release. |
| `L2Fx_HoldSpinAge(elapsed)` | Spin age (без hold cap). |
| `L2Fx_HoldLoopAgeNorm(...)` | ColorScale loop age после hold. |
| `L2Fx_HoldSizeAgeNorm(...)` | SizeScale age: freeze on hold, resume on release. |

---

## L2FxMeshEmitterVertex.hlsl

URP mesh emitter vertex pipeline (builtin path).

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshBuiltin_ComputeTiming(...)` | delay, lifetime, age, ageNorm. |
| `L2Fx_MeshBuiltin_ColorScaleRepeatsParam(repeats)` | UE ColorScaleRepeats → shader param. |
| `L2Fx_MeshBuiltin_BuildColorScaleArrays(...)` | Build ColorScale arrays. |
| `L2Fx_MeshBuiltin_BuildSizeScaleArrays(...)` | Build SizeScale arrays. |
| `L2Fx_MeshBuiltin_SampleSizeScaleScalar(...)` | Sample SizeScale → scalar multiplier. |
| `L2Fx_MeshBuiltin_StartLocationOffsetM(offsetUU)` | StartLocationOffset UU → meters. |
| `L2Fx_MeshBuiltin_TransformVertexOS(...)` | Full mesh vertex transform (size/spin/uv). |
| `L2Fx_MeshBuiltin_TransformVertexOS_SplitAge(...)` | Split motion/spin age (hold support). |
| `L2Fx_MeshBuiltin_ApplyMainTexST(uv, st)` | `_MainTex_ST` transform. |
| `L2Fx_MeshBuiltin_ApplyAtlasRemap(uv, remap, minMax)` | Atlas UV remap. |
| `L2Fx_MeshBuiltin_ResolveUv(...)` | UV path: planar / atlas / default. |
| `L2Fx_MeshBuiltin_SampleBaseTint(...)` | Base tint from ColorScale + fade. |

---

## L2FxMeshEmitter.hlsl

Legacy mesh emitter color.

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshInitAxes(...)` | Initialize mesh emitter axes. |
| `L2Fx_MeshComputeColor(...)` | ColorScale color (normalized age). |
| `L2Fx_MeshComputeColorAbsolute(...)` | ColorScale color (absolute time). |

---

## L2FxMeshEmitterUrp.hlsl

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshUrp_ObjectToHClip(posOS, clipDepthBias)` | OS → HClip с depth bias. |

---

## L2FxMeshFragment.hlsl

Fragment helpers: alpha, masks, tint, ground shadow.

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshFrag_AlphaFeather(texAlpha, feather)` | Soft alpha edge feather. |
| `L2Fx_QuadEdgeSoftMask(uv01, edgeSoftness)` | Soft mask по краям quad UV. |
| `L2Fx_QuadEdgeSoftMaskSelective(uv01, edgeSoftnessXY)` | Per-axis edge soft mask. |
| `L2Fx_RadialUvSoftMask(uv01, edgeSoftness)` | Radial soft mask от центра UV. |
| `L2Fx_AlphaDilatedSample(tex, sampler, uv, texelSize, dilateTexels)` | Max-filter dilate для alpha. |
| `L2Fx_MeshFrag_SampleTextureAlpha(...)` | Alpha: texture / luma / ignore alpha. |
| `L2Fx_MeshFrag_SampleTextureAlphaSoft(...)` | Soft luma alpha (dim plasma fill). |
| `L2Fx_MeshFrag_SpriteTintRgb(texRgb, tintRgb)` | RGB tint multiply. |
| `L2Fx_MeshFrag_ApplyAlphaPowerStrength(mask, power, strength)` | `pow(mask,power)×strength`. |
| `L2Fx_MeshFrag_ApplyGroundShadow(inout rgb, mask, ...)` | Ground shadow darkening. |
| `L2Fx_MeshFrag_MagicCircleLumaUvSplit(...)` | Magic circle ribbon/luma UV split. |
| `L2Fx_MeshFrag_DarkenMinSource(rgb, mask)` | Darken blend min(source, dest). |

---

## L2FxMeshBrightenD3d9.hlsl

PTDS_Brighten D3D9-style mesh brighten.

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshBrighten_SoftTailWeight(...)` | Weight для soft tail brighten. |
| `L2Fx_MeshBrighten_TexHueTint(texRgb)` | Hue tint для brighten tex. |
| `L2Fx_MeshBrighten_D3d9TexFactor(...)` | D3D9 TFactor-style brighten combine. |

---

## L2FxBrightenAlpha.hlsl

PTDS_Brighten (Blend `SrcAlpha One`) alpha rebuild.

| Функция / тип | Описание |
|---------------|----------|
| `L2Fx_BrightenAlphaTuning` | Struct tuning params (halo, rays, history). |
| `L2Fx_BrightenAlphaRaw(tex, alphaFromLuma, floor, ignoreAlpha)` | Raw alpha до brighten logic. |
| `L2Fx_BrightenApplyTextureContribution(...)` | RGB + alpha blend weight для SrcAlpha One. |
| `L2Fx_BrightenFinalize(rgb, alphaBlend, opacity, emitterAlpha)` | Final brighten half4. |

---

## L2FxColorScaleSoft.hlsl

| Функция | Описание |
|---------|----------|
| `L2Fx_SampleColorScaleSoft(...)` | ColorScale с soft interpolation между keys. |
| `L2Fx_ColorScaleRepeatsParam(repeats)` | UE repeats → shader param. |

---

## L2FxMeshLifetimeAlpha.hlsl

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshLifetimeAlphaHold(...)` | Lifetime alpha с hold cap. |

---

## L2FxMeshLifetimeScale.hlsl

Lifetime-driven mesh scale curves.

| Функция | Описание |
|---------|----------|
| `L2Fx_MeshLifetimeScalePathT(...)` | Path parameter t для scale curve. |
| `L2Fx_MeshLifetimeScaleCurve5(...)` | 5-key scale curve sample. |
| `L2Fx_MeshLifetimePostBurstScale(...)` | Post-burst scale multiplier. |
| `L2Fx_MeshLifetimeScaleMultiplier(...)` | Combined lifetime scale multiplier. |

---

## Типичные паттерны

### Spawn region (как `.uc`)
```hlsl
float3 posUe = L2Fx_SpawnRegionOffsetUe(
    _PolarAzimuthDeg.xy, _PolarPitchDeg.xy, _PolarRadius.xy,
    _StartLocationOffset.xyz,
    float3(_StartLocationRangeX.x, _StartLocationRangeY.x, _StartLocationRangeZ.x),
    float3(_StartLocationRangeX.y, _StartLocationRangeY.y, _StartLocationRangeZ.y),
    pSeed, _StartTime);
float3 spawnOfs = L2Fx_UeVectorToUnity(posUe);
```

### PTVD_StartPositionAndOwner (Kirakira-style)
```hlsl
float3 spawnWS = TransformObjectToWorld(spawnOfs);
float3 dirWS = normalize(spawnWS - _OwnerWorldPos.xyz);
float3 vel = dirWS * speed * _SpawnUnitScale;
float3 disp = L2Fx_DisplacementLinearVelocityLoss(vel, acc, float3(0,0,0), age);
```

### Per-particle seed в edit mode
C# `L2FxAtlasPreviewSlotSeedSync` пишет `_Seed = (slot+1)*17.31` через MaterialPropertyBlock когда `_DebugAtlasPreview=1` и `_StartTime=0`.

---

## Обновление документа

При добавлении нового `L2Fx_*.hlsl` или публичной функции — дополни соответствующую секцию в этом файле.

_Последнее обновление: 2026-06-18 (добавлен L2FxMotionEase)._
