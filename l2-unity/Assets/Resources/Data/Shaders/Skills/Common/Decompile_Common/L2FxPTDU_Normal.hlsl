#ifndef L2_FX_PTDU_NORMAL_INCLUDED
#define L2_FX_PTDU_NORMAL_INCLUDED

// USpriteEmitter::FillVertexBuffer — UseDirectionAs=PTDU_Normal (runtime value 2).
// Quad lies in the plane perpendicular to the particle Normal.
// _SurfaceNormals is Unity object space (UE (x,y,z) → Unity (x,z,y)).
// Default Normal is UE local (0,0,1) → Unity local (0,1,0).
// PTRS_Actor is the owner GameObject TRS (GetObjectToWorldMatrix), same as mesh.

float3 L2FxPTDU_Normal_ResolveWS(float3 surfaceNormalMaterial)
{
    float3 localNormal = dot(surfaceNormalMaterial, surfaceNormalMaterial) > 1e-6
        ? surfaceNormalMaterial
        : float3(0.0, 1.0, 0.0);
    float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), localNormal);
    return dot(normalWS, normalWS) > 1e-6 ? normalize(normalWS) : float3(0.0, 1.0, 0.0);
}

float3 L2FxPTDU_Normal_PositionWS(
    float3 centerWS,
    float2 quadXY,
    float sizeXM,
    float sizeYM,
    float3 surfaceNormalMaterial)
{
    float3 normalWS = L2FxPTDU_Normal_ResolveWS(surfaceNormalMaterial);
    float3 refAxis = abs(normalWS.y) > 0.99 ? float3(1.0, 0.0, 0.0) : float3(0.0, 1.0, 0.0);
    float3 tangent = normalize(cross(refAxis, normalWS));
    float3 bitangent = cross(normalWS, tangent);
    return centerWS
        + tangent * (quadXY.x * sizeXM)
        + bitangent * (quadXY.y * sizeYM);
}

#endif
