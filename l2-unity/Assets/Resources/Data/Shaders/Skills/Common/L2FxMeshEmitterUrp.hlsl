#ifndef L2_FX_MESH_EMITTER_URP_INCLUDED
#define L2_FX_MESH_EMITTER_URP_INCLUDED

// URP glue for mesh emitters: Core.hlsl + vertex math + clip bias.
// Draw tags live on L2FxUnifiedMeshEmitter.shader: Transparent + UniversalForwardOnly.
// Geometry + UniversalForward is invisible on this Deferred URP (same bug as DropMesh).
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "L2FxMeshEmitterVertex.hlsl"

float4 L2Fx_MeshUrp_ObjectToHClip(float3 posOS, float clipDepthBias)
{
    float4 positionHCS = TransformObjectToHClip(float4(posOS, 1.0));
    positionHCS.z -= clipDepthBias * positionHCS.w;
    return positionHCS;
}

#endif // L2_FX_MESH_EMITTER_URP_INCLUDED
