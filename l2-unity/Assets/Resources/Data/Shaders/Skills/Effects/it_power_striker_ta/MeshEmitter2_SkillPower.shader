// it_power_striker_ta / MeshEmitter2 "MeshEmitter2" (s_u002_a)
// UC: skill_power01, SpinParticles StartSpin XYZ Max=1 (no SPS),
// SizeScale Keys3 (0→1, 0.7→0.5, 1→0.01), StartSizeRange 0.11..0.22,
// ColorScale 5 keys + Repeats=15, Opacity=0.72, FadeIn/Out,
// no DrawStyle → PTDS_Translucent (Blend One One), RenderTwoSided.
// Libs: MeshSpin, MeshSizeScale, MeshColorFade, GeometryTest, PTDS.
Shader "L2/Effects/it_power_striker_ta/MeshEmitter2_SkillPower"
{
    Properties
    {
        _MainTex ("Texture (mesh material)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.45, 0.45, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 1)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSizeRange ("StartSize Min Max (Uniform)", Vector) = (0.11, 0.22, 0, 0)

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0, 1, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.7, 0.5, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (1, 0.01, 0, 0)

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpin X / Yaw", Vector) = (0, 1, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / Pitch", Vector) = (0, 1, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / Roll", Vector) = (0, 1, 0, 0)
        _SpsYawPitchRollUc ("SpinsPerSecond Yaw Pitch Roll", Vector) = (0, 0, 0, 0)
        _SpinCCWorCW ("SpinCCWorCW X Y Z", Vector) = (0, 0, 0, 0)

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 1, 1, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 15
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("ColorScale 1 Time", Range(0, 1)) = 0.357143
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 0.74902)
        _ColorKey2Time ("ColorScale 2 Time", Range(0, 1)) = 0.789286
        _ColorKey2 ("ColorScale 2", Color) = (0.501961, 0.501961, 0.501961, 1)
        _ColorKey3Time ("ColorScale 3 Time", Range(0, 1)) = 0.914286
        _ColorKey3 ("ColorScale 3", Color) = (1, 1, 1, 0.74902)
        _ColorKey4Time ("ColorScale 4 Time", Range(0, 1)) = 1
        _ColorKey4 ("ColorScale 4", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 0.1845
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 0.252
        _Opacity ("Opacity", Range(0, 2)) = 0.72
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "MeshEmitter2_SkillPower"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
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
                float4 _StartSizeRange;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float _SpinParticles;
                float4 _StartSpinYawRangeUc;
                float4 _StartSpinPitchRangeUc;
                float4 _StartSpinRollRangeUc;
                float4 _SpsYawPitchRollUc;
                float4 _SpinCCWorCW;
                float4 _ColorMultiplier;
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
                float _FadeIn;
                float _FadeInEndTime;
                float _FadeOut;
                float _FadeOutStartTime;
                float _Opacity;
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

            float ResolveAgeSeconds(out float lifetime)
            {
                lifetime = max(L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
                if (_UseManualAge > 0.5)
                {
                    return _ManualAge;
                }

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                return _StartTime > 0.0
                    ? L2Fx_AgeSeconds(_Time.y, _StartTime, delay)
                    : max(0.0, _Time.y - delay);
            }

            float ResolveSizeScale(float ageNorm)
            {
                return L2Fx_MeshSizeScale_ScalarFromKeys5(
                    ageNorm,
                    _UseSizeScale,
                    0.0,
                    1.0,
                    0.0,
                    3,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    1.0, 0.01,
                    1.0, 0.01);
            }

            float4 ResolveColor(float ageSeconds, float lifetime)
            {
                float4 color = L2Fx_MeshColorFade_FullKeys6(
                    ageSeconds,
                    lifetime,
                    _ColorScaleRepeats,
                    _ColorMultiplier.xyz,
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
                    1.0, _ColorKey4);
                return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    color, _L2SpriteColorGammaToLinear);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetime;
                float ageSeconds = ResolveAgeSeconds(lifetime);
                float ageNorm = saturate(ageSeconds / lifetime);
                float sizeScale = ResolveSizeScale(ageNorm);

                float startSize = L2Fx_RandomRange(_StartSizeRange.xy, _Seed, _StartTime, 11.0);
                float finalScale = L2Fx_GetFinalMeshScale(startSize, sizeScale, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * finalScale;

                if (_SpinParticles > 0.5)
                {
                    float3 startYawPitchRollUru =
                        L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            _Seed,
                            _StartTime);
                    float3 directionSign = float3(
                        _SpinCCWorCW.x == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.y == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.z == 0.0 ? -1.0 : 1.0);
                    float3 spinRateC012 = L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
                        _SpsYawPitchRollUc.xyz,
                        directionSign);
                    float3 yawPitchRollUru = L2Fx_MeshSpin_EvaluateYawPitchRollUru(
                        startYawPitchRollUru,
                        spinRateC012,
                        ageSeconds);
                    float3 pitchYawRollRadians = L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(
                        L2Fx_MeshSpin_YawPitchRollUruToRadians(yawPitchRollUru));
                    localMeshOS = L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
                        localMeshOS,
                        pitchYawRollRadians);
                }

                float3 positionWS = TransformObjectToWorld(localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                float visible = (_UseManualAge > 0.5 || (ageSeconds >= 0.0 && ageSeconds < lifetime)) ? 1.0 : 0.0;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * visible);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * IN.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
