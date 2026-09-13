#ifndef L2_FX_HE_VERT_MESH_INCLUDED
#define L2_FX_HE_VERT_MESH_INCLUDED

// =============================================================================
// UVertMeshEmitter — High Elf (Engine_essens_high_elves.dll)
// Target UC: d_mon_fire_ta VertMeshEmitter47 / VertexMesh'LineageEffectMeshes.sh2'
//
// Visual "motion / expansion" is NOT particle loc/vel/SizeScale (live: all zero /
// size locked 0.12). It is UVertMeshInstance vertex-frame animation (UE1 VertMesh).
// =============================================================================
//
// ASM map (verified 2026-09-10):
//
// UVertMeshEmitter::UpdateParticles
//   1) UParticleEmitter::UpdateParticles(dt)     // pool physics (here: idle)
//   2) for i in [0, min(Active, Cap)):
//        inst = GetMeshInstance(i)
//        inst+0xCC = animTime[i]                  // float* @ emitter+0x524
//        vcall inst[+0xB0](dt)  == UpdateAnimation(dt)
//        animTime[i] = inst+0xCC                  // write back
//
// UVertMeshInstance::UpdateAnimation(dt)
//   advances anim clock at instance+0xCC:
//     if rate(+0xC8) >= 0:  time += rate * dt
//     else: speed-scaled path from Actor velocity
//
// UVertMeshInstance::GetFrame(...)
//   GetAnimSeq(FName @+0xC4) → FMeshAnimSeq
//   uses time @+0xCC to pick/tween vertex frames into FVector* out
//   (classic VertMesh morph: verts move → looks like expand/swirl)
//
// UVertMeshInstance::RenderPreProcess(FAnimMeshVertexStream&)
//   copies animated verts into GPU stream; called from VertMeshEmitter::Render
//
// UVertMeshEmitter::SpawnParticle
//   GetMeshInstance; vcall inst[+0xB4](..., Owner+0x410, VertexMesh+0x20, ...)
//   animTime[slot]=0; extraFloat[slot]=0
//
// Live ParticlePhysicsLog 2026-09-10 (d_mon_fire_ta VertMesh47 / sh2):
//   UC_SCALE_PROBE sizeRatio=1.000 → RAW_UC_no_bake
//   StartSize=0.12 constant; loc/vel=0; Owner+0x410=1.0
//   animTime advances (~0.04 → 0.42) while particle spin URU stays 0
//   FadeInBool@+0xF4=0 but FadeInEnd>0 — ColorFade still runs (Brighten path)
//
// Hook dumps VERTMESH_FRAME_BANK (all frames) + NORMAL_BANK + instance recon vs tweenA.
// =============================================================================

#include "../L2FxMeshSpawnParticle.hlsl"
#include "../L2FxMeshSpin.hlsl"
#include "../L2FxMeshSizeScale.hlsl"
#include "../L2FxMeshColorFade.hlsl"
#include "../L2FxMeshMotion.hlsl"
#include "../../L2FxCoreGeometryTest.hlsl"

// Owner+0x410 confirmed 1.0 for fire_ta VertMesh — no instance scale multiply needed.
//
// GetFrame (ASM / live): scaled = animTime * frameCount;
//   i0 = floor(scaled); alpha = frac(scaled);
//   f0 = (startVert + i0) % N; f1 = (startVert + i0 + 1) % N;
//   V = V[f0] + (V[f1]-V[f0])*alpha
// Instance rate = seqRate / frameCount (sh2: 30/21). animTime 0→~1 over life 0.7.
// Frame XYZ bank: L2VertMeshFrameBank / sh2_frame_positions (VAT: u=vert, v=frame).

float L2FxHE_VertMesh_ResolveUnitScale(float heUnitScaleEnable, float heUnitScale)
{
    // fire_ta VertMesh live ratio=1.0 (no HE bake). Keep passthrough unless probe says bake.
    return (heUnitScaleEnable > 0.5 && heUnitScale > 1e-6) ? heUnitScale : 1.0;
}

float3 L2FxHE_VertMesh_StartSizeUu(float3 sizeRangeSampleUu, float heUnit)
{
    // Always pass heUnitScaleEnable=0 for fire_ta VertMesh (RAW_UC).
    return sizeRangeSampleUu * L2FxHE_VertMesh_ResolveUnitScale(0.0, heUnit);
}

