# Vampiric Touch — orbs (SpriteEmitter5) и хвост (SpriteEmitter2)

**Дата:** 2026-05-26

Краткая сводка работ по `bl_vampiric_touch_ta`: полёт двух orbs, шлейф искр, затухание и второй хвост.

---

## SpriteEmitter5 / SpriteEmitter5_1 (orbs)

- Дуга home-projectile: резкий подъём и разлёт **у монстра**, длинный спуск к игроку (не «горка» в середине).
- Новые параметры в `CompositeHomeProjectileConfig` + `HomeProjectileService`: `pathPeelAlongLine`, `pathApexAlongLine`, `pathPeakHeightAlongLine`, `pathEarlyClimbFactor`, `pathDistanceHeightFactor`.
- Настройки в `el_vampiric_touch_composite.prefab` (apex ~0.34, peel ~0.12, height offset ↑, side offset ↑).
- Фазовая скорость: `pathAscentSpeedScale` 1.44, `pathDescentSpeedScale` 0.8.
- Скорость полёта orbs: `_homeFlightSpeedScale` 0.8 на группах.

## SpriteEmitter2 (хвост)

- `VampiricTouchSpark.shader`: `_UseTrailPathFade`, fade по длине хвоста (после `saturate` альфы).
- `HomeProjectileTrailVelocityProvider`: позиция по возрасту частицы (`placeByParticleAge`), не по индексу child.
- Длина хвоста: больше частиц (`_maxCount` 30, `_cloneParticlesToMaxCount`, lifetime/history 0.5s), без растягивания scale.
- `ParticleGroup.EnsureRuntimeParticleCapacity` — runtime-клоны quad-слотов (opt-in, только где включён флаг).

## Второй хвост (SpriteEmitter5_1)

- Добавлен binding `SpriteEmitter2_to_SpriteEmitter5_1` в `bl_vampiric_touch_ta.prefab` (`tailRoot` + `velocitySource` → `SpriteEmitter5_1`).

## Фиксы по ходу

- `_preserveShaderTimeInContinuousLoop: 1` ломал respawn — оставлен `0`.
- CS0136: `step` → `linearStep` в `HomeProjectileService`.
- Trail provider обновляет список renderer’ов при появлении runtime-клонов.

## Ключевые файлы

| Файл |
|------|
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/bl_vampiric_touch_ta.prefab` |
| `Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_composite.prefab` |
| `Assets/Scripts/Rendering/Effects/HomeProjectileService.cs` |
| `Assets/Scripts/Effects/Core/PrefabHelpers/HomeProjectileTrailVelocityProvider.cs` |
| `Assets/Scripts/Rendering/Effects/ParticleGroup.cs` |
| `Assets/Resources/Data/Shaders/Skills/Vampiric/Touch/VampiricTouchSpark.shader` |
