// UE bl_curse_poison_ta MeshEmitter0 "Wave": PTDS_Brighten, etcpotion01.
// Reference: e_u004_a MeshEmitter6 "Rings" — same mesh, L2SkillEffect + fx_m_t0006, raw mesh UV.
// fx_m_t*: black RGB = transparent (alpha channel is opaque; use luma mask).
Shader "L2/Effects/PoisonWave"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0006 for etcpotion01)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (1.2, 1.5, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.03

        _ColorScale0 ("ColorScale[0]", Color) = (1, 0.690196, 0.815686, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1.0
        _ColorScale1 ("ColorScale[1]", Color) = (0.529412, 0.262745, 0.596078, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 2
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorMultMin ("ColorMult Min", Color) = (1, 1, 1, 1)
        _ColorMultMax ("ColorMult Max", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 2)) = 0.6
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1.0
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha (fx_m_t A=255)", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma (black = transparent)", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        _AlphaEdgeFeather ("Alpha edge feather", Range(0, 0.25)) = 0.02

        _StartSize ("Start Size (mesh scale XYZ)", Vector) = (1, 1, 1, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize x UU->m (0.01)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1.0
        [Toggle] _SizeScaleHorizontalOnly ("SizeScale on XZ only (no vertical grow)", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1.0
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0.0
        _SizeScaleCount ("SizeScale Count", Int) = 3
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1.0
        _SizeScaleParam ("SizeScale Param", Float) = 0.0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.0
        _SizeScaleVal0 ("SizeScale Value[0] (70% at spawn)", Float) = 4.55
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.7
        _SizeScaleVal1 ("SizeScale Value[1] (hold 70%)", Float) = 4.55
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 1.0
        _SizeScaleVal2 ("SizeScale Value[2] (100%)", Float) = 6.5
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1.0
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 6.5
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1.0
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 6.5

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1.0
        _StartSpinRange ("Start Spin Range rev (Min,Max)", Vector) = (0, 1, 0, 0)
        _SpinsPerSecond ("Spins Per Second (rev/s)", Float) = 1.2
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0.0

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _RandomZOffsetUU ("StartLocationRange Z UU (Min,Max)", Vector) = (-3, 3, 0, 0)
        _SphereRadiusUU ("SphereRadiusRange UU (Min,Max)", Vector) = (3, 3, 0, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0.01
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
                    "L2FxGpuInstancing" = "On"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MeshEmitter0_Wave"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "../../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../../Common/L2FxMeshFragment.hlsl"

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
                uint _ColorScaleCount;
                float _bAlphaBlend;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float _Opacity;
                float _EmitterAlpha;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaEdgeFeather;
                float4 _StartSize;
                float _ApplyUuToStartSize;
                float _UniformSize;
                float _SizeScaleHorizontalOnly;
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
                float _SizeScaleTime4;
                float _SizeScaleVal4;
                float _SpinParticles;
                float4 _StartSpinRange;
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float4 _StartLocationOffset;
                float4 _RandomZOffsetUU;
                float4 _SphereRadiusUU;
                float _MeshYOffset;
                float _ClipDepthBias;
            CBUFFER_END

            #include "../../Common/L2FxInstancing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float ageNorm : TEXCOORD1;
            };

            float3 WaveRandomOffsetUU(float seed, float startTime)
            {
                float z = L2Fx_RandomRange(_RandomZOffsetUU.xy, seed, startTime, 113.0);
                float radius = L2Fx_RandomRange(_SphereRadiusUU.xy, seed, startTime, 127.0);
                float angle = L2Fx_RandomRange(float2(0.0, 6.2831853), seed, startTime, 131.0);
                return float3(cos(angle) * radius, sin(angle) * radius, z);
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;

                float delay, lifetime, age, ageNorm;
                L2Fx_MeshBuiltin_ComputeTiming(
                    _Time.y, _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, age, ageNorm);
                OUT.ageNorm = ageNorm;

                float startSpinRev = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;
                float3 startLocationUU = _StartLocationOffset.xyz + WaveRandomOffsetUU(_Seed, _StartTime);
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
                    _SizeScaleTime4, _SizeScaleVal4,
                    startLocationUU, _MeshYOffset, _SizeScaleHorizontalOnly);

                OUT.positionHCS = L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);

                // Same as L2SkillEffect mesh path: mesh UV as authored, only _MainTex_ST.
                float2 uvMeshUnused;
                L2Fx_MeshBuiltin_ResolveUv(
                    IN.uv, IN.positionOS.xyz,
                    0.0, 0.5, 1.0, 0.0, float4(0.5, 0.5, 0, 0),
                    _MainTex_ST, 0.0, float4(0, 0, 1, 1),
                    OUT.uv, uvMeshUnused);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half4 color = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    0.0,
                    _ColorScaleCount,
                    _ColorScale0, _ColorScaleTime1, _ColorScale1,
                    0.0, half4(1, 1, 1, 1),
                    _bAlphaBlend,
                    _ColorMultMin.rgb, _ColorMultMax.rgb,
                    _Opacity, _EmitterAlpha);

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                mask = L2Fx_MeshFrag_AlphaFeather(mask, _AlphaEdgeFeather);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                half3 rgb = color.rgb * texColor.rgb * (half)mask;
                half alpha = (half)saturate(color.a * mask * lifeAlpha);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}
