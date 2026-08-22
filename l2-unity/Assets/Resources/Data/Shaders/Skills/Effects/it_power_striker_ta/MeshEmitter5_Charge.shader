// it_power_striker_ta / MeshEmitter5 (s_u002_a)
// UC: skill_charge01, DrawStyle=PTDS_Brighten, SpinParticles SPS=0.3 + StartSpin Max=1,
// UniformSize 0.275, UseSizeScale (no SizeScale keys → mul=1),
// StartVelocityRange ±33 XYZ, VelocityLoss=2, InitialDelay=0.5, Lifetime=0.4,
// FadeIn/Out, no UseColorScale (white + fades only).
// Libs: MeshSpin, MeshMotion WithDrag, MeshColorFade, GeometryTest, PTDS.
// NOTE: skill_charge01 FBX not in repo yet — shader ready, mesh asset missing.
Shader "L2/Effects/it_power_striker_ta/MeshEmitter5_Charge"
{
    Properties
    {
        _MainTex ("Texture (mesh material)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        // Prefab may already apply UC InitialDelay=0.5 — keep here for shader-only preview.
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0.5, 0.5, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.4, 0.4, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 1)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSize ("StartSize (UniformSize)", Float) = 0.275

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpin X / Yaw", Vector) = (0, 1, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / Pitch", Vector) = (0, 1, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / Roll", Vector) = (0, 1, 0, 0)
        _SpsYawPitchRollUc ("SpinsPerSecond Yaw Pitch Roll", Vector) = (0.3, 0.3, 0.3, 0)
        _SpinCCWorCW ("SpinCCWorCW X Y Z", Vector) = (0, 0, 0, 0)

        _StartVelocityRangeXUc ("StartVelocity X Min Max UU", Vector) = (-33, 33, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max UU", Vector) = (-33, 33, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max UU", Vector) = (-33, 33, 0, 0)
        _VelocityLossRangeUc ("VelocityLoss per-axis 1/s", Vector) = (2, 2, 2, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (0, 0, 0, 0)

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 1, 1, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 0
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("ColorScale 1 Time", Range(0, 1)) = 1
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 0.039
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 0.069
        _Opacity ("Opacity", Range(0, 2)) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Brighten — L2FxPTDS_DrawStyle.hlsl
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off

        Pass
        {
            Name "MeshEmitter5_Charge"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshColorFade.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSpin.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshMotion.hlsl"
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
                float _StartSize;
                float _SpinParticles;
                float4 _StartSpinYawRangeUc;
                float4 _StartSpinPitchRangeUc;
                float4 _StartSpinRollRangeUc;
                float4 _SpsYawPitchRollUc;
                float4 _SpinCCWorCW;
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float4 _VelocityLossRangeUc;
                float4 _AccelerationUc;
                float4 _ColorMultiplier;
                float _ColorScaleRepeats;
                float4 _ColorKey0;
                float _ColorKey1Time;
                float4 _ColorKey1;
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
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1);
                return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    color, _L2SpriteColorGammaToLinear);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetime;
                float ageSeconds = ResolveAgeSeconds(lifetime);

                float finalScale = L2Fx_GetFinalMeshScale(_StartSize, 1.0, _L2FxWorldCalibration);
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

                float3 startVelUe = float3(
                    L2Fx_RandomRange(_StartVelocityRangeXUc.xy, _Seed, _StartTime, 101.0),
                    L2Fx_RandomRange(_StartVelocityRangeYUc.xy, _Seed, _StartTime, 103.0),
                    L2Fx_RandomRange(_StartVelocityRangeZUc.xy, _Seed, _StartTime, 107.0));
                float3 locUe = L2Fx_MeshMotion_EvaluatePositionUeWithDrag(
                    float3(0, 0, 0),
                    startVelUe,
                    _AccelerationUc.xyz,
                    _VelocityLossRangeUc.xyz,
                    ageSeconds);
                float3 motionOS = L2Fx_UcPositionToUnityMeters(locUe, _L2FxWorldCalibration);

                float3 positionWS = TransformObjectToWorld(motionOS + localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                float visible = (_UseManualAge > 0.5 || (ageSeconds >= 0.0 && ageSeconds < lifetime)) ? 1.0 : 0.0;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * visible);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color;
                col.rgb *= (half)_RgbBoost;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
