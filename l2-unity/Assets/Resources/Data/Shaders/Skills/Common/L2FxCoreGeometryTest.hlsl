#ifndef L2FX_CORE_GEOMETRY_INCLUDED
#define L2FX_CORE_GEOMETRY_INCLUDED

// UE2.5 macro-world: 1 Unity meter = 52.5 Unreal Units
static const float L2_UU_TO_METERS = 1.0 / 52.5;

// Local quad vertex scale (object space) 
// Final quad diameter before Transform scaling = (sizeUU / 52.5) * K * quadSpan.
float L2Fx_GetFinalVertexSizeMeters(float sizeUU, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.0;
    float sizeInMeters = sizeUU * L2_UU_TO_METERS;
    return (sizeInMeters * k);
}

// UE2.5 positional quantities from .uc (StartLocationOffset, ranges, polar
// radius, velocity, acceleration) are in UU. Healing-potion validation:
// StartLocationOffset=(Z=8) visually aligns with the original after:
//   UE(X,Y,Z) -> Unity(X,Z,Y), meters = UU / 52.5 * K.
// With verified K=1.8, Z=8 maps to Unity Y ~= 0.274 m.
float3 L2Fx_UcPositionToUnityMeters(float3 uePositionUU, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.8;
    return float3(uePositionUU.x, uePositionUU.z, uePositionUU.y) * L2_UU_TO_METERS * k;
}


//example

//v2f vert(Attributes input)
//    v2f o;
    // ... ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜ p ...
    
   // float meshScale = L2Fx_GetFinalMeshScale(p.startSize, p.relativeSize, 1.4f);
    
    // ˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜ FBX-˜˜˜˜˜˜
   // float3 scaledVertex = input.vertex.xyz * meshScale;
    
    // ˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜ (˜˜˜˜˜˜˜ ˜˜˜ ˜ ˜˜˜˜˜˜, ˜˜˜˜˜˜˜˜, -0.28)
   // float3 worldPosition = p.position + scaledVertex;
    
   // o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0));
   // return o;
//}
// ˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜, ˜˜ ˜˜˜˜˜˜ ˜˜˜ MeshEmitter
// Final mesh local scale = sizeUU * sizeScale * K.
float L2Fx_GetFinalMeshScale(float sizeUU, float sizeScale, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.8;
    
    // ˜˜˜ ˜˜˜˜ StartSizeRange ˜ ˜˜˜˜˜˜ SizeScale ˜ ˜˜˜ ˜˜˜˜˜˜ Scale-˜˜˜˜˜˜˜˜˜.
    // ˜˜˜˜˜˜˜˜˜ 52.5 ˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜.
    float finalScale = sizeUU * sizeScale * k;
    
    return finalScale;
}

#endif
