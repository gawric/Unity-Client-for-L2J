// m_u008_b sprite emitters: SpriteEmitter6 "AngelDust" (fx_m_t0000, same as VampiricTouchSpark tail).
// SpriteEmitter1 flash uses L2/Effects/ShieldTaFlash.
Shader "L2/Effects/ShieldTaSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (1.3, 1.8, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.165
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.51

        _Opacity ("Opacity", Range(0, 2)) = 0.73
        _TextureDilateTexels ("Texture Dilate Texels", Range(0, 24)) = 2.2
        _RgbBoost ("RGB Boost", Range(0, 16)) = 2.46
        _AlphaBoost ("Alpha Boost", Range(0, 16)) = 16
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        [Toggle] _UseBrightenBlend ("Brighten additive (off = alpha blend)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 10
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.521429
        _ColorScale1 ("ColorScale[1]", Color) = (0.5, 0.5, 0.5, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorMultMin ("ColorMult Min", Color) = (1, 1, 1, 1)
        _ColorMultMax ("ColorMult Max", Color) = (1, 1, 0.905, 1)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (90, 90, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (15, 15, 0, 0)
        _SpawnUnitScale ("Spawn/velocity UU->Unity (0.01)", Float) = 0.01
        _OwnerWorldPos ("Owner World Pos (ParticleGroup)", Vector) = (0, 0, 0, 0)

        _VelocityRangeX ("Velocity X UU (Min,Max)", Vector) = (25, 35, 0, 0)
        _VelocityRangeY ("Velocity Y UU (Min,Max)", Vector) = (25, 35, 0, 0)
        _VelocityRangeZ ("Velocity Z UU (Min,Max)", Vector) = (25, 35, 0, 0)
        _VelocityLossRange ("Velocity Loss UU (Min,Max)", Vector) = (2.5, 2.5, 0, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (2, 2, 0, 0)
        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0.0625
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.1
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 2
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.18
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.41
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 0.7
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 0.01

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0.3, 0.5, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 14
        _SubdivisionEnd ("Subdivision End", Float) = 16
        [Toggle] _UseRandomSubdivision ("Use Random Subdivision", Float) = 1
        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 1

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (show selected cell)", Float) = 0
        _DebugAtlasPreviewAlpha ("Debug Preview Alpha", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 1
        _DebugAtlasBackground ("Debug Preview Background", Color) = (0.03, 0.04, 0.08, 1)
        [Toggle] _DebugSpawnRegion ("Debug Spawn Region Wire (Scene)", Float) = 0
        _DebugSpawnRegionColor ("Debug Spawn Region Color", Color) = (0, 1, 1, 0.9)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ShieldTaSpriteBrighten"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _USEBRIGHTENBLEND_OFF
            #pragma shader_feature_local _DEBUGSPAWNREGION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"
            #include "../Common/L2FxMeshParticleMotion.hlsl"
            #include "../Common/L2FxMeshEmitterVertex.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxParticleAnim.hlsl"
            #include "../Common/L2FxAtlasDebug.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float _StartTime;
                float _HasLifetime;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _Seed;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float _Opacity;
                float _TextureDilateTexels;
                float _RgbBoost;
                float _AlphaBoost;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _bAlphaBlend;
                float _UseBrightenBlend;
                float _ColorScaleRepeats;
                uint _ColorScaleCount;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _SpawnUnitScale;
                float4 _OwnerWorldPos;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float4 _VelocityRangeZ;
                float4 _VelocityLossRange;
                float4 _SizeRange;
                float _BillboardScale;
                float _UniformSize;
                float _UseSizeScale;
                float _SizeScaleTime0;
                float _SizeScaleVal0;
                float _SizeScaleTime1;
                float _SizeScaleVal1;
                float _SizeScaleTime2;
                float _SizeScaleVal2;
                float _SizeScaleTime3;
                float _SizeScaleVal3;
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _UseRandomSubdivision;
                float _BlendBetweenSubdivisions;
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewAlpha;
                float _DebugAtlasPreviewBoost;
                float4 _DebugAtlasBackground;
                float _DebugSpawnRegion;
                float4 _DebugSpawnRegionColor;
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
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float4 tint : COLOR;
                nointerpolation float particleSeed : TEXCOORD2;
                float flipbookBlend : TEXCOORD3;
                float ageNorm : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float ageNorm = saturate(age / max(lifetime, 1e-4));
                OUT.ageNorm = ageNorm;
                OUT.particleSeed = pSeed;

                float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                posUe += _StartLocationOffset.xyz;

                float motionComp = L2Fx_MotionCompensationForManualBillboardScale(_BillboardScale);
                float3 spawnOfs = L2Fx_UeVectorToUnity(posUe) * _SpawnUnitScale * motionComp;

                float3 velUe = float3(
                    L2Fx_RandomRange(_VelocityRangeX.xy, pSeed, _StartTime, 101.0),
                    L2Fx_RandomRange(_VelocityRangeY.xy, pSeed, _StartTime, 103.0),
                    L2Fx_RandomRange(_VelocityRangeZ.xy, pSeed, _StartTime, 107.0));
                float speed = length(velUe);
                float2 hDir = L2Fx_OutwardDirectionXZ(
                    spawnOfs, _PolarAzimuthDeg.xy, pSeed, _StartTime, 181.0);
                float3 vel = float3(hDir.x, 0.0, hDir.y) * speed * _SpawnUnitScale * motionComp;
                float loss = L2Fx_RandomRange(_VelocityLossRange.xy, pSeed, _StartTime, 109.0) * _SpawnUnitScale * motionComp;

                float3 baseSize = L2Fx_StartSize(
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime);
                baseSize *= L2Fx_MeshBuiltin_SampleSizeScaleScalar(
                    ageNorm,
                    0.0,
                    1.0,
                    4u,
                    _UseSizeScale,
                    0.0,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    _SizeScaleTime2, _SizeScaleVal2,
                    _SizeScaleTime3, _SizeScaleVal3,
                    1.0, 1.0);

                float3 quadOS = IN.positionOS.xyz * baseSize;

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    float3 nrm = IN.normalOS;
                    L2Fx_ApplyMeshScalarSpin(quadOS, nrm, true, angle);
                }

                float3 disp = L2Fx_DisplacementLinearHorizontalVelocityLoss(
                    vel, float3(0, 0, 0), loss, age);
                spawnOfs += disp;
                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = L2Fx_CameraBillboardPositionWS(
                    centerWS,
                    quadOS,
                    _BillboardScale,
                    0.0);

                OUT.positionHCS = TransformWorldToHClip(posWS);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                float fBlend = 0.0;
                float2 uvA;
                float2 uvB;

                if (_UseRandomSubdivision > 0.5)
                {
                    int fi = L2Fx_FlipbookSubDivisionRandomFrame(pSeed, _StartTime, s0, s1, 41.0);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fi, uSub, vSub);
                    uvB = uvA;
                }
                else if (_BlendBetweenSubdivisions > 0.5)
                {
                    int fa;
                    int fb;
                    L2Fx_FlipbookBlendFrames(ageNorm, s0, s1, fa, fb, fBlend);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fa, uSub, vSub);
                    uvB = L2Fx_FlipbookAtlasUV(quadUv, fb, uSub, vSub);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, s0, s1);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fi, uSub, vSub);
                    uvB = uvA;
                }

                OUT.uvAtlasA = uvA;
                OUT.uvAtlasB = uvB;
                OUT.flipbookBlend = fBlend;

                float ctimes[8];
                float4 ccols[8];
                L2Fx_BuildColorScaleArrays5(
                    _ColorScaleCount,
                    _ColorScale0,
                    _ColorScaleTime1, _ColorScale1,
                    _ColorScaleTime2, _ColorScale2,
                    1.0, float4(1, 1, 1, 1),
                    1.0, float4(1, 1, 1, 1),
                    ctimes,
                    ccols);

                float csParam = max(_ColorScaleRepeats, 1.0) - 1.0;
                float4 cs = L2Fx_SampleColorScale(
                    ageNorm,
                    csParam,
                    _ColorScaleCount,
                    ctimes,
                    ccols,
                    _bAlphaBlend > 0.5);

                float4 colorMult = lerp(_ColorMultMin, _ColorMultMax, L2Fx_Hash11(pSeed * 19.0 + _StartTime));
                OUT.tint = float4(cs.rgb * colorMult.rgb, cs.a);

                return OUT;
            }

            half4 ShieldTaDilatedSample(float2 uv)
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (_TextureDilateTexels <= 0.001)
                {
                    return tex;
                }

                float2 s = _MainTex_TexelSize.xy * _TextureDilateTexels;
                float2 sx = float2(s.x, 0.0);
                float2 sy = float2(0.0, s.y);
                float2 sd = s * 0.7071;

                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sx * 0.35));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sx * 0.35));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sy * 0.35));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sy * 0.35));

                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sx * 0.7));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sx * 0.7));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sy * 0.7));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sy * 0.7));

                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sx));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sx));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sy));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sy));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sd));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - sd));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(sd.x, -sd.y)));
                tex = max(tex, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-sd.x, sd.y)));
                return tex;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pSeed = IN.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);

                half4 texA = ShieldTaDilatedSample(IN.uvAtlasA);
                half4 texB = ShieldTaDilatedSample(IN.uvAtlasB);
                half4 mixed = lerp(texA, texB, (half)IN.flipbookBlend);

                if (_DebugAtlasPreview > 0.5)
                {
                    float previewMask = L2Fx_MeshFrag_SampleTextureAlpha(
                        mixed, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                    return L2Fx_AtlasDebugPreviewColor(
                        mixed,
                        previewMask,
                        _DebugAtlasPreviewAlpha,
                        _DebugAtlasPreviewBoost,
                        _DebugAtlasBackground);
                }

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    mixed, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                #if defined(_USEBRIGHTENBLEND_OFF)
                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)mask * (half)_Opacity;
                half alpha = (half)saturate(mask * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), alpha);
                #else
                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)(mask * _RgbBoost);
                half alpha = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), alpha);
                #endif
            }

            ENDHLSL
        }
    }
}
