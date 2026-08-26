#ifndef L2FX_CORE_GEOMETRY_INCLUDED
#define L2FX_CORE_GEOMETRY_INCLUDED

// UE2.5 macro-world: 1 Unity meter = 52.5 Unreal Units
static const float L2_UU_TO_METERS = 1.0 / 52.5;

// UE SpriteEmitter Particle.Size is a half-extent: FillVertexBuffer emits
// corners at Location +/- Size. Unity's unit quad spans [-0.5,+0.5], so its
// local diameter must be 2 * Size after UU-to-meters world calibration.
// DrawScale is intentionally not part of this conversion.
float L2Fx_GetFinalVertexSizeMeters(float sizeUU, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.0;
    float sizeInMeters = sizeUU * L2_UU_TO_METERS;
    return sizeInMeters * k * 2.0;
}

// UE2.5 positional quantities from .uc (StartLocationOffset, ranges, polar
// radius, velocity, acceleration) are in the same UU as pawns/terrain:
//   UE(X,Y,Z) -> Unity(X,Z,Y), meters = UU / 52.5.
// Mesh/sprite size K (_L2FxWorldCalibration 1.8 / 1.1) is NOT applied here.
// Size K only scales mesh/quad vertices (GetFinalMeshScale / GetFinalVertexSizeMeters).
// Passing worldCalibK keeps the call signature; it must not change trajectories.
float3 L2Fx_UcPositionToUnityMeters(float3 uePositionUU, float worldCalibK)
{
    worldCalibK = 1.0;
    return float3(uePositionUU.x, uePositionUU.z, uePositionUU.y)
        * L2_UU_TO_METERS * worldCalibK;
}

// Final mesh local scale = sizeUU * sizeScale * K.
float L2Fx_GetFinalMeshScale(float sizeUU, float sizeScale, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.8;
    return sizeUU * sizeScale * k;
}

#endif
