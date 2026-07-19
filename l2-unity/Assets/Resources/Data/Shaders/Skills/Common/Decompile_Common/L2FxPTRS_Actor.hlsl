#ifndef L2_FX_PTRS_ACTOR_INCLUDED
#define L2_FX_PTRS_ACTOR_INCLUDED

// UMeshEmitter::RenderParticles — UseRotationFrom=PTRS_Actor (runtime value 1).
//
// Verified against live Interlude WorldMat dumps on 2026-07-16:
//   World = T(-OwnerPivot) * S(FinalSize) * R(Spin) * T(ParticleLocation)
//         * R(OwnerRotation) * T(OwnerLocation)
//
// Matrix convention in this file is UE2's:
// - row-major storage;
// - row vectors: mul(position, matrix);
// - translation in matrix row 3 (.m[12..14]).
//
// Spin values are raw particle-slot components:
//   c0 = slot+0x30/+0x3C X, c1 = Y, c2 = Z.
// The renderer calls RotationURU(c1, c0, c2), so c0/c1 are swapped before
// entering FRotationMatrix(Pitch, Yaw, Roll).
//
// OwnerRotation and OwnerPivot participated in the disassembled branch. The
// current live reference effect has both values at zero; use the companion
// reference file when validating a non-zero owner rotation/pivot.

static const float L2FX_PTRS_ACTOR_URU_PER_TURN = 65536.0;
static const float L2FX_PTRS_ACTOR_URU_TO_RAD = 6.28318530718 / L2FX_PTRS_ACTOR_URU_PER_TURN;

float4x4 L2FxPTRSActor_TranslationUe(float3 translationUe)
{
    return float4x4(
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        translationUe.x, translationUe.y, translationUe.z, 1.0);
}

float4x4 L2FxPTRSActor_ScaleUe(float3 scale)
{
    return float4x4(
        scale.x, 0.0, 0.0, 0.0,
        0.0, scale.y, 0.0, 0.0,
        0.0, 0.0, scale.z, 0.0,
        0.0, 0.0, 0.0, 1.0);
}

// Exact UE2 FRotationMatrix / client sub_1BC63F0 coefficients.
// Input is radians in (Pitch, Yaw, Roll) order.
float4x4 L2FxPTRSActor_RotationPitchYawRollUe(float3 pitchYawRollRadians)
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

    return float4x4(
        cosYaw * cosPitch,
        cosPitch * sinYaw,
        sinPitch,
        0.0,

        sinPitch * sinRoll * cosYaw - cosRoll * sinYaw,
        cosYaw * cosRoll + sinYaw * sinPitch * sinRoll,
        -sinRoll * cosPitch,
        0.0,

        -(sinYaw * sinRoll + cosYaw * sinPitch * cosRoll),
        sinRoll * cosYaw - sinPitch * cosRoll * sinYaw,
        cosPitch * cosRoll,
        0.0,

        0.0, 0.0, 0.0, 1.0);
}

float4x4 L2FxPTRSActor_RotationUruPitchYawRoll(float3 pitchYawRollUru)
{
    return L2FxPTRSActor_RotationPitchYawRollUe(
        pitchYawRollUru * L2FX_PTRS_ACTOR_URU_TO_RAD);
}

// Mirrors (int)(spinRate * particleTime + startSpin) in RenderParticles,
// followed by RotationURU(v179, v180, v181) where:
//   v179 = c1, v180 = c0, v181 = c2.
float3 L2FxPTRSActor_EvaluateSpinPitchYawRollUru(
    float3 spinRateC012,
    float3 startSpinC012,
    float particleTimeSeconds)
{
    float3 components = trunc(startSpinC012 + spinRateC012 * particleTimeSeconds);
    return float3(components.y, components.x, components.z);
}

