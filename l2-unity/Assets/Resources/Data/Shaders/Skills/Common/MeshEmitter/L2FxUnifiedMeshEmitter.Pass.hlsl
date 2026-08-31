#ifndef L2_FX_UNIFIED_MESH_EMITTER_PASS_INCLUDED
#define L2_FX_UNIFIED_MESH_EMITTER_PASS_INCLUDED

// Pass LightMode must stay UniversalForwardOnly. This project's URP is Deferred +
// depth priming: unlit Geometry/UniversalForward never draws. See Tags on
// L2FxUnifiedMeshEmitter.shader. Do not swap the slot to SpriteEmitter to "fix" it.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../L2FxCoreGeometryTest.hlsl"
#include "../Decompile_Common/L2FxAppRand.hlsl"
#include "../Decompile_Common/L2FxMeshSpawnParticle.hlsl"
#include "../Decompile_Common/L2FxStartLocationRange.hlsl"
#include "../Decompile_Common/L2FxSpritePolar.hlsl"
#include "../Decompile_Common/L2FxPTVD_StartPositionAndOwner.hlsl"
#include "../Decompile_Common/L2FxPTVD_OwnerAndStartPosition.hlsl"
#include "../Decompile_Common/L2FxMeshMotion.hlsl"
#include "../Decompile_Common/L2FxMeshSizeScale.hlsl"
#include "../Decompile_Common/L2FxMeshColorFade.hlsl"
#include "../Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
#include "../Decompile_Common/L2FxMeshSpin.hlsl"
#include "../Decompile_Common/L2FxPTRS_Actor.hlsl"
#include "../Decompile_Common/L2FxPTDS_DrawStyle.hlsl"
#include "../Decompile_Common/L2FxD3d9FixedFunction.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_SecondTex);
SAMPLER(sampler_SecondTex);

// Modes are material data, not formula replacements:
// Spawn: 0=None, 1=Z-only SpawnParticle, 2=full XYZ SpawnParticle,
//        3=StartLocationRange coin path.
// Motion: 0=None, 1=ballistic, 2=velocity-loss drag.
// Transform: 0=regular MeshSpin path, 1=PTRS_Actor.
// Size: 0=uniform, 1=XY + sampled Z, 2=full XYZ.
CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _SecondTex_ST;
    float _UseSecondTex;
    float _StartTime;
    float _Seed;
    float4 _InitialDelayRange;
    float4 _LifetimeRange;
    float _UseManualAge;
    float _ManualAge;

    float _SpawnMode;
    float _FullTlsShape;
    float _PtvdMode;
    float _MotionMode;
    float _TransformMode;
    float _SizeMode;
    float _OffsetSource;
    float _SpinSpsMode;

    float _L2FxWorldCalibration;
    float _StartSize;
    float4 _StartSizeRange;
    float _StartSizeXY;
    float4 _StartSizeZRange;
    float4 _StartSizeRangeXUc;
    float4 _StartSizeRangeYUc;
    float4 _StartSizeRangeZUc;
    float _UseSizeScale;
    float _SizeKeyCount;
    float4 _SizeKey0;
    float4 _SizeKey1;
    float4 _SizeKey2;
    float4 _SizeKey3;
    float4 _SizeKey4;

    float4 _StartLocationOffsetUc;
    float4 _StartLocationOffsetUe;
    float4 _StartLocationZRangeUU;
    float4 _StartLocationRangeXUc;
    float4 _StartLocationRangeYUc;
    float4 _StartLocationRangeZUc;
    float4 _PolarThetaRangeUc;
    float4 _PolarPhiRangeUc;
    float4 _PolarRadiusRangeUc;
    float4 _StartVelocityZRangeUU;
    float4 _StartVelocityRangeXUc;
    float4 _StartVelocityRangeYUc;
    float4 _StartVelocityRangeZUc;
    float4 _AccelerationUc;
    float4 _VelocityLossRangeUc;
    float _MeshSpawnRandStateBits;

    float _SpinParticles;
    float4 _StartSpinYawRangeUc;
    float4 _StartSpinPitchRangeUc;
    float4 _StartSpinRollRangeUc;
    float4 _SpsYawPitchRollUc;
    float4 _SpsYawRangeUc;
    float4 _SpsPitchRangeUc;
    float4 _SpsRollRangeUc;
    float4 _SpinCCWorCW;
    float _StartSpinRandStateBits;

    float4 _ColorMultiplier;
    float4 _ColorMulMin;
    float4 _ColorMulMax;
    float _ColorScaleRepeats;
    float4 _ColorKey0;
    float _ColorKey1Time;
    float4 _ColorKey1;
    float _ColorKey2Time;
    float4 _ColorKey2;
    float _ColorKey3Time;
    float4 _ColorKey3;
    float _ColorKey4Time;
    float4 _ColorKey4;
    float _ColorKey5Time;
    float4 _ColorKey5;
    float _FadeIn;
    float _FadeInEndTime;
    float _FadeOut;
    float _FadeOutStartTime;
    float _Opacity;
    float _RgbBoost;
    float _L2SpriteColorGammaToLinear;
    float _AlphaClipThreshold;
    float _DebugMeshOut;
