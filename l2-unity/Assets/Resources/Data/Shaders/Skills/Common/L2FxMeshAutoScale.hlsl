#ifndef L2_FX_MESH_AUTOSCALE_INCLUDED
#define L2_FX_MESH_AUTOSCALE_INCLUDED

// Shared MeshEmitter size policy for new dedicated HLSL shaders.
// MeshEmitter StartSizeRange is treated as mesh-local scale by default; only
// opt into UU->meter conversion for shaders/materials that explicitly need it.

#include "L2FxUcToUnityConvert.hlsl"

float L2Fx_MeshAutoScaleSafeMultiplier(float value)
{
    return L2Fx_UcToUnitySafePositiveScale(value);
}

float3 L2Fx_MeshAutoScaleResolveStartSize(
    float3 startSizeUe,
    float applyUuToStartSize,
    float spawnUnitScale)
{
    L2Fx_UcToUnityMeshConvertData data;
    data.applyUuToStartSize = applyUuToStartSize;
    data.spawnUnitScale = spawnUnitScale;
    data.effectScale = 1.0;
    data.meshScale = 1.0;
    data.meshSpinDirection = 1.0;

    return L2Fx_UcToUnityMeshSize(startSizeUe, data);
}

float3 L2Fx_MeshAutoScaleApply(
    float3 startSizeUnity,
    float effectScale,
    float meshScale)
{
    L2Fx_UcToUnityMeshConvertData data;
    data.applyUuToStartSize = 0.0;
    data.spawnUnitScale = 1.0;
    data.effectScale = effectScale;
    data.meshScale = meshScale;
    data.meshSpinDirection = 1.0;

    return startSizeUnity
        * L2Fx_UcToUnitySafePositiveScale(data.effectScale)
        * L2Fx_UcToUnitySafePositiveScale(data.meshScale);
}

float3 L2Fx_MeshAutoScaleResolveAndApply(
    float3 startSizeUe,
    float applyUuToStartSize,
    float spawnUnitScale,
    float effectScale,
    float meshScale)
{
    L2Fx_UcToUnityMeshConvertData data;
    data.applyUuToStartSize = applyUuToStartSize;
    data.spawnUnitScale = spawnUnitScale;
    data.effectScale = effectScale;
    data.meshScale = meshScale;
    data.meshSpinDirection = 1.0;

    return L2Fx_UcToUnityMeshSize(startSizeUe, data);
}

#endif // L2_FX_MESH_AUTOSCALE_INCLUDED
