#ifndef L2_FX_MESH_EMITTER_INCLUDED
#define L2_FX_MESH_EMITTER_INCLUDED

// Reusable Unreal/Lineage MeshEmitter helpers.
// Geometry still comes from Unity MeshFilter/Renderer; these helpers reproduce
// per-emitter mesh scale, spin and ColorScale behavior from .uc data.
#include "L2FxParticleAnim.hlsl"

float3 L2Fx_MeshEmitterLocalPosition(
    float3 positionOS,
    float normalizedAge,
    float ageSeconds,
    float2 sizeRangeX,
    float2 sizeRangeY,
    float2 sizeRangeZ,
    float uniformSize,
    float useSizeScale,
    float4 sizeScale0,
    float4 sizeScale1,
    float4 sizeScale2,
    float spinParticles,
    float2 startSpinX,
    float2 startSpinY,
    float2 startSpinZ,
    float2 spinsPerSecondX,
    float2 spinsPerSecondY,
    float2 spinsPerSecondZ,
    float spinSpeedMultiplier,
    float applyStartSizeToMesh,
    float transformCarriesStartSize,
    float meshScaleMultiplier,
    float seed,
    float startTime)
{
    float3 baseSize = L2Fx_StartSize(sizeRangeX, sizeRangeY, sizeRangeZ, uniformSize, seed, startTime);
    float sizeScale = L2Fx_SizeScale(normalizedAge, useSizeScale, sizeScale0, sizeScale1, sizeScale2);
    float3 startSizeScale = baseSize * sizeScale;
    float3 meshScale = lerp(float3(1.0, 1.0, 1.0), startSizeScale, saturate(applyStartSizeToMesh));
    float3 localPos = positionOS * meshScale * meshScaleMultiplier;

    if (spinParticles > 0.5)
    {
        float3 angles = L2Fx_RotationAngles(
            ageSeconds,
            startSpinX,
            startSpinY,
            startSpinZ,
            spinsPerSecondX * spinSpeedMultiplier,
            spinsPerSecondY * spinSpeedMultiplier,
            spinsPerSecondZ * spinSpeedMultiplier,
            seed,
            startTime);

        if (applyStartSizeToMesh < 0.5 && transformCarriesStartSize > 0.5)
        {
            float3 safeScale = max(abs(startSizeScale), float3(1e-4, 1e-4, 1e-4));
            float3 sizedPos = positionOS * startSizeScale;
            sizedPos = L2Fx_RotateX(sizedPos, angles.x);
            sizedPos = L2Fx_RotateY(sizedPos, angles.y);
            sizedPos = L2Fx_RotateZ(sizedPos, angles.z);
            return (sizedPos / safeScale) * meshScaleMultiplier;
        }

        localPos = L2Fx_RotateX(localPos, angles.x);
        localPos = L2Fx_RotateY(localPos, angles.y);
        localPos = L2Fx_RotateZ(localPos, angles.z);
    }

    return localPos;
}

float3 L2Fx_ColorScaleThreeKeysRepeating(
    float normalizedAge,
    float useColorScale,
    float colorScale0Time,
    float colorScale1Time,
    float colorScale2Time,
    float colorScaleRepeats,
    float legacyColorScaleRepeats,
    float3 color0,
    float3 color1,
    float3 color2)
{
    if (useColorScale < 0.5)
    {
        return float3(1.0, 1.0, 1.0);
    }

    float repeats = colorScaleRepeats > 0.5 ? colorScaleRepeats : max(0.0, legacyColorScaleRepeats);
    float t = repeats > 0.5 ? frac(normalizedAge * repeats) : saturate(normalizedAge);
    float t0 = saturate(colorScale0Time);
    float t1 = max(t0 + 1e-4, saturate(colorScale1Time));
    float t2 = max(t1 + 1e-4, saturate(colorScale2Time));

    if (t <= t1)
    {
        return lerp(color0, color1, saturate((t - t0) / (t1 - t0)));
    }

    return lerp(color1, color2, saturate((t - t1) / (t2 - t1)));
}

#endif
