#ifndef L2_FX_HE_REVOLUTION_INCLUDED
#define L2_FX_HE_REVOLUTION_INCLUDED

// UseRevolution — Engine_essens_high_elves.dll
// UParticleEmitter::UpdateParticles loc_208E7A36.
//
// Gate: test byte ptr [emitter+0x1FC], 10h
// Bind from UC UseRevolution; smorke has RPS without the flag → skip.
//
// SpawnParticle loc_208EAACC:
//   CenterOffsetRange GetRand(+0x200) → slot+0x54
//   RPS range GetRand(+0x218)         → slot+0x60
// Multiplier slot+0x6C/70/74 (default 1).
//
// Per tick (HE SSE, sub_20155F10 = RotateAngleAxis):
//   rel = Location - Center
//   angleURU = trunc(multiplier * rps * dt * 65535.0)  // dword_20DB5BD8
//   rel = RotateAngleAxis(rel, angleX, (1,0,0))
//   rel = RotateAngleAxis(rel, angleY, (0,1,0))
//   rel = RotateAngleAxis(rel, angleZ, (0,0,1))
//   Location = Center + rel
// Velocity is NOT rotated.
//
// GPU helper applies one shot with dt = age (X then Y then Z).
// 1147 Sprite only has RPS.Z.

static const float L2FX_HE_REV_URU_PER_TURN = 65535.0;
static const float L2FX_HE_REV_URU_TO_RAD =
    6.28318530718 / 65536.0;

float3 L2FxHE_Revolution_RotateAngleAxisUe(float3 v, float angleUru, float3 axis)
{
    float s;
    float c;
    sincos(angleUru * L2FX_HE_REV_URU_TO_RAD, s, c);
    float3 n = axis;
    float xx = n.x * n.x;
    float yy = n.y * n.y;
    float zz = n.z * n.z;
    float xy = n.x * n.y;
    float yz = n.y * n.z;
    float zx = n.z * n.x;
    float xs = n.x * s;
    float ys = n.y * s;
    float zs = n.z * s;
    float omc = 1.0 - c;
    return float3(
        (omc * xx + c) * v.x + (omc * xy - zs) * v.y + (omc * zx + ys) * v.z,
        (omc * xy + zs) * v.x + (omc * yy + c) * v.y + (omc * yz - xs) * v.z,
        (omc * zx - ys) * v.x + (omc * yz + xs) * v.y + (omc * zz + c) * v.z);
}

float L2FxHE_Revolution_AngleUru(float multiplier, float rpsTurnsPerSec, float dtSeconds)
{
    return trunc(multiplier * rpsTurnsPerSec * dtSeconds * L2FX_HE_REV_URU_PER_TURN);
}

float3 L2FxHE_Revolution_ApplyUe(
    float useRevolution,
    float3 locationUe,
    float3 revolutionCenterUe,
    float3 rpsTurnsPerSec,
    float3 revolutionsMultiplier,
    float ageSeconds)
{
    if (useRevolution < 0.5)
    {
        return locationUe;
    }

    float3 rel = locationUe - revolutionCenterUe;
    float ax = L2FxHE_Revolution_AngleUru(revolutionsMultiplier.x, rpsTurnsPerSec.x, ageSeconds);
    float ay = L2FxHE_Revolution_AngleUru(revolutionsMultiplier.y, rpsTurnsPerSec.y, ageSeconds);
    float az = L2FxHE_Revolution_AngleUru(revolutionsMultiplier.z, rpsTurnsPerSec.z, ageSeconds);
    rel = L2FxHE_Revolution_RotateAngleAxisUe(rel, ax, float3(1.0, 0.0, 0.0));
    rel = L2FxHE_Revolution_RotateAngleAxisUe(rel, ay, float3(0.0, 1.0, 0.0));
    rel = L2FxHE_Revolution_RotateAngleAxisUe(rel, az, float3(0.0, 0.0, 1.0));
    return revolutionCenterUe + rel;
}

float3 L2FxHE_Revolution_ApplyUeDefaultMul(
    float useRevolution,
    float3 locationUe,
    float3 revolutionCenterUe,
    float3 rpsTurnsPerSec,
    float ageSeconds)
{
    return L2FxHE_Revolution_ApplyUe(
        useRevolution,
        locationUe,
        revolutionCenterUe,
        rpsTurnsPerSec,
        float3(1.0, 1.0, 1.0),
        ageSeconds);
}

float3 L2FxHE_Revolution_ApplyIntegratedMultiplierUe(
    float useRevolution,
    float3 locationUe,
    float3 revolutionCenterUe,
    float3 rpsTurnsPerSec,
    float3 integratedMultiplierSeconds)
{
    if (useRevolution < 0.5)
        return locationUe;

    float3 rel = locationUe - revolutionCenterUe;
    float3 angleUru = trunc(
        rpsTurnsPerSec *
        integratedMultiplierSeconds *
        L2FX_HE_REV_URU_PER_TURN);
    rel = L2FxHE_Revolution_RotateAngleAxisUe(
        rel, angleUru.x, float3(1.0, 0.0, 0.0));
    rel = L2FxHE_Revolution_RotateAngleAxisUe(
        rel, angleUru.y, float3(0.0, 1.0, 0.0));
    rel = L2FxHE_Revolution_RotateAngleAxisUe(
        rel, angleUru.z, float3(0.0, 0.0, 1.0));
    return revolutionCenterUe + rel;
}

#endif
