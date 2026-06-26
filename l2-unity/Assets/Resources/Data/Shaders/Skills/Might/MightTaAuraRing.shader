// m_u004_b SpriteEmitter2: polar aura ring (fx_m_t0033), static spawn + spin + SizeScale growth.
// RenderDoc pipeline 47819 (Image 47803 / fx_m_t0033): Blend ONE+ONE, Z off, Cull none.
// No PTVD_StartPositionAndOwner funnel — that path lives in L2/Effects/MightTaSprite (gel sparks).
Shader "L2/Effects/MightTaAuraRing"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0033)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (2, 2, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.049
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.154
        _FadeOutPower ("FadeOut Power", Range(0.25, 4)) = 1

        _Opacity ("Opacity", Range(0, 2)) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        _AlphaBoost ("Alpha Boost", Range(0, 16)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        [Toggle] _UseSoftLumaAlpha ("Soft luma alpha", Float) = 1
        _LumaAlphaPower ("Luma alpha power", Range(0.2, 2)) = 0.55
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorScaleRepeats ("ColorScale Repeats", Float) = 60
        _ColorScaleCount ("ColorScale Count", Int) = 2
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleTime3 ("ColorScale Time[3]", Range(0, 1)) = 1
        _ColorScale3 ("ColorScale[3]", Color) = (1, 1, 1, 1)

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, 8, 0)
        _StartLocationRangeX ("StartLocationRange X UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartLocationRangeY ("StartLocationRange Y UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartLocationRangeZ ("StartLocationRange Z UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (0, 0, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale", Float) = 1
        _UcStartLocationRangeScale ("UC StartLocationRange Scale", Float) = 1
        _UcPolarRadiusScale ("UC PolarRadius Scale", Float) = 1
        _SpawnUnitScale ("Spawn UU->Unity (0.01)", Float) = 0.01

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (5.5, 5.5, 0, 0)
        _L2FxEffectScale ("L2 Fx Effect Scale", Float) = 1
        _L2FxSpriteScale ("L2 Fx Sprite Scale", Float) = 1
        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 5
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.6
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.07
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.8
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.14
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 2.6
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 0.34
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 3
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 3.4

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0.3, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 4
        _SubdivisionEnd ("Subdivision End", Float) = 7
        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 1
        _AtlasInsetTexels ("Atlas inset (texels, anti-bleed)", Range(0, 2)) = 1

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (_StartTime=0)", Float) = 0
        [Toggle] _DebugAtlasPreviewLoop ("Debug Preview Loop", Float) = 0
        _DebugAtlasPreviewAge ("Debug Preview Life (0-1 of Lifetime)", Range(0, 1)) = 0.25
        _DebugAtlasPreviewSizeScale ("Debug Preview Size Multiplier", Range(0.5, 32)) = 8
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 1
        _DebugAtlasBackground ("Debug Preview Background", Color) = (0.03, 0.04, 0.08, 1)
        [Toggle] _DebugSpawnRegion ("Debug Spawn Region Wire (Scene)", Float) = 0
        _DebugSpawnRegionColor ("Debug Spawn Region Color", Color) = (1, 0.15, 0.55, 1)
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
        ZTest Off

        Pass
        {
            Name "MightTaAuraRing"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"
            #include "../Common/L2FxMeshParticleMotion.hlsl"
            #include "../Common/L2FxMeshEmitterVertex.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxAtlasDebug.hlsl"
            #include "../Common/L2FxSpawnRegionDebug.hlsl"
            #include "../Common/L2FxSpriteAutoScale.hlsl"
            #include "../Common/L2FxUcToUnityConvert.hlsl"

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
                float _FadeOutPower;
                float _Opacity;
                float _RgbBoost;
                float _AlphaBoost;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _UseSoftLumaAlpha;
                float _LumaAlphaPower;
                float _bAlphaBlend;
                float _ColorScaleRepeats;
                uint _ColorScaleCount;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                float _ColorScaleTime3;
                float4 _ColorScale3;
                float4 _StartLocationOffset;
                float4 _StartLocationRangeX;
                float4 _StartLocationRangeY;
                float4 _StartLocationRangeZ;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _UcStartLocationOffsetScale;
                float _UcStartLocationRangeScale;
                float _UcPolarRadiusScale;
                float _SpawnUnitScale;
                float4 _SizeRange;
                float _L2FxEffectScale;
                float _L2FxSpriteScale;
                float _BillboardScale;
                float _UniformSize;
                float _UseSizeScale;
                float _UseRegularSizeScale;
                float _SizeScaleRepeats;
                float _SizeScaleParam;
                uint _SizeScaleCount;
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
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _SpinCCWorCW;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _BlendBetweenSubdivisions;
                float _AtlasInsetTexels;
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewLoop;
                float _DebugAtlasPreviewAge;
                float _DebugAtlasPreviewSizeScale;
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
            };

            L2Fx_UcToUnitySpriteConvertData AuraRingSpriteConvertData()
            {
                L2Fx_UcToUnitySpriteConvertData data;
                data.effectScale = _L2FxEffectScale;
                data.spriteScale = _L2FxSpriteScale;
                return data;
            }

            L2Fx_UcToUnitySpriteSpawnData AuraRingSpriteSpawnData()
            {
                L2Fx_UcToUnitySpriteSpawnData spawn;
                spawn.azimuthDegMinMax = _PolarAzimuthDeg.xy;
                spawn.polarPitchDegMinMax = _PolarPitchDeg.xy;
                spawn.radiusMinMax = _PolarRadius.xy;
                spawn.startLocationOffsetUe = _StartLocationOffset.xyz;
                spawn.startLocationRangeMinUe = float3(
                    _StartLocationRangeX.x,
                    _StartLocationRangeY.x,
                    _StartLocationRangeZ.x);
                spawn.startLocationRangeMaxUe = float3(
                    _StartLocationRangeX.y,
                    _StartLocationRangeY.y,
                    _StartLocationRangeZ.y);
                spawn.ucPolarRadiusScale = _UcPolarRadiusScale;
                spawn.ucStartLocationOffsetScale = _UcStartLocationOffsetScale;
                spawn.ucStartLocationRangeScale = _UcStartLocationRangeScale;
                spawn.spawnUnitScale = _SpawnUnitScale;
                return spawn;
            }

            void AuraRingResolveAtlasUvs(
                float2 quadUv,
                int uSub,
                int vSub,
                int s0,
                int s1,
                float ageNorm,
                out float2 uvA,
                out float2 uvB,
                out float fBlend)
            {
                fBlend = 0.0;
                float2 texel = _MainTex_TexelSize.xy;
                float inset = _AtlasInsetTexels;
                if (_BlendBetweenSubdivisions > 0.5)
                {
                    int fa;
                    int fb;
                    L2Fx_FlipbookBlendFrames(ageNorm, s0, s1, fa, fb, fBlend);
                    uvA = L2Fx_FlipbookAtlasUV_Padded(quadUv, fa, uSub, vSub, texel, inset);
                    uvB = L2Fx_FlipbookAtlasUV_Padded(quadUv, fb, uSub, vSub, texel, inset);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, s0, s1);
                    uvA = L2Fx_FlipbookAtlasUV_Padded(quadUv, fi, uSub, vSub, texel, inset);
                    uvB = uvA;
                }
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float slotAge = max(0.0, _Time.y - _StartTime);
                float motionAge = max(0.0, slotAge - delay);
                motionAge = min(motionAge, lifetime);
                float age = motionAge;
                float ageNorm = saturate(age / max(lifetime, 1e-4));
                OUT.particleSeed = pSeed;

                float scenePreview = L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime);
                if (scenePreview > 0.5)
                {
                    lifetime = max(lifetime, 1e-4);
                    age = L2Fx_AtlasDebug_ResolvePreviewAge(
                        _DebugAtlasPreviewLoop,
                        _DebugAtlasPreviewAge,
                        _Time.y,
                        lifetime);
                    ageNorm = saturate(age / lifetime);
                }

                float3 spawnOfs = L2Fx_UcToUnitySpriteSpawnOffset(
                    AuraRingSpriteSpawnData(),
                    pSeed,
                    _StartTime);
                L2Fx_UcToUnitySpriteConvertData spriteConvert = AuraRingSpriteConvertData();
                float3 baseSize;

                if (scenePreview > 0.5)
                {
                    L2Fx_UcToUnitySpriteConvertData previewConvert = spriteConvert;
                    previewConvert.effectScale = 1.0;
                    baseSize = L2Fx_UcToUnitySpriteStartSize(
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _UniformSize > 0.5,
                        pSeed,
                        _StartTime,
                        previewConvert) * max(_DebugAtlasPreviewSizeScale, 0.5);
                }
                else
                {
                    float sizeMul = L2Fx_MeshBuiltin_SampleSizeScaleScalar(
                        ageNorm,
                        _SizeScaleParam,
                        _SizeScaleRepeats,
                        _SizeScaleCount,
                        _UseSizeScale,
                        _UseRegularSizeScale,
                        _SizeScaleTime0, _SizeScaleVal0,
                        _SizeScaleTime1, _SizeScaleVal1,
                        _SizeScaleTime2, _SizeScaleVal2,
                        _SizeScaleTime3, _SizeScaleVal3,
                        _SizeScaleTime4, _SizeScaleVal4);

                    baseSize = L2Fx_UcToUnitySpriteStartSize(
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _UniformSize > 0.5,
                        pSeed,
                        _StartTime,
                        spriteConvert) * sizeMul;
                }

                float3 quadOS = IN.positionOS.xyz * baseSize;

                if (_SpinParticles > 0.5 && scenePreview < 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    sps = L2Fx_ApplySpinCCWorCW_Scalar(sps, _SpinCCWorCW);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    L2Fx_ApplyMeshScalarSpin(quadOS, IN.normalOS, true, angle);
                }

                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = L2Fx_CameraBillboardPositionWS(centerWS, quadOS, _BillboardScale, 0.0);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                float fBlend;
                float2 uvA;
                float2 uvB;
                AuraRingResolveAtlasUvs(quadUv, uSub, vSub, s0, s1, ageNorm, uvA, uvB, fBlend);
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
                    _ColorScaleTime3, _ColorScale3,
                    1.0, float4(1, 1, 1, 1),
                    ctimes,
                    ccols);

                float csParam = max(_ColorScaleRepeats, 1.0) - 1.0;
                OUT.tint = L2Fx_SampleColorScale(
                    ageNorm,
                    csParam,
                    _ColorScaleCount,
                    ctimes,
                    ccols,
                    _bAlphaBlend > 0.5);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pSeed = IN.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                lifetime = max(lifetime, 1e-4);

                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 mixed = lerp(texA, texB, (half)IN.flipbookBlend);

                float scenePreview = L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime);
                if (scenePreview > 0.5)
                {
                    float mask = L2Fx_MeshFrag_SampleTextureAlphaSoft(
                        mixed,
                        _AlphaFromLuma,
                        _LumaAlphaFloor,
                        _LumaAlphaPower,
                        _UseSoftLumaAlpha,
                        _IgnoreMainTexAlpha);
                    float lifeAlpha = L2Fx_AtlasDebug_PreviewLifeAlpha(
                        _DebugAtlasPreviewLoop,
                        _DebugAtlasPreviewAge,
                        _Time.y,
                        lifetime,
                        _HasLifetime,
                        _FadeIn,
                        _FadeInEndTime,
                        _Fadeout,
                        _FadeoutStartTime,
                        _FadeOutPower);
                    half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)(_RgbBoost * _DebugAtlasPreviewBoost);
                    half intensity = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                    rgb *= intensity;
                    half3 previewRgb = lerp((half3)_DebugAtlasBackground.rgb, rgb, (half)saturate(mask));
                    return half4(saturate(previewRgb), 1.0);
                }

                float mask = L2Fx_MeshFrag_SampleTextureAlphaSoft(
                    mixed,
                    _AlphaFromLuma,
                    _LumaAlphaFloor,
                    _LumaAlphaPower,
                    _UseSoftLumaAlpha,
                    _IgnoreMainTexAlpha);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                if (_Fadeout > 0.5)
                {
                    lifeAlpha = pow(saturate(lifeAlpha), max(_FadeOutPower, 0.0001));
                }

                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)_RgbBoost;
                half intensity = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                rgb *= intensity;
                if (intensity < (half)1e-4)
                {
                    rgb = half3(0.0, 0.0, 0.0);
                }

                // ONE+ONE additive (RenderDoc): fade/mask baked into rgb; alpha unused by blend.
                return half4(saturate(rgb), 1.0);
            }

            ENDHLSL
        }
    }
}