CBUFFER_END

// Remaps timing and appRand values to ParticleGroup GPU slot data only when
// Unity selected an instanced variant. ParticleSingle reads the CBUFFER above.
#include "../L2FxInstancing.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR0;
    nointerpolation float4 debugData : TEXCOORD1;
};

float L2FxUnified_ResolveAge(float lifetime, float delay)
{
    if (_UseManualAge > 0.5)
        return _ManualAge;
    return _StartTime > 0.0
        ? L2Fx_AgeSeconds(_Time.y, _StartTime, delay)
        : max(0.0, _Time.y - delay);
}

float L2FxUnified_ResolveSizeScale(float ageNorm)
{
    return L2Fx_MeshSizeScale_ScalarFromKeys5(
        ageNorm,
        _UseSizeScale,
        0.0,
        1.0,
        0.0,
        (int)clamp(_SizeKeyCount, 1.0, 5.0),
        _SizeKey0.x, _SizeKey0.y,
        _SizeKey1.x, _SizeKey1.y,
        _SizeKey2.x, _SizeKey2.y,
        _SizeKey3.x, _SizeKey3.y,
        _SizeKey4.x, _SizeKey4.y);
}

float4 L2FxUnified_ResolveColor(float ageSeconds, float lifetime, float3 colorMul)
{
    float4 color = L2Fx_MeshColorFade_FullKeys6(
        ageSeconds,
        lifetime,
        _ColorScaleRepeats,
        colorMul,
        _FadeIn,
        _FadeInEndTime,
        _FadeOut,
        _FadeOutStartTime,
        _Opacity,
        _ColorKey0,
        _ColorKey1Time, _ColorKey1,
        _ColorKey2Time, _ColorKey2,
        _ColorKey3Time, _ColorKey3,
        _ColorKey4Time, _ColorKey4,
        _ColorKey5Time, _ColorKey5);
    return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
        color, _L2SpriteColorGammaToLinear);
}

