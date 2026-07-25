// it_teleport_v1_ca / MeshEmitter1 "MC"
// UC: etc_spawn01 / fx_m_t0053, UniformSize StartSize=0.6, MaxParticles=1,
// ColorScale 3 keys + Repeats=300, ColorMul=(0.4, 0.5, 0.9),
// FadeInEnd=0.6, FadeOutStart=30, Lifetime=30, ForcedFade,
// StartLocationOffset Z=7 (no PolarShape — live locLocal=(0,0,7)),
// UseSizeScale Keys6, UseRegularSizeScale=False,
// no DrawStyle in UC → live/legacy TeleportCaMesh: PTDS_Translucent (One One),
// RenderTwoSided, no Spin.
Shader "L2/Effects/it_teleport_v1_ca/MeshEmitter1_MC"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0053)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (30, 30, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 30)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSize ("StartSize (UniformSize)", Float) = 0.6
        _StartLocationOffsetUU ("StartLocationOffset UE X Y Z", Vector) = (0, 0, 7, 0)

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0, 0.8, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.5, 1.3, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (0.95, 1.3, 0, 0)
        _SizeKey3 ("SizeScale 3 Time Size", Vector) = (0.98, 1, 0, 0)
        _SizeKey4 ("SizeScale 4 Time Size", Vector) = (0.99, 0.7, 0, 0)
        _SizeKey5 ("SizeScale 5 Time Size", Vector) = (1, 0.2, 0, 0)

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (0.4, 0.5, 0.9, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 300
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("ColorScale 1 Time", Range(0, 1)) = 0.725
        _ColorKey1 ("ColorScale 1", Color) = (0.717647, 0.717647, 0.717647, 1)
        _ColorKey2Time ("ColorScale 2 Time", Range(0, 1)) = 1
        _ColorKey2 ("ColorScale 2", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 0.6
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 30
        _Opacity ("Opacity", Range(0, 2)) = 1
        // fx_m_t0053 is sRGB ON — leave off unless Linear midtones look wrong.
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Translucent — L2FxPTDS_DrawStyle.hlsl (TeleportCaMesh RenderDoc One+One)
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "MC"
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
                float4 _StartLocationOffsetUU;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float4 _SizeKey5;
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
                // Live-proven: param=1, repeats=0, Keys6, Regular=False.
                return L2Fx_MeshSizeScale_ScalarFromKeys6(
                    ageNorm,
                    _UseSizeScale,
                    0.0,
                    1.0,
                    0.0,
                    6,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y,
                    _SizeKey5.x, _SizeKey5.y);
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
                float finalScale = L2Fx_GetFinalMeshScale(_StartSize, sizeScale, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * finalScale;
                float3 locationOffset = L2Fx_UcPositionToUnityMeters(
                    _StartLocationOffsetUU.xyz,
                    _L2FxWorldCalibration);
                float3 positionWS = TransformObjectToWorld(locationOffset + localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * ResolveParticleVisibility(ageSeconds, lifetime));
                return OUT;
            }

            // TeleportCaMesh RenderDoc FF: out = sample(t0, uv) * factor.
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
