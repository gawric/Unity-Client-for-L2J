// it_teleport_v1_ca / MeshEmitter2 "Column"
// UC: etc_spawn00 / fx_m_t0053, StartSize=(0.8,0.8,0.6) non-uniform,
// ColorScale 3 keys + Repeats=300, ColorMul=(0.2,0.25,0.25), Opacity=0.39,
// FadeInEnd=1.68, FadeOutStart=28, Lifetime=28, InitialDelay=2 (prefab _startDelay),
// StartLocationOffset Z=-6, SpinParticles SPS.X=0.05 SpinCCWorCW.X=1,
// SizeScale Keys2 (0.92→1, 1→0.1), UseRegularSizeScale=False,
// no DrawStyle → TeleportCaColumn RenderDoc: PTDS_Translucent (One One).
Shader "L2/Effects/it_teleport_v1_ca/MeshEmitter2_Column"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0053)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        // Prefab MeshEmitter._startDelay=2 already applies UC InitialDelay — keep 0 here.
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (28, 28, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 28)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSizeXY ("StartSize X/Y", Float) = 0.8
        _StartSizeZ ("StartSize Z", Float) = 0.6
        _StartLocationOffsetUU ("StartLocationOffset UE X Y Z", Vector) = (0, 0, -6, 0)

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0.92, 1, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (1, 0.1, 0, 0)

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpin X / Yaw", Vector) = (0, 0, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / Pitch", Vector) = (0, 0, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / Roll", Vector) = (0, 0, 0, 0)
        _SpsYawPitchRollUc ("SpinsPerSecond Yaw Pitch Roll", Vector) = (0.05, 0, 0, 0)
        // UC SpinCCWorCW.X=1. L2FxMeshSpin/Wave: 0 => negate, else +1.
        // Live Column slot showed Yaw=-3276.75 with UC=1 — if Unity spins the
        // wrong way, fix sign in L2FxMeshSpin (do not invent effect-local flip).
        _SpinCCWorCW ("SpinCCWorCW X Y Z", Vector) = (1, 0, 0, 0)

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (0.2, 0.25, 0.25, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 300
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("ColorScale 1 Time", Range(0, 1)) = 0.685714
        _ColorKey1 ("ColorScale 1", Color) = (0.654902, 0.654902, 0.654902, 1)
        _ColorKey2Time ("ColorScale 2 Time", Range(0, 1)) = 1
        _ColorKey2 ("ColorScale 2", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 1.68
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 28
        _Opacity ("Opacity", Range(0, 2)) = 0.39
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Translucent — L2FxPTDS_DrawStyle.hlsl (TeleportCaColumn RenderDoc One+One)
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Column"
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
                float _StartSizeXY;
                float _StartSizeZ;
                float4 _StartLocationOffsetUU;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
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
                // Live-proven SizeScale: param=1, repeats=0. Keys2 from UC.
                return L2Fx_MeshSizeScale_ScalarFromKeys5(
                    ageNorm,
                    _UseSizeScale,
                    0.0,
                    1.0,
                    0.0,
                    2,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    1.0, 0.1,
                    1.0, 0.1,
                    1.0, 0.1);
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
                    1.0, _ColorKey2,
                    1.0, _ColorKey2,
                    1.0, _ColorKey2);
                return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    color, _L2SpriteColorGammaToLinear);
            }

            float ResolveParticleVisibility(float ageSeconds, float lifetime)
            {
                if (_UseManualAge > 0.5)
                {
                    return 1.0;
                }
                return (ageSeconds >= 0.0 && ageSeconds < lifetime) ? 1.0 : 0.0;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetime;
                float ageSeconds = ResolveAgeSeconds(lifetime);
                float ageNorm = saturate(ageSeconds / lifetime);
                float sizeScale = ResolveSizeScale(ageNorm);

                float scaleXY = L2Fx_GetFinalMeshScale(
                    _StartSizeXY, sizeScale, _L2FxWorldCalibration);
                float scaleZ = L2Fx_GetFinalMeshScale(
                    _StartSizeZ, sizeScale, _L2FxWorldCalibration);
                // UE FinalSize (X,Y,Z) -> Unity local scale (X,Z,Y). Same as Wave.
                float3 localMeshOS = IN.positionOS.xyz * float3(scaleXY, scaleZ, scaleXY);

                if (_SpinParticles > 0.5)
                {
                    float3 startYawPitchRollUru =
                        L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            _Seed,
                            _StartTime);
                    // SpinCCWorCW==0 => negate (L2FxMeshSpin / Wave).
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

                float3 locationOffset = L2Fx_UcPositionToUnityMeters(
                    _StartLocationOffsetUU.xyz,
                    _L2FxWorldCalibration);
                float3 positionWS = TransformObjectToWorld(locationOffset + localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * ResolveParticleVisibility(ageSeconds, lifetime));
                return OUT;
            }

            // TeleportCaColumn RenderDoc FF: out = sample(t0) * textureFactor.
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
