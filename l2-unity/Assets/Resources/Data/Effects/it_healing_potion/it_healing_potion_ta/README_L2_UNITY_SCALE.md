# 📐 Lineage 2 (UE2.5) to Unity (URP) Particle Scale Matrix

Этот документ фиксирует эталонную математику переноса размеров и геометрии спрайтовых эмиттеров (`SpriteEmitter`) из конфигурационных файлов `.uc` Lineage 2 в самописный движок частиц на базе Unity URP.

## 🛑 КРИТИЧЕСКИЙ АРХИТЕКТУРНЫЙ ПРИНЦИП
1. **CPU Симуляция:** Все расчеты на стороне C# (координаты, скорости, ускорения) ведутся в **чистых Unreal Units (UU)** напрямую из `.uc`.
2. **Root GameObject Scale:** На корневом объекте эффекта в Unity выставляется **`Scale = DrawScale`** из `.uc` (например `0.05` для хилки, `0.25` для ауры).
3. **DrawScale в шейдере НЕ компенсируется.** Unity применяет `Transform.Scale` к вершинам через `unity_ObjectToWorld`. Размеры частиц на экране линейно зависят от `DrawScale` актора — как в рантайме Lineage 2.
4. **Единая мировая калибровка:** `_L2FxWorldCalibration = 7.0` на всех материалах без исключения. Ручная подгонка K под отдельные эффекты запрещена.

---

## 🔬 Физические константы и верификация рантайма
На основе анализа дампов памяти `Engine.dll` (`UParticleEmitter::RenderParticles`) и живых логов рантайма Lineage 2 зафиксировано:
* Международный стандарт макро-мира UE2.5: **`1 метр Unity = 52.5 Unreal Units (UU)`**.
* Поле `size=(X,Y,Z)` в структуре частицы (`FParticle` / 224 bytes) хранит **чистый ДИАМЕТР (полную ширину спрайта)**, рассчитанный как `StartSizeRange * SizeScale(t)`.
* Умножение локальных вершин квада на `2.0` (компенсация радиуса) — **ЗАПРЕЩЕНО**. Дефолтный Quad Unity (1x1, ±0.5) при умножении на диаметр садится в `border` край в край.

---

## 🏎️ Эталонная формула в Вершинном Шейдере (`vert`)

```hlsl
#include "../Common/L2FxCoreGeometry.hlsl"
#include "../Common/L2FxFlipbook.hlsl"

float sizeUU = ResolveStartSizeUU() * EvaluateDynamicSizeScale(_TestSizeScaleAge);
float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
float3 quadOS = IN.positionOS.xyz * sizeM;
OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));

// Atlas UV — через L2FxFlipbook.hlsl
OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
// BlendBetweenSubdivisions: L2Fx_FlipbookAtlasUVBlend(...)

// Финальный диаметр в мире = (sizeUU / 52.5) * K * DrawScale
```

---

## 📊 Сквозной математический пример (Сверка с логами)

### Входные данные из `m_u004_b.uc` (`SpriteEmitter2`):
* `StartSizeRange=(X=(Min=5.5, Max=5.5))`
* `DrawScale = 0.05`
* Текущий возраст частицы (`lifeNorm`) = `0.7051`
* Множитель размера по кривой на этом кадре (`SizeScale`) = `3.22`

### Рантайм-процесс:
1. Вычисление размера в UU: 
   $$5.5 \times 3.22 = 17.72 \text{ UU}$$
2. Перевод в метры Unity: 
   $$17.72 \text{ UU} / 52.5 = 0.3375 \text{ м}$$
3. Глобальная калибровка мира: 
   $$0.3375 \text{ м} \times 7.0 = 2.3625 \text{ м}$$ *(локальный масштаб квада в Vertex Stage)*
4. Отрисовка движком Unity (DrawScale на root): 
   $$2.3625 \text{ м} \times 0.05 = 0.118 \text{ м}$$

С учетом прозрачных полей текстуры флипбука, видимое ядро частицы визуально сжимается до размера, сопоставимого с оригиналом Lineage 2.

---

## 🛠️ Инструкция для AI-агентов при расширении шейдера
1. **Никогда** не делите размер на `currentDrawScale` / `lossyScale` внутри шейдера — это ломает зависимость размеров от DrawScale.
2. **Никогда** не возвращайте коэффициенты `0.01` или умножение на `2.0`.
3. `_L2FxWorldCalibration` всегда `7.0` — не подгонять под отдельные материалы.
4. При импорте кривых `SizeScale` закладывайте защиту для `SpriteEmitter0`: если `_SizeKey0.x > 0.0`, на отрезке `[0.0, key0.x]` интерполируйте от `1.0` до `key0.y`.
5. Индексация атласа — через `L2FxFlipbook.hlsl`.
