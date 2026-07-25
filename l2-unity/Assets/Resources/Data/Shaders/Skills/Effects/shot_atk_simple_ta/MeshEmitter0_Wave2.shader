// shot_atk_simple_ta / MeshEmitter0 "Wave2"
// UC: shockwave00, UseMeshBlendMode=False, RenderTwoSided, SpinParticles,
//   SizeScale grow, FadeOut only (0.0375), Opacity=0.49, ColorScale×2 white,
//   ColorMul (1,1,1), StartSize (0.15, 0.15, 0.15) isotropic, Life=0.25,
//   StartLocationRange X=-3 (Y/Z=0), StartVelocity X=3, Acceleration=0,
//   StartSpin Z only [0,1], no SPS / no PTRS.
// Near 1:1 retarget of shot_N_atk_v1_ta MeshEmitter226 "ShockWave" — only
// numeric UC values changed, formulas/library calls are identical.
// RelativeWarmupTime: 0 (ParticleGroup setting, not a shader property).
// Blend: LIVE RenderDoc One/One Add (PTDS_Translucent). UC lists PTDS_Brighten.
//
// Spawn: L2FxMeshSpawnParticle_SampleLocVelSize (same Wave/Spirit stream).
// Spin: L2FxMeshSpin StartSpin only (SPS ranges 0).
Shader "L2/Effects/shot_atk_simple_ta/MeshEmitter0_Wave2"
{
    Properties
    {
        _MainTex ("Texture (shockwave00)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.25, 0.25, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 0.25)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartLocationOffsetUe ("StartLocationOffset UE XYZ", Vector) = (0, 0, 0, 0)
        // UC StartLocationRange X=-3 (Min=Max=-3); Y/Z=0.
        _StartLocationRangeXUc ("StartLocationRange X", Vector) = (-3, -3, 0, 0)
        _StartLocationRangeYUc ("StartLocationRange Y", Vector) = (0, 0, 0, 0)
        _StartLocationRangeZUc ("StartLocationRange Z", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (3, 3, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (0, 0, 0, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (0, 0, 0, 0)
        _StartSizeRangeXUc ("StartSize X Min Max", Vector) = (0.15, 0.15, 0, 0)
        _StartSizeRangeYUc ("StartSize Y Min Max", Vector) = (0.15, 0.15, 0, 0)
        _StartSizeRangeZUc ("StartSize Z Min Max", Vector) = (0.15, 0.15, 0, 0)
        _MeshSpawnRandStateBits ("appRand TLS before StartVelocity (uint bits)", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        // UC SizeScale(0..3): (0.09,1.45) (0.23,1.8) (0.62,2) (1,2.2)
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0.09, 1.45, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.23, 1.8, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (0.62, 2, 0, 0)
        _SizeKey3 ("SizeScale 3 Time Size", Vector) = (1, 2.2, 0, 0)
        _SizeKey4 ("SizeScale 4 Time Size", Vector) = (1, 2.2, 0, 0)

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        // UC StartSpinRange=(Z=(Max=1)); X/Y default 0.
        _StartSpinYawRangeUc ("StartSpin X / Yaw", Vector) = (0, 0, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / Pitch", Vector) = (0, 0, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / Roll", Vector) = (0, 1, 0, 0)
        // No SpinsPerSecondRange in UC -> 0 (orientation only).
        _SpsYawRangeUc ("SpinsPerSecond X / Yaw", Vector) = (0, 0, 0, 0)
        _SpsPitchRangeUc ("SpinsPerSecond Y / Pitch", Vector) = (0, 0, 0, 0)
        _SpsRollRangeUc ("SpinsPerSecond Z / Roll", Vector) = (0, 0, 0, 0)
        _SpinCCWorCW ("SpinCCWorCW X Y Z", Vector) = (1, 1, 1, 0)
        _StartSpinRandStateBits ("appRand TLS before StartSpin (uint bits)", Float) = 0

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 1, 1, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 0
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("ColorScale 1 Time", Float) = 1
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("ColorScale 2 Time", Float) = 1
        _ColorKey2 ("ColorScale 2", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 0
        _FadeInEndTime ("FadeIn End sec", Float) = 0
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 0.0375
        _Opacity ("Opacity", Range(0, 2)) = 0.49
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // LIVE: BlendEnable True, Src=One Dst=One Op=Add (color+alpha), write RGBA.
        // L2FxPTDS_DrawStyle Translucent — not Brighten (UC DrawStyle label).
        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Wave2"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSpawnParticle.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshMotion.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSizeScale.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshColorFade.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSpin.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTDS_DrawStyle.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _StartTime;
                float _Seed;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _UseManualAge;
                float _ManualAge;
                float _L2FxWorldCalibration;
                float4 _StartLocationOffsetUe;
                float4 _StartLocationRangeXUc;
                float4 _StartLocationRangeYUc;
                float4 _StartLocationRangeZUc;
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float4 _AccelerationUc;
                float4 _StartSizeRangeXUc;
                float4 _StartSizeRangeYUc;
                float4 _StartSizeRangeZUc;
                float _MeshSpawnRandStateBits;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float _SpinParticles;
                float4 _StartSpinYawRangeUc;
                float4 _StartSpinPitchRangeUc;
                float4 _StartSpinRollRangeUc;
                float4 _SpsYawRangeUc;
                float4 _SpsPitchRangeUc;
                float4 _SpsRollRangeUc;
                float4 _SpinCCWorCW;
                float _StartSpinRandStateBits;
                float4 _ColorMultiplier;
                float _ColorScaleRepeats;
                float4 _ColorKey0;
                float _ColorKey1Time;
                float4 _ColorKey1;
                float _ColorKey2Time;
                float4 _ColorKey2;
                float _FadeIn;
                float _FadeInEndTime;
                float _FadeOut;
                float _FadeOutStartTime;
                float _Opacity;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR0;
            };

            float ResolveSizeScale(float ageNorm)
            {
                return L2Fx_MeshSizeScale_ScalarFromKeys5(
                    ageNorm,
                    _UseSizeScale,
                    0.0,
                    1.0,
                    0.0,
                    4,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y);
            }

            float4 ResolveColor(float ageSeconds, float lifetime, float3 colorMulRgb)
            {
                float4 color = L2Fx_MeshColorFade_FullKeys6(
                    ageSeconds,
                    lifetime,
                    _ColorScaleRepeats,
                    colorMulRgb,
                    _FadeIn,
                    _FadeInEndTime,
                    _FadeOut,
                    _FadeOutStartTime,
                    _Opacity,
                    _ColorKey0,
                    _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2,
                    1.0, _ColorKey2,
                    1.0, _ColorKey2,
                    1.0, _ColorKey2);
                return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    color, _L2SpriteColorGammaToLinear);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 velocityUe;
                float3 locationUe;
                float3 colorMulRgb;
                float lifetimeSeconds;
                float initialDelaySeconds;
                float3 sizeUe;

                uint meshSpawnState = asuint(_MeshSpawnRandStateBits);
                if (meshSpawnState != 0u)
                {
                    L2Fx_MeshSpawnParticle_SampleLocVelSize(
                        _StartVelocityRangeXUc.xy,
                        _StartVelocityRangeYUc.xy,
                        _StartVelocityRangeZUc.xy,
                        _StartLocationRangeXUc.xy,
                        _StartLocationRangeYUc.xy,
                        _StartLocationRangeZUc.xy,
                        float2(_ColorMultiplier.x, _ColorMultiplier.x),
                        float2(_ColorMultiplier.y, _ColorMultiplier.y),
                        float2(_ColorMultiplier.z, _ColorMultiplier.z),
                        _LifetimeRange.xy,
                        _InitialDelayRange.xy,
                        float2(1.0, 1.0),
                        _StartSizeRangeXUc.xy,
                        _StartSizeRangeYUc.xy,
                        _StartSizeRangeZUc.xy,
                        meshSpawnState,
                        velocityUe,
                        locationUe,
                        colorMulRgb,
                        lifetimeSeconds,
                        initialDelaySeconds,
                        sizeUe);
                }
                else
                {
                    lifetimeSeconds = max(
                        L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
                    initialDelaySeconds = L2Fx_RandomInitialDelay(
                        _InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                    velocityUe = float3(
                        L2Fx_RandomRange(_StartVelocityRangeXUc.xy, _Seed, _StartTime, 17.0),
                        L2Fx_RandomRange(_StartVelocityRangeYUc.xy, _Seed, _StartTime, 19.0),
                        L2Fx_RandomRange(_StartVelocityRangeZUc.xy, _Seed, _StartTime, 23.0));
                    locationUe = float3(0.0, 0.0, 0.0);
                    sizeUe = float3(
                        L2Fx_RandomRange(_StartSizeRangeXUc.xy, _Seed, _StartTime, 29.0),
                        L2Fx_RandomRange(_StartSizeRangeYUc.xy, _Seed, _StartTime, 31.0),
                        L2Fx_RandomRange(_StartSizeRangeZUc.xy, _Seed, _StartTime, 37.0));
                    colorMulRgb = _ColorMultiplier.xyz;
                }

                lifetimeSeconds = max(lifetimeSeconds, 1e-4);
                float ageSeconds;
                if (_UseManualAge > 0.5)
                {
                    ageSeconds = _ManualAge;
                }
                else
                {
                    ageSeconds = _StartTime > 0.0
                        ? L2Fx_AgeSeconds(_Time.y, _StartTime, initialDelaySeconds)
                        : max(0.0, _Time.y - initialDelaySeconds);
                }

                float ageNorm = saturate(ageSeconds / lifetimeSeconds);
                float sizeScale = ResolveSizeScale(ageNorm);

                float scaleX = L2Fx_GetFinalMeshScale(sizeUe.x, sizeScale, _L2FxWorldCalibration);
                float scaleY = L2Fx_GetFinalMeshScale(sizeUe.y, sizeScale, _L2FxWorldCalibration);
                float scaleZ = L2Fx_GetFinalMeshScale(sizeUe.z, sizeScale, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * float3(scaleX, scaleZ, scaleY);

                if (_SpinParticles > 0.5)
                {
                    uint spinState = asuint(_StartSpinRandStateBits);
                    float3 startYawPitchRollUru;
                    float3 spsUc;
                    if (spinState != 0u)
                    {
                        float3 startSpinUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            spinState);
                        startYawPitchRollUru = startSpinUc * L2FX_MESH_SPIN_UC_TO_URU;
                        spsUc = L2Fx_FRangeVector_GetRandYawPitchRoll(
                            _SpsYawRangeUc.xy,
                            _SpsPitchRangeUc.xy,
                            _SpsRollRangeUc.xy,
                            spinState);
                    }
                    else
                    {
                        startYawPitchRollUru = L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            _Seed,
                            _StartTime);
                        spsUc = float3(0.0, 0.0, 0.0);
                    }

                    float3 directionSign = float3(
                        _SpinCCWorCW.x == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.y == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.z == 0.0 ? -1.0 : 1.0);
                    float3 spinRateC012 = L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
                        spsUc, directionSign);
                    float3 yawPitchRollUru = L2Fx_MeshSpin_EvaluateYawPitchRollUru(
                        startYawPitchRollUru, spinRateC012, ageSeconds);
                    float3 pitchYawRollRadians = L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(
                        L2Fx_MeshSpin_YawPitchRollUruToRadians(yawPitchRollUru));
                    localMeshOS = L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
                        localMeshOS, pitchYawRollRadians);
                }

                float3 startLocUe = locationUe + _StartLocationOffsetUe.xyz;
                float3 locUe = L2Fx_MeshMotion_EvaluatePositionUe(
                    startLocUe,
                    velocityUe,
                    _AccelerationUc.xyz,
                    ageSeconds);
                float3 motionOS = L2Fx_UcPositionToUnityMeters(locUe, _L2FxWorldCalibration);

                float3 positionWS = TransformObjectToWorld(motionOS + localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                float visible = (_UseManualAge > 0.5 || (ageSeconds >= 0.0 && ageSeconds < lifetimeSeconds))
                    ? 1.0 : 0.0;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetimeSeconds, colorMulRgb) * visible);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return half4(
                    tex.rgb * IN.color.rgb * (half)_RgbBoost,
                    tex.a * IN.color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
