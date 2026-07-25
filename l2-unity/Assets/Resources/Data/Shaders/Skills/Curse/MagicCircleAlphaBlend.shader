// UE MeshEmitter2 curse_poison: PTDS_AlphaBlend, magiccircleblack00 (3 submesh, mesh UV 0..1 per shard).
Shader "L2/Effects/MagicCircleAlphaBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (4, 4, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.8
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 3.2

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1.0
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 2
        _ColorScaleRepeats ("ColorScale Repeats (cycles/lifetime)", Float) = 200
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorMultMin ("ColorMult Min", Color) = (1, 1, 1, 1)
        _ColorMultMax ("ColorMult Max", Color) = (1, 1, 1, 1)

        _Opacity ("Opacity", Range(0, 2)) = 1.0
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1.0
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha (RGB only)", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from luma (black bg → transparent)", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02

        _StartSize ("Start Size (mesh scale XYZ)", Vector) = (0.4, 0.4, 0.4, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize × UU→m (0.01)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1.0
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1.0
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0.0
        _SizeScaleCount ("SizeScale Count", Int) = 5
        _SizeScaleRepeats ("SizeScale Repeats (cycles/lifetime)", Float) = 3.0
        _SizeScaleParam ("SizeScale Param", Float) = 0.0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1.0
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.12
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.015
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.37
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 0.995
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 0.59
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1.01
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1.0
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 1.0

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1.0
        _StartSpin ("Start Spin", Range(-10000, 10000)) = 0.0
        _SpinsPerSecond ("Spins Per Second", Float) = 0.02
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 1.0

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 1, 0)
        _MeshYOffset ("Lift above ground plane (m)", Float) = 0.015
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.002

        [Toggle] _UsePlanarMeshUv ("Planar UV from mesh position", Float) = 0
        _PlanarUvScale ("Planar UV scale (if normalize off)", Float) = 0.5
        [Toggle] _PlanarUvUseXZ ("Planar UV use XZ (off = XY)", Float) = 1
        [Toggle] _PlanarUvNormalizeExtents ("Planar UV fill mesh (normalize)", Float) = 0
        _PlanarUvMeshHalfExtents ("Planar mesh half-extents OS (plane X,Y)", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+25"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "MeshEmitter2"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"

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
                float _ColorScaleRepeats;
                float _bAlphaBlend;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float _Opacity;
                float _EmitterAlpha;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float4 _StartSize;
                float _ApplyUuToStartSize;
                float _UniformSize;
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
                float _StartSpin;
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float4 _StartLocationOffset;
                float _MeshYOffset;
                float _ClipDepthBias;
                float _UsePlanarMeshUv;
                float _PlanarUvScale;
                float _PlanarUvUseXZ;
                float _PlanarUvNormalizeExtents;
                float4 _PlanarUvMeshHalfExtents;
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

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;
                L2Fx_MeshBuiltin_TransformVertexOS(
                    posOS, nrmOS,
                    _SpinParticles, _StartSpin, _SpinsPerSecond, _SpinCCWorCW, age, ageNorm,
                    _StartSize.xyz, _ApplyUuToStartSize,
                    _UseSizeScale, _UseRegularSizeScale,
                    _SizeScaleParam, _SizeScaleRepeats, _SizeScaleCount,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    _SizeScaleTime2, _SizeScaleVal2,
                    _SizeScaleTime3, _SizeScaleVal3,
                    _SizeScaleTime4, _SizeScaleVal4,
                    _StartLocationOffset.xyz, _MeshYOffset, 0.0);

                OUT.positionHCS = L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);

                float2 uvMeshUnused;
                L2Fx_MeshBuiltin_ResolveUv(
                    IN.uv, IN.positionOS.xyz,
                    _UsePlanarMeshUv, _PlanarUvScale, _PlanarUvUseXZ, _PlanarUvNormalizeExtents, _PlanarUvMeshHalfExtents,
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
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, _ColorScaleTime1, _ColorScale1,
                    0.0, float4(1, 1, 1, 1),
                    _bAlphaBlend,
                    _ColorMultMin.rgb, _ColorMultMax.rgb,
                    _Opacity, _EmitterAlpha);

                color.rgb *= texColor.rgb;
                color.a *= L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);

                color.a *= (half)L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                return saturate(color);
            }

            ENDHLSL
        }
    }
}