void L2FxUnified_ResolveSpawn(
    out float3 locationUe,
    out float3 velocityUe,
    out float3 sizeUe,
    out float3 colorMul,
    out float lifetime,
    out float delay)
{
    float3 offsetUe = _OffsetSource > 0.5
        ? _StartLocationOffsetUe.xyz
        : _StartLocationOffsetUc.xyz;
    locationUe = offsetUe;
    velocityUe = float3(0.0, 0.0, 0.0);
    sizeUe = float3(_StartSize, _StartSize, _StartSize);
    colorMul = _ColorMultiplier.xyz;
    lifetime = max(L2Fx_RandomLifetime(
        _LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
    delay = L2Fx_RandomInitialDelay(
        _InitialDelayRange.xy, _Seed, _StartTime, 3.0);

    uint state = asuint(_MeshSpawnRandStateBits);

    if (_SpawnMode > 1.5 && _SpawnMode < 2.5)
    {
        float3 sampledLocation;
        float sampledLifetime;
        float sampledDelay;
        if (state != 0u)
        {
            if (_FullTlsShape > 0.5)
            {
                L2Fx_MeshSpawnParticle_SampleVelPolarSize(
                    _StartVelocityRangeXUc.xy,
                    _StartVelocityRangeYUc.xy,
                    _StartVelocityRangeZUc.xy,
                    _PolarThetaRangeUc.xy,
                    _PolarPhiRangeUc.xy,
                    _PolarRadiusRangeUc.xy,
                    float2(_ColorMulMin.x, _ColorMulMax.x),
                    float2(_ColorMulMin.y, _ColorMulMax.y),
                    float2(_ColorMulMin.z, _ColorMulMax.z),
                    _LifetimeRange.xy,
                    _InitialDelayRange.xy,
                    float2(1.0, 1.0),
                    _StartSizeRangeXUc.xy,
                    _StartSizeRangeYUc.xy,
                    _StartSizeRangeZUc.xy,
                    state,
                    velocityUe,
                    sampledLocation,
                    colorMul,
                    sampledLifetime,
                    sampledDelay,
                    sizeUe);
            }
            else
            {
                L2Fx_MeshSpawnParticle_SampleLocVelSize(
                    _StartVelocityRangeXUc.xy,
                    _StartVelocityRangeYUc.xy,
                    _StartVelocityRangeZUc.xy,
                    _StartLocationRangeXUc.xy,
                    _StartLocationRangeYUc.xy,
                    _StartLocationRangeZUc.xy,
                    float2(_ColorMulMin.x, _ColorMulMax.x),
                    float2(_ColorMulMin.y, _ColorMulMax.y),
                    float2(_ColorMulMin.z, _ColorMulMax.z),
                    _LifetimeRange.xy,
                    _InitialDelayRange.xy,
                    float2(1.0, 1.0),
                    _StartSizeRangeXUc.xy,
                    _StartSizeRangeYUc.xy,
                    _StartSizeRangeZUc.xy,
                    state,
                    velocityUe,
                    sampledLocation,
                    colorMul,
                    sampledLifetime,
                    sampledDelay,
                    sizeUe);
            }
            locationUe = offsetUe + sampledLocation;
            lifetime = max(sampledLifetime, 1e-4);
            delay = sampledDelay;
        }
        else
        {
            velocityUe = float3(
                L2Fx_RandomRange(_StartVelocityRangeXUc.xy, _Seed, _StartTime, 17.0),
                L2Fx_RandomRange(_StartVelocityRangeYUc.xy, _Seed, _StartTime, 19.0),
                L2Fx_RandomRange(_StartVelocityRangeZUc.xy, _Seed, _StartTime, 23.0));
            if (_FullTlsShape > 0.5)
            {
                locationUe = offsetUe + L2Fx_SpritePolar_CartesianUe(
                    L2Fx_RandomRange(_PolarThetaRangeUc.xy, _Seed, _StartTime, 29.0),
                    L2Fx_RandomRange(_PolarPhiRangeUc.xy, _Seed, _StartTime, 31.0),
                    L2Fx_RandomRange(_PolarRadiusRangeUc.xy, _Seed, _StartTime, 37.0));
            }
            else
            {
                locationUe += float3(
                    L2Fx_RandomRange(_StartLocationRangeXUc.xy, _Seed, _StartTime, 29.0),
                    L2Fx_RandomRange(_StartLocationRangeYUc.xy, _Seed, _StartTime, 31.0),
                    L2Fx_RandomRange(_StartLocationRangeZUc.xy, _Seed, _StartTime, 37.0));
            }
            sizeUe = float3(
                L2Fx_RandomRange(_StartSizeRangeXUc.xy, _Seed, _StartTime, 41.0),
                L2Fx_RandomRange(_StartSizeRangeYUc.xy, _Seed, _StartTime, 43.0),
                L2Fx_RandomRange(_StartSizeRangeZUc.xy, _Seed, _StartTime, 47.0));
            colorMul = _ColorMulMin.xyz;
        }
        return;
    }

    if (_SpawnMode > 2.5)
    {
        if (state != 0u)
        {
            lifetime = max(L2Fx_FRange_GetRand(_LifetimeRange.xy, state), 1e-4);
            velocityUe.z = L2Fx_FRange_GetRand(_StartVelocityRangeZUc.xy, state);
            locationUe = L2Fx_StartLocationRange_ApplyUe(
                offsetUe,
                _StartLocationRangeXUc.xy,
                _StartLocationRangeYUc.xy,
                _StartLocationRangeZUc.xy,
                state);
        }
        else
        {
            velocityUe.z = L2Fx_RandomRange(
                _StartVelocityRangeZUc.xy, _Seed, _StartTime, 17.0);
        }
        return;
    }

    if (_SpawnMode > 0.5)
    {
        float sampledLocZ;
        float sampledVelZ;
        float sampledSizeZ;
        if (state != 0u)
        {
            L2Fx_MeshSpawnParticle_SampleLocVelSizeZ(
                _StartLocationZRangeUU.xy,
                _StartVelocityZRangeUU.xy,
                _StartSizeZRange.xy,
                float2(_StartSizeXY, _StartSizeXY),
                float2(_ColorMultiplier.x, _ColorMultiplier.x),
                float2(_ColorMultiplier.y, _ColorMultiplier.y),
                float2(_ColorMultiplier.z, _ColorMultiplier.z),
                _LifetimeRange.xy,
                _InitialDelayRange.xy,
                float2(1.0, 1.0),
                state,
                sampledLocZ,
                sampledVelZ,
                sampledSizeZ);
        }
        else
        {
            sampledLocZ = L2Fx_RandomRange(
                _StartLocationZRangeUU.xy, _Seed, _StartTime, 13.0);
            sampledVelZ = L2Fx_RandomRange(
                _StartVelocityZRangeUU.xy, _Seed, _StartTime, 17.0);
            sampledSizeZ = L2Fx_RandomRange(
                _StartSizeZRange.xy, _Seed, _StartTime, 11.0);
        }
        locationUe.z += sampledLocZ;
        velocityUe.z = sampledVelZ;
        sizeUe = float3(_StartSizeXY, _StartSizeXY, sampledSizeZ);
        return;
    }

    velocityUe = float3(
        L2Fx_RandomRange(_StartVelocityRangeXUc.xy, _Seed, _StartTime, 17.0),
        L2Fx_RandomRange(_StartVelocityRangeYUc.xy, _Seed, _StartTime, 19.0),
        L2Fx_RandomRange(_StartVelocityRangeZUc.xy, _Seed, _StartTime, 23.0));
    if (_SizeMode > 2.5)
    {
        float uniformSize = L2Fx_RandomRange(
            _StartSizeRange.xy, _Seed, _StartTime, 11.0);
        sizeUe = float3(uniformSize, uniformSize, uniformSize);
    }
    else if (_SizeMode > 1.5)
    {
        sizeUe = float3(
            L2Fx_RandomRange(_StartSizeRangeXUc.xy, _Seed, _StartTime, 29.0),
            L2Fx_RandomRange(_StartSizeRangeYUc.xy, _Seed, _StartTime, 31.0),
            L2Fx_RandomRange(_StartSizeRangeZUc.xy, _Seed, _StartTime, 37.0));
    }
    else if (_SizeMode > 0.5)
    {
        float sampledSizeZ = L2Fx_RandomRange(
            _StartSizeZRange.xy, _Seed, _StartTime, 11.0);
        sizeUe = float3(_StartSizeXY, _StartSizeXY, sampledSizeZ);
    }
}

void L2FxUnified_ResolveSpin(
    float ageSeconds,
    out float3 startSpinC012,
    out float3 spinRateC012)
{
    startSpinC012 = float3(0.0, 0.0, 0.0);
    spinRateC012 = float3(0.0, 0.0, 0.0);
    if (_SpinParticles <= 0.5)
        return;

    uint state = asuint(_StartSpinRandStateBits);
    float3 spinCcwOrCw = _SpinCCWorCW.xyz;
    if (state != 0u)
    {
        if (_SpinSpsMode > 0.5)
        {
            L2Fx_MeshSpin_SpawnYawPitchRollUruFromAppRandState(
                _StartSpinYawRangeUc.xy,
                _StartSpinPitchRangeUc.xy,
                _StartSpinRollRangeUc.xy,
                _SpsYawRangeUc.xy,
                _SpsPitchRangeUc.xy,
                _SpsRollRangeUc.xy,
                spinCcwOrCw,
                state,
                startSpinC012,
                spinRateC012);
            return;
        }

        uint spinState = state;
        float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
            _StartSpinYawRangeUc.xy,
            _StartSpinPitchRangeUc.xy,
            _StartSpinRollRangeUc.xy,
            spinState);
        L2Fx_FRangeVector_GetRandYawPitchRoll(
            _SpsYawRangeUc.xy,
            _SpsPitchRangeUc.xy,
            _SpsRollRangeUc.xy,
            spinState);
        float3 spinsPerSecondUc = _SpsYawPitchRollUc.xyz;
        L2Fx_MeshSpin_ApplySpinCCWorCW_Uc(spinsPerSecondUc, spinCcwOrCw, spinState);
        startSpinC012 = startSpinUc * L2FX_MESH_SPIN_UC_TO_URU;
        spinRateC012 =
            L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(spinsPerSecondUc);
        return;
    }

    startSpinC012 = L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
        _StartSpinYawRangeUc.xy,
        _StartSpinPitchRangeUc.xy,
        _StartSpinRollRangeUc.xy,
        _Seed,
        _StartTime);
    float3 spsUc = _SpinSpsMode > 0.5
        ? float3(
            L2Fx_RandomRange(_SpsYawRangeUc.xy, _Seed, _StartTime, 41.0),
            L2Fx_RandomRange(_SpsPitchRangeUc.xy, _Seed, _StartTime, 43.0),
            L2Fx_RandomRange(_SpsRollRangeUc.xy, _Seed, _StartTime, 47.0))
        : _SpsYawPitchRollUc.xyz;
    spinRateC012 = L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
        L2Fx_MeshSpin_ApplySpinCCWorCW_Hashed(
            spsUc, spinCcwOrCw, _Seed, _StartTime));
}

