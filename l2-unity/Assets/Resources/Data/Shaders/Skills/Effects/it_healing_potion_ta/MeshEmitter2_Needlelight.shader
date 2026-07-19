// it_healing_potion_ta / MeshEmitter2 "needlelight"
// UC: PTRS_Actor, SpinParticles, SizeScale, FadeIn/Out, PTDS_Brighten, UseMeshBlendMode=False
Shader "L2/Effects/it_healing_potion_ta/MeshEmitter2_Needlelight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (1.2, 1.2, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 1.2)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSize ("StartSize X/Y/Z", Float) = 0.0216
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0.14, 6, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.37, 8, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (0.56, 8.5, 0, 0)
        _SizeKey3 ("SizeScale 3 Time Size", Vector) = (1, 9, 0, 0)
        _SizeKey4 ("SizeScale 4 Time Size", Vector) = (1, 9, 0, 0)

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpin X / Yaw", Vector) = (0, 1, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / Pitch", Vector) = (0, 1, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / Roll", Vector) = (0, 0, 0, 0)
        _SpsYawPitchRollUc ("SpinsPerSecond Yaw Pitch Roll", Vector) = (0, 0, 0, 0)
        _StartSpinRandStateBits ("appRand TLS before StartSpin (uint bits)", Float) = 0

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 0.412, 0.023, 0)
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 0.03
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 0.28
        _Opacity ("Opacity", Range(0, 2)) = 1
        // ON when atlas sRGB=OFF and ColorMul looks too bright in Linear.
        // Lib: Decompile_Common/L2FxSpriteColorGammaLinear.hlsl (kirakira + this needlelight).
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Brighten — live D3DDrv + RenderDoc
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Needlelight"
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
            #include "../../Common/Decompile_Common/L2FxPTRS_Actor.hlsl"
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
                float4 _SpsYawPitchRollUc;
                float _StartSpinRandStateBits;
                float4 _ColorMultiplier;
                float4 _ColorKey0;
                float4 _ColorKey1;
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
                    4,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y);
            }

            float4 ResolveColor(float ageSeconds, float lifetime)
            {
                // Two white ColorScale keys (RelativeTime 0 and 1).
                float4 color = L2Fx_MeshColorFade_FullKeys6(
                    ageSeconds,
                    lifetime,
                    0.0,
                    _ColorMultiplier.xyz,
                    _FadeIn,
                    _FadeInEndTime,
                    _FadeOut,
                    _FadeOutStartTime,
                    _Opacity,
                    _ColorKey0,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1);
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
                float finalScale = L2Fx_GetFinalMeshScale(_StartSize, sizeScale, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * finalScale;

                if (_SpinParticles > 0.5)
                {
                    float3 startYawPitchRollUru =
                        L2Fx_MeshSpin_StartYawPitchRollUruFromAppRandState(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            asuint(_StartSpinRandStateBits));
                    // PTRS_Actor spin uses same trunc(rate*t+start) then RotationURU(c1,c0,c2).
                    float3 spinRateC012 = L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
                        _SpsYawPitchRollUc.xyz,
                        float3(1.0, 1.0, 1.0));
                    // MeshSpin Evaluate expects (Yaw,Pitch,Roll); PTRS EvaluateSpin swaps for matrix.
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
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * ResolveParticleVisibility(ageSeconds, lifetime));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
