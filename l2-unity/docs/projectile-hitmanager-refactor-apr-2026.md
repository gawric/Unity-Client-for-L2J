# Projectile/HitManager Refactor (Apr 2026)

Документ фиксирует последние изменения по цепочке `ProjectileManager -> CompositePrefabEffect -> HitManager` после перехода на time-based hit для `EffectOnly` и удаления legacy-события из `HitManager`.

---

## Что изменилось (основное)

- Полностью удален `OnHitColliderHandled` из `HitManager`.
- Для `EffectOnly` попаданий теперь используется только событие `ProjectileManager.OnHitEffectProjectile`.
- Логика подготовки projectile-hit частично вынесена из `CompositePrefabEffect` в `HitManager`:
  - проверки валидности события,
  - нормализация направления удара,
  - soulshot-логика (`effectId=99998` + сброс флага).
- Добавлена передача `attackerEntityId` в событии `OnHitEffectProjectile`, чтобы не зависеть от `projectilePrefab.transform` (важно для `fl`-эффектов без `Entity` в иерархии).
- Получение атакующего для soulshot переведено на `World.Instance.GetEntityNoLockSync(attackerEntityId)`.

---

## Почему это нужно

- `GameObject projectilePrefab` (особенно с суффиксом `fl`) может быть отдельным визуальным объектом без `Entity`.
- Поиск атакующего через `GetComponentInParent<Entity>()` в таком случае нестабилен.
- Передача `attackerEntityId` через `ProjectileData` дает детерминированный источник истины.

---

## Обновленные классы и методы

## `HitManager`

Файл: `Assets/Scripts/Game/Manager/HitManager/HitManager.cs`

- Удалено:
  - `public event Action<Transform, MonsterStateMachine, Vector3, Vector3> OnHitColliderHandled;`
- Добавлено/изменено:
  - `TryPrepareProjectileEffectHit(...)`:
    - принимает `attackerEntityId`,
    - проверяет входные данные,
    - нормализует `hitDirection`,
    - резолвит атакующего через `World.Instance.GetEntityNoLockSync(attackerEntityId)`,
    - если `attacker.IsSoulshotCharged == true`, вызывает `EffectManager.Instance.PlayerImpactEffect(99998, hitPoint)` и сбрасывает флаг.
  - `ResolveEntityFromWorld(int entityId)`:
    - маленький helper для резолва сущности по id.

---

## `ProjectileData`

Файл: `Assets/Scripts/Game/Manager/ProjectileManager/Model/ProjectileData.cs`

- Добавлено поле:
  - `public int attackerEntityId;`
- Поле копируется в copy-конструкторе и инициализируется в конструкторах по умолчанию.

---

## `IProjectileManager`

Файл: `Assets/Scripts/Game/Manager/ProjectileManager/IProjectileManager.cs`

- Изменена сигнатура события:
  - было: `event Action<GameObject, Transform, Vector3, Vector3> OnHitEffectProjectile;`
  - стало: `event Action<GameObject, Transform, Vector3, Vector3, int> OnHitEffectProjectile;`
    (последний аргумент — `attackerEntityId`).

---

## `ProjectileManager`

Файл: `Assets/Scripts/Game/Manager/ProjectileManager/ProjectileManager.cs`

- Изменена сигнатура `OnHitEffectProjectile` под новый контракт с `attackerEntityId`.
- В `LaunchProjectile(...)`:
  - добавлен резолв `attackerEntityId` (через helper),
  - `attackerEntityId` записывается в `ProjectileData`.
- В момент time-hit (`journeyProgress >= 1f` для `EffectOnly`):
  - событие `OnHitEffectProjectile` вызывается с `projectile.attackerEntityId`.
- Добавлен helper:
  - `ResolveProjectileAttackerEntityId(GameObject projectilePrefab)`.

---

## `CompositePrefabEffect`

Файл: `Assets/Scripts/Rendering/Effects/CompositePrefabEffect.cs`

- `HandleProjectileEffectHit(...)` обновлен:
  - принимает `attackerEntityId`,
  - вызывает `HitManager.TryPrepareProjectileEffectHit(...)`,
  - при успехе применяет `resolvedHitPoint/resolvedHitDirection` в локальный контекст и спавнит `OnHitCollider`-части.
- Удалены остатки подписки на `HitManager.OnHitColliderHandled` (событие больше не используется).

---

## `AbstractAttackEvents`

Файл: `Assets/Scripts/Controller/StateMachine/State/Events/AbstractAttackEvents.cs`

- Метод-подписчик `OnHitEffectProjectile(...)` обновлен под новую сигнатуру с `attackerEntityId`.
- Логи по projectile-цепочке теперь могут показывать id атакующего.

---

## Текущий runtime flow для EffectOnly

1. `ProjectileManager` ведет projectile по времени.
2. В конце полета (time-based trigger) вычисляется hit-точка (включая anchor-resolve).
3. `ProjectileManager.OnHitEffectProjectile(...)` отправляет:
   - `prefab`,
   - `target`,
   - `hitPoint`,
   - `hitDirection`,
   - `attackerEntityId`.
4. `CompositePrefabEffect` принимает событие и делегирует подготовку в `HitManager.TryPrepareProjectileEffectHit(...)`.
5. `HitManager` применяет soulshot-правило и возвращает нормализованные данные удара.
6. `CompositePrefabEffect` спавнит отложенные части (`spawnTiming=OnHitCollider`).

---

## Итого

- Убрана устаревшая связка через `OnHitColliderHandled`.
- Центр hit-подготовки для projectile вынесен в `HitManager`.
- Добавлен надежный канал `attackerEntityId` для world-resolve атакующего.
- Soulshot-визуал для projectile теперь не зависит от иерархии `fl`-объекта.

