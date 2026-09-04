#ifndef L2_FX_HE_PROJECTION_NORMAL_INCLUDED
#define L2_FX_HE_PROJECTION_NORMAL_INCLUDED

// UC ProjectionNormal (UE Z-up) → Unity material vector for L2FxPTDU_Normal.
//
// HE USpriteEmitter::Initialize: SafeNormal(ProjectionNormal @+0x510) → +0x528.
// FillVB case 4 loads +0x528 as Dir (loc_209D3928 / loc_209D4374).
// (0,0,90) becomes (0,0,1) UE, not 90 degrees.
// Quad math stays in ../L2FxPTDU_Normal.hlsl.
//
// 1147:
//   d_mon_fire2_ca dipan10  UseDirectionAs=PTDU_Normal, ProjectionNormal=(0,0,90)
//     SafeNormal + UE→Unity → Unity +Y.
//   d_mon_fire_ta center    ProjectionNormal=(1,0,0), no UseDirectionAs
//     Default PTDU_None — this vector is unused unless the layer is Normal.

#include "../L2FxPTDU_Normal.hlsl"

float3 L2FxHE_ProjectionNormal_UeToUnityMaterial(float3 projectionNormalUe)
{
    return float3(projectionNormalUe.x, projectionNormalUe.z, projectionNormalUe.y);
}

float3 L2FxHE_ProjectionNormal_PositionWS(
    float3 centerWS,
    float2 quadXY,
    float sizeXM,
    float sizeYM,
    float3 projectionNormalUe)
{
    return L2FxPTDU_Normal_PositionWS(
        centerWS,
        quadXY,
        sizeXM,
        sizeYM,
        L2FxHE_ProjectionNormal_UeToUnityMaterial(projectionNormalUe));
}

#endif
