// el_vampiric_touch_ta MeshEmitter34_2: center diamond submesh (VampireEye UV on fx_m_t0032).
// Fixed-function style: sample texture and multiply by textureFactor, then lifetime fade.
// Vertex timing/spin/size matches VampiricFury00 so all three materials fade in sync.
Shader "L2/Effects/VampiricTouchDiamond"
{
    Properties
    {
        _MainTex ("Mask UV (diamond submesh: fx_m_t0009)", 2D) = "white" {}
        [HDR] _TextureFactor ("Texture Factor (RenderDoc)", Color) = (0.295, 0.039, 0.07, 1)
        _TextureContrast ("Texture Contrast", Range(0, 1)) = 0.45
        _TextureFloor ("Texture Dark Floor", Range(0, 1)) = 0.35

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (1.6, 1.6, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.224
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.784

        _ColorScale0 ("ColorScale[0]", Color) = (0.9, 0.9, 0.9, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.5
        _ColorScale1 ("ColorScale[1]", Color) = (0.95, 0.95, 0.95, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1.0
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats (UE VampireEye=10)", Float) = 10
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _Opacity ("Opacity", Range(0, 2)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1
        [Toggle] _OpaqueTextureAlpha ("Opaque texture alpha (RenderDoc diamond)", Float) = 0

        _StartSize ("Start Size (mesh vertex scale XYZ)", Vector) = (0.6, 1.107, 1.107, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize × 0.01 (mesh verts in raw UE UU)", Float) = 0
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1.0
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0.0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats (cycles/lifetime)", Float) = 1.0
        _SizeScaleParam ("SizeScale Param", Float) = 0.0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.5
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.2
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.0
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.45
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.02
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1.0
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1.03

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1.0
        _StartSpinRange ("Start Spin Range rev (Min,Max)", Vector) = (1, 1, 0, 0)
        _SpinsPerSecond ("Spins Per Second", Float) = 0.0
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0.0

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MeshEmitter34_Diamond"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../../Common/L2FxMeshEmitterUrp.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _StartTime;
                float _HasLifetime;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _Seed;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float _bAlphaBlend;
                float4 _TextureFactor;
                float _TextureContrast;
                float _TextureFloor;
                float _Opacity;
                float _EmitterAlpha;
                float _OpaqueTextureAlpha;
                float4 _StartSize;
                float _ApplyUuToStartSize;
                float _UseSizeScale;
                float _UseRegularSizeScale;
                uint _SizeScaleCount;
                float _SizeScaleRepeats;
                float _SizeScaleParam;
                float _SizeScaleTime0;
                float _SizeScaleVal0;
                float _SizeScaleTime1;
                float _SizeScaleVal1;
                float _SizeScaleTime2;
                float _SizeScaleVal2;
                float _SizeScaleTime3;
                float _SizeScaleVal3;
                float _SpinParticles;
                float4 _StartSpinRange;
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float4 _StartLocationOffset;
                float _MeshYOffset;
                float _ClipDepthBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float ageNorm : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float delay, lifetime, age, ageNorm;
                L2Fx_MeshBuiltin_ComputeTiming(
                    _Time.y, _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, age, ageNorm);
                OUT.ageNorm = ageNorm;

                float startSpinRev = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;
                L2Fx_MeshBuiltin_TransformVertexOS(
                    posOS, nrmOS,
                    _SpinParticles, startSpinRev, _SpinsPerSecond, _SpinCCWorCW, age, ageNorm,
                    _StartSize.xyz, _ApplyUuToStartSize,
                    _UseSizeScale, _UseRegularSizeScale,
                    _SizeScaleParam, _SizeScaleRepeats, _SizeScaleCount,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    _SizeScaleTime2, _SizeScaleVal2,
                    _SizeScaleTime3, _SizeScaleVal3,
                    1.0, 1.0,
                    _StartLocationOffset.xyz, _MeshYOffset, 0.0);

                OUT.positionHCS = L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float colorScaleTimeA = min(_ColorScaleTime1, _ColorScaleTime2);
                float colorScaleTimeB = max(_ColorScaleTime1, _ColorScaleTime2);
                half4 tint = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, colorScaleTimeA, _ColorScale1,
                    colorScaleTimeB, _ColorScale2,
                    _bAlphaBlend,
                    float3(1, 1, 1), float3(1, 1, 1),
                    _Opacity, _EmitterAlpha);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                half3 compressedTexture = lerp(_TextureFloor.xxx, texColor.rgb, saturate(_TextureContrast));
                half4 fixedFunctionColor = half4(compressedTexture, texColor.a) * _TextureFactor;
                half3 rgb = fixedFunctionColor.rgb * tint.rgb;
                half textureAlpha = _OpaqueTextureAlpha > 0.5 ? 1.0h : fixedFunctionColor.a;
                half alpha = (half)(textureAlpha * tint.a * lifeAlpha);

                return half4(saturate(rgb), saturate(alpha));
            }

            ENDHLSL
        }
    }
}
