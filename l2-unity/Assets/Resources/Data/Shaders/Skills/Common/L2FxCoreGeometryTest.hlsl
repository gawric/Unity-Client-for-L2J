#ifndef L2FX_CORE_GEOMETRY_INCLUDED
#define L2FX_CORE_GEOMETRY_INCLUDED

// Include after Core.hlsl (needs unity_ObjectToWorld).

// UE2.5 macro-world: 1 Unity meter = 52.5 Unreal Units
static const float L2_UU_TO_METERS = 1.0 / 52.5;

float L2Fx_ExtractObjectDrawScale()
{
    float drawScale = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
    return max(drawScale, 1e-4);
}

// Local quad vertex scale (object space) with DrawScale compensation.
// Final world diameter = (sizeUU / 52.5) * K * quadSpan  (DrawScale-neutral).
float L2Fx_GetFinalVertexSizeMeters(float sizeUU, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.0;
    float sizeInMeters = sizeUU * L2_UU_TO_METERS;
    float drawScale = L2Fx_ExtractObjectDrawScale();
    return (sizeInMeters * k) / drawScale;
}

#endif
