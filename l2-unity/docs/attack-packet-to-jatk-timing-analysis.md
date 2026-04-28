# Attack -> jAtk Timing Analysis

Документ фиксирует разбор цепочки от получения серверного пакета `Attack` до запуска анимации `jAtk*`, включая текущий источник времени удара и точку интеграции для таймингового hit-эффекта без `SwordCollider`.

---

## Цель

- Понять полный runtime flow: сеть -> combat state -> animation trigger.
- Уточнить, где берется длительность удара (так как в `Attack` нет поля времени).
- Подготовить базу для перехода на временной trigger hit-эффекта (например, на `50%` длительности удара).

---

## 1) Цепочка от пакета `Attack` до анимации `jAtk*`

1. Пакет попадает в боевую очередь и исполняется через быстрый диспетчер:
   - `Assets/Scripts/Networking/ClientLibrary/ConcurrentQueue/Server/IncomingGameCombatQueue.cs`
   - `Assets/Scripts/Networking/FastSinglExecuter.cs`

2. Opcode `ATTACK` (`0x05`) маршрутизируется как боевое событие:
   - `Assets/Scripts/Networking/ClientLibrary/PacketInterlude/GameServer/GSInterludeCombatPacketType.cs`

3. Парсинг `Attack`:
   - `Assets/Scripts/Networking/ClientLibrary/PacketInterlude/GameServer/ServerServer/World/Combat/Attack/Attack.cs`
   - Извлекаются:
     - attacker/target id,
     - урон первого хита,
     - флаги хита,
     - массив дополнительных хитов (`sizeHit`) для multi/double-hit,
     - позиции attacker/target.

4. Для игрока-атакующего:
   - `PlayerStateMachine.ChangeIntention(INTENTION_ATTACK, attackPacket)`
   - `NewAttackIntention.Enter(...)`:
     - разворот к цели,
     - запись ожидаемого урона цели (`targetEntity.SetDamage(...)`),
     - запись флагов self-hit (`PlayerEntity.Instance.SetSelfHit(...)`),
     - переход в состояние атаки.
   - Файл: `Assets/Scripts/Controller/StateMachine/Intention/NewIntention/NewAttackIntention.cs`

5. Запуск анимации:
   - `NewAttackState.HandleEvent(READY_TO_ACT)` выбирает базовый trigger:
     - `jatk01_`, `jatk02_`, `jatk03_`
   - Далее `AnimationManager.PlayAnimationTrigger(...)`.
   - Файл: `Assets/Scripts/Controller/StateMachine/State/NewAttackState.cs`

6. Финальное имя анимации зависит от оружия:
   - `BaseAnimationManager.GetFinalNameAnim(...)` -> `baseName + weaponSuffix`
   - Суффикс берется из экипированного оружия (`Gear.WeaponAnim`):
     - `1HS`, `2HS`, `dual`, `pole`, `bow`, `hand`
   - Файлы:
     - `Assets/Scripts/Controller/StateMachine/AnimationSystem/Abstract/BaseAnimationManager.cs`
     - `Assets/Scripts/Combat/Gear/Gear.cs`
     - `Assets/Scripts/Game/Item/Enums/WeaponType.cs`

---

## 2) Что именно приходит в `Attack` (и чего не хватает)

В `Attack` есть:
- урон,
- crit/miss/shield/soulshot флаги,
- дополнительные хиты (multi-hit/double-hit через массив).

В `Attack` **нет**:
- явного поля длительности удара,
- отдельного server timestamp для hit-момента melee.

Следствие: клиент не может взять hit-time напрямую из пакета `Attack`.

---

## 3) Где вычисляется время удара сейчас

Текущее время melee-атаки вычисляется по статам/анимации:

- Базовая формула:
  - `CalculateTimeL2j(pAtkSpeed) = max(100, 500000 / pAtkSpeed)` (мс)
  - `Assets/Scripts/Utils/CalcBaseParam.cs`

- Множитель скорости анимации:
  - `patkspd = clipLength * 1000 / _pAtkSpd`
  - `Assets/Scripts/Animation/BaseAnimationController.cs`

Итог:
- длительность удара не приходит из `Attack`,
- она получается из `pAtkSpd` + длины клипа/скорости аниматора,
- тип оружия влияет через выбор клипа/суффикса `jAtk*`.

---

## 4) Как сейчас определяется факт попадания

Текущий melee hit-момент основан на геометрии (`SwordCollider`), а не на фиксированном времени:

1. Анимационное событие `OnAnimationHit` открывает окно проверки удара.
2. `SwordCollisionService` в `LateUpdate` проверяет пересечение траектории меча и target collider.
3. По пересечению вызывается `HitManager.HandleHitCollider(...)` и играется impact effect.

Ключевые файлы:
- `Assets/Scripts/Controller/StateMachine/State/Events/AbstractAttackEvents.cs`
- `Assets/Scripts/Game/Manager/SwordCollision/SwordCollisionManager.cs`
- `Assets/Scripts/Game/Manager/HitManager/HitManager.cs`

---

## 5) Вывод для перехода на тайминговый hit-эффект

Если цель — показывать `soulshots` эффект по времени (например, `~600ms` при ударе `1200ms`), а не по пересечению collider:

- Лучший уровень интеграции: state/animation orchestration (`NewAttackState` + связанный runtime контекст атаки), а не `SwordCollisionService`.
- На старте атаки (`READY_TO_ACT`) можно запускать таймер:
  - `attackDurationMs = resolveBy(pAtkSpd, clipLength, animator speed)`
  - `hitMomentMs = attackDurationMs * hitFraction` (например `0.5`)
- В `hitMomentMs` триггерить визуальный hit-эффект через `EffectManager/HitManager`.

Это даст:
- детерминированный hit-визуал,
- независимость от коллайдеров/геометрических артефактов,
- предсказуемое поведение между разными ригами/оружием.

---

## 6) Важные замечания перед внедрением

- Для разных `jAtk*` и оружий длительность может отличаться (разные клипы, разные множители).
- Нужен единый способ получения "эффективной длительности текущего удара" в рантайме.
- Желательно оставить `SwordCollider` как fallback/debug режим на переходный период.
- Для production стоит ввести data-driven `hitFraction` (по weapon type/animation group), а не хардкодить одно значение.

---

## Краткое резюме

- `Attack` сообщает **что** произошло (урон/флаги/мультихит), но не сообщает **когда внутри swing** должен быть hit-frame.
- Тайминг удара на клиенте уже вычисляется из attack speed и анимационного клипа.
- Для нового подхода с time-based soulshot hit правильнее триггерить эффект по доле длительности удара, а не по `SwordCollider` пересечению.
