// it_teleport_v1_ca / MeshEmitter0 "DarkMatter"
// UC: magiccircleblack02, UniformSize StartSize=0.8, MaxParticles=1,
// ColorScale white→white, ColorMul=(0.1, 0.001, 0.001),
// FadeInEnd=1.68, FadeOutStart=27.72, Lifetime=28, InitialDelay=2,
// ForcedFade, PTDS_AlphaBlend, RenderTwoSided, no Spin/SizeScale/motion.
Shader "L2/Effects/it_teleport_v1_ca/MeshEmitter0_DarkMatter"
{
    Properties
    {
        _MainTex ("Texture (fx magiccircle)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        // UC InitialDelayRange=(2,2). Prefab MeshEmitter._startDelay often already
        // applies this — set 0 here if spawn is delayed by the controller.
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (2, 2, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (28, 28, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 28)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSize ("StartSize (UniformSize)", Float) = 0.8

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (0.1, 0.001, 0.001, 0)
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 1.68
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 27.72
        _Opacity ("Opacity", Range(0, 2)) = 1
        // ON when atlas sRGB=OFF and ColorMul looks wrong in Linear.
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_AlphaBlend — L2FxPTDS_DrawStyle.hlsl
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "DarkMatter"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
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

            float4 ResolveColor(float ageSeconds, float lifetime)
            {
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
                // No SizeScale in UC — RelativeSize stays 1.
                float finalScale = L2Fx_GetFinalMeshScale(_StartSize, 1.0, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * finalScale;
                float3 positionWS = TransformObjectToWorld(localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * ResolveParticleVisibility(ageSeconds, lifetime));
                return OUT;
            }

            // Live FF PS (RenderDoc SPIR-V): out = sample(t0, uv) * factor.
            // Circle silhouette = texture alpha (AlphaBlend), not luma cutout.
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
