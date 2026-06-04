# wh_heal_ta: beam + dust — заметки по работе

**Дата последнего обновления:** 8 мая 2026

## Контекст

`wh_heal_ta` — вторая часть heal-эффекта: пыль (`SpriteEmitter1`) и падающие лучи (`BeamEmitter0`) по конфигу Interlude (`BeamEndPoints`, `DetermineEndPointBy=PTEP_Offset`, `LowFrequencyPoints`/`HighFrequencyPoints=2`, текстура `fx_m_t0004` и т.д.). Цель — приблизить вид к оригиналу (мягкие вертикальные полосы, тёплое свечение у ног, без жёстких стыков и «серой стены»).

## Что сделано

### Shader `L2WhHealTA` (`Assets/Resources/Data/Shaders/Skills/Heal/L2WhHealTA.shader`)

- Один шейдер покрывает режим спрайтов и режим beam (`_UseDirectionAs`: 0 sprite, 3 beam).
- **Длина луча** вынесена из UC-семантики `BeamEndPoints.offset`: свойство `_BeamEndOffset` (например `(0, -190, 0)`), длина как `length(_BeamEndOffset)`; **ширина** — из `_SizeRangeX` (UC `StartSizeRange.X` 6–10, в Unity поджато для тонких streaks).
- **Вертикальный billboard** для beam с выравниванием «верх луча» относительно `center` (`beamTopAlign` от половины длины).
- **Мягкие края quad:** `_BeamEdgeFeather`, `_BeamEndFeather` — `smoothstep` по `uv.x` / `uv.y`, только для beam.
- **Яркое «ядро» полосы:** `_BeamCoreStrength`, `_BeamCorePower` — профиль по центру `uv.x`.
- **Тёплое свечение у основания:** `_BeamFootGlowStrength`, `_BeamFootGlowPower`, `_BeamFootWarmTint` — усиление к низу луча (`uv.y` = низ у текущей развёртке strip mesh).
- Фрагмент по-прежнему близок к fixed-function: `texture * vertexColor * colorScale`, плюс перечисленные beam-маски.

### Runtime mesh `L2BeamStripMeshBuilder` (`Assets/Scripts/Rendering/Effects/L2BeamStripMeshBuilder.cs`)

- Подменяет mesh у дочерних `MeshFilter` на процедурную **beam-strip** ленту (2 вершины на «сегмент», как в UE по формуле из `Initialize`).
- Для `wh_heal_ta` с `HighFrequencyPoints=2` выставлено **`_segments: 1`**, без искусственного изгиба (`_centerWobble: 0`).
- Заданы **расширенные `Mesh.bounds`** (`_boundsSize`), чтобы луч не отсекался frustum’ом: в шейдере геометрия сильно растягивается, а исходный AABB остаётся маленьким.

### Материал `BeamEmitter0.mat` (`Assets/Resources/Data/Effects/wh_heal/wh_heal_ta/BeamEmitter0.mat`)

- Шейдер `L2WhHealTA`, `_UseDirectionAs: 3`.
- Ключевые тюнинги по ходу итераций:
  - `_BeamEndOffset`, `_SizeRangeX` (ширина), `_Alpha`, `_DstBlend` (alpha blend, не жёсткий stacking как у чистого additive).
  - Feather / core / foot-glow параметры (см. инспектор после последнего коммита).
  - Смещение цвета через `_ColorMultiplierRange*` под тёплый тон.
- `_StartLocationOffset` и прочий spawn — по маппингу из UC (с учётом масштаба Unity).

### Префабы

- `wh_heal_ta.prefab`: на корневом `BeamEmitter0` висит `L2BeamStripMeshBuilder` с актуальными `_segments`, `_boundsSize`.
- `wh_heal_composite.prefab`: ссылка `wh_heal_ta` исправлена (раньше ошибочно указывала на `wh_heal_ca`). `wh_heal_ca`: `attachmentPoint: CasterPosition` (6), `followResolvedTransform: 0`. `wh_heal_ta`: `spawnTiming: OnAnimationShoot` (5) — этот паттерн `ca` переиспользуется в `wh_сure_posion_composite` (см. [2026-06-04-cure-poison-composite.md](./2026-06-04-cure-poison-composite.md)).

### Смежное (из той же ветки разговора)

- `ParticleGroup`: флаг `_preserveShaderTimeInContinuousLoop` вместо хардкода по имени шейдера, чтобы не ломать эффекты вроде WindStrike.
- `EffectShaderLifetimeHelper` / `CompositePrefabEffect` / `CompositeProjectileLaunchHelper`: правки lifetime и `RevealOnShoot` без глобальных регрессий.

## Что ещё имеет смысл сделать

1. **Финальный визуальный матч** под твой билд: тонкая подстройка `_BeamFootGlowStrength`, `_BeamCoreStrength`, `_Alpha`, `_SizeRangeX` на реальной сцене и HDR/bloom (оригинал часто выглядит ярче за счёт поста).
2. **Проверка UV настоящей текстуры** `fx_m_t0004` в Unity (иногда импорт/alpha даёт лишнюю «серость») — при расхождении с RenderDoc оригинала.
3. **Один раз сверить длину** `_BeamEndOffset` с полной цепочкой UE: `offset.Z`, `DrawScale` на корне эффекта, ваш множитель UE→Unity (чтобы не подгонять глазами).
4. **Другие скиллы на `L2WhHealTA`:** если переиспользуешь шейдер, для sprite-частей оставить `_Beam*`/`foot` в нуле или завести отдельный material variant.
5. По желанию: **документировать** точное соответствие полей UC ↔ shader properties в одной таблице (отдельный абзац или `docs`).

## Ключевые файлы

| Назначение | Путь |
|------------|------|
| Шейдер | `Assets/Resources/Data/Shaders/Skills/Heal/L2WhHealTA.shader` |
| Материал лучей | `Assets/Resources/Data/Effects/wh_heal/wh_heal_ta/BeamEmitter0.mat` |
| Strip mesh | `Assets/Scripts/Rendering/Effects/L2BeamStripMeshBuilder.cs` |
| Префаб эффекта | `Assets/Resources/Data/Effects/wh_heal/wh_heal_ta/wh_heal_ta.prefab` |
| Композит | `Assets/Resources/Data/Effects/wh_heal/wh_heal_composite.prefab` |