// Advance instance animTime (UVertMeshInstance::UpdateAnimation, rate >= 0 path).
float L2FxHE_VertMesh_AdvanceAnimTime(float animTime, float seqRate, float frameCount, float dt)
{
    float rate = (frameCount > 1e-6) ? (seqRate / frameCount) : 0.0;
    return animTime + rate * dt;
}

void L2FxHE_VertMesh_GetFrameIndices(
    float animTime,
    float frameCount,
    out float frame0,
    out float frame1,
    out float alpha)
{
    float n = max(frameCount, 1.0);
    float scaled = max(animTime, 0.0) * n;
    float i0 = floor(scaled);
    alpha = scaled - i0;
    float i0m = i0 - floor(i0 / n) * n; // i0 % n for non-neg
    frame0 = i0m;
    frame1 = i0m + 1.0;
    if (frame1 >= n)
    {
        frame1 = 0.0;
    }
}

// Sample VAT: width=vertCount, height=frameCount, RGB=XYZ in UU (point / Load).
float3 L2FxHE_VertMesh_SampleFramePos(Texture2D posTex, float vertIndex, float frameIndex)
{
    return posTex.Load(int3((int)vertIndex, (int)frameIndex, 0)).xyz;
}

float3 L2FxHE_VertMesh_GetFramePos(
    Texture2D posTex,
    float vertIndex,
    float animTime,
    float frameCount)
{
    float f0, f1, a;
    L2FxHE_VertMesh_GetFrameIndices(animTime, frameCount, f0, f1, a);
    float3 p0 = L2FxHE_VertMesh_SampleFramePos(posTex, vertIndex, f0);
    float3 p1 = L2FxHE_VertMesh_SampleFramePos(posTex, vertIndex, f1);
    return lerp(p0, p1, a);
}

// Live fire_ta: sizeRatio=1.0, Owner+0x410=1. Bank verts are raw UU (same as sprites),
// not imported MeshEmitter FBX. Unity world K is SpriteEmitter 1.1 (player ~0.85m),
// not MeshEmitter 1.8. localOS = GetFrame_UU * StartSize * sizeScale * K / 52.5.
float3 L2FxHE_VertMesh_MeshScale(float3 sizeRangeSampleUu, float sizeScale, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.1;
    return L2FxHE_VertMesh_StartSizeUu(sizeRangeSampleUu, 0.0) * sizeScale * k;
}

// GetFrame UU → Unity mesh OS. Size is UE(X,Y,Z); Unity mesh is UE(X,Z,Y).
float3 L2FxHE_VertMesh_FrameToLocalMeshOS(float3 frameUu, float3 sizeUu)
{
    float3 unityAxes = float3(frameUu.x, frameUu.z, frameUu.y) * L2_UU_TO_METERS;
    return unityAxes * float3(sizeUu.x, sizeUu.z, sizeUu.y);
}

// animTime from particle age (spawn→now), matching UpdateAnimation(rate>=0).
float L2FxHE_VertMesh_AnimTimeFromAge(float ageSeconds, float seqRate, float frameCount)
{
    return L2FxHE_VertMesh_AdvanceAnimTime(0.0, seqRate, frameCount, ageSeconds);
}

// Confirmed live: same Brighten ColorFade as Mesh (Translucent peak RGB=Opacity*255, A=255).
float4 L2FxHE_VertMesh_ColorFade(
    float4 colorScale,
    float3 colorMultiplier,
    float ageSeconds,
    float lifetimeSeconds,
    float fadeInEndTime,
    float fadeOutStartTime,
    float opacity)
{
    // Live fades in even when emitter fadeIn bool dump is 0 — enable when End>0.
    float fadeIn = fadeInEndTime > 1e-6 ? 1.0 : 0.0;
    float fadeOut = 1.0;
    float alphaBlend = 0.0; // PTDS_Translucent → RGBA fade + Opacity on RGB
    return L2Fx_MeshColorFade_Apply(
        colorScale,
        colorMultiplier,
        ageSeconds,
        lifetimeSeconds,
        fadeIn,
        fadeInEndTime,
        fadeOut,
        fadeOutStartTime,
        opacity,
        alphaBlend);
}

#endif
