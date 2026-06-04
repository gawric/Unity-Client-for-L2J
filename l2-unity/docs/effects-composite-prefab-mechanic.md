# Composite VFX Mechanic (Prefab-based)

## Что реализовано

Мы перешли на модель запуска сложного эффекта как **одного эффекта в базе**, без разбиения `EffectDatabase` на множество служебных записей (`ca/fl/pr/ta` и т.д.).

Теперь:

- В `EffectDatabase` хранится одна запись, например `WindStrike`.
- Эта запись ссылается на один prefab-композит.
- Композитный prefab внутри содержит список частей (других prefab-эффектов) и правила их привязки к caster/target.

Это уменьшает размер `EffectDatabase` и упрощает сопровождение большого количества скиллов.

---

## Ключевые классы

### `EffectManager`

Файл: `Assets/Scripts/Game/Manager/EffectManager/EffectManager.cs`

Роль:

- Обычный вход запуска эффекта: `PlayEffect(id, target, castData)`.
- По `id` берет prefab из `EffectDatabase`.
- Создает `BaseEffect`, вызывает `Setup(...)`, затем `Play()`.

Важно: `EffectManager` теперь не содержит отдельной data-driven composite-системы. Композитность инкапсулирована внутри самого prefab через `CompositePrefabEffect`.

### `CompositePrefabEffect`

Файл: `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs`

Роль:

- Наследуется от `BaseEffect`.
- Содержит массив `CompositePrefabPart[]`.
- В `Play()` спавнит все части, настраивает им owner/settings/castData и запускает их.

Структура части:

- `prefab` — ссылка на часть эффекта.
- `settingsOverride` — опциональные индивидуальные настройки.
- `attachmentPoint` — куда привязывать (caster root, weapon, target и т.д.).
- `followResolvedTransform` — следовать ли за найденным transform.
- `inheritRotation` — брать ли rotation от точки привязки.

### `DefaultEffectAttachmentResolver`

Файл: `Assets/Scripts/Game/Manager/EffectManager/Composite/DefaultEffectAttachmentResolver.cs`

Роль:

- По типу привязки (`EffectAttachmentPoint`) и контексту (`EffectResolveContext`) находит:
  - `Transform`, если нужно следование;
  - `worldPosition` для спавна.
- Имеет fallback-логику, если конкретная кость/сокет не найдены.

### `EffectResolveContext`

Файл: `Assets/Scripts/Game/Manager/EffectManager/Composite/EffectResolveContext.cs`

Роль:

- Runtime-контекст для resolver:
  - caster/target ids;
  - caster/target entities;
  - caster/target transforms;
  - `MagicCastData`;
  - optional `HitPoint`.

### `EffectAttachmentPoint`

Файл: `Assets/Scripts/Game/Manager/EffectManager/Composite/EffectAttachmentPoint.cs`

Роль:

- Единый enum абстрактных точек привязки:
  - `CasterRoot`
  - `CasterLowerBody`
  - `WeaponSocket`
  - `TargetRoot`
  - `TargetLowerBody`
  - `WorldHitPoint`
  - `CasterPosition`
  - `TargetPosition`
  - `TargetCenter` — центр капсулы / bounds цели
  - `TargetOverHead` — над макушкой (Y из bounds, XZ из капсулы/root)
  - `CasterCenter` — центр кастера

---

## Поток выполнения (Runtime Flow)

1. Сервер присылает каст скилла.
2. Игровая логика вызывает `SkillExecutor.ExecuteSkillOverride(...)`.
3. `SkillExecutor` вызывает `EffectManager.PlayEffect(skill.Id, entity.transform, entity.GetMagicCastData())`.
4. `EffectManager` берет по `skill.Id` prefab из `EffectDatabase`.
5. Если это обычный эффект — он играет как раньше.
6. Если это prefab с `CompositePrefabEffect`:
   - внутри `CompositePrefabEffect` строится `EffectResolveContext`;
   - для каждой части вызывается resolver;
   - часть инстанцируется и запускается как независимый `BaseEffect`.

Итог: один `skillId` запускает сложный составной VFX, но внешне это остается стандартным `PlayEffect`.

---

## Почему это решение

- Не раздувает `EffectDatabase`.
- Хорошо масштабируется на десятки/сотни скиллов.
- Логику привязок можно улучшать централизованно в resolver.
- Сохраняет обратную совместимость с существующим pipeline эффектов.

---

## Как настроить новый составной эффект

1. Создать композитный prefab (например `el_wind_strike_composite`).
2. Добавить на root компонент `CompositePrefabEffect`.
3. Заполнить `_parts` ссылками на части (`ca`, `fl`, `pr`, `ta`) и их `attachmentPoint`.
4. Добавить одну запись в `EffectDatabase`:
   - `id = skillId`
   - `prefab = el_wind_strike_composite`
   - `settings = базовые settings`
5. Проверить в игре запуск по `skillId`.

---

## Примечания

- Задержки и тайминги частей должны управляться внутри самих частей/шейдеров/настроек `BaseEffect` и `EffectPart`.
- Композитный root не вводит отдельную delay-систему.
- Если конкретный сокет/кость отсутствует у модели, resolver использует fallback на доступный transform.

---

