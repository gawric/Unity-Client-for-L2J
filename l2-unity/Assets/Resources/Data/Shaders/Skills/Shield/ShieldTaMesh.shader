// m_u008_b mesh emitters: MeshEmitter2 "shield" (white_shield00), MeshEmitter0 "Ring" (magiccirclewhite01).
Shader "L2/Effects/ShieldTaMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (2, 2, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.3
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.74

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.317857
        _ColorScale1 ("ColorScale[1]", Color) = (0.905882, 0.819608, 0.819608, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 0.589286
        _ColorScale2 ("ColorScale[2]", Color) = (0.772549, 0.772549, 0.772549, 1)
        _ColorScaleTime3 ("ColorScale Time[3]", Range(0, 1)) = 0.828571
        _ColorScale3 ("ColorScale[3]", Color) = (0.905882, 0.901961, 0.847059, 1)
        _ColorScaleTime4 ("ColorScale Time[4]", Range(0, 1)) = 1
        _ColorScale4 ("ColorScale[4]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 5
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 30
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 0

        _ColorMultMin ("ColorMult Min", Color) = (1, 1, 1, 1)
        _ColorMultMax ("ColorMult Max", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 2)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1
        _RgbBoost ("RGB Boost (D3D9 MODULATE2X)", Range(0.25, 4)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        _AlphaEdgeFeather ("Alpha edge feather", Range(0, 0.25)) = 0

        [Toggle] _SplitRibbonByLum ("Split ribbon vs lines by luma", Float) = 0
        _SoftLumMin ("Soft band smoothstep min", Range(0, 1)) = 0
        _SoftLumMax ("Soft band smoothstep max", Range(0, 1)) = 0.35
        _LineLumMin ("Line band smoothstep min", Range(0, 1)) = 0.35
        _LineLumMax ("Line band smoothstep max", Range(0, 1)) = 1
        _SoftOpacityMul ("Soft ribbon strength", Range(0, 3)) = 0.5
        _LineOpacityMul ("Sharp lines strength", Range(0, 4)) = 1.2
        _SoftRgbBoost ("Soft ribbon RGB boost", Range(0, 4)) = 0.4
        _LineRgbBoost ("Sharp lines RGB boost", Range(0, 6)) = 0.8

        _StartSize ("Start Size (mesh scale XYZ)", Vector) = (0.1, 0.1, 0.1, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize x UU->m (0.01)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.09
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.08
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.2
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.02
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 0.69
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 2.1

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _StartSpin ("Start Spin rev", Float) = 0.255
        _SpinsPerSecond ("Spins Per Second rev/s", Float) = 0.1
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0

        _StartLocationOffset ("StartLocationOffset (UU Z-up)", Vector) = (0, 0, 0, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001

        [Toggle] _UsePlanarMeshUv ("Planar UV from mesh position", Float) = 0
        _PlanarUvScale ("Planar UV scale", Float) = 0.5
        [Toggle] _PlanarUvUseXZ ("Planar UV use XZ", Float) = 1
        [Toggle] _PlanarUvNormalizeExtents ("Planar UV normalize", Float) = 0
        _PlanarUvMeshHalfExtents ("Planar mesh half-extents", Vector) = (0.5, 0.5, 0, 0)

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Mesh Texture Preview", Float) = 0
        _DebugAtlasPreviewAlpha ("Debug Preview Alpha", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 1
        _DebugAtlasBackground ("Debug Preview Background", Color) = (0.03, 0.04, 0.08, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ShieldTaMesh"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxAtlasDebug.hlsl"

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
                float _ColorScaleTime3;
                float4 _ColorScale3;
                float _ColorScaleTime4;
                float4 _ColorScale4;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float _bAlphaBlend;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float _Opacity;
                float _EmitterAlpha;
                float _RgbBoost;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaEdgeFeather;
                float _SplitRibbonByLum;
                float _SoftLumMin;
                float _SoftLumMax;
                float _LineLumMin;
                float _LineLumMax;
                float _SoftOpacityMul;
                float _LineOpacityMul;
                float _SoftRgbBoost;
                float _LineRgbBoost;
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
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewAlpha;
                float _DebugAtlasPreviewBoost;
                float4 _DebugAtlasBackground;
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
                float2 uvMesh : TEXCOORD2;
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

                L2Fx_MeshBuiltin_ResolveUv(
                    IN.uv, IN.positionOS.xyz,
                    _UsePlanarMeshUv, _PlanarUvScale, _PlanarUvUseXZ, _PlanarUvNormalizeExtents, _PlanarUvMeshHalfExtents,
                    _MainTex_ST, 0.0, float4(0, 0, 1, 1),
                    OUT.uv, OUT.uvMesh);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                if (_DebugAtlasPreview > 0.5)
                {
                    float previewMask = L2Fx_MeshFrag_SampleTextureAlpha(
                        texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                    previewMask = L2Fx_MeshFrag_AlphaFeather(previewMask, _AlphaEdgeFeather);
                    return L2Fx_AtlasDebugPreviewColor(
                        texColor,
                        previewMask,
                        _DebugAtlasPreviewAlpha,
                        _DebugAtlasPreviewBoost,
                        _DebugAtlasBackground);
                }

                float ctimes[8];
                float4 ccols[8];
                L2Fx_BuildColorScaleArrays5(
                    _ColorScaleCount,
                    _ColorScale0,
                    _ColorScaleTime1, _ColorScale1,
                    _ColorScaleTime2, _ColorScale2,
                    _ColorScaleTime3, _ColorScale3,
                    _ColorScaleTime4, _ColorScale4,
                    ctimes,
                    ccols);

                float csParam = max(_ColorScaleRepeats, 1.0) - 1.0;
                float4 cs = L2Fx_SampleColorScale(
                    IN.ageNorm,
                    csParam,
                    _ColorScaleCount,
                    ctimes,
                    ccols,
                    _bAlphaBlend > 0.5);

                float4 colorMult = lerp(_ColorMultMin, _ColorMultMax, L2Fx_Hash11(_Seed * 17.0 + _StartTime));
                half4 color = half4(cs.rgb * colorMult.rgb * _Opacity, cs.a * _EmitterAlpha);

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                mask = L2Fx_MeshFrag_AlphaFeather(mask, _AlphaEdgeFeather);

                half3 tinted = saturate(color.rgb * texColor.rgb * (half)_RgbBoost);
                color.rgb = L2Fx_MeshFrag_MagicCircleLumaUvSplit(
                    tinted, texColor, IN.uvMesh,
                    _SplitRibbonByLum,
                    _SoftLumMin, _SoftLumMax, _LineLumMin, _LineLumMax,
                    _SoftOpacityMul, _LineOpacityMul, _SoftRgbBoost, _LineRgbBoost,
                    0.0, float4(0, 0, 0, 0), 0.0, 0.0,
                    0.0, 0.0, 0.0, 0.0);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                half alpha = (half)saturate(color.a * mask * lifeAlpha);
                return half4(saturate(color.rgb), alpha);
            }

            ENDHLSL
        }
    }
}
