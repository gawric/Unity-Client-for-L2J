#ifndef L2_FX_MESH_SIZE_SCALE_INCLUDED
#define L2_FX_MESH_SIZE_SCALE_INCLUDED

// UMeshEmitter SizeScale curve sampler.
//
// CONFIRMED for LineageEffect.m_u004_b_2 / MeshEmitter6:
//   L2    lifeNorm=0.8933 startSize=0.0650 finalSize=0.3372
//   Unity lifeNorm=0.8924 startSize=0.0650 finalSize=0.3371
//
// The matching engine behavior is:
//   finalSize = StartSize * SizeScale(phase)
//   phase     = frac((SizeScaleParam + SizeScaleRepeats) * lifeNorm)
// For the verified emitter, SizeScaleParam=1 and SizeScaleRepeats=0,
// so its explicit [0..1] curve is sampled once per lifetime.
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

#endif // L2_FX_MESH_SIZE_SCALE_INCLUDED
