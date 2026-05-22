#ifndef L2_FX_MESH_EMITTER_VERTEX_INCLUDED
#define L2_FX_MESH_EMITTER_VERTEX_INCLUDED

// Pipeline-agnostic mesh emitter vertex math (curse magic circle, ground shadow).
// Include from URP shaders via L2FxMeshEmitterUrp.hlsl (after Core.hlsl).

#include "L2FxEmitterSpawn.hlsl"
#include "L2FxMeshParticleMotion.hlsl"

void L2Fx_MeshBuiltin_ComputeTiming(
    float currentTime,
    float4 initialDelayRange,
    float4 lifetimeRange,
    float seed,
    float startTime,
    out float delay,
    out float lifetime,
    out float age,
    out float ageNorm)
{
    delay = L2Fx_RandomInitialDelay(initialDelayRange.xy, seed, startTime, 3.0);
    lifetime = L2Fx_RandomLifetime(lifetimeRange.xy, seed, startTime, 7.0);
    age = L2Fx_AgeSeconds(currentTime, startTime, delay);
    ageNorm = saturate(age / max(lifetime, 1e-4));
}

float L2Fx_MeshBuiltin_ColorScaleRepeatsParam(float colorScaleRepeats)
{
    return max(colorScaleRepeats, 1.0) - 1.0;
}

void L2Fx_MeshBuiltin_BuildColorScaleArrays(
    uint colorScaleCount,
    float4 colorScale0,
    float colorScaleTime1,
    float4 colorScale1,
    float colorScaleTime2,
    float4 colorScale2,
    out float times[8],
    out float4 colors[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        colors[i] = float4(1, 1, 1, 1);
    }

    times[0] = 0.0;
    colors[0] = colorScale0;

    if (colorScaleCount >= 2)
    {
        times[1] = colorScaleTime1;
        colors[1] = colorScale1;
    }

    if (colorScaleCount >= 3)
    {
        times[2] = colorScaleTime2;
        colors[2] = colorScale2;
    }
}

void L2Fx_MeshBuiltin_BuildSizeScaleArrays(
    uint sizeScaleCount,
    float sizeScaleTime0, float sizeScaleVal0,
    float sizeScaleTime1, float sizeScaleVal1,
    float sizeScaleTime2, float sizeScaleVal2,
    float sizeScaleTime3, float sizeScaleVal3,
    float sizeScaleTime4, float sizeScaleVal4,
    out float times[8],
    out float3 values[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        values[i] = float3(1, 1, 1);
    }

    times[0] = sizeScaleTime0;
    values[0] = sizeScaleVal0.xxx;

    if (sizeScaleCount >= 2)
    {
        times[1] = sizeScaleTime1;
        values[1] = sizeScaleVal1.xxx;
    }

    if (sizeScaleCount >= 3)
    {
        times[2] = sizeScaleTime2;
        values[2] = sizeScaleVal2.xxx;
    }

    if (sizeScaleCount >= 4)
    {
        times[3] = sizeScaleTime3;
        values[3] = sizeScaleVal3.xxx;
    }

    if (sizeScaleCount >= 5)
    {
        times[4] = sizeScaleTime4;
        values[4] = sizeScaleVal4.xxx;
    }
}

float L2Fx_MeshBuiltin_SampleSizeScaleScalar(
    float ageNorm,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleTime0, float sizeScaleVal0,
    float sizeScaleTime1, float sizeScaleVal1,
    float sizeScaleTime2, float sizeScaleVal2,
    float sizeScaleTime3, float sizeScaleVal3,
    float sizeScaleTime4, float sizeScaleVal4)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float stimes[8];
    float3 svals[8];
    L2Fx_MeshBuiltin_BuildSizeScaleArrays(
        sizeScaleCount,
        sizeScaleTime0, sizeScaleVal0,
        sizeScaleTime1, sizeScaleVal1,
        sizeScaleTime2, sizeScaleVal2,
        sizeScaleTime3, sizeScaleVal3,
        sizeScaleTime4, sizeScaleVal4,
        stimes,
        svals);

    float3 ss = L2Fx_SampleSizeScale(
        ageNorm,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        stimes,
        svals,
        (useRegularSizeScale > 0.5));

    return ss.x;
}

float3 L2Fx_MeshBuiltin_StartLocationOffsetM(float3 startLocationOffsetUU)
{
    return float3(
        startLocationOffsetUU.x,
        startLocationOffsetUU.z,
        startLocationOffsetUU.y) * L2FX_UU_TO_UNITY;
}

