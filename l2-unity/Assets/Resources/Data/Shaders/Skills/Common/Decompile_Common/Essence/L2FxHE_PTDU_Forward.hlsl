#ifndef L2_FX_HE_PTDU_FORWARD_INCLUDED
#define L2_FX_HE_PTDU_FORWARD_INCLUDED

// HE FillVertexBuffer UseDirectionAs BYTE @+0x50C, value 3 = PTDU_Forward.
// loc_209D3CA9: Dir = Normalize(Location - OldLocation)  // velocity, not owner X
// jumptable cases 3,4 = loc_209D4374 (shared with PTDU_Normal):
//   GetNonParallel inlined (|comp| > 0.57 → pick other axis)
//   AxisX = Size.Y(+0x7C) * (Dir × axis)
//   AxisY = Size.X(+0x78) * SafeNormal(second basis)
// Dir is the sheet NORMAL. Do not reuse L2FxPTDU_Up_Axes (that is streak).
//
// 1147: d_mon_fire2_ca smorke.

#include "../L2FxPTDU_Up.hlsl"

static const float L2FX_HE_PTDU_FORWARD = 3.0;

float3 L2FxHE_GetNonParallel(float3 v)
{
    float ax = abs(v.x);
    float ay = abs(v.y);
    float az = abs(v.z);
    if (ax <= ay && ax <= az)
    {
        return float3(1.0, 0.0, 0.0);
    }
    if (ay <= az)
    {
        return float3(0.0, 1.0, 0.0);
    }
    return float3(0.0, 0.0, 1.0);
}

void L2FxHE_PTDU_Forward_Axes(
    float3 dir,
    float sizeX,
    float sizeY,
    out float3 axisX,
    out float3 axisY)
{
    float3 np = L2FxHE_GetNonParallel(dir);
    float3 tangent = L2FxPTDU_Up_SafeNormalize(
        cross(dir, np),
        float3(1.0, 0.0, 0.0));
    float3 bitangent = L2FxPTDU_Up_SafeNormalize(
        cross(tangent, dir),
        float3(0.0, 1.0, 0.0));
    axisX = tangent * sizeY;
    axisY = bitangent * sizeX;
}

float3 L2FxHE_PTDU_Forward_FillVbCorner(
    float3 location,
    float3 oldLocation,
    float sizeX,
    float sizeY,
    float sx,
    float sy)
{
    float3 dir = L2FxPTDU_Up_Dir(location, oldLocation);
    float3 axisX;
    float3 axisY;
    L2FxHE_PTDU_Forward_Axes(dir, sizeX, sizeY, axisX, axisY);
    return L2FxPTDU_Up_Corner(location, axisX, axisY, sx, sy);
}

float3 L2FxHE_PTDU_Forward_PositionUnityFromQuadOs(
    float3 locationUnity,
    float3 oldLocationUnity,
    float sizeXMeters,
    float sizeYMeters,
    float2 quadOsXy)
{
    float3 dir = L2FxPTDU_Up_DirUnity(locationUnity, oldLocationUnity);
    float3 axisX;
    float3 axisY;
    L2FxHE_PTDU_Forward_Axes(dir, sizeXMeters, sizeYMeters, axisX, axisY);
    float sx = quadOsXy.y;
    float sy = quadOsXy.x;
    return L2FxPTDU_Up_Corner(locationUnity, axisX, axisY, sx, sy);
}

#endif
