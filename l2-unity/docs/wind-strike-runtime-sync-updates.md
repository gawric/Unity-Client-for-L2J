# Wind Strike Runtime Sync Updates

Этот документ фиксирует изменения, которые были внесены для синхронизации:

- серверного тайминга каста/выстрела,
- запуска `MagicShot`,
- расчета `FlightTime`,
- runtime-поведения composite-частей `el_wind_strike_*`.

---

## 1) Синхронизация анимационной цепочки (await и finish events)

### Проблема

`await` в анимационном раннере завершался по "чужому" событию завершения (`OnAnimationFinished`), из-за чего `CastEnd`/`MagicShot` могли перескакиваться.

### Что сделано

- В `AnimationManager` добавлено ожидание по конкретному ожидаемому имени анимации:
  - `_expectedFinishNameByObjectId`
  - фильтрация в `OnAnimationFinished(...)`
- Подписка на событие завершения для `objectId` сделана один раз:
  - `_awaitSubscribedObjectIds`
  - `EnsureAwaitSubscribed(...)`
- В `AnimationModel` `SubscribeToInternalEvents()` сделан идемпотентным.

### Результат

- `await` больше не проскакивает по неподходящим `finishedName`.
- Цепочка `CastMid -> CastEnd -> MagicShot` стала стабильной.

---

## 2) Двойной запуск `MagicShot` (Runner + ForceSync)

### Проблема

`MagicShot` запускался из двух источников:

- шаг раннера (`idx=2`),
- принудительный trigger в `PlayerOverriddenMagicAtk` из `CastEnd`.

Это давало дубли/гонки.

### Что сделано

- Добавлен `MagicShotCoordinator` (static), дедуп по:
  - `objectId`
  - `castStartMs` (из `castData.StartTime * 1000`)
- Интеграция в оба источника запуска:
  - `AnimationManager.PlayerAnimationTrigger(...)` (Runner)
  - `PlayerOverriddenMagicAtk` (ForceSync)
- Исправлен порядок в `AnimationManager`: coordinator-check выполняется **до** reset trigger'ов, чтобы blocked runner не сбрасывал уже выставленный force trigger.
- Удален `CompleteCast(...)` на `OnStateExit` `MagicShot`, чтобы в рамках одного каста повторный запуск не проходил.

### Результат

- Один реальный `MagicShot` на один каст.
- Оба механизма запуска сохранены как fallback, но без дублей.

---

## 3) Точный момент `MagicShot` с учетом event внутри клипа

### Проблема

Фактический shot (`OnAnimationShoot`) запаздывал, так как trigger `MagicShot` запускался около `serverShoot`, но сам event внутри клипа находится на `shotEventTime`.

### Что сделано

В `PlayerOverriddenMagicAtk` расчет момента force trigger изменен:

- было: `serverShoot - blendComp`
- стало: `serverShoot - shotEventTime - blendComp`

### Результат

- `FIRE_SYNC` ближе к серверному `serverTimeToShoot`.
- Визуальный полет projectile перестал "схлопываться".

---

## 4) Синхронизация `FlightTime` в `NewMagicSkillsState`

### Проблема

`ResolveMagicFlightTimeMs` возвращал фиксированные `1000ms`, что расходилось с расчетом полета в `ProjectileManager`.

### Что сделано

В `NewMagicSkillsState.ResolveMagicFlightTimeMs(...)` добавлен расчет как в `ProjectileManager`:

- `distance -> speed -> flightTime` через `ProjectileFlightTimeCalculator`
- `hitOffset = 0.3`
- перевод в миллисекунды
- минимальный порог `350ms`
- fallback `1000ms` если нет цели/distance невалиден

### Результат

- `castData.FlightTime` и runtime projectile flight стали согласованными.

---

## 5) Composite projectile visibility до shoot

### Проблема

Нужна отдельная настройка: показывать часть до `OnAnimationShoot` или нет.

### Что сделано

В `CompositeProjectileConfig` добавлен флаг:

- `showBeforeAnimationShoot` (default: `true`)

Логика:

- если `launchMode=OnAnimationShoot` и `showBeforeAnimationShoot=false`,
  часть скрывается до shoot-события и показывается в `ProcessShootEvent`.