## Что доработано после запуска

Ниже изменения, добавленные после первой версии механики.

### 1) Расширение `CompositePrefabPart`

Файл: `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs`

В часть композита добавлены новые параметры:

- `spawnTiming` — когда спавнить часть:
  - `Immediate`
  - `OnServerShootTime` (`HitTime - FlightTime`)
  - `OnFlightTimeElapsed` (`FlightTime`)
  - `OnHitTime` (`HitTime`)
- `positionOffset` — смещение от attachment point.
- `scale` — ручной масштаб части.
- `normalizeOffsetByOwnerHeight` — масштабирование `positionOffset` по росту модели.
- `referenceHeight` — эталонный рост, относительно которого нормализуется offset.

Enum таймингов вынесен в отдельный файл:

- `Assets/Scripts/Game/Manager/EffectManager/Composite/CompositePartSpawnTiming.cs`

### 2) Тайминги фаз композита

`CompositePrefabEffect` теперь поддерживает отложенный спавн частей через coroutine:

- мгновенные части играют сразу;
- части с delay/таймингом ставятся в очередь и спавнятся позже.

Это позволяет в одном композите моделировать фазовый эффект:

- `ca` — старт;
- `pr` — орб около кастера;
- `fl` — старт полета на `serverTimeToShoot`;
- `ta` — попадание на `HitTime`.

### 3) Привязка и стабильный follow

При `followResolvedTransform = true` часть теперь:

- создается без parent;
- затем привязывается `SetParent(..., true)`.

Это защищает от наследования “плохого” scale кости при инстансе.

### 4) Resolver для `LowerBody`

Файл: `Assets/Scripts/Game/Manager/EffectManager/Composite/DefaultEffectAttachmentResolver.cs`

Улучшена логика `LowerBody`:

- сначала пытается найти `pelvis/hips` (позиция центра корпуса);
- fallback на ноги/root, если таз недоступен;
- для follow используется стабильный `root` (если доступен), чтобы избежать пропадания эффекта на некоторых ригах.

Также добавлен доступ к gear из entity:

- `Assets/Scripts/Game/Entity/Entity.cs` -> `public Gear Gear => _gear;`

### 5) Runtime lifetime от `serverHitTimeMs` только для Composite

Введен базовый класс:

- `Assets/Scripts/Rendering/Effects/TimedCompositeEffectBase.cs`

Он клонирует `EffectSettings` на runtime и задает:

- `defaultLifeTime = castData.HitTime`
- `hideTime = min(hideTime, defaultLifeTime)`

Это применяется только в composite-ветке, без изменения остальных типов эффектов.

Важно: применение shader-параметра `LifeTimeRange` было откатано (чтобы не замедлять внутреннюю анимацию VFX).

### 6) Рефакторинг кода

Добавлены:

- `TimedCompositeEffectBase` — общий lifecycle/runtime settings для timed composite.
- `CompositeEffectUtilities` (`Assets/Scripts/Rendering/Effects/CompositeEffectUtilities.cs`) — статические функции:
  - расчет delay,
  - расчет spawn position/rotation,
  - построение `EffectResolveContext`.

`SpawnPart` в `CompositePrefabEffect` разбит на мелкие методы с понятными именами:

- валидация part;
- resolve attachment;
- spawn instance;
- attach/follow;
- apply scale;
- resolve settings;
- setup/play;
- debug logging.

### 7) Debug логирование

Добавлены dev-only логи:

- в `MagicCastData.Setup(...)` — `serverHitMs`, `HitTime`, `FlightTime`;
- в `CompositePrefabEffect` — постановка частей в очередь и фактический spawn части.

### 8) `TargetOverHead` и VFX на модели (июнь 2026)

Файл: `DefaultEffectAttachmentResolver.cs`

- **`TargetOverHead`:** высота из renderer bounds персонажа, **горизонталь (X/Z)** из `CharacterController.center` или `transform.position`, не из `bounds.center` (оружие/плащ иначе смещают эффект вбок).
- В bounds для OverHead **не учитываются** renderer’ы под `BaseEffect` (иначе дочерние VFX раздувают/смещают точку).

**Паттерн для `*_ca` в composite (heal, cure poison):**

- `attachmentPoint = CasterPosition` (6), `followResolvedTransform = false` — как в `wh_heal_composite`.
- **Не** использовать `CasterRoot` + follow для крупного ground VFX на кастере, если другие части цепляются к `TargetOverHead` на том же entity (self-buff).

Подробности Cure Poison: [2026-06-04-cure-poison-composite.md](./2026-06-04-cure-poison-composite.md).

---

## Текущая настройка WindStrike prefab

Файл: `Assets/Resources/Data/Effects/el_wind_strike/el_wind_strike_composite.prefab`

Сейчас в `_parts` настроены:

- `el_wind_strike_ca`
- `el_wind_strike_pr`

Для `el_wind_strike_pr` используется:

- `attachmentPoint = CasterLowerBody`
- `followResolvedTransform = true`
- `positionOffset` + опциональная нормализация по росту (`normalizeOffsetByOwnerHeight`).

Рекомендация: финальные значения `positionOffset` подбирать в игре на 2-3 расах (низкая/средняя/высокая), затем фиксировать.
