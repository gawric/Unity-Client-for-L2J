# El Ice Bolt Icefrag: shader notes

## Итоги за сегодня
- Для `el_ice_bolt_ta` добавлен отдельный shader-only эффект разлёта льда: `Assets/Resources/Data/Shaders/Skills/Ice/L2IceFrag.shader`.
- Для `icefrag` создан отдельный материал: `Assets/Resources/Data/Effects/el_ice_bolt/el_ice_bolt_ta/M_IceFrag.mat`.
- `MeshEmitter6` в `Assets/Resources/Data/Effects/el_ice_bolt/el_ice_bolt_ta/el_ice_bolt_ta.prefab` переведён на новый icefrag-материал, чтобы не смешивать поведение с `iceberg`.
- Общий внешний вид льда вынесен в `Assets/Resources/Data/Shaders/Skills/Ice/L2IceLook.hlsl`.
- Общая mesh-particle логика движения вынесена в `Assets/Resources/Data/Shaders/Skills/Common/L2FxMeshParticleMotion.hlsl`.

## Текущее поведение шейдера
- `L2IceFrag.shader` отвечает за разлёт ледяных осколков через vertex shader, без Unity PhysX.
- Внешний вид берётся из `L2IceLook.hlsl`: `_MainTexture`, `_SpecMask`, `_EnvCube`, `_Tint`, fresnel, edge glow, alpha.
- Движение берётся из `L2FxMeshParticleMotion.hlsl`:
  - `L2Fx_SpawnOffsetPolarYDegrees` - polar spawn вокруг Unity Y-up оси.
  - `L2Fx_DampedDisplacement` - velocity + drag + acceleration.
  - `L2Fx_OutwardDirectionXZ` - горизонтальное направление разлёта.
  - `L2Fx_ApplyMeshParticleSpin` - вращение позиции и нормали mesh-осколка.
- `L2IceApprox.shader` оставлен для текущего поведения ледника/iceberg, но теперь тоже использует общий визуальный ice-look.

## Текущее соответствие материал/эмиттер
- `M_IceFrag.mat` настраивает разлёт через параметры:
  - `_OutwardSpeed` - дальность/сила разлёта по сторонам.
  - `_UpVelocity` - высота подлёта.
  - `_Acceleration` - падение, сейчас по Y вниз.
  - `_VelocityLossRange` - торможение, больше значение означает короче разлёт.
  - `_LifetimeRange` и `_FadeoutStartTime` - время жизни и резкость исчезновения.
- Для `ParticleGroup` у `icefrag` важно держать burst-настройки синхронно с количеством renderer-слотов:
  - `_isBurstSpawning = true`;
  - `_hasFixedDuration = true`;
  - `_maxCount` равен количеству `MeshEmitter6` slots;
  - `_duration` близко к shader lifetime.

## Следующие шаги по настройке
- Для меньшей дальности уменьшать `_OutwardSpeed` или увеличивать `_VelocityLossRange` в `M_IceFrag.mat`.
- Для меньшего “фонтана” уменьшать `_UpVelocity`.
- Для более резкого исчезновения поднимать `_FadeoutStartTime` ближе к `_LifetimeRange`.
- Для большего количества осколков добавлять renderer-слоты в `icefrag` и выставлять `_maxCount` равным их количеству.
