# Magic cast timing, HitTime, wall penalty — рукопожатие (**обновление 2026‑05‑02**, доп. тот же день)

Документ заменён: фиксируем **работу от 02.05.2026** (разбор логов, переход Animator, **`wallPenalty` в `MagicCastData`**, телеметрия `OnAnimationShoot`). Контент от **01.05** ниже сохранён **кратким оглавлением** — детальный перечень откатов и старых идей см. при необходимости в истории коммита / старой версии файла в git.

Пайплайн по-прежнему: **пакет / каст → `MagicCastData` → `SkillAnimationRunner` + `AnimationManager` → `PlayerOverriddenMagicAtk` / событие `OnAnimationShoot` → `CompositePrefabEffect.ProcessShootEvent` → `ProjectileManager`** (см. также `docs/core-vfx-animation-classes.md`).

---

## 1. Короткий бэктрек с 2026‑05‑01 (без расшифровки)

- Единый `timingScale`, флаг **`ShouldIgnoreFlightTimeForCast`**, логи `[MAGIC_PROJECTILE_SYNC]` и т.д.
- Из снаряда **откатили** подгон EffectOnly под дедлайн `HitTime` и инспекторные множители (см. прежние обсуждения в команде).

---

## 2. Что сделали **2026‑05‑02** (суть)

### 2.1. Диагностика «почему ShootEvent позже `serverTimeToShoot`»

На длинном касте (**~8 с server hit**) совпало:

| Наблюдение | Вывод |
|------------|--------|
| `[MAGIC_PROJECTILE_SYNC] AnimatorTriggerMagicShot`: `deltaToShoot` отрицательный | Триггер `MagicShot` на стыке раннера после `CastEnd` — **специально** раньше `serverShoot`; «хвост» до ивента идёт **внутри стейта MagicShot**. |
| `[ANIM_SHOOT_EVENT]`: `L0 clip` = нужный расовый `MagicShot`, `normT≈shotEvent/time`, `speed≈timingScale` | «Чужой клип» / другое имя клипа как причина **отвергнуто**. Место **`OnAnimationShoot` на этом клипе** совпадает с тем, что в расчёт кладём как `durShotToEvent`. |
| `globalSinceCast(trigger)` vs `MAGIC_ENTER` MagicShot **`globalAtEnter`** | Основная задержка **~300 мс** — **переход графа** после триггера до фактического **входа** в состояние `MagicShot` (бленд CastEnd→MagicShot). После входа прогресс до ивента **согласуется** с `shotEvent/speed`. |
| Сравнение кастов **8 с vs 4 с vs ~1.8 с** | Длиннее **server HitTime** → ниже общий темп аниматора (**`timingScale`**); те же авторские переходы в секундах **дольше** по стеночным часам → рассинхрон **ощутимее** на длинном касте; на очень коротком — почти незаметен. |

Итого: упрощённая модель **`timeline / serverTimeToShoot`** **не учитывала** wall-clock «дыру» между **триггером MagicShot** и тем, когда стейт реально считает прогресс до `OnAnimationShoot`.

**Дополнительно:** при разборе нашли связь запаздывания **выстрела по цели** (момент попадания относительно ожидания по серверу) с **`fixed duration` 0.25 с** на фазах каста в графе Animator: жёсткая длительность шага не совпадает с «идеальным» растягиванием под окно `serverTimeToShoot`, из‑за чего без отдельной компенсации визуал систематически отстаёт; это упростило обоснование перехода к **постоянному offset ~150 мс** в `MagicCastData` при временно выключенной кривой penalty.

---

### 2.2. Компенсация в `MagicCastData.Setup` (история + **актуальный код**)

Файл: `Assets/Scripts/Controller/StateMachine/AnimationSystem/Models/MagicShotModel/MagicCastData.cs`

**Общая идея (не менялась):** из номинального окна сервера до выстрела что‑то **вычитается**, суммарная длительность клипов до `OnAnimationShoot` делится на **укороченное** окно → выше **`timingScale`** → фазы идут **быстрее по wall‑clock**, выстрел/снаряд визуально **ближе** к серверному дедлайну (компенсация неучтённых переходов CastEnd→MagicShot и т.п., см. §2.1).

**Не перепутать знак:** больше вычитаемое из окна → **уже `AdjustedShootWindow`** → **выше `timingScale`** → **быстрее** анимация до события.

---

#### 2.2.1. Первый вариант 02.05 (в документе раньше описан как «текущий»)

Черновик **`wall penalty`** по **`HitTime`**:

1. **`serverTimeToShoot = max(0.01, HitTime − FlightTime)`**.
2. **`tLin = InverseLerp(2, 8, Clamp(hit, 2, 8))`**, **`curveT = Pow(tLin, 0.76)`**.
3. **`AnimatorWallPenaltySeconds = Lerp(0.07, 0.60, curveT)`** — дно порядка **70 мс**, на длинном касте после клампа до **~600 мс**.

Дальнейшие итерации в чате **заменили** это семейство; в репозитории **нет** задачи навсегда держаться за `Pow` + **0.60** на 8 с.

---

