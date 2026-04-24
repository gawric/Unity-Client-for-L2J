# Core VFX/Animation Classes

Этот документ описывает ключевые runtime-классы, которые участвуют в синхронизации:

- серверного каста (`HitTime` / `FlightTime` / `serverTimeToShoot`)
- анимации (`CastMid` / `CastEnd` / `MagicShot`)
- composite VFX и projectile-частей.

---

## Основной поток

1. В `NewMagicSkillsState` создается/обновляется `MagicCastData`.
2. `EffectManager` запускает root-эффект по `skillId`.
3. Если root — `CompositePrefabEffect`, он спавнит части (`CompositePrefabPart[]`) по таймингам.
4. Часть `pr` (или любая part с projectile launch mode) уходит в `ProjectileManager` по shoot-событию.
5. `ParticleGroup` внутри частей поддерживает непрерывный loop до конца runtime duration.

---

## Классы эффектов

### `EffectManager`
**Path:** `Assets/Scripts/Game/Manager/EffectManager/EffectManager.cs`

- Точка входа запуска эффекта: `PlayEffect(id, target, castData)`.
- Инстанцирует `BaseEffect`, вызывает `Setup(...)` и `Play()`.
- Не знает деталей composite — это внутри `CompositePrefabEffect`.

### `TimedCompositeEffectBase`
**Path:** `Assets/Scripts/Rendering/Effects/TimedCompositeEffectBase.cs`

- Базовый класс для timed composite-эффектов.
- Клонирует `EffectSettings` на runtime, чтобы задать lifetime от `castData.HitTime + tail`.
- Содержит общие helper-методы:
  - queue delayed parts,
  - cleanup coroutines,
  - unsubscribe shoot sources,
  - attach/follow helpers,
  - scale/loop override helpers.

### `CompositePrefabEffect`
**Path:** `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs`

- Главный оркестратор composite VFX.
- Для каждой части:
  - resolve attachment point,
  - spawn instance,
  - setup cast/settings,
  - apply loop/shader-lifetime overrides.
- Подписывается на shoot-события и переводит нужные части в projectile launch.

### `CompositePrefabPart`
**Path:** `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs`

- Конфиг одной части composite:
  - attachment + timing + offset/scale/follow,
  - cast/lifetime overrides,
  - `projectile` блок (`launchMode`, `impactType`, `settingsOverride`).

### `ParticleGroup`
**Path:** `Assets/Scripts/Rendering/Effects/ParticleGroup.cs`

- Локальный emitter-групповой контроллер внутри `L2Particle`.
- Рассчитывает runtime duration из max:
  - group duration,
  - cast hit,
  - settings lifetime,
  - legacy fallback.
- Поддерживает runtime continuous loop (через override от `CompositePrefabPart`).

### `CompositeProjectileLaunchHelper`
**Path:** `Assets/Scripts/Rendering/Effects/CompositeProjectileLaunchHelper.cs`

- Helper для projectile launch логики частей:
  - проверка launch mode,
  - обработка shoot launch по частям,
  - построение `ProjectileData`,
  - вызов `ProjectileManager.LaunchProjectile`.

### `EffectShaderLifetimeHelper`
**Path:** `Assets/Scripts/Rendering/Effects/EffectShaderLifetimeHelper.cs`

- Универсальный helper для shader lifetime-параметров:
  - `_HasLifetime` / `_HasLifeTime`
  - `_LifetimeRange` / `_LifeTimeRange`
- Используется для сценария: выключить lifetime на hold-фазе и включить только в финальном fade.

---

## Классы анимации и тайминга каста

### `MagicCastData`
**Path:** `Assets/Scripts/Controller/StateMachine/AnimationSystem/Models/MagicShotModel/MagicCastData.cs`

- Runtime snapshot серверного каста:
  - `StartTime`,
  - `HitTime`,
  - `FlightTime`,
  - `serverTimeToShoot`,
  - рассчитанные speed-множители для фаз анимации.

### `PlayerOverriddenMagicAtk` (обязательно)
**Path:** `Assets/Scripts/Animation/Player/PlayerOverriddenMagicAtk.cs`

Ключевая роль этого класса:

- Это `StateMachineBehaviour`, который управляет скоростью/переходами магических анимационных состояний игрока.
- На входе в state:
  - берет актуальный `MagicCastData`,
  - ставит `animator.speed` для текущей фазы (`SpeedMid` / `SpeedEnd` / `SpeedShot`),
  - считывает время `OnAnimationShoot` внутри клипа.
- В `OnStateUpdate`:
  - в `CastEnd` принудительно триггерит `MagicShot` около серверного shoot-time (с небольшой компенсацией blend),
  - тем самым убирает накопившийся дрейф из-за глобального `animator.speed`.
- В `OnStateExit`:
  - возвращает `animator.speed = 1`.

Итог: `PlayerOverriddenMagicAtk` держит момент shot-анимации синхронным с серверным таймингом каста.

### `AnimationManager`
**Path:** `Assets/Scripts/Controller/StateMachine/AnimationSystem/AnimationManager.cs`

- Запускает анимации и маршрутизирует animation events.
- Источник shoot-событий, на которые подписывается `CompositePrefabEffect`.

---

## Классы projectile

### `ProjectileManager`
**Path:** `Assets/Scripts/Game/Manager/ProjectileManager/ProjectileManager.cs`

- Управляет полетом projectile-инстансов.
- Принимает стартовую позицию, target и `ProjectileData`.
- Для visual-only projectile поддерживает сценарии, где объект части эффекта используется как летящий projectile.

### `ProjectileFlightTimeCalculator`
**Path:** `Assets/Scripts/Game/Manager/ProjectileManager/ProjectileFlightTimeCalculator.cs`

- Единый helper расчета speed/flightTime.
- Используется, чтобы логика полета была консистентна между системами.

---

## Что важно для поддержки

- Для стабильной синхронизации ориентироваться на `MagicCastData`.
- Для composite part-конфигов использовать `projectile.launchMode`, а не разрозненные bool-флаги.
- Для долгоживущих визуалов включать `continuousLoop` override на нужных частях.
- Финальный shader collapse делать только в конце (`enableFinalShaderLifetimeOnFade`), чтобы избежать раннего схлопывания.
