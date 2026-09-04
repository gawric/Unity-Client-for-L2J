#ifndef L2_FX_HE_VERT_MESH_INCLUDED
#define L2_FX_HE_VERT_MESH_INCLUDED

// UVertMeshEmitter — same particle pool / size / fade / spin / motion as Mesh.
// HE ASM: VertexMesh pointer @+0x548 (Sprite UseDirectionAs is @+0x50C).
// Bind Unity mesh from UC LineageEffectMeshes.sh2.
// HE UVertMeshEmitter::UpdateParticles loc_20A3F100:
//   1) call UParticleEmitter::UpdateParticles
//   2) if mesh @+0x548: GetMeshInstance(i), anim time @+0x524[i] → instance+0xCC
// Particle formulas stay in the Mesh includes. Bind mesh from UC sh2.

#include "../L2FxMeshSpawnParticle.hlsl"
#include "../L2FxMeshSpin.hlsl"
#include "../L2FxMeshSizeScale.hlsl"
#include "../L2FxMeshColorFade.hlsl"
#include "../L2FxMeshMotion.hlsl"

#endif
