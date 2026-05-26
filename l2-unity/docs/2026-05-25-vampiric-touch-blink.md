# Vampiric Touch — SpriteEmitter10 (VampireBlink)

**Дата:** 2026-05-25

Порт эффекта `VampireBlink` из UE (`m_u003_c.uc`, `SpriteEmitter10`) в Unity: шейдер, материал, префаб, общая библиотека HLSL.

---

## Цель

Воспроизвести blink Vampiric Touch (skill 1147): атлас `fx_m_t0006`, ячейка 4, `PTDS_Brighten`, три вертикальных quad, мягкое halo и пульс ColorScale.

---

## Ключевые файлы

| Файл | Назначение |
|------|------------|
| `Assets/Resources/Data/Shaders/Skills/Vampiric/Touch/VampiricTouchBlink.shader` | Шейдер blink |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/SpriteEmitter10.mat` | Материал |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/bl_vampiric_touch_ta.prefab` | ParticleGroup + 3 quad |
| `Assets/Scripts/Rendering/Effects/RandomizeChildYawOnEnable.cs` | Случайный поворот Y при spawn |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/m_u003_c.uc` | Референс UE |

### Общая библиотека (для будущих шейдеров)

| Файл | Содержание |
|------|------------|
| `Assets/Resources/Data/Shaders/Skills/Common/L2FxColorScaleSoft.hlsl` | Мягкий ColorScale-пульс |
| `Assets/Resources/Data/Shaders/Skills/Common/L2FxSpriteMultiSheet.hlsl` | PTDU_Up, несколько листов, маска в плоскости камеры |
| `Assets/Resources/Data/Shaders/Skills/Common/L2FxBrightenAlpha.hlsl` | Brighten: подъём alpha из RGB, halo fill |

Существующие рабочие шейдеры (Fury00, MeshEmitter и др.) **не менялись** — только blink переведён на includes.

---

## UE / RenderDoc (эталон)

- **FS:** `out = sample(t0) × in_Color0`, blend `SrcAlpha + One`
- **VS:** spin нет; три quad = разные `in_Position`, yaw на child (0° / 120° / 240°)
- **ColorScale:** 3 ключа, `ColorScaleRepeats=25` в uc (в Unity смягчили до ~0.625 pulse)
- **Subdivision:** uc `4→5`; в Unity временно **только cell 4** (`_SubdivisionStart/End=4`, blend off)
- **Pixel history (сердце):** Shader Out ~`(0.82, 0.79, 0.51)`, A≈1
- **Pixel history (halo):** тёмный тёплый RGB `(~0.2, ~0.06, ~0.08)`, A≈1 — не «пустота», а низкий RGB + накопление 3 pass
- **Mesh CSV (`sprite1.csv`):** три quad — **разные плоскости** в 3D (не одна billboard-плоскость)

---

## Геометрия (главное исправление)

**Было:** один цилиндрический billboard + поворот UV в плоскости → три quad компланарны.

**Стало:** `L2Fx_PtduUpMultiSheetPositionWS` — для каждого child свой лист: `camXZ` повёрнут на yaw child (0/120/240), `up = world Y`.

**Случайный угол:** `RandomizeChildYawOnEnable` на `SpriteEmitter10` — общий random Y (0–360°) + сохранённые базовые углы детей.

---

## Alpha / halo / текстура

- Проблема: `Alpha from Gray Scale` даёт низкую alpha у **тёмного** тёплого halo → при `SrcAlpha One` почти нет вклада.
- В оригинале вклад идёт от **RGB текстуры** и накопления 3 quad («дымка» = много полупрозрачных листов).
- Добавлены (через `L2FxBrightenAlpha.hlsl`): `_HaloInteriorFill`, `_HistoryRgbAlphaFill`, `_FaintRayFill`, `_HistoryRgbBoost` и др.
- Рекомендация по текстуре: **Input Texture Alpha** с нарисованным alpha в halo, не только Gray Scale на чёрном фоне.

---

## Обрезка лучей (круглая форма)

1. Сначала маска по UV quad — слабый эффект (лучи «вытянуты» поворотом плоскости).
2. Затем сфера в world space — горизонт обрезался раньше вертикали (разный `size X/Y`).
3. **Итог:** `L2Fx_SpriteViewSoftMask` — круг в плоскости **camera right/up**, радиус от `min(sizeX, sizeY) × _WorldMaskRadiusScale`.

Параметры в материале: `_RadialSoftMask`, `_WorldMaskRadiusScale`, `_RadialMaskSoftness`.

---

## Моргание (ColorScale)

| Параметр | Значение (финал) |
|----------|------------------|
| `_ColorScaleRepeats` | `0.625` |
| `_ColorScaleSmoothness` | `0.4` |
| Кривая | bright `1.0` → dim `0.65` @ 0.568 → bright `1.0` |

`L2Fx_ColorScaleRepeatsParam`: поддержка repeats &lt; 1 (медленный один «вдох»).

---

## Сравнение с оригиналом (~оценка)

- Композиция / форма: ~75%
- Цвет: ~70%
- Масса halo: ~55–60% (Unity чище, круглее; оригинал — пятнистая дымка от наложения листов)
- Итого: ~65–70%

---

## Параметры материала (ориентир)

- Subdiv: **4–4 only**
- Blink pulses: **0.625**
- World mask radius scale: **~0.48**
- History / halo fill: подстроены под импорт текстуры; при новой `fx_m_t0006_A` с нормальным alpha — уменьшать fill

---

## Не трогали

- `ParticleGroup.cs`
- `ParticleSingle.cs` (spawn-all-slots — откат)
- Другие шейдеры эффектов (кроме рефактора blink на common includes)

---

## Следующие шаги (опционально)

- Текстура с корректным alpha-каналом в halo
- Тонкая подстройка fill после замены текстуры
- Включение cell 5 (`SubdivisionEnd=5`, слабый mix) когда cell 4 стабилен
- Отдельно: `SpriteEmitter0` (VampireFlash), `MeshEmitter34`

---

## Home projectile (m_u003_b) — 2026-05-26

Порт «светлячков» Vampiric Touch: частицы вылетают с цели на `OnAnimationShoot`, летят дугой к кастеру, затем уничтожаются. Отдельный pipeline, не `ProjectileManager`.

### Цель (оригинал UE)

- Skill 1147: `APawn::SkillEffectShot`, projectile `m_u003_b` ×2, полёт `FNMover::Init` mode **USEPATH**
- Скорость ~450 UE → **~4.5–8.75** Unity m/s
- Дуга: вылет **вбок + вверх**, к середине пик, затем снижение к поясу кастера (не вертикальный «крючок» из головы в грудь)

### Архитектура Unity

| Файл | Назначение |
|------|------------|
| `Assets/Scripts/Rendering/Effects/HomeProjectileService.cs` | `HomeProjectileService`, `HomeProjectileMover` |
| `Assets/Scripts/Rendering/Effects/ParticleGroupHomeFlight.cs` | `ParticleGroupHomeFlightProfile`, `HomeProjectileFlightCoordinator` |
| `Assets/Scripts/Rendering/Effects/CompositeHomeProjectileLaunchHelper.cs` | Запуск на `OnAnimationShoot` |
| `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs` | `CompositeHomeProjectileConfig` на `CompositePrefabPart` |
| `Assets/Scripts/Effects/Core/EffectPart.cs` | `SetOwnerWorldPosOverride` для шейдеров |
| `Assets/Scripts/Effects/Core/Base/BaseEffect.cs` | `PrepareDestroyOnHomeArrival`, `DestroyHomeArrivalImmediate` |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_composite.prefab` | Part `el_vampiric_touch_ta`, `homeProjectile` |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/bl_vampiric_touch_ta.prefab` | 2 flight-группы: `SpriteEmitter10`, `SpriteEmitter10_1` |

### Composite / prefab

- `el_vampiric_touch_ta`: `spawnTiming: OnAnimationShoot`, `attachmentPoint: TargetCenter (8)`
- `homeProjectile`: `speed: 8.75`, `pathSideOffset: 1.25`, `pathHeightOffset: 0.44`, `homeAttachmentPoint: CasterLowerBody`, `mirrorDualFlight: 0`
- `bl_vampiric_touch_ta`: две `ParticleGroup` с `Home Flight Anchor` — множители `+1` и `-1` (зеркальные дуги)
- `overrideContinuousLoop: 1` на composite part; destroy отложен до прилёта (`PrepareDestroyOnHomeArrival`)

### ParticleGroup — переопределения полёта

На каждой группе (Inspector → **Home Projectile Flight**):

- `Home Flight Anchor` — отдельный `transform` летит своим `HomeProjectileMover`
- `Path Side Offset Multiplier` — `1` влево, `-1` зеркально вправо
- `Path Side / Height / Speed Scale` — масштаб параметров composite

`L2Particle.RefreshParticleGroups()` добавляет в play только runtime mirror-anchor, не все дочерние `EffectPart`.

### Траектория (итог)

- **Cubic Bezier** (3 control point): первый handle на линии монстр→кастер (spawn **внутри цели**), второй — бок + высота
- Бок: `-caster.right` × `pathSideOffset` × `pathSideOffsetMultiplier`
- Высота: `pathHeightOffset` + ~11.2% горизонтальной дистанции
- При `pathT >= 1` — завершение дуги, `BeginArrivalFade`

### Lifecycle / баги, которые чинили

1. **Distance по root, шейдер по PlayerEntity** — `_OwnerWorldPos` брал позицию игрока; mover мерил `transform` на точке спавна → fade не срабатывал. Fix: `SetOwnerWorldPosOverride` на летящей группе.
2. **Fade 0.35s оставлял respawn** — continuous loop + отцепленные группы. Fix: `DestroyHomeArrivalImmediate` на группе + coordinator ждёт оба mover.
3. **Третья частица** — дублирование anchor + `mirrorDualFlight` + лишние `EffectPart` в `ResetTimer`. Fix: только 2 prefab-группы, `mirrorDualFlight: 0`.
4. **Spawn сбоку от монстра** — боковой offset в единственном quadratic control. Fix: cubic, первый handle без lateral offset.
5. **`StopPart` у ParticleSingle** был закомментирован — восстановлен для остановки loop при arrival.

### Логи

Фильтр консоли: `[HOME_PROJECTILE]` — launch, update (`pathT`, `moveDelta`, `sideMul`), `BeginArrivalFade`, `DestroyHomeArrivalImmediate`.

### Параметры для подстройки (Inspector)

На `el_vampiric_touch_ta` → part → `homeProjectile` / на каждой `ParticleGroup`:

| Параметр | Смысл |
|----------|--------|
| `speed` | Скорость полёта |
| `pathSideOffset` | Ширина боковой дуги |
| `pathHeightOffset` | Доп. высота пика |
| `pathStartLineFactor` | Стартовый «вылет» вдоль хорды |
| `fadeStartDistance` | Дистанция до старта уничтожения (~0.5) |
| `homePathSideOffsetMultiplier` | Знак стороны (`1` / `-1`) на группе |

### Не трогали / опционально

- `MeshEmitter34`, `SpriteEmitter0` на цели (глаз/flash) — отдельные части composite, не home flight
- Точное совпадение 2× `m_u003_b` из ASM по таймингу — сейчас 2 группы в одном prefab
