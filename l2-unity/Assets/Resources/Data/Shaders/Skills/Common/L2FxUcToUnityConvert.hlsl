#ifndef L2_FX_UC_TO_UNITY_CONVERT_INCLUDED
#define L2_FX_UC_TO_UNITY_CONVERT_INCLUDED

// Conversion layer for new dedicated shaders:
// raw .uc material values -> Unity-ready values consumed by size/spin helpers.

#include "L2FxMeshParticleMotion.hlsl"
#include "L2FxSpawnRegionDebug.hlsl"

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

// Polar/box spawn region from raw .uc (StartLocationPolarRange + StartLocation*).
struct L2Fx_UcToUnitySpriteSpawnData
{
    float2 azimuthDegMinMax;
    float2 polarPitchDegMinMax;
    float2 radiusMinMax;
    float3 startLocationOffsetUe;
    float3 startLocationRangeMinUe;
    float3 startLocationRangeMaxUe;
    float ucPolarRadiusScale;
    float ucStartLocationOffsetScale;
    float ucStartLocationRangeScale;
    float spawnUnitScale;
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

// Post-UC anisotropic calibration (RenderDoc L2 vs Unity). Keep UC StartSizeRange unchanged;
// tune only via widthScale / heightScale on the material (PTDU_Up: X=sheet width, Y=sheet height).
float3 L2Fx_UcToUnitySpriteAnisotropicTune(float3 sizeM, float widthScale, float heightScale)
{
    float w = widthScale > 0.0 ? widthScale : 1.0;
    float h = heightScale > 0.0 ? heightScale : 1.0;
    return float3(sizeM.x * w, sizeM.y * h, sizeM.z * h);
}

float L2Fx_UcToUnityResolveSpawnUnitScale(float spawnUnitScale)
{
    return spawnUnitScale > 0.0 ? spawnUnitScale : L2FX_UU_TO_UNITY;
}

float3 L2Fx_UcToUnitySpriteSpawnOffset(
    L2Fx_UcToUnitySpriteSpawnData spawn,
    float seed,
    float startTime)
{
    float3 offsetUe = L2Fx_UcToUnityApplyScale3(
        spawn.startLocationOffsetUe,
        spawn.ucStartLocationOffsetScale);
    float3 posUe = L2Fx_SpawnRegionOffsetUe(
        spawn.azimuthDegMinMax,
        spawn.polarPitchDegMinMax,
        L2Fx_UcToUnityApplyScale2(spawn.radiusMinMax, spawn.ucPolarRadiusScale),
        offsetUe,
        L2Fx_UcToUnityApplyScale3(spawn.startLocationRangeMinUe, spawn.ucStartLocationRangeScale),
        L2Fx_UcToUnityApplyScale3(spawn.startLocationRangeMaxUe, spawn.ucStartLocationRangeScale),
        seed,
        startTime);

    return L2Fx_UeVectorToUnity(posUe) * L2Fx_UcToUnityResolveSpawnUnitScale(spawn.spawnUnitScale);
}

float3 L2Fx_UcToUnitySpriteStartSize(
    float2 sizeRangeX,
    float2 sizeRangeY,
    float2 sizeRangeZ,
    bool uniformSize,
    float seed,
    float startTime,
    L2Fx_UcToUnitySpriteConvertData data)
{
    float3 baseSizeUnity = L2Fx_StartSize(
        float3(sizeRangeX.x, sizeRangeY.x, sizeRangeZ.x),
        float3(sizeRangeX.y, sizeRangeY.y, sizeRangeZ.y),
        uniformSize,
        seed,
        startTime);

    return L2Fx_UcToUnitySpriteSize(baseSizeUnity, data);
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
