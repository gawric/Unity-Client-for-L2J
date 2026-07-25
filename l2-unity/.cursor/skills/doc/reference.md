# El Ice Bolt: заметки по сессии (30 Apr 2026)

## Итоги за сегодня

- Проверили путь через skybox/cubemap и протестировали отражения окружения для ледяного материала.
- Добавляли временный editor-инструмент для запекания cubemap и позже удалили его.
- Продолжили приводить `el_ice_bolt_ta` (`iceberg`) к поведению эмиттера из Lineage 2.
- Довели `L2IceApprox` ближе к L2-поведению: логика времени жизни/fade, размера и вращения перенесена в шейдер.
- Подтвердили, что размер частиц стал ближе к проектному baseline после выравнивания с подходом `L2SkillEffect`.

## Текущий шейдер: `L2IceApprox`

Путь: `Assets/Resources/Data/Shaders/Skills/Ice/L2IceApprox.shader`

Реализованные возможности:

- **Модель рендера**
  - Прозрачный альфа-блендинг (`Blend SrcAlpha OneMinusSrcAlpha`)
  - Двусторонний рендер (`Cull Off`)
  - Unlit forward pass с spec-mask + fresnel + опциональным env cubemap

- **Тайминги времени жизни и появления**
  - `_HasLifetime`
  - `_StartTime`
  - `_InitialDelayRange`
  - `_LifetimeRange`
  - `_FadeIn`, `_FadeInEndTime`
  - `_Fadeout`, `_FadeoutStartTime`
  - Выходная альфа умножается на вычисленный envelope времени жизни (`lifeAlpha`)

- **Логика размера (выровнена со стилем `L2SkillEffect`)**
  - `_SizeRangeX`, `_SizeRangeY`, `_SizeRangeZ`
  - `_UniformSize`
  - `_UseSizeScale`
  - `_Seed`
  - `_SizeScale0..2` (time/scale keys)
  - Шейдер выбирает стартовый размер из диапазона через seed-based random и затем применяет анимированный size scale по нормализованному возрасту.

- **Логика вращения в шейдере**
  - `_SpinParticles`
  - `_StartSpinRangeX/Y/Z`
  - `_SpinsPerSecondRangeX/Y/Z`
  - Позиции вершин вращаются в object space на основе случайных стартовых углов и угловой скорости по осям в зависимости от возраста частицы.

## Соответствие задумке L2 `MeshEmitter3`

Для следующего смысла конфига:
- `MaxParticles=3`, `RespawnDeadParticles=False`
- спавн в одной зоне, но с разной ориентацией/вращением
- `UseSizeScale=True`, `UniformSize=True`
- `StartSizeRange` + `SizeScale` keys
- `LifetimeRange` + `InitialDelayRange` + fade-поведение

Текущее разделение ответственности:

- **Сторона `ParticleGroup` / эмиттера**
  - количество частиц и burst-логика (`maxCount`, тайминг burst)
  - жизненный цикл инстансов частиц

- **Сторона шейдера (`L2IceApprox`)**
  - визуальный envelope частицы (delay/lifetime/fade alpha)
  - размер из диапазона + масштаб по возрасту
  - вращение от стартового угла + угловая скорость
  - ледяной шейдинг (main tex/spec mask/fresnel/env)

## Быстрый чеклист настройки

- Если эффект слишком яркий: уменьшай `_EnvStrength`, `_FresnelStrength` или `_SpecMaskStrength`.
- Если эффект слишком синий: нейтрализуй `_Tint` и `_EdgeColor`.
- Если эффект слишком большой/маленький: сначала настраивай `_SizeRangeX/Y/Z`, потом ключи `_SizeScale`.
- Если вращение слишком хаотичное: сужай `_SpinsPerSecondRange*` и `_StartSpinRange*`.
- Если появление/исчезновение слишком резкое: настраивай `_FadeInEndTime` и `_FadeoutStartTime` относительно `_LifetimeRange`.