void L2Fx_MeshBuiltin_TransformVertexOS(
    inout float3 posOS,
    inout float3 nrmOS,
    float spinParticles,
    float startSpin,
    float spinsPerSecond,
    float spinCCWorCW,
    float age,
    float ageNorm,
    float3 startSize,
    float applyUuToStartSize,
    float useSizeScale,
    float useRegularSizeScale,
    float sizeScaleParam,
    float sizeScaleRepeats,
    uint sizeScaleCount,
    float sizeScaleTime0, float sizeScaleVal0,
    float sizeScaleTime1, float sizeScaleVal1,
    float sizeScaleTime2, float sizeScaleVal2,
    float sizeScaleTime3, float sizeScaleVal3,
    float sizeScaleTime4, float sizeScaleVal4,
    float3 startLocationOffsetUU,
    float meshYOffset,
    float sizeScaleHorizontalOnly)
{
    if (spinParticles > 0.5)
    {
        float sps = L2Fx_ApplySpinCCWorCW_Scalar(spinsPerSecond, spinCCWorCW);
        float angle = L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(startSpin, sps, age);
        L2Fx_ApplyMeshSpinAroundY(posOS, nrmOS, true, angle);
    }

    float sizeScale = L2Fx_MeshBuiltin_SampleSizeScaleScalar(
        ageNorm,
        sizeScaleParam,
        sizeScaleRepeats,
        sizeScaleCount,
        useSizeScale,
        useRegularSizeScale,
        sizeScaleTime0, sizeScaleVal0,
        sizeScaleTime1, sizeScaleVal1,
        sizeScaleTime2, sizeScaleVal2,
        sizeScaleTime3, sizeScaleVal3,
        sizeScaleTime4, sizeScaleVal4);

    float3 startSizeM = startSize;
    if (applyUuToStartSize > 0.5)
    {
        startSizeM *= L2FX_UU_TO_UNITY;
    }

    float3 sizeMul = startSizeM * sizeScale;
    if (sizeScaleHorizontalOnly > 0.5)
    {
        sizeMul = float3(startSizeM.x * sizeScale, startSizeM.y, startSizeM.z * sizeScale);
    }

    posOS *= sizeMul;
    posOS += L2Fx_MeshBuiltin_StartLocationOffsetM(startLocationOffsetUU);
    posOS.y += meshYOffset;
}

float2 L2Fx_MeshBuiltin_ApplyMainTexST(float2 uv01, float4 mainTexST)
{
    return uv01 * mainTexST.xy + mainTexST.zw;
}

float2 L2Fx_MeshBuiltin_ApplyAtlasRemap(float2 uv01, float atlasUvRemap, float4 atlasUvMinMax)
{
    if (atlasUvRemap > 0.5)
    {
        return lerp(atlasUvMinMax.xy, atlasUvMinMax.zw, saturate(uv01));
    }

    return uv01;
}

void L2Fx_MeshBuiltin_ResolveUv(
    float2 meshUv,
    float3 positionOS,
    float usePlanarMeshUv,
    float planarUvScale,
    float planarUvUseXZ,
    float planarUvNormalizeExtents,
    float4 planarUvMeshHalfExtents,
    float4 mainTexST,
    float atlasUvRemap,
    float4 atlasUvMinMax,
    out float2 uvSample,
    out float2 uvMesh)
{
    float2 uv01;
    if (usePlanarMeshUv < 0.5)
    {
        uv01 = meshUv;
    }
    else
    {
        float2 mp = (planarUvUseXZ > 0.5) ? positionOS.xz : positionOS.xy;
        if (planarUvNormalizeExtents > 0.5)
        {
            float2 he = max(planarUvMeshHalfExtents.xy, float2(1e-5, 1e-5));
            float len2 = dot(mp, mp);
            uv01 = (len2 < 1e-12) ? float2(0.5, 0.5) : (mp / (2.0 * he) + float2(0.5, 0.5));
        }
        else
        {
            uv01 = mp * planarUvScale + float2(0.5, 0.5);
        }
    }

    uvMesh = uv01;
    uv01 = L2Fx_MeshBuiltin_ApplyAtlasRemap(uv01, atlasUvRemap, atlasUvMinMax);
    uvSample = L2Fx_MeshBuiltin_ApplyMainTexST(uv01, mainTexST);
}

float4 L2Fx_MeshBuiltin_SampleBaseTint(
    float ageNorm,
    float colorScaleRepeats,
    uint colorScaleCount,
    float4 colorScale0,
    float colorScaleTime1,
    float4 colorScale1,
    float colorScaleTime2,
    float4 colorScale2,
    float bAlphaBlend,
    float3 colorMultMin,
    float3 colorMultMax,
    float opacity,
    float emitterAlpha)
{
    float ctimes[8];
    float4 ccols[8];
    L2Fx_MeshBuiltin_BuildColorScaleArrays(
        colorScaleCount,
        colorScale0,
        colorScaleTime1,
        colorScale1,
        colorScaleTime2,
        colorScale2,
        ctimes,
        ccols);

    float csParam = L2Fx_MeshBuiltin_ColorScaleRepeatsParam(colorScaleRepeats);
    float4 color = L2Fx_SampleColorScale(
        ageNorm,
        csParam,
        colorScaleCount,
        ctimes,
        ccols,
        (bAlphaBlend > 0.5));

    color.rgb = L2Fx_ApplyColorMultiplier(
        color.rgb,
        colorMultMin,
        colorMultMax,
        1.0,
        0.0);

    color.a *= opacity * emitterAlpha;
    return color;
}

#endif // L2_FX_MESH_EMITTER_VERTEX_INCLUDED