// Builds the exact UE2 row-vector WorldMat for UseRotationFrom=PTRS_Actor.
// finalSize must already contain all particle size/size-scale calculations.
float4x4 L2FxPTRSActor_BuildWorldUeRowMatrix(
    float3 ownerLocationUe,
    float3 ownerRotationPitchYawRollUru,
    float3 ownerPivotUe,
    float3 particleLocationUe,
    float3 finalSize,
    float3 spinRateC012,
    float3 startSpinC012,
    float particleTimeSeconds)
{
    float3 spinPitchYawRollUru = L2FxPTRSActor_EvaluateSpinPitchYawRollUru(
        spinRateC012,
        startSpinC012,
        particleTimeSeconds);

    float4x4 tNegativePivot = L2FxPTRSActor_TranslationUe(-ownerPivotUe);
    float4x4 scale = L2FxPTRSActor_ScaleUe(finalSize);
    float4x4 spin = L2FxPTRSActor_RotationUruPitchYawRoll(spinPitchYawRollUru);
    float4x4 particleTranslation = L2FxPTRSActor_TranslationUe(particleLocationUe);
    float4x4 ownerRotation = L2FxPTRSActor_RotationUruPitchYawRoll(ownerRotationPitchYawRollUru);
    float4x4 ownerTranslation = L2FxPTRSActor_TranslationUe(ownerLocationUe);

    return mul(
        mul(
            mul(tNegativePivot, scale),
            mul(spin, particleTranslation)),
        mul(ownerRotation, ownerTranslation));
}

float3 L2FxPTRSActor_TransformPositionUe(
    float3 localPositionUe,
    float3 ownerLocationUe,
    float3 ownerRotationPitchYawRollUru,
    float3 ownerPivotUe,
    float3 particleLocationUe,
    float3 finalSize,
    float3 spinRateC012,
    float3 startSpinC012,
    float particleTimeSeconds)
{
    float4x4 world = L2FxPTRSActor_BuildWorldUeRowMatrix(
        ownerLocationUe,
        ownerRotationPitchYawRollUru,
        ownerPivotUe,
        particleLocationUe,
        finalSize,
        spinRateC012,
        startSpinC012,
        particleTimeSeconds);
    return mul(float4(localPositionUe, 1.0), world).xyz;
}

// UE-to-Unity positional basis used by this project. Apply after the exact
// UE-side transform; do not use Unity's column-vector matrix convention on
// L2FxPTRSActor_BuildWorldUeRowMatrix directly.
float3 L2FxPTRSActor_UeVectorToUnity(float3 vectorUe)
{
    return float3(vectorUe.x, vectorUe.z, vectorUe.y);
}

float3 L2FxPTRSActor_UnityVectorToUe(float3 vectorUnity)
{
    return float3(vectorUnity.x, vectorUnity.z, vectorUnity.y);
}

// Local mesh path used when the Unity GameObject already carries owner TRS
// (TransformObjectToWorld). Matches the S(FinalSize)*R(Spin) portion of
// RenderParticles WorldMat. finalSizeUe must stay in UE axis order (X,Y,Z),
// e.g. Wave StartSize (XY, XY, Z) — do not pre-swap thin Z onto Unity Y.
float3 L2FxPTRSActor_TransformLocalMeshUnity(
    float3 localPositionUnity,
    float3 finalSizeUe,
    float3 spinRateC012,
    float3 startSpinC012,
    float particleTimeSeconds)
{
    float3 localPositionUe = L2FxPTRSActor_UnityVectorToUe(localPositionUnity);
    float3 spinPitchYawRollUru = L2FxPTRSActor_EvaluateSpinPitchYawRollUru(
        spinRateC012,
        startSpinC012,
        particleTimeSeconds);
    float4x4 scale = L2FxPTRSActor_ScaleUe(finalSizeUe);
    float4x4 spin = L2FxPTRSActor_RotationUruPitchYawRoll(spinPitchYawRollUru);
    float3 spunUe = mul(float4(localPositionUe, 1.0), mul(scale, spin)).xyz;
    return L2FxPTRSActor_UeVectorToUnity(spunUe);
}

float3 L2FxPTRSActor_TransformPositionUnity(
    float3 localPositionUnity,
    float3 ownerLocationUe,
    float3 ownerRotationPitchYawRollUru,
    float3 ownerPivotUe,
    float3 particleLocationUe,
    float3 finalSize,
    float3 spinRateC012,
    float3 startSpinC012,
    float particleTimeSeconds,
    float uuToUnity)
{
    float3 localPositionUe = float3(
        localPositionUnity.x,
        localPositionUnity.z,
        localPositionUnity.y);
    float3 worldPositionUe = L2FxPTRSActor_TransformPositionUe(
        localPositionUe,
        ownerLocationUe,
        ownerRotationPitchYawRollUru,
        ownerPivotUe,
        particleLocationUe,
        finalSize,
        spinRateC012,
        startSpinC012,
        particleTimeSeconds);
    return L2FxPTRSActor_UeVectorToUnity(worldPositionUe) * uuToUnity;
}

#endif // L2_FX_PTRS_ACTOR_INCLUDED
