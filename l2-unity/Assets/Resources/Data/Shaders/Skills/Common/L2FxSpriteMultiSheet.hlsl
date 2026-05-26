#ifndef L2_FX_SPRITE_MULTI_SHEET_INCLUDED
#define L2_FX_SPRITE_MULTI_SHEET_INCLUDED

// UE PTDU_Up: multiple vertical sprite sheets rotated around world Y (0 / 120 / 240 on child transforms).
// Include after Core.hlsl and L2FxSpriteEmitterVertex.hlsl.

float L2Fx_SpriteYawRadiansFromObjectMatrix()
{
    return atan2(unity_ObjectToWorld._m02, unity_ObjectToWorld._m00);
}

float3 L2Fx_SpriteCameraForwardXZ(float3 centerWS)
{
    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
    float3 camXZ = float3(toCamera.x, 0.0, toCamera.z);
    return dot(camXZ, camXZ) > 1e-8 ? normalize(camXZ) : float3(0.0, 0.0, 1.0);
}

// One vertical sheet: camera bearing in XZ rotated by sheetYaw, world Y up.
float3 L2Fx_PtduUpMultiSheetPositionWS(
    float3 centerWS,
    float3 quadOS,
    float3 sizeM,
    float sheetYawRadians)
{
    float3 camXZ = L2Fx_SpriteCameraForwardXZ(centerWS);
    float sy = sin(sheetYawRadians);
    float cy = cos(sheetYawRadians);
    float3 sheetDir = float3(
        cy * camXZ.x - sy * camXZ.z,
        0.0,
        sy * camXZ.x + cy * camXZ.z);

    float3 upWS = float3(0.0, 1.0, 0.0);
    float3 rightWS = normalize(cross(upWS, sheetDir));
    float2 quadXY = float2(quadOS.x * sizeM.x, quadOS.y * sizeM.y);
    return centerWS + rightWS * quadXY.x + upWS * quadXY.y;
}

// Offset in camera right/up for round soft clip (same radius on screen vertical and horizontal).
void L2Fx_SpriteViewOffsetAndMaskRadius(
    float3 centerWS,
    float3 posWS,
    float3 sizeM,
    float maskRadiusScale,
    out float2 viewOffset,
    out float maskRadius)
{
    float3 effectOffsetWS = posWS - centerWS;
    float3 camXZ = L2Fx_SpriteCameraForwardXZ(centerWS);
    float3 camRight = normalize(cross(float3(0.0, 1.0, 0.0), camXZ));
    float3 camUp = float3(0.0, 1.0, 0.0);
    viewOffset = float2(dot(effectOffsetWS, camRight), dot(effectOffsetWS, camUp));
    maskRadius = min(sizeM.x, sizeM.y) * maskRadiusScale;
}

float L2Fx_SpriteViewSoftMask(float2 viewOffset, float maskRadius, float edgeSoftnessFraction)
{
    float dist = length(viewOffset);
    float soft = maskRadius * edgeSoftnessFraction;
    float r0 = max(maskRadius - soft, 0.0);
    float r1 = maskRadius + soft * 0.5;
    return 1.0 - smoothstep(r0, r1, dist);
}

#endif // L2_FX_SPRITE_MULTI_SHEET_INCLUDED