#### 2.2.2. Промежуточные решения (тот же день, см. историю правок)

| Шаг | Суть |
|-----|------|
| Два точечных якоря | Запрос: **150 мс при Hit≈8 с**, **100 мс при Hit≈4 с** (линейно между ними, кламп снаружи). |
| «Без якорей», срез верха | Отказ от отдельной точки на 4 с: линейная шкала **`Clamp(hit, 2, 8)` → `InverseLerp(2,8)`**, **`Lerp(0.07, 0.15)`** — верх был **~150 мс** вместо **~600 мс**, низ сохранён **~70 мс** при коротком касте после клампа. |
| **`fixed duration` 0.25 с** | Выявлена причина **задержки выстрела по цели**: фазы каста с фиксированной длительностью **0.25 с** не подстраиваются под окно сервера так же, как «чистые» секунды в формуле — без компенсации накапливается расхождение по времени попадания. |
| Фикс. длительности клипов | Вывод: при **fixed duration** тайминг **предсказуемее**; **динамический** штраф по HitTime временно избыточен, достаточно малого постоянного **offset**. |

---

#### 2.2.3. Актуально в коде (последняя версия §2 на 02.05)

- **`MagicCastWallPenaltyDisabledForTesting = true`** — ветка **`AnimatorWallPenaltySeconds`** по кривой **не используется** для окна; **`AnimatorWallPenaltySeconds = 0`** в логике.
- Вместо неё — **фиксированный offset** **`CastShootWindowFixedOffsetSeconds = 0.15`** (**150 мс**):  
  **`AdjustedShootWindowSeconds = max(0.01, serverTimeToShoot − 0.15)`**.
- Код **wall penalty** (линейная модель **`Lerp(WallPenaltySecondsMin, WallPenaltySecondsMax)`** после **`Clamp(2…8)`**) остаётся в **`else`** и **не удалён** намеренно — комментарий в исходнике: можно вернуть, переключив флаг **`false`** (без отката из git).
- Константы ветки penalty (если включить): **`WallPenaltyClampHitMinSeconds = 2`**, **`WallPenaltyClampHitMaxSeconds = 8`**, **`WallPenaltySecondsMin = 0.07`**, **`WallPenaltySecondsMax = 0.15`** (без **`Pow`**).
- Как и раньше: **`timingScale = timelineDuration / AdjustedShootWindowSeconds`**, **`SpeedMid = SpeedEnd = SpeedShot = timingScale`**.

Дальнейшие шаги по тюнингу: смотреть **`[CAST_WALL_PENALTY]`** (`offsetMs`, `winNom`/`winAdj`, **`deltaScale`**) и связку **`[MAGIC_PROJECTILE_SYNC]`** / **`deltaGlobalToShoot`**; при необходимости вернуть penalty или изменить **`CastShootWindowFixedOffsetSeconds`**.

---

### 2.3. Логи

| Тег | Зачем |
|-----|--------|
| **`[CAST_WALL_PENALTY]`** | Пишется **каждый каст**: **`DISABLED`**, **`offsetMs`** (полезно при выключенной кривой — сейчас **150** при активном фикс‑offset), **`penaltyMs`**, `tLin`, `curveT`, `winNom` / `winAdj`, **`scaleNoPenalty` vs `scaleApplied`** и **`deltaScale`**. |
| **`[CastTimingSetup]`** | При `UNITY_EDITOR \|\| DEVELOPMENT_BUILD`: полный разнос длительностей и финальный `timingScale`. |
| **`[ANIM_SHOOT_EVENT]`** | При `UNITY_EDITOR \|\| DEVELOPMENT_BUILD`: `DISPATCH` / `SKIP_DEDUP_SAME_FRAME`, имя активного клипа на layer 0 (+ layer 1), `normT`, `speed`, `eventArg`; дедуп второго **`OnAnimationShoot`** на том же кадре (логика в `BaseAnimationController.OnAnimationShoot` → статический вызов в `AnimationManager.LogAnimationShootFromAnimator`). |

Прежние теги **`[MAGIC_PROJECTILE_SYNC]`**, **`[MAGIC_ENTER]`** и т.д. остаются полезными для связки триггер / вход в стейт / `ShootEvent` / LAUNCH.

---

### 2.4. Связные файлы (шпаргалка)

| Тема | Путь |
|------|------|
| Окно каста, штраф, `timingScale` | `MagicCastData.cs` |
| Ввод каста, длины, `shotEvent` | `NewMagicSkillsState.cs`, `AnimationManager.cs` |
| Speed по фазам, ForceSync выкл | `PlayerOverriddenMagicAtk.cs` |
| Триггеры цикла, await | `AnimationManager.cs`, `SkillAnimationRunner.cs` |
| `OnAnimationShoot` → орбы | `AnimationEventsBase`, `CompositePrefabEffect.cs`, `CompositeProjectileLaunchHelper.cs` |

---

## Связанные документы в `docs/`

- `core-vfx-animation-classes.md`
- `effects-composite-prefab-mechanic.md`
- `wind-strike-runtime-sync-updates.md`

---

*Продолжение в новом чате: приложи этот файл первой строкой.*
