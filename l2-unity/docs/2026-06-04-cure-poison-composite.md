# Cure Poison (wh_cure_poison) — сессия 4 июня 2026

**Скилл:** Cure Poison (в `GlobalEffect` / skill **1012**).  
**Папка ассетов:** `Assets/Resources/Data/Effects/cure_posion/` (опечатка `posion` в имени папки сохранена).  
**Эталон UE:** `wh_cure_poison_ta.uc`.

---

## Состав composite

Файл: `Assets/Resources/Data/Effects/cure_posion/wh_сure_posion_composite.prefab`  
(в имени prefab кириллическая «с» в `сure` — не латинская `c`.)

| Part | Prefab | Attachment | Spawn | Follow | Примечание |
|------|--------|------------|-------|--------|------------|
| `wh_cure_poison_ta` | `bl_cure_poison_ta` | `TargetLowerBody` (4) | `OnAnimationShoot` (5) | да | полёт home + попадание на цель |
| `wh_cure_poison_ta_head` | `bl_cure_poison_ta_head` | `TargetOverHead` (9) | `OnAnimationShoot` (5) | да | кольца `windAround` над головой |
| `wh_cure_poison_ca` | **`wh_heal_ca`** (тот же GUID) | `CasterPosition` (6) | `Immediate` (0) | **нет** | низ каста; не `CasterRoot` + follow |

**Важно:** `wh_cure_poison_ca` — это префаб heal’а (`wh_heal_ca`), не отдельный cure_ca. Настройки `ca` в composite **как в** `wh_heal_composite.prefab`.

---

## Эмиттеры `bl_cure_poison_ta` / `bl_cure_poison_ta_head`

| Группа UE | Unity | Шейдер / скрипт |
|-----------|-------|-----------------|
| `SpriteEmitter8` BlueDust | `SpriteEmitter8` (часто выкл.) | placeholder |
| `MeshEmitter0` LightSplash | `lightSplash` / `MeshEmitter0` | **`CurePoisonLightSplash.shader`** + `MeshEmitter0.mat` |
| `MeshEmitter1` windAround | `windAround` → 3× `MeshEmitter1` | **`PoisonWave.shader`** + `MeshEmitter1.mat` |

### LightSplash

- Отдельный шейдер: `Assets/Resources/Data/Shaders/Skills/Cure/CurePoisonLightSplash.shader`
- Меш `lightcone00`, additive `One One`, атлас `fx_m_t0005`
- SizeScale / ColorScale по `.uc` MeshEmitter0

### windAround (кольца)

- Шейдер: **`L2/Effects/PoisonWave`** — **общий с curse** (`bl_curse_poison_ta/MeshEmitter0.mat`)
- **Не менять default Properties в `PoisonWave.shader`** — только `MeshEmitter1.mat`
- Материал: белый ColorScale, opacity 0.4, `StartLocationOffset Z=20` UU
- SizeScale в mat — пока кривая curse/ice (4.55…), тюнинг расширения — вручную в mat

### Корневой scale префаба

- Root `bl_cure_poison_ta_head`: **`localScale = 0.05`** (как у curse)
- `windAround`: **`localScale = 1.4`**

---

## `EffectPrefabStaggeredDrift` (падение колец)

Файл: `Assets/Scripts/Effects/Core/PrefabHelpers/EffectPrefabStaggeredDrift.cs`  
Висит на **`windAround`** в `bl_cure_poison_ta_head`.

- Двигает **direct children** (`MeshEmitter1`, `(1)`, `(2)`)
- Скорость в **метрах мира**; `InverseTransformVector` учитывает scale префаба
- **`Include Inactive Children = true`** — иначе после `PlayPart()` слоты выкл. и `Restart()` обнуляет targets
- Отложенный `Restart()` через 2 кадра (после composite attach / `PlayPart`)

---

## Последовательный спавн 3 колец

На `windAround` → `ParticleGroup`:

| Параметр | Значение |
|----------|----------|
| `_isBurstSpawning` | **0** |
| `_maxCount` | 3 |
| `_countPerSecond` | **10** (~0.1 с между кольцами) |
| `_staggerDelaySeconds` (drift) | **0.1** |

Порядок слотов = порядок в `_particles` / дети в иерархии.

---

## Баги composite / attachment (исправлено 4 июня)

### 1) `ta_head` на ~5 м при включённом `ca`

**Причина:** `wh_cure_poison_ca` был с `CasterRoot` + `followResolvedTransform = true`.  
VFX `wh_heal_ca` становился **дочерним** персонажа → его renderer’ы попадали в `GetComponentsInChildren<Renderer>` при расчёте **`TargetOverHead`** → `bounds.max.y` раздувался.

**Fix (composite):** как `wh_heal_composite`:

- `attachmentPoint = CasterPosition` (6)
- `followResolvedTransform = false`

### 2) `ta_head` сбоку от оси персонажа

**Причина:** `TargetOverHead` ставил **X/Z из `bounds.center`** (оружие, плащ смещают AABB).

**Fix (код):** `DefaultEffectAttachmentResolver.cs`:

- для `TargetOverHead`: **Y** из bounds персонажа, **X/Z** из **CharacterController.center** или **root.position**
- в bounds **не учитываются** renderer’ы под `BaseEffect` (дочерние VFX)
- при наличии `CharacterController` — полная точка над головой из капсулы (приоритетнее bounds)

---

## Ось вращения windAround

- `PoisonWave` крутит через `L2Fx_ApplyMeshSpinAroundY` (yaw Unity) — как у других эффектов на этом шейдере
- В mat: `_SpinsPerSecond = 1.2`, `_UniformSize = 1`, `_StartSpinRange (0,1)`
- **Не** подставлять UE `SpinsPerSecond=2` без проверки — оси UE ≠ Unity
- Отдельный шейдер под windAround **не добавляли** (по запросу)

---

## Что ещё не сделано / WIP

- [ ] Отдельный prefab `wh_cure_poison_ca` (сейчас reuse `wh_heal_ca` / тот же GUID)
- [ ] SizeScale windAround по `.uc` (2→3.3) только в `MeshEmitter1.mat` — тюнинг вручную
- [ ] `StartVelocity` / `Acceleration` windAround в шейдере (сейчас частично drift)
- [ ] `SpriteEmitter8` BlueDust

---

## Файлы, тронутые в сессии

```
Assets/Resources/Data/Effects/cure_posion/
  wh_сure_posion_composite.prefab
  wh_cure_poison_ta/
    bl_cure_poison_ta_head.prefab
    MeshEmitter0.mat
    MeshEmitter1.mat

Assets/Resources/Data/Shaders/Skills/Cure/
  CurePoisonLightSplash.shader (+ .meta)

Assets/Scripts/Effects/Core/PrefabHelpers/
  EffectPrefabStaggeredDrift.cs (+ .meta)

Assets/Scripts/Game/Manager/EffectManager/Composite/
  DefaultEffectAttachmentResolver.cs
```

**Не трогать без необходимости:** `Assets/Resources/Data/Shaders/Skills/Curse/Poison/PoisonWave.shader` (default properties).

---

## См. также

- [effects-composite-prefab-mechanic.md](./effects-composite-prefab-mechanic.md) — общая механика composite
- [wh-heal-ta-beam-notes-may-2026.md](./wh-heal-ta-beam-notes-may-2026.md) — паттерн `wh_heal_ca` в composite
