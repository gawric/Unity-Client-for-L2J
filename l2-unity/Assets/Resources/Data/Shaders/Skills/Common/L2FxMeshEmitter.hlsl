#ifndef L2_FX_MESH_EMITTER_INCLUDED
#define L2_FX_MESH_EMITTER_INCLUDED

// ═══════════════════════════════════════════════════════════════
// L2_FX_MESH_EMITTER — UE3 UMeshEmitter, Unity-corrected
//
// Based on UE3 disassembly:
//
//   Mesh_WorldMatrix_Construction:
//     W = T(pos) * R(spinAngle) * R(right,up) * S(size)
//     size = StartSize * Scale3D * DrawScale
//
//   Particle_RotationAxes (FParticle):
//     +0xC8 RightAxis    +0xD4 UpAxis
//
//   Vertex_CalcSpinAngle:
//     Angle = (StartSpin + SpinsPerSecond * Age) * SPIN_TO_RAD
//
// Depends on: L2_FX_EMITTER_SPAWN_INCLUDED
// ═══════════════════════════════════════════════════════════════

#include "L2FxEmitterSpawn.hlsl"
#include "L2FxParticleAnim.hlsl"

#define L2FX_MESH_SPIN_TO_RAD L2FX_SPIN_TO_RAD

// ───────────────────────────────────────────────────────────────
// Mesh world matrix — TRS order: T * Rspin * Raxes * S
// ───────────────────────────────────────────────────────────────

float4x4 L2Fx_MeshWorldMatrix(
    float3 worldPos,
    float spinAngleRad,
    float3 particleRight,
    float3 particleUp,
    float3 startSize, // already in Unity meters (× UU_TO_UNITY from Spawn)
    float3 scale3D,
    float drawScale)
{
    // Scale
    float3 finalSize = startSize * scale3D * drawScale;
    float4x4 S = float4x4(
        finalSize.x, 0, 0, 0,
        0, finalSize.y, 0, 0,
        0, 0, finalSize.z, 0,
        0, 0, 0, 1);

    // Axes
    float3 forward = normalize(cross(particleRight, particleUp));
    float3 right = normalize(particleRight);
    float3 up = normalize(cross(forward, right));

    float4x4 Raxes = float4x4(
        right.x, up.x, forward.x, 0,
        right.y, up.y, forward.y, 0,
        right.z, up.z, forward.z, 0,
        0, 0, 0, 1);

    // Spin (around local Z)
    float c = cos(spinAngleRad);
    float s = sin(spinAngleRad);
    float4x4 Rspin = float4x4(
        c, -s, 0, 0,
        s, c, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    // Translation
    float4x4 T = float4x4(
        1, 0, 0, worldPos.x,
        0, 1, 0, worldPos.y,
        0, 0, 1, worldPos.z,
        0, 0, 0, 1);

    return mul(T, mul(Rspin, mul(Raxes, S)));
}

// ───────────────────────────────────────────────────────────────
// Init particle axes at spawn
// ───────────────────────────────────────────────────────────────

void L2Fx_MeshInitAxes(
    int useRotationFrom,
    float4x4 actorRotation,
    out float3 right,
    out float3 up)
{
    if (useRotationFrom == 1)
    {
        right = normalize(float3(actorRotation[0].x, actorRotation[0].y, actorRotation[0].z));
        up = normalize(float3(actorRotation[1].x, actorRotation[1].y, actorRotation[1].z));
    }
    else
    {
        right = float3(1, 0, 0);
        up = float3(0, 1, 0);
    }
}

// ───────────────────────────────────────────────────────────────
// Mesh particle color pipeline
// ───────────────────────────────────────────────────────────────

float4 L2Fx_MeshComputeColor(
    float normalizedAge,
    float colorScaleParam, uint colorScaleCount,
    float colorScaleTimes[8], float4 colorScaleColors[8],
    bool bAlphaBlend,
    float fadeInTime, float4 fadeInColor,
    float fadeOutTime, float4 fadeOutColor,
    bool forcedFade,
    float3 colorMultMin, float3 colorMultMax,
    float seed, float startTime,
    float opacity, float emitterAlpha)
{
    float4 col = L2Fx_SampleColorScale(
        normalizedAge, colorScaleParam,
        colorScaleCount, colorScaleTimes, colorScaleColors, bAlphaBlend);

    col = L2Fx_ApplyFadeInOut(
        col, normalizedAge, fadeInTime, fadeInColor,
        fadeOutTime, fadeOutColor, forcedFade);

    col.rgb = L2Fx_ApplyColorMultiplier(
        col.rgb, colorMultMin, colorMultMax, seed, startTime);

    col.a *= opacity * emitterAlpha;
    return saturate(col);
}

#endif // L2_FX_MESH_EMITTER_INCLUDED