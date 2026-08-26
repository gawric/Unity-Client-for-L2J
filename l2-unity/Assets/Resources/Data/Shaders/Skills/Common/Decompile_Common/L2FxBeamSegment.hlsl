#ifndef L2_FX_BEAM_SEGMENT_INCLUDED
#define L2_FX_BEAM_SEGMENT_INCLUDED

// UBeamEmitter straight-sheet geometry — VERIFIED on the original Lineage 2
// Interlude client (Engine.dll, live wh_heal_ta capture, 2026-08-23).
//
// Verified runtime values:
//   DetermineEndPointBy = PTEP_Offset (2)
//   end = start + (0, 0, -190) UU
//   LowFrequencyPoints = 3
//   HighFrequencyPoints = 10
//   HF[i].t = i / (HF - 1), locations are a linear start→end interpolation
//   StartSizeRange.X is the rendered sheet width; particle Size.Y/Z are unused
//   points remain static after spawn when BeamNoiseRange is zero
//
// CPU mesh contract (L2BeamEmitterStripBuilder):
//   positionOS.z = normalized position along the beam, 0..1
//   positionOS.x = across-sheet coordinate, -0.5..+0.5
//   UV.x = along beam, UV.y = edge 0/1
//
// Verified scope: one camera-facing straight sheet, PTEP_Offset, no noise.

#include "L2FxAppRand.hlsl"

float3 L2Fx_Beam_PtepOffsetEndUe(
    float3 startUe,
    float2 offsetX,
    float2 offsetY,
    float2 offsetZ,
    inout uint state)
{
    float3 offsetUe =
        L2Fx_FRangeVector_GetRandYawPitchRoll(offsetX, offsetY, offsetZ, state);
    return startUe + offsetUe;
}

// Width in Unity meters from the live-proven StartSizeRange.X value.
float L2Fx_Beam_HalfWidthMeters(float sizeUU, float worldCalibration)
{
    float calibration = worldCalibration > 0.0 ? worldCalibration : 1.4;
    return 0.5 * sizeUU * (1.0 / 52.5) * calibration;
}

float3 L2Fx_Beam_BillboardPointWS(
    float3 startWS,
    float3 endWS,
    float along,
    float across,
    float widthMeters,
    float3 cameraWS)
{
    float3 axis = endWS - startWS;
    float axisLengthSq = dot(axis, axis);
    float3 beamDirection =
        axisLengthSq > 1e-12 ? axis * rsqrt(axisLengthSq) : float3(0, 1, 0);
    float3 axisPoint = lerp(startWS, endWS, saturate(along));

    float3 toCamera = cameraWS - axisPoint;
    float toCameraLengthSq = dot(toCamera, toCamera);
    float3 viewDirection = toCameraLengthSq > 1e-12
        ? toCamera * rsqrt(toCameraLengthSq)
        : float3(0, 0, -1);

    float3 right = cross(beamDirection, viewDirection);
    float rightLengthSq = dot(right, right);
    if (rightLengthSq < 1e-12)
    {
        right = cross(beamDirection, float3(0, 1, 0));
        rightLengthSq = dot(right, right);
        right = rightLengthSq < 1e-12
            ? float3(1, 0, 0)
            : right * rsqrt(rightLengthSq);
    }
    else
    {
        right *= rsqrt(rightLengthSq);
    }

    return axisPoint + right * (across * widthMeters);
}

#endif // L2_FX_BEAM_SEGMENT_INCLUDED
