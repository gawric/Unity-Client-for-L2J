# el_ice_bolt Tail Notes (Apr 2026)

Короткий лог изменений по эффекту `el_ice_bolt_ca`, чтобы быстро восстановить контекст.

## Что сделали

- Добавили `EffectPrefabRotator`:
  - `Assets/Scripts/Effects/Core/PrefabHelpers/EffectPrefabRotator.cs`
  - Нужен для вращения root-префаба эффекта (орбита шаров вокруг персонажа).

- Добавили/рефакторнули `OrbitalTrailVelocityProvider`:
  - `Assets/Scripts/Effects/Core/PrefabHelpers/OrbitalTrailVelocityProvider.cs`
  - Нужен для вычисления хвоста по velocity (synthetic orbit) и прокидывания в `StartVelocityRange*` материалов.
  - Рефактор: вместо нескольких компонентов на дочках сделан один root-компонент со списком bindings (`tail -> source`).

## Что поменяли в prefab

Файл:
- `Assets/Resources/Data/Effects/el_ice_bolt/el_ice_bolt_ca/el_ice_bolt_ca.prefab`

Итоговая схема:
- один `OrbitalTrailVelocityProvider` на root `el_ice_bolt_ca`;
- bindings для:
  - `waterdrop_a`
  - `waterdrop_b`
  - `steam_a`
  - `steam_b`

Ранее лишние компоненты `OrbitalTrailVelocityProvider` на дочерних узлах удалены.

## Что меняли в материалах

### Water drops

- `SpriteEmitter35.mat`
- `SpriteEmitter38.mat`

По ходу подбора направление/флаги пробовались в разных комбинациях.
Рабочее поведение хвоста waterdrop достигалось в связке:
- корректные binding-и в `OrbitalTrailVelocityProvider`;
- подбор `trailSign`/`invertTrailDirection`/`orbitRadiusLocalOverride` per binding.

### Steam

- `SpriteEmitter34.mat`
- `SpriteEmitter37.mat`

Важно: для дыма проверяли `UseDirectionAs/GetVelocityDirectionFrom`, но поведение сильно зависит от вашего шейдера/эмуляции L2 (семантика флагов может быть нетривиальной).

## Практические выводы

- Для `waterdrop` лучше управлять хвостом через `OrbitalTrailVelocityProvider` (velocity/radius/sign), а не через постоянные правки материалов.
- Для `steam` заметный сдвиг позиции в текущем сетапе дает `StartLocationOffset` (особенно `Z`), а не всегда `StartLocationPolarRangeZ`.
- Если у одного шара хвост правильный, а у второго «впереди» — обычно это проблема знака (`trailSign`) или радиуса в конкретном binding.

## Быстрые ручки на завтра

В `OrbitalTrailVelocityProvider` (bindings):
- `velocityScale` — длина хвоста;
- `rangeSpread` — кучность/ширина;
- `trailSign`, `invertTrailDirection` — сторона хвоста;
- `orbitRadiusLocalOverride` — положение хвоста относительно шара.

В материалах `Steam`:
- `StartLocationOffset` (сначала `Z`) — смещение зоны спавна;
- `StartVelocityRange*`, `LifetimeRange`, `StartSizeRange` — плотность/форма шлейфа.
