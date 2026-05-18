// UE bl_curse_poison_ta MeshEmitter4 "blackclaw": PTDS_Darken, black_poison00 mesh burst on target.
Shader "L2/Effects/PoisonBlackClaw"
{
    Properties
    {
        _MainTex ("Texture (optional; mesh UV)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0.2, 0.2, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (0.6, 0.6, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.093

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1.0
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 2
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorMultMin ("ColorMult Min", Color) = (0.844, 1, 0.652, 1)
        _ColorMultMax ("ColorMult Max", Color) = (0.844, 1, 0.652, 1)

        _Opacity ("Opacity", Range(0, 1)) = 0.82
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1.0
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from luma (black bg → mask)", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        [Toggle] _UseVertexColorMask ("Also use mesh vertex color as mask", Float) = 0
        [HDR] _ClawStreakColor ("Claw streak color (#443340 ref)", Color) = (0.267, 0.204, 0.251, 1)
        _StreakLumaVariation ("Streak brightness variation from tex luma", Range(0, 0.5)) = 0.18
        _AlphaEdgeFeather ("Mask edge feather", Range(0, 0.25)) = 0.015

        _StartSize ("Start Size (mesh vertex scale)", Vector) = (1, 1, 1, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize × 0.01 (mesh verts in raw UE UU)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1.0
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1.0
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0.0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats (cycles/lifetime)", Float) = 1.0
        _SizeScaleParam ("SizeScale Param", Float) = 0.0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1.0
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.07
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 2.5
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.37
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 3.5
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1.0
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 4.3

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1.0
        _StartSpinRange ("Start Spin Range rev (Min,Max)", Vector) = (0, 1, 0, 0)
        _SpinsPerSecond ("Spins Per Second", Float) = 0.0
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0.0

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0.01
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001

        [Toggle] _UsePlanarMeshUv ("Planar UV from mesh position", Float) = 0
        _PlanarUvScale ("Planar UV scale", Float) = 0.5
        [Toggle] _PlanarUvUseXZ ("Planar UV use XZ (off = XY)", Float) = 1
        [Toggle] _PlanarUvNormalizeExtents ("Planar UV fill mesh", Float) = 0
        _PlanarUvMeshHalfExtents ("Planar mesh half-extents OS", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        // UE PTDS_Darken ≈ min(dst, streakColor). URP: alpha-blend streak #443340 (min/multiply → pure black).
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MeshEmitter4_BlackClaw"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float _UseVertexColorMask;
                float4 _ClawStreakColor;
                float _StreakLumaVariation;
                float _AlphaEdgeFeather;
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
                float _SpinParticles;
                float4 _StartSpinRange;
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float ageNorm : TEXCOORD1;
                float4 meshColor : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float delay, lifetime, age, ageNorm;
                L2Fx_MeshBuiltin_ComputeTiming(
                    _Time.y, _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, age, ageNorm);
                OUT.ageNorm = ageNorm;
                OUT.meshColor = IN.color;

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
                    _StartLocationOffset.xyz, _MeshYOffset);

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

                if (_UseVertexColorMask > 0.5)
                {
                    float vMask = saturate(dot(IN.meshColor.rgb, float3(0.299, 0.587, 0.114)));
                    float vPart = (_IgnoreMainTexAlpha < 0.5) ? max(vMask, IN.meshColor.a) : vMask;
                    mask = max(mask, vPart);
                }

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                float strength = saturate(mask * color.a * lifeAlpha);

                // Reference claw color #443340; UC ColorMultiplierRange; luma only shapes mask/brightness.
                float lum = saturate(dot(texColor.rgb, float3(0.299, 0.587, 0.114)));
                float3 clawRgb = _ClawStreakColor.rgb * _ColorMultMin.rgb;
                float lumaMul = 1.0 - _StreakLumaVariation + lum * _StreakLumaVariation;
                clawRgb *= lumaMul;

                return half4((half3)saturate(clawRgb), (half)strength);
            }

            ENDHLSL
        }
    }
}
