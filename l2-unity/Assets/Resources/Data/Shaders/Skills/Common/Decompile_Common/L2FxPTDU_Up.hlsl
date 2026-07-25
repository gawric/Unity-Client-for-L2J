#ifndef L2_FX_PTDU_UP_INCLUDED
#define L2_FX_PTDU_UP_INCLUDED

// L2_FX_PTDU_UP - SPRITE EMITTER ONLY - Decompile_Common
//
// USpriteEmitter::FillVertexBuffer — UseDirectionAs=PTDU_Up (runtime value 1).
//
// Live-proven 2026-07-20 on e_u031_a SpriteEmitter7 "upline":
//   FillVertexBufferSnapshot.log — match=1, often maxAbsDiff=0.000000 UU
//   variant=cross(Loc-Cam,Dir), signX=1, signY=-1, cameraBillboard=ownerLocal
//   when CoordinateSystem@+0x108==1.
//
// Contract (all vectors in the SAME space as particle Location):
//   Dir   = Normalize(Location - OldLocation)
//   Cam   = SceneNode camera; if CS==1 → WorldToOwnerLocal(Cam, Owner.Loc, Owner.Rot)
//   Perp  = Normalize(cross(Loc - Cam, Dir))
//   AxisX = Dir  * Size.Y          // streak / track length
//   AxisY = (-1) * Perp * Size.X   // sheet width (live signY=-1)
//   corners: Loc ± AxisX ± AxisY (FillVB order 0..3)
//
// Size.X / Size.Y roles are intentional UE2 PTDU_Up semantics (not FaceCamera).
// Do NOT use L2FxSpriteMultiSheet PTDU helpers as this formula.
//
// Scale/axes (UU→meters, UE Z→Unity Y): not here — wrapper includes
// L2FxCoreGeometryTest and applies _L2FxWorldCalibration like kirakira.

static const float L2FX_PTDU_UP_URU_PER_TURN = 65536.0;
static const float L2FX_PTDU_UP_URU_TO_RAD =
    6.28318530718 / L2FX_PTDU_UP_URU_PER_TURN;

// Live FillVB match used AxisY = -Perp * Size.X.
static const float L2FX_PTDU_UP_AXIS_Y_SIGN = -1.0;

float3 L2FxPTDU_Up_SafeNormalize(float3 v, float3 fallback)
{
    float lenSq = dot(v, v);
    return lenSq > 1e-12 ? v * rsqrt(lenSq) : fallback;
}

float3 L2FxPTDU_Up_Dir(float3 location, float3 oldLocation)
{
    return L2FxPTDU_Up_SafeNormalize(
        location - oldLocation,
        float3(0.0, 0.0, 1.0));
}

float3 L2FxPTDU_Up_Perp(float3 location, float3 cameraSameSpace, float3 dir)
{
    return L2FxPTDU_Up_SafeNormalize(
        cross(location - cameraSameSpace, dir),
        float3(1.0, 0.0, 0.0));
}

// Interlude FRotationMatrix axes (sub_1BC63F0 / same as PTRS_Actor / FillVB hook).
void L2FxPTDU_Up_OwnerAxesFromPitchYawRollRadians(
    float3 pitchYawRollRadians,
    out float3 xAxis,
    out float3 yAxis,
    out float3 zAxis)
{
    float sinPitch;
    float cosPitch;
    float sinYaw;
    float cosYaw;
    float sinRoll;
    float cosRoll;
    sincos(pitchYawRollRadians.x, sinPitch, cosPitch);
    sincos(pitchYawRollRadians.y, sinYaw, cosYaw);
    sincos(pitchYawRollRadians.z, sinRoll, cosRoll);

    xAxis = float3(cosYaw * cosPitch, cosPitch * sinYaw, sinPitch);
    yAxis = float3(
        sinPitch * sinRoll * cosYaw - cosRoll * sinYaw,
        cosYaw * cosRoll + sinYaw * sinPitch * sinRoll,
        -sinRoll * cosPitch);
    zAxis = float3(
        -(sinYaw * sinRoll + cosYaw * sinPitch * cosRoll),
        sinRoll * cosYaw - sinPitch * cosRoll * sinYaw,
        cosPitch * cosRoll);
}

float3 L2FxPTDU_Up_WorldToOwnerLocal(
    float3 worldPoint,
    float3 ownerLocation,
    float3 ownerPitchYawRollUru)
{
    float3 delta = worldPoint - ownerLocation;
    float3 xAxis;
    float3 yAxis;
    float3 zAxis;
    L2FxPTDU_Up_OwnerAxesFromPitchYawRollRadians(
        ownerPitchYawRollUru * L2FX_PTDU_UP_URU_TO_RAD,
        xAxis,
        yAxis,
        zAxis);
    return float3(dot(delta, xAxis), dot(delta, yAxis), dot(delta, zAxis));
}