Varyings vert(Attributes IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    Varyings OUT;

    float3 locationUe;
    float3 velocityUe;
    float3 sizeUe;
    float3 colorMul;
    float lifetime;
    float delay;
    L2FxUnified_ResolveSpawn(
        locationUe, velocityUe, sizeUe, colorMul, lifetime, delay);
    locationUe = L2Fx_ApplySpawnLocationAddUe(locationUe);
    if (_PtvdMode > 1.5)
        velocityUe = L2FxPTVD_OwnerAndStartPosition(
            velocityUe, locationUe, float3(0.0, 0.0, 0.0));
    else if (_PtvdMode > 0.5)
        velocityUe = L2FxPTVD_StartPositionAndOwner(
            velocityUe, locationUe, float3(0.0, 0.0, 0.0));
    // SpawnMode 2 samples Size FRangeVector XYZ for TLS, then returns.
    // SizeMode 0 = UniformSize: keep X only (L2Fx_MeshSize_ApplyUniformSize).
    if (_SizeMode < 0.5)
        sizeUe = L2Fx_MeshSize_ApplyUniformSize(sizeUe);

    float ageSeconds = L2FxUnified_ResolveAge(lifetime, delay);
    float ageNorm = saturate(ageSeconds / lifetime);
    float sizeScale = L2FxUnified_ResolveSizeScale(ageNorm);
    float3 finalSizeUe = float3(
        L2Fx_GetFinalMeshScale(sizeUe.x, sizeScale, _L2FxWorldCalibration),
        L2Fx_GetFinalMeshScale(sizeUe.y, sizeScale, _L2FxWorldCalibration),
        L2Fx_GetFinalMeshScale(sizeUe.z, sizeScale, _L2FxWorldCalibration));

    float3 startSpinC012;
    float3 spinRateC012;
    L2FxUnified_ResolveSpin(ageSeconds, startSpinC012, spinRateC012);

    float3 localMeshOS;
    if (_TransformMode > 0.5)
    {
        localMeshOS = L2FxPTRSActor_TransformLocalMeshUnity(
            IN.positionOS.xyz,
            finalSizeUe,
            spinRateC012,
            startSpinC012,
            ageSeconds);
    }
    else
    {
        // Imported mesh axes are UE(X,Z,Y) in Unity.
        localMeshOS = IN.positionOS.xyz
            * float3(finalSizeUe.x, finalSizeUe.z, finalSizeUe.y);
        if (_SpinParticles > 0.5)
        {
            float3 yawPitchRollUru = L2Fx_MeshSpin_EvaluateYawPitchRollUru(
                startSpinC012, spinRateC012, ageSeconds);
            float3 pitchYawRollRadians =
                L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(
                    L2Fx_MeshSpin_YawPitchRollUruToRadians(yawPitchRollUru));
            localMeshOS = L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
                localMeshOS, pitchYawRollRadians);
        }
    }

    float3 evaluatedLocationUe = locationUe;
    if (_MotionMode > 1.5)
    {
        evaluatedLocationUe = L2Fx_MeshMotion_EvaluatePositionUeWithDrag(
            locationUe,
            velocityUe,
            _AccelerationUc.xyz,
            _VelocityLossRangeUc.xyz,
            ageSeconds);
    }
    else if (_MotionMode > 0.5)
    {
        evaluatedLocationUe = L2Fx_MeshMotion_EvaluatePositionUe(
            locationUe,
            velocityUe,
            _AccelerationUc.xyz,
            ageSeconds);
    }

    float3 motionOS = L2Fx_UcPositionToUnityMeters(
        evaluatedLocationUe, _L2FxWorldCalibration);
    float3 positionWS = TransformObjectToWorld(motionOS + localMeshOS);
    OUT.positionHCS = TransformWorldToHClip(positionWS);
    OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

    float visible = (_UseManualAge > 0.5
        || (ageSeconds >= 0.0 && ageSeconds < lifetime)) ? 1.0 : 0.0;
    OUT.color = (half4)(
        L2FxUnified_ResolveColor(ageSeconds, lifetime, colorMul) * visible);
    OUT.debugData = float4(localMeshOS, ageSeconds);
    return OUT;
}

half4 frag(Varyings IN) : SV_Target
{
    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    if (_AlphaClipThreshold >= 0.0)
        clip(tex.a - (half)_AlphaClipThreshold);

    half4 color;
    if (_UseSecondTex > 0.5)
    {
        float2 uv1 = IN.uv * _SecondTex_ST.xy + _SecondTex_ST.zw;
        half4 tex1 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, uv1);
        color = L2Fx_D3d9_Modulate2xTwoTexTFactor(tex, tex1, IN.color);
    }
    else
    {
        color = half4(
            tex.rgb * IN.color.rgb * (half)_RgbBoost,
            tex.a * IN.color.a);
    }
    color.rgb += (half3)(IN.debugData.xyz * (_DebugMeshOut * 1e-10));
    return color;
}

#endif
