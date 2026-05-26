#ifndef L2_FX_COLOR_SCALE_SOFT_INCLUDED
#define L2_FX_COLOR_SCALE_SOFT_INCLUDED

// Soft ColorScale pulse for UE SpriteEmitter (blink-style).
// Include after L2FxEmitterSpawn.hlsl.

float4 L2Fx_SampleColorScaleSoft(
    float normalizedAge,
    float colorScaleParam,
    uint colorScaleCount,
    float colorScaleTimes[8],
    float4 colorScaleColors[8],
    bool bAlphaBlend,
    float smoothness)
{
    if (colorScaleCount == 0)
    {
        return float4(1, 1, 1, 1);
    }

    float sp = frac((colorScaleParam + 1.0) * normalizedAge);

    uint idx = 0;
    while (idx < colorScaleCount && colorScaleTimes[idx] < sp)
    {
        idx++;
    }

    float4 prevCol;
    float prevT;
    float4 nextCol;
    float nextT;

    if (idx == 0)
    {
        prevCol = float4(1, 1, 1, 1);
        prevT = 0.0;
        nextCol = colorScaleColors[0];
        nextT = colorScaleTimes[0];
    }
    else
    {
        prevCol = colorScaleColors[idx - 1];
        prevT = colorScaleTimes[idx - 1];
        nextCol = (idx < colorScaleCount) ? colorScaleColors[idx] : prevCol;
        nextT = (idx < colorScaleCount) ? colorScaleTimes[idx] : prevT + 1e-4;
    }

    float ts = (sp - prevT) / max(nextT - prevT, 1e-4);
    float tsSoft = smoothstep(0.0, 1.0, ts);
    ts = lerp(ts, tsSoft, saturate(smoothness));

    float4 col = lerp(prevCol, nextCol, ts);
    col.a = bAlphaBlend ? col.a : 1.0;
    return col;
}

float L2Fx_ColorScaleRepeatsParam(float colorScaleRepeats)
{
    return max(colorScaleRepeats, 0.001) - 1.0;
}

#endif // L2_FX_COLOR_SCALE_SOFT_INCLUDED
