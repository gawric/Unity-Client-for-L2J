// it_healing_potion_ta / m_u004_b SpriteEmitter2 - calib shader (L2FxCoreGeometryTest + L2FxSpriteSizeScale).
// Particle age from ParticleSingle (_StartTime/_Seed); optional loop for SizeScale/flipbook preview.

Shader "L2/Effects/Calib/HealingPotionTaSpriteEmitter2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 2.17
        _SizeRange ("Start Size UU Min Max", Vector) = (5.5, 5.5, 0, 0)

        [Header(ParticleSingle Runtime)]
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _LifetimeRange ("Lifetime Range Min Max sec", Vector) = (2, 2, 0, 0)
        _InitialDelayRange ("Initial Delay Range Min Max sec", Vector) = (0, 0.01, 0, 0)
        [Toggle] _LoopSizeScalePreview ("Loop SizeScale Flipbook Preview", Float) = 1

        [Header(Test)]
        [Toggle] _TestDisableSizeScale ("Test Disable SizeScale", Float) = 0

        [Header(SizeScale m_u004_b)]
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0, 0.6, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.07, 1.8, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (0.14, 2.6, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (0.34, 3, 0, 0)
        _SizeKey4 ("Size Key 4 Time Size", Vector) = (1, 3.4, 0, 0)

        [Header(Flipbook)]
        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 4
        _SubdivisionEnd ("Subdivision End", Float) = 7

        _RgbBoost ("RGB Boost", Range(0, 16)) = 7
        _LumaAlphaFloor ("Luma Alpha Floor", Range(0, 0.25)) = 0.003
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "HealingPotionTaCalibSE2"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxCoreGeometryTest.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteSizeScale.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float _StartTime;
                float _Seed;
                float4 _LifetimeRange;
                float4 _InitialDelayRange;
                float _LoopSizeScalePreview;
                float _TestDisableSizeScale;
                float _SizeScaleRepeats;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _RgbBoost;
                float _LumaAlphaFloor;
            CBUFFER_END

            float ResolveStartSizeUU()
            {
                float minUU = _SizeRange.x;
                float maxUU = _SizeRange.y;
                if (maxUU < minUU)
                {
                    float t = minUU;
                    minUU = maxUU;
                    maxUU = t;
                }
                return (minUU + maxUU) * 0.5;
            }

            float ResolveParticleAgeNorm()
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = max(L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);

                if (_StartTime <= 0.0)
                {
                    age = _Time.y;
                }

                if (_LoopSizeScalePreview > 0.5)
                {
                    return frac(age / lifetime);
                }

                return saturate(age / lifetime);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float flipBlend : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float ageNorm = ResolveParticleAgeNorm();

                float sizeMul = L2Fx_SpriteSizeScale_ScalarFromUniforms(
                    ageNorm,
                    1.0,
                    _SizeScaleRepeats,
                    5,
                    false,
                    false,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y);

                if (_TestDisableSizeScale > 0.5)
                {
                    sizeMul = 1.0;
                }

                float sizeUU = ResolveStartSizeUU() * sizeMul;
                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
                float3 quadOS = IN.positionOS.xyz * sizeM;
                OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                L2Fx_FlipbookAtlasUVBlend(
                    IN.uv,
                    ageNorm,
                    uSub,
                    vSub,
                    s0,
                    s1,
                    OUT.uvAtlasA,
                    OUT.uvAtlasB,
                    OUT.flipBlend);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 tex = lerp(texA, texB, (half)IN.flipBlend);
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float mask = saturate((lum - _LumaAlphaFloor) / max(1.0 - _LumaAlphaFloor, 1e-4));
                half3 rgb = tex.rgb * (half)_RgbBoost * (half)mask;
                return half4(saturate(rgb), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
