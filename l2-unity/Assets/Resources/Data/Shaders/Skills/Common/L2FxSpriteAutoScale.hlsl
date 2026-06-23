#ifndef L2_FX_SPRITE_AUTOSCALE_INCLUDED
#define L2_FX_SPRITE_AUTOSCALE_INCLUDED

// Shared SpriteEmitter size policy for new dedicated HLSL shaders.
// SpriteEmitter StartSizeRange stays in raw UE units; L2Fx_StartSize applies
// the project UU->Unity conversion before these multipliers are applied.

#include "L2FxEmitterSpawn.hlsl"
#include "L2FxUcToUnityConvert.hlsl"

float L2Fx_SpriteAutoScaleSafeMultiplier(float value)
{
    return L2Fx_UcToUnitySafePositiveScale(value);
}

float3 L2Fx_SpriteAutoScaleApply(
    float3 baseSizeUnity,
    float effectScale,
    float spriteScale)
{
    L2Fx_UcToUnitySpriteConvertData data;
    data.effectScale = effectScale;
    data.spriteScale = spriteScale;

    return L2Fx_UcToUnitySpriteSize(baseSizeUnity, data);
}

float3 L2Fx_SpriteAutoScaleStartSize(
    float2 sizeRangeX,
    float2 sizeRangeY,
    float2 sizeRangeZ,
    bool uniformSize,
    float seed,
    float startTime,
    float effectScale,
    float spriteScale)
{
    float3 baseSizeUnity = L2Fx_StartSize(
        sizeRangeX,
        sizeRangeY,
        sizeRangeZ,
        uniformSize,
        seed,
        startTime);

    return L2Fx_SpriteAutoScaleApply(baseSizeUnity, effectScale, spriteScale);
}

#endif // L2_FX_SPRITE_AUTOSCALE_INCLUDED
