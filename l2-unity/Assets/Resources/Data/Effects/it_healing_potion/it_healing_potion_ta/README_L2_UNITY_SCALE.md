# 📐 Lineage 2 (UE2.5) to Unity (URP) Particle Scale Matrix

## 🛑 Архитектурный принцип (DrawScale-neutral)
1. **CPU:** симуляция в чистых UU из `.uc`; root `Transform.Scale = DrawScale` (0.05 / 0.25).
2. **GPU:** `L2Fx_GetFinalVertexSizeMeters` делит на `drawScale` из `unity_ObjectToWorld` — нейтрализует двойное сжатие Unity.
3. **K:** одна константа **`_L2FxWorldCalibration = 2.17`** на все материалы (мир 0.85 м vs эталон 1.85 м). Для чистой L2-физики можно `1.0`. Подгонка K под эффект запрещена.

---

## 🏎️ Формула в `vert`

```hlsl
float sizeUU = ResolveStartSizeUU() * sizeMul;
float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
float3 quadOS = IN.positionOS.xyz * sizeM;
OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));
```

**Финальный world diameter** = `(sizeUU / 52.5) × K × quadSpan` — не зависит от DrawScale.

---

## SizeScale SE2
Используется `L2Fx_SpriteSizeScale_ScalarFromUniforms` с нативной фазой `frac((Repeats + 1) * lifeNorm)`.

---

## Ожидаемые размеры при K = 2.17
| Эмиттер | sizeUU | world diameter |
|---|---|---|
| SE1 дымка (20 UU, static) | 20 | ~0.83 m |
| SE2 кольцо (5.5 × 3.0 peak) | 16.5 | ~0.68 m mesh → визуально ~пол-тела на 0.85 m персонаже |

---

## Файлы
- `Assets/Resources/Data/Shaders/Skills/Common/L2FxCoreGeometry.hlsl`
- Calib shaders: `HealingPotionTaCalibSpriteEmitter0/2`, `HealingPotionTa2CalibSpriteEmitter1/6`
