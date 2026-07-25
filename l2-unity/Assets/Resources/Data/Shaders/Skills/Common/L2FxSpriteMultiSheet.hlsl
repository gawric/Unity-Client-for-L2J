#ifndef L2_FX_SPRITE_MULTI_SHEET_INCLUDED
#define L2_FX_SPRITE_MULTI_SHEET_INCLUDED

// UE PTDU_Up: streak along world Y (velocity up); sheet plane from emitter radial XZ + optional child yaw (0/120/240).
// Include after Core.hlsl and L2FxSpriteEmitterVertex.hlsl.

float L2Fx_SpriteYawRadiansFromObjectMatrix()
{
    return atan2(unity_ObjectToWorld._m02, unity_ObjectToWorld._m00);
}

float3 L2Fx_SpriteEffectOriginWS()
{
    return float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
}

float3 L2Fx_SpriteCameraForwardXZ(float3 centerWS)
{
    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
    float3 camXZ = float3(toCamera.x, 0.0, toCamera.z);
    return dot(camXZ, camXZ) > 1e-8 ? normalize(camXZ) : float3(0.0, 0.0, 1.0);
}

// Polar ring / PTDU_Up: face outward from emitter pivot on XZ (not camera).
float3 L2Fx_PtduUpSheetDirRadialXZ(float3 centerWS)
{
    float3 effectOriginWS = L2Fx_SpriteEffectOriginWS();
    float3 radial = float3(centerWS.x - effectOriginWS.x, 0.0, centerWS.z - effectOriginWS.z);
    if (dot(radial, radial) > 1e-8)
    {
        return normalize(radial);
    }

    float yaw = L2Fx_SpriteYawRadiansFromObjectMatrix();
    return float3(sin(yaw), 0.0, cos(yaw));
}

float3 L2Fx_PtduUpRotateDirY(float3 dirXZ, float yawRadians)
{
    float sy = sin(yawRadians);
    float cy = cos(yawRadians);
    return float3(
        cy * dirXZ.x - sy * dirXZ.z,
        0.0,
        sy * dirXZ.x + cy * dirXZ.z);
}

// L2 PTDU_Up: world Y = streak/velocity up; sheet normal from radial XZ (+ child yaw for multi-sheet).
float3 L2Fx_PtduUpMultiSheetPositionWS(
    float3 centerWS,
    float3 quadOS,
    float3 sizeM,
    float sheetYawRadians)
{
    float3 sheetDir = L2Fx_PtduUpRotateDirY(
        L2Fx_PtduUpSheetDirRadialXZ(centerWS),
        sheetYawRadians);

    float3 upWS = float3(0.0, 1.0, 0.0);
    float3 rightWS = normalize(cross(upWS, sheetDir));
    float2 quadXY = float2(quadOS.x * sizeM.x, quadOS.y * sizeM.y);
    return centerWS + rightWS * quadXY.x + upWS * quadXY.y;
}

// Optional cylindrical billboard (camera XZ + world Y up) for effects that still need view alignment.
float3 L2Fx_PtduUpCylindricalBillboardPositionWS(
    float3 centerWS,
    float3 quadOS,
    float3 sizeM,
    float sheetYawRadians)
{
    float3 camXZ = L2Fx_SpriteCameraForwardXZ(centerWS);
    float3 sheetDir = L2Fx_PtduUpRotateDirY(camXZ, sheetYawRadians);

    float3 upWS = float3(0.0, 1.0, 0.0);
    float3 rightWS = normalize(cross(upWS, sheetDir));
    float2 quadXY = float2(quadOS.x * sizeM.x, quadOS.y * sizeM.y);
    return centerWS + rightWS * quadXY.x + upWS * quadXY.y;
}

// Unity built-in quad: mesh V grows upward (OpenGL-style). UE SpriteEmitter PTDU_Up + PointCoord:
// sheet V grows opposite to world Up (+Z UE / +Y Unity). Apply after TRANSFORM_TEX, before flipbook.
float2 L2Fx_PtduUpQuadUv01(float2 quadUv01)
{
    return float2(quadUv01.x, 1.0 - quadUv01.y);
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