Дополнительно:

- для `spawnTiming=OnHitCollider` pre-shoot hide логика отключена, иначе часть могла остаться скрытой навсегда.

---

## 6) Spawn timing `OnHitCollider` для `CompositePrefabPart`

### Что сделано

- В `CompositePartSpawnTiming` добавлен режим:
  - `OnHitCollider`
- В `CompositePrefabEffect` добавлена очередь `_pendingHitColliderParts`:
  - части с этим таймингом не спавнятся по времени,
  - спавнятся по событию hit.

---

## 7) Источник hit-события перенесен на `HitManager`

### Причина

Не завязываться напрямую на внутренние события `ProjectileManager`, чтобы будущие изменения projectile-логики не ломали effect-trigger.

### Что сделано

- В `HitManager` добавлен event:
  - `OnHitColliderHandled`
- В `HandleHitCollider(...)` event вызывается после обработки hit.
- `CompositePrefabEffect` подписан на `HitManager.Instance.OnHitColliderHandled`.

---

## 8) Спавн части по точке и направлению удара

### Что сделано

- В `EffectResolveContext` добавлены:
  - `HasHitDirection`
  - `HitDirection`
- В `CompositePrefabEffect.HandleHitManagerCollider(...)` в контекст временно записываются:
  - `HitPoint = hitPointCollider`
  - `HitDirection = hitDirection`
- Для `attachmentPoint = WorldHitPoint` rotation части строится через `Quaternion.LookRotation(HitDirection)`.

### Результат

- Hit-вспышка может появляться в фактической точке столкновения и ориентироваться по направлению удара.

---

## 9) Отключение timed lifetime для отдельных частей

### Проблема

Некоторым частям (например `ta` flash) не нужно жить до `cast HitTime`.

### Что сделано

- В `CompositePrefabPart` добавлен флаг:
  - `useCastTimedLifetime` (default: `true`)
- В `TimedCompositeEffectBase.CreateRuntimeSettings(...)` добавлен параметр `applyTimedLifetime`.
- Для `useCastTimedLifetime=false` lifetime не растягивается под cast-time.

---

## 10) Отдельный `settingsOverride` для `el_wind_strike_ta`

### Что сделано

Создан отдельный asset:

- `Assets/Scripts/Database/Effects/Data/Settings/EffectWindStrikeTAFlash.asset`

Подключен в `el_wind_strike_composite` как `settingsOverride` для `el_wind_strike_ta`.

Цель:

- короткая вспышка (`defaultLifeTime=0.35`, `hideTime=0.12`)
- без влияния на другие эффекты.

---

## 11) Диагностические логи для `ta`

### Добавлены теги

- `L2Particle`:
  - `[TA_LIFETIME_SETUP]`
  - `[TA_LIFETIME_PLAY]`
  - `[TA_LIFETIME_DESTROY]`
- `ParticleGroup` (фильтр на owner `el_wind_strike_ta`):
  - `[TA_PARTICLE_PLAYPART]`
  - `[TA_PARTICLE_FIRST_SPAWN]`

### Вывод по логам

`WindMesh` стартует сразу (порядка `0.009-0.010s`), проблема "долгой жизни" была связана с runtime duration/loop, а не с задержкой старта.

---

## 12) Изменение состава `L2Particle._particleGroups` в `el_wind_strike_ta`

### Что сделано

В `el_wind_strike_ta.prefab` в массиве `_particleGroups` оставлен только `WindMesh`.

Цель:

- убрать одновременный запуск `Core` и `Ring`,
- оставить только нужный визуальный слой.

---

## Рекомендованная конфигурация для `el_wind_strike_ta` (текущее направление)

- `spawnTiming`: `OnHitCollider`
- `attachmentPoint`: `WorldHitPoint`
- `projectile.launchMode`: `Disabled`
- `useCastTimedLifetime`: `false`
- отдельный `settingsOverride`: `EffectWindStrikeTAFlash`
- в `ParticleGroup` (если нужен flash):
  - `Has Fixed Duration = true`
  - короткий `Duration` (например `0.2-0.35`)

