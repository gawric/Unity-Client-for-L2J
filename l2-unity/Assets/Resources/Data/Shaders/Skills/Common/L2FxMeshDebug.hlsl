#ifndef L2_FX_MESH_DEBUG_INCLUDED
#define L2_FX_MESH_DEBUG_INCLUDED

// Editor / Scene-view preview for mesh emitters.
// With _StartTime=0 and _HasLifetime=1, editor _Time.y makes age >> lifetime so the mesh
// disappears without Play. Enable _DebugMeshPreview and scrub _DebugMeshPreviewAge (pause),
// or enable _DebugMeshPreviewLoop to auto-play age on repeat via fmod(_Time.y, lifetime).
//
// Preview is active only while _StartTime ~= 0 (Scene/material asset). Play/runtime always
// sets _StartTime on spawn, so debug never overrides production timing in game.
//
// Caller must include L2FxMeshEmitterUrp.hlsl before this file.

#include "L2FxAtlasDebug.hlsl"

float L2Fx_MeshDebug_IsPreviewActive(float debugMeshPreview, float startTime)
{
    return (debugMeshPreview > 0.5 && startTime <= 1e-4) ? 1.0 : 0.0;
}

float L2Fx_MeshDebug_ResolvePreviewAge(
    float debugMeshPreviewLoop,
    float debugMeshPreviewAge,
    float currentTime,
    float lifetime)
{
    lifetime = max(lifetime, 1e-4);

    if (debugMeshPreviewLoop > 0.5)
    {
        return fmod(max(0.0, currentTime), lifetime);
    }

    return max(0.0, debugMeshPreviewAge);
}

void L2Fx_MeshDebug_ComputeTiming(
    float debugMeshPreview,
    float debugMeshPreviewLoop,
    float debugMeshPreviewAge,
    float hasLifetime,
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
    if (L2Fx_MeshDebug_IsPreviewActive(debugMeshPreview, startTime) < 0.5)
    {
        L2Fx_MeshBuiltin_ComputeTiming(
            currentTime, initialDelayRange, lifetimeRange, seed, startTime,
            delay, lifetime, age, ageNorm);

        // Test / buff mode: _HasLifetime off keeps alpha alive and loops age every lifetime cycle.
        if (hasLifetime < 0.5)
        {
            lifetime = max(lifetime, 1e-4);
            age = fmod(max(0.0, age), lifetime);
            ageNorm = saturate(age / lifetime);
        }

        return;
    }

    delay = L2Fx_RandomInitialDelay(initialDelayRange.xy, seed, startTime, 3.0);
    lifetime = L2Fx_RandomLifetime(lifetimeRange.xy, seed, startTime, 7.0);
    lifetime = max(lifetime, 1e-4);
    age = L2Fx_MeshDebug_ResolvePreviewAge(
        debugMeshPreviewLoop, debugMeshPreviewAge, currentTime, lifetime);
    ageNorm = saturate(age / lifetime);
}

float L2Fx_MeshDebug_LifetimeAlphaAtAge(
    float age,
    float lifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOutEnabled,
    float fadeoutStartTime)
{
    float lt = max(0.0001, lifetime);
    age = max(0.0, age);

    float fadeInMul = 1.0;
    if (fadeIn >= 0.5)
    {
        float fadeInEnd = max(0.0001, fadeInEndTime);
        fadeInMul = saturate(age / fadeInEnd);
    }

    float fadeOutMul = 1.0;
    if (fadeOutEnabled >= 0.5)
    {
        float fadeStart = clamp(fadeoutStartTime, 0.0, lt);
        float fadeDuration = max(0.0001, lt - fadeStart);
        float fadeT = saturate((age - fadeStart) / fadeDuration);
        fadeOutMul = 1.0 - fadeT;
    }

    return saturate(fadeInMul * fadeOutMul);
}

float L2Fx_MeshDebug_LifetimeAlpha(
    float debugMeshPreview,
    float previewAge,
    float timeY,
    float hasLifetime,
    float startTime,
    float initialDelay,
    float lifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeOutEnabled,
    float fadeoutStartTime)
{
    if (L2Fx_MeshDebug_IsPreviewActive(debugMeshPreview, startTime) > 0.5)
    {
        return L2Fx_MeshDebug_LifetimeAlphaAtAge(
            previewAge,
            lifetime,
            fadeIn,
            fadeInEndTime,
            fadeOutEnabled,
            fadeoutStartTime);
    }

    // disableShaderLifetime / buff loop: use previewAge from vert (already fmod-looped in ComputeTiming).
    if (hasLifetime < 0.5)
    {
        lifetime = max(lifetime, 1e-4);
        return L2Fx_MeshDebug_LifetimeAlphaAtAge(
            max(0.0, previewAge),
            lifetime,
            fadeIn,
            fadeInEndTime,
            fadeOutEnabled,
            fadeoutStartTime);
    }

    return L2Fx_LifetimeAlpha(
        timeY, hasLifetime, startTime, initialDelay, lifetime,
        fadeIn, fadeInEndTime, fadeOutEnabled, fadeoutStartTime);
}

#endif
