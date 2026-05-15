# Curse poison / Magic circle — шейдеры и материалы (сессия 2026-05-14)

**Дата снимка состояния:** 2026-05-14. От этой даты можно продолжать работу без разбора истории чата.

## Затронутые файлы (актуально на конец сессии)

| Файл | Назначение |
|------|------------|
| `Assets/Resources/Data/Shaders/Skills/Curse/MagicCircleBrighten.shader` | MeshEmitter1: аддитив `Blend SrcAlpha One`, атлас UV remap, опциональный split по luma + split по UV-слою двух «звёзд» на одном мешe. |
| `Assets/Resources/Data/Effects/curse_poison/bl_curse_poison_ca/MeshEmitter1.mat` | Материал пентаграммы / яркого кольца (тот же шейдер). |
| `Assets/Scripts/Rendering/Effects/ParticleSingle.cs` | После `renderer.materials` копирует в runtime-инстансы fade/lifetime/atlas/alpha/feather/EmitterAlpha и **все** float/vector параметры split из shared-материала. |
| `Assets/Resources/Data/Shaders/Skills/Curse/Poison/ShadowCurse.shader` | MeshEmitter5: тень земли (откат «soften»/Particle копий тени делался по запросу — трогать только при новой задаче). |
| `Assets/Resources/Data/Effects/curse_poison/bl_curse_poison_ca/MeshEmitter5.mat` | Тень под эффектом. |

Текстуры: `fx_m_0000_s.png` (MeshEmitter1), `fx_m_t0054` (MeshEmitter5) — в `SysTextures/LineageEffectsTextures/…`.

---

## MagicCircleBrighten — что делает шейдер сейчас

1. **Цвет:** ColorScale + ColorMult + `tex2D(_MainTex, uv)` после remap/ST.
2. **Альфа:** `_Opacity * _EmitterAlpha`, опционально `texColor.a` и `_AlphaEdgeFeather`, если **`_IgnoreMainTexAlpha` выключен** (0 = учитывать альфу текстуры; важно для дыма на чёрном фоне при импорте From Grayscale).
3. **Split ribbon vs lines by luma (`_SplitRibbonByLum`):**  
   - `lineW` = `smoothstep(LineLumMin, LineLumMax, lum)` или при вырожденном диапазоне `lineW = lum`.  
   - `softW` = smoothstep по soft-диапазону × `(1 - lineW)` если soft max > min, иначе 0.  
   - `fInner = softW * SoftOpacity * SoftRgb + lineW * LineOpacity * LineRgb`.  
   - Если split выключен — просто `tinted`.
4. **Split outer vs inner by UV (`_SplitByUvLayer`):**  
   - В вершинный шейдер передаётся **`uvMesh`** (сырые UV меша до atlas remap; для planar — planar до remap).  
   - Manhattan-расстояние до `_UvLayerCenter.xy`.  
   - `outerW = 1 - smoothstep(DistMin, DistMax, dist)`.  
   - `fOuter` считается с теми же `softW`/`lineW`, но множители **`OuterSoft*` / `OuterLine*`**.  
   - `f = lerp(fInner, fOuter, outerW)`.

**Текущие значения в `MeshEmitter1.mat` (curse):**  
`SplitRibbonByLum=1`, `SplitByUvLayer=1`, soft-пороги и soft-mul в 0, `LineLumMin/Max=0` (ветка `lineW=lum`), `LineOpacityMul=4`, `LineRgbBoost=6`, outer line 3 / rgb 1.1, `UvLayerDistMin/Max` 0.0008 / 0.028, центр UV ~(0.2193, 0.435).

---

## Что пробовали и откатили (кратко, чтобы не повторять зря)

- **Dim по luminance** широкой заливки vs линий — давало «темнеет вся сетка», убрано из `MagicCircleBrighten`.
- **`_SharpLines`** + один `UvLayerFullInnerDist` + `lerp(1, lineBoost, innerW)` — пользователь откатил: эффект стал слишком слабым/«прозрачным» по сравнению с предыдущей схемой `tinted * f`. В коде этого варианта **больше нет**.
- **ShadowCurse:** `_GroundShadowSoften`, копирование ground-shadow в `ParticleSingle`, `fx_m_t0054` alpha From Grayscale — откат по запросу (нет проблемы с тенью).

---

## Контекст по текстурам (без правок в этом доке)

- В Photoshop у `fx_m_0000_s` **нет отдельного канала Alpha** — чёрный фон, серое/белое в RGB. В Unity для такого атласа логичен импорт **From Grayscale** для альфы, если не рисовали альфу вручную.
- **RenderDoc:** VertexInput показывает два типа UV на мешe; разделение «две звезды» подтверждается разной развёрткой — отсюда идея **UV mask** в шейдере (`SplitByUvLayer`), а не обязательно вторая текстура-маска (вторая текстура возможна в оригинале клиента — нужен разбор FS/сэмплеров в захвате).

---

## Откуда продолжить завтра

1. Подогнать визуал **только** через `MeshEmitter1.mat`: `Line*`, `Outer*`, `UvLayerDist*`, `UvLayerCenter`, при необходимости временно `SplitByUvLayer=0` чтобы изолировать luma-ветку.  
2. Если нужна **вторая маска из текстуры** — проверить оригинальный пайплайн (второй `sample` в FS) и добавить второй сэмплер в Unity, не смешивая с UV-хаком до ясности.  
3. Убедиться, что **ParticleSingle** после спавна копирует все нужные `_`-свойства (список в `TryCopyShaderLifetimeFadeFromShared`).

---

## Связанные заметки в этом репо

- `docs/wh-heal-ta-beam-notes-may-2026.md` — другой эффект, тот же класс `ParticleSingle` может касаться.