// coordinateSystem: emitter +0x108 (0=world, 1=owner local particle Loc/Old).
float3 L2FxPTDU_Up_ResolveCameraSameSpace(
    float3 cameraWorld,
    float coordinateSystem,
    float3 ownerLocation,
    float3 ownerPitchYawRollUru)
{
    if (coordinateSystem > 0.5)
    {
        return L2FxPTDU_Up_WorldToOwnerLocal(
            cameraWorld,
            ownerLocation,
            ownerPitchYawRollUru);
    }

    return cameraWorld;
}

void L2FxPTDU_Up_Axes(
    float3 dir,
    float3 perp,
    float sizeX,
    float sizeY,
    out float3 axisX,
    out float3 axisY)
{
    axisX = dir * sizeY;
    axisY = perp * (L2FX_PTDU_UP_AXIS_Y_SIGN * sizeX);
}

// sx,sy in {-1,+1} matching FillVB corner order.
float3 L2FxPTDU_Up_Corner(float3 location, float3 axisX, float3 axisY, float sx, float sy)
{
    return location + axisX * sx + axisY * sy;
}

float3 L2FxPTDU_Up_FillVbCorner(
    float3 location,
    float3 oldLocation,
    float3 cameraWorld,
    float coordinateSystem,
    float3 ownerLocation,
    float3 ownerPitchYawRollUru,
    float sizeX,
    float sizeY,
    float sx,
    float sy)
{
    float3 dir = L2FxPTDU_Up_Dir(location, oldLocation);
    float3 cam = L2FxPTDU_Up_ResolveCameraSameSpace(
        cameraWorld,
        coordinateSystem,
        ownerLocation,
        ownerPitchYawRollUru);
    float3 perp = L2FxPTDU_Up_Perp(location, cam, dir);
    float3 axisX;
    float3 axisY;
    L2FxPTDU_Up_Axes(dir, perp, sizeX, sizeY, axisX, axisY);
    return L2FxPTDU_Up_Corner(location, axisX, axisY, sx, sy);
}

// Same-space path: Loc/Cam/Size already share one space after CS resolution
// (typically owner-local when CS==1 and the Unity GO is the effect owner).
// Unity built-in quad: OS.xy in [-0.5,+0.5]; OS.y -> streak (AxisX), OS.x -> width (AxisY).
float3 L2FxPTDU_Up_PositionSameSpaceFromQuadOs(
    float3 locationSame,
    float3 oldLocationSame,
    float3 cameraSameSpace,
    float sizeX,
    float sizeY,
    float2 quadOsXy)
{
    float3 dir = L2FxPTDU_Up_Dir(locationSame, oldLocationSame);
    float3 perp = L2FxPTDU_Up_Perp(locationSame, cameraSameSpace, dir);
    float3 axisX;
    float3 axisY;
    L2FxPTDU_Up_Axes(dir, perp, sizeX, sizeY, axisX, axisY);
    float sx = quadOsXy.y * 2.0;
    float sy = quadOsXy.x * 2.0;
    return L2FxPTDU_Up_Corner(locationSame, axisX, axisY, sx, sy);
}

// Unity path after L2FxCoreGeometryTest (L2Fx_UcPositionToUnityMeters / GetFinalVertexSizeMeters):
// Loc/Old/Cam/Size are already Unity Y-up meters. Dir fallback must be Unity +Y
// (UE FillVB fallback +Z becomes +Y after Z-up→Y-up).
float3 L2FxPTDU_Up_DirUnity(float3 locationUnity, float3 oldLocationUnity)
{
    return L2FxPTDU_Up_SafeNormalize(
        locationUnity - oldLocationUnity,
        float3(0.0, 1.0, 0.0));
}

float3 L2FxPTDU_Up_PositionUnityFromQuadOs(
    float3 locationUnity,
    float3 oldLocationUnity,
    float3 cameraUnityOs,
    float sizeXMeters,
    float sizeYMeters,
    float2 quadOsXy)
{
    float3 dir = L2FxPTDU_Up_DirUnity(locationUnity, oldLocationUnity);
    float3 perp = L2FxPTDU_Up_Perp(locationUnity, cameraUnityOs, dir);
    float3 axisX;
    float3 axisY;
    L2FxPTDU_Up_Axes(dir, perp, sizeXMeters, sizeYMeters, axisX, axisY);
    float sx = quadOsXy.y * 2.0;
    float sy = quadOsXy.x * 2.0;
    return L2FxPTDU_Up_Corner(locationUnity, axisX, axisY, sx, sy);
}

#endif // L2_FX_PTDU_UP_INCLUDED
