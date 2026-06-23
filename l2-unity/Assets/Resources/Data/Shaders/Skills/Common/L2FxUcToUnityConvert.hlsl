#ifndef L2_FX_UC_TO_UNITY_CONVERT_INCLUDED
#define L2_FX_UC_TO_UNITY_CONVERT_INCLUDED

// Conversion layer for new dedicated shaders:
// raw .uc material values -> Unity-ready values consumed by size/spin helpers.

#include "L2FxMeshParticleMotion.hlsl"

struct L2Fx_UcToUnityMeshConvertData
{
    float applyUuToStartSize;
    float spawnUnitScale;
    float effectScale;
    float meshScale;
    float meshSpinDirection;
};

struct L2Fx_UcToUnitySpriteConvertData
{
    float effectScale;
    float spriteScale;
};

float L2Fx_UcToUnitySafePositiveScale(float value)
{
    return value > 0.0 ? value : 1.0;
}

// Per-effect correction layer for values intentionally stored as raw .uc units
// in materials. Keep authored UC values visible in the inspector, then apply
// measured Unity compensation before the usual UE-axis/world conversion.
float L2Fx_UcToUnityApplyScale(float value, float scale)
{
    return value * scale;
}

float2 L2Fx_UcToUnityApplyScale2(float2 value, float scale)
{
    return value * scale;
}

float3 L2Fx_UcToUnityApplyScale3(float3 value, float scale)
{
    return value * scale;
}

float L2Fx_UcToUnitySpinDirection(float value)
{
    if (value > 0.0)
    {
        return 1.0;
    }

    if (value < 0.0)
    {
        return -1.0;
    }

    return 1.0;
}

float3 L2Fx_UcToUnityMeshSize(
    float3 startSizeUe,
    L2Fx_UcToUnityMeshConvertData data)
{
    float3 startSizeUnity = L2Fx_UeVectorToUnity(startSizeUe);
    if (data.applyUuToStartSize > 0.5)
    {
        startSizeUnity *= data.spawnUnitScale;
    }

    return startSizeUnity
        * L2Fx_UcToUnitySafePositiveScale(data.effectScale)
        * L2Fx_UcToUnitySafePositiveScale(data.meshScale);
}

float3 L2Fx_UcToUnitySpriteSize(
    float3 baseSizeUnity,
    L2Fx_UcToUnitySpriteConvertData data)
{
    return baseSizeUnity
        * L2Fx_UcToUnitySafePositiveScale(data.effectScale)
        * L2Fx_UcToUnitySafePositiveScale(data.spriteScale);
}

float3 L2Fx_UcToUnityStartLocationOffset(
    float3 startLocationOffsetUe,
    L2Fx_UcToUnityMeshConvertData data)
{
    return L2Fx_UeVectorToUnity(startLocationOffsetUe) * data.spawnUnitScale;
}

float L2Fx_UcToUnityMeshSpinRate(
    float spinsPerSecond,
    L2Fx_UcToUnityMeshConvertData data)
{
    return spinsPerSecond * L2Fx_UcToUnitySpinDirection(data.meshSpinDirection);
}

#endif // L2_FX_UC_TO_UNITY_CONVERT_INCLUDED
