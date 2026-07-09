#ifndef L2_FX_SPRITE_SIZE_SCALE_INCLUDED
#define L2_FX_SPRITE_SIZE_SCALE_INCLUDED

// L2_FX_SPRITE_SIZE_SCALE - SPRITE EMITTER ONLY
// UE3 USpriteEmitter runtime SIZE via SizeScale curve (decompile-verified).
// Engine formula:
//   scaleT  = frac((SizeScaleRepeats + 1) * lifeNorm)
//   sizeMul = piecewise_linear(SizeScale keys, scaleT)
//   Size    = StartSize * sizeMul
//
// IMPORTANT: implicit SizeScale(0) = (RelativeTime=0, RelativeSize=0).
// Pass sizeScaleParam=1.0 into L2Fx_SampleSizeScale (via helpers below).

#include "../L2FxEmitterSpawn.hlsl"

// Build SizeScale key arrays from .uc uniforms.
// Key 0 is implicit (t=0, s=0); keys 1..3 map to SizeScale(1..3) from .uc.
void L2Fx_SpriteSizeScale_BuildKeys(
    uint count,
    float t1, float s1,
    float t2, float s2,
    float t3, float s3,
    out float times[8],
    out float3 values[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        values[i] = float3(1, 1, 1);
    }

    times[0] = 0.0;
    values[0] = float3(0, 0, 0);
    if (count >= 2) { times[1] = t1; values[1] = float3(s1, s1, s1); }
    if (count >= 3) { times[2] = t2; values[2] = float3(s2, s2, s2); }
    if (count >= 4) { times[3] = t3; values[3] = float3(s3, s3, s3); }
}

// Sample scalar size multiplier for a sprite emitter (UseRegularSizeScale=False path).
float L2Fx_SpriteSizeScale_Scalar(
    float normalizedAge,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float sizeScaleTimes[8],
    float3 sizeScaleValues[8],
    bool bUseRegularSizeScale)
{
    // Sprite frequency = (Repeats + 1) => param=1.0 + repeats from .uc.
    const float kSpriteSizeScaleParam = 1.0;
    float3 mul = L2Fx_SampleSizeScale(
        normalizedAge,
        kSpriteSizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        sizeScaleTimes,
        sizeScaleValues,
        bUseRegularSizeScale);
    return mul.x;
}

// Apply SizeScale to a spawn StartSize vector.
float3 L2Fx_SpriteSizeScale_Apply(
    float3 startSize,
    float normalizedAge,
    float useSizeScale,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float sizeScaleTimes[8],
    float3 sizeScaleValues[8],
    bool bUseRegularSizeScale,
    bool bUniformSize)
{
    if (useSizeScale < 0.5)
        return startSize;

    float3 mul = L2Fx_SampleSizeScale(
        normalizedAge,
        1.0,
        sizeScaleRepeats,
        sizeScaleCount,
        sizeScaleTimes,
        sizeScaleValues,
        bUseRegularSizeScale);

    if (bUniformSize)
        mul = mul.x.xxx;

    return startSize * mul;
}

// One-call facade: raw .uc SizeScale uniforms -> final size in UE units (before UU_TO_UNITY).
float3 L2Fx_SpriteSizeScale_FullKeys(
    float3 startSize,
    float normalizedAge,
    float useSizeScale,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float t1, float s1,
    float t2, float s2,
    float t3, float s3,
    bool bUseRegularSizeScale,
    bool bUniformSize)
{
    float times[8];
    float3 values[8];
    L2Fx_SpriteSizeScale_BuildKeys(
        sizeScaleCount,
        t1, s1,
        t2, s2,
        t3, s3,
        times,
        values);

    return L2Fx_SpriteSizeScale_Apply(
        startSize,
        normalizedAge,
        useSizeScale,
        sizeScaleRepeats,
        sizeScaleCount,
        times,
        values,
        bUseRegularSizeScale,
        bUniformSize);
}

// Build from material uniforms Time0/Val0 .. Time4/Val4.
// implicitKeyZero: SpriteEmitter0-style - engine key0=(t=0,s=0), slots 1..N-1 use Time1/Val1..
//                  When false, use Time0/Val0.. as UC SizeScale(0..) (SpriteEmitter2 ring).
void L2Fx_SpriteSizeScale_BuildKeysFromUniforms(
    uint sizeScaleCount,
    bool implicitKeyZero,
    float time0, float val0,
    float time1, float val1,
    float time2, float val2,
    float time3, float val3,
    float time4, float val4,
    out float times[8],
    out float3 values[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        values[i] = float3(1, 1, 1);
    }

    if (implicitKeyZero)
    {
        times[0] = 0.0;
        values[0] = float3(0, 0, 0);
        if (sizeScaleCount >= 2) { times[1] = time1; values[1] = float3(val1, val1, val1); }
        if (sizeScaleCount >= 3) { times[2] = time2; values[2] = float3(val2, val2, val2); }
        if (sizeScaleCount >= 4) { times[3] = time3; values[3] = float3(val3, val3, val3); }
        if (sizeScaleCount >= 5) { times[4] = time4; values[4] = float3(val4, val4, val4); }
    }
    else
    {
        times[0] = time0;
        values[0] = float3(val0, val0, val0);
        if (sizeScaleCount >= 2) { times[1] = time1; values[1] = float3(val1, val1, val1); }
        if (sizeScaleCount >= 3) { times[2] = time2; values[2] = float3(val2, val2, val2); }
        if (sizeScaleCount >= 4) { times[3] = time3; values[3] = float3(val3, val3, val3); }
        if (sizeScaleCount >= 5) { times[4] = time4; values[4] = float3(val4, val4, val4); }
    }
}

float L2Fx_SpriteSizeScale_ScalarFromUniforms(
    float normalizedAge,
    float useSizeScale,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    bool implicitKeyZero,
    bool bUseRegularSizeScale,
    float time0, float val0,
    float time1, float val1,
    float time2, float val2,
    float time3, float val3,
    float time4, float val4)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float times[8];
    float3 values[8];
    L2Fx_SpriteSizeScale_BuildKeysFromUniforms(
        sizeScaleCount,
        implicitKeyZero,
        time0, val0,
        time1, val1,
        time2, val2,
        time3, val3,
        time4, val4,
        times,
        values);

    return L2Fx_SpriteSizeScale_Scalar(
        normalizedAge,
        sizeScaleRepeats,
        sizeScaleCount,
        times,
        values,
        bUseRegularSizeScale);
}

#endif // L2_FX_SPRITE_SIZE_SCALE_INCLUDED
