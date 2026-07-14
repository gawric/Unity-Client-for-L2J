# 📐 Lineage 2 (UE2.5) to Unity (URP) Particle Scale Matrix

## 🛑 Архитектурный принцип
1. **CPU:** симуляция использует значения из `.uc`.
2. **GPU:** `L2Fx_GetFinalVertexSizeMeters` переводит размер спрайта из UU в размер локальной геометрии quad.
3. **K:** подтверждённая общая константа **`_L2FxWorldCalibration = 1.8`**. Подгонка K под отдельный эффект запрещена.

---

## 🏎️ Формула в `vert`

```hlsl
float sizeUU = ResolveStartSizeUU() * sizeMul;
float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
float3 quadOS = IN.positionOS.xyz * sizeM;
OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));
```

**Размер quad до Transform scaling** = `(sizeUU / 52.5) × K × quadSpan`.

---

## SizeScale SE2
Используется `L2Fx_SpriteSizeScale_ScalarFromUniforms` с нативной фазой `frac((Repeats + 1) * lifeNorm)`.

---

## Ожидаемые размеры при K = 1.8
| Эмиттер | sizeUU | world diameter |
|---|---|---|
| SE1 дымка (20 UU, static) | 20 | ~0.69 m |
| SE2 кольцо (5.5 × 3.0 peak) | 16.5 | ~0.57 m mesh → визуально ~две трети тела на 0.85 m персонаже |

---

## Файлы
- `Assets/Resources/Data/Shaders/Skills/Common/L2FxCoreGeometry.hlsl`
- Calib shaders: `HealingPotionTaCalibSpriteEmitter0/2`, `HealingPotionTa2CalibSpriteEmitter1/6`
