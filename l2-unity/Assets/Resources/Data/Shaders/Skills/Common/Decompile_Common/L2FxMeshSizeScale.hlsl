#ifndef L2_FX_MESH_SIZE_SCALE_INCLUDED
#define L2_FX_MESH_SIZE_SCALE_INCLUDED

// UMeshEmitter SizeScale curve sampler.
//
// CONFIRMED for LineageEffect.m_u004_b_2 / MeshEmitter6:
//   L2    lifeNorm=0.8933 startSize=0.0650 finalSize=0.3372
//   Unity lifeNorm=0.8924 startSize=0.0650 finalSize=0.3371
//
// CONFIRMED Keys6 + phase for LineageEffect.e_u031_a / MeshEmitter1 "MC"
//   (ParticleSnapshot 2026-07-20):
//   finalSize = StartSize * SizeScale(phase)
//   phase     = frac((SizeScaleParam + SizeScaleRepeats) * lifeNorm)
//   SizeScaleParam=1, SizeScaleRepeats=0 → phase=lifeNorm; delta=0 vs live.
//
// The generic curve primitive is shared with other emitters in
// L2FxEmitterSpawn.hlsl. This facade keeps MeshEmitter key semantics explicit:
// key 0 is authored (not SpriteEmitter's implicit zero key).

#include "../L2FxEmitterSpawn.hlsl"

void L2Fx_MeshSizeScale_BuildKeys5(
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4,
    out float times[8],
    out float3 values[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        values[i] = float3(1, 1, 1);
    }

    if (sizeScaleCount >= 1) { times[0] = time0; values[0] = value0.xxx; }
    if (sizeScaleCount >= 2) { times[1] = time1; values[1] = value1.xxx; }
    if (sizeScaleCount >= 3) { times[2] = time2; values[2] = value2.xxx; }
    if (sizeScaleCount >= 4) { times[3] = time3; values[3] = value3.xxx; }
    if (sizeScaleCount >= 5) { times[4] = time4; values[4] = value4.xxx; }
}

void L2Fx_MeshSizeScale_BuildKeys6(
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4,
    float time5, float value5,
    out float times[8],
    out float3 values[8])
{
    L2Fx_MeshSizeScale_BuildKeys5(
        sizeScaleCount,
        time0, value0,
        time1, value1,
        time2, value2,
        time3, value3,
        time4, value4,
        times,
        values);
    if (sizeScaleCount >= 6) { times[5] = time5; values[5] = value5.xxx; }
}

float L2Fx_MeshSizeScale_ScalarFromKeys5(
    float lifeNorm,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float times[8];
    float3 values[8];
    L2Fx_MeshSizeScale_BuildKeys5(
        sizeScaleCount,
        time0, value0,
        time1, value1,
        time2, value2,
        time3, value3,
        time4, value4,
        times,
        values);

    return L2Fx_SampleSizeScale(
        lifeNorm,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        times,
        values,
        useRegularSizeScale > 0.5).x;
}

float L2Fx_MeshSizeScale_ScalarFromKeys6(
    float lifeNorm,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4,
    float time5, float value5)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float times[8];
    float3 values[8];
    L2Fx_MeshSizeScale_BuildKeys6(
        sizeScaleCount,
        time0, value0,
        time1, value1,
        time2, value2,
        time3, value3,
        time4, value4,
        time5, value5,
        times,
        values);

    return L2Fx_SampleSizeScale(
        lifeNorm,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        times,
        values,
        useRegularSizeScale > 0.5).x;
}

float L2Fx_MeshSizeScale_Apply(
    float startSize,
    float lifeNorm,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4)
{
    return startSize * L2Fx_MeshSizeScale_ScalarFromKeys5(
        lifeNorm,
        useSizeScale,
        useRegularSizeScale,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        time0, value0,
        time1, value1,
        time2, value2,
        time3, value3,
        time4, value4);
}

float L2Fx_MeshSizeScale_ApplyKeys6(
    float startSize,
    float lifeNorm,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float time0, float value0,
    float time1, float value1,
    float time2, float value2,
    float time3, float value3,
    float time4, float value4,
    float time5, float value5)
{
    return startSize * L2Fx_MeshSizeScale_ScalarFromKeys6(
        lifeNorm,
        useSizeScale,
        useRegularSizeScale,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        time0, value0,
        time1, value1,
        time2, value2,
        time3, value3,
        time4, value4,
        time5, value5);
}

#endif // L2_FX_MESH_SIZE_SCALE_INCLUDED
