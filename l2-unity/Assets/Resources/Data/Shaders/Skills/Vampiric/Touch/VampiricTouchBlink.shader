// UE m_u003_c SpriteEmitter10 "VampireBlink": PTDS_Brighten, fx_m_t0006 4x2, subdiv 4.
// Shared logic: Common/L2FxColorScaleSoft.hlsl, L2FxSpriteMultiSheet.hlsl, L2FxBrightenAlpha.hlsl
// Runtime: RandomizeChildYawOnEnable.cs on SpriteEmitter10 prefab root.
Shader "L2/Effects/VampiricTouchBlink"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0006)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (1.911, 1.911, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.1911
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.97461

        _Opacity ("Opacity", Range(0, 2)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from RGB luma (if import has no grayscale A)", Float) = 0
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        [Header(Blink Pulse)]
        _ColorScaleRepeats ("Blink Pulses Per Lifetime (less = slower)", Float) = 0.625
        _ColorScaleSmoothness ("Pulse edge softness", Range(0, 1)) = 0.4
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.567857
        _ColorScale1 ("ColorScale[1]", Color) = (0.65, 0.65, 0.65, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _SpawnUnitScale ("UE UU to Unity meters (only if ApplyUu on)", Float) = 0.01

        _SizeRangeX ("Start Size X (Min,Max)", Vector) = (5, 5, 0, 0)
        _SizeRangeY ("Start Size Y (Min,Max)", Vector) = (10, 10, 0, 0)
        _SizeRangeZ ("Start Size Z (Min,Max)", Vector) = (10, 10, 0, 0)
        [Toggle] _UniformSize ("Uniform Size (use X only)", Float) = 0
        [Toggle] _ApplyUuToStartSize ("StartSize × 0.01 — only if sizes in raw UE UU", Float) = 0

        _VelocityRangeZ ("Velocity Z UU (Min,Max)", Vector) = (0, 0, 0, 0)

        [Toggle] _UseDirectionAsUp ("PTDU_Up (cylindrical billboard, world Y up)", Float) = 1
        _ProjectionNormal ("ProjectionNormal (UE)", Vector) = (1, 0, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 2
        _SubdivisionStart ("Subdivision Start", Float) = 4
        _SubdivisionEnd ("Subdivision End", Float) = 4
        [Toggle] _BlendBetweenSubdivisions ("Mix Second Cell (off = cell 4 only)", Float) = 0
        _SubdivisionBlendStrength ("Second Cell Mix", Range(0, 1)) = 0
        _AlphaPower ("Brighten Alpha Curve (<1 lifts halo mids)", Range(0.35, 2)) = 0.62
        _RgbAlphaModulate ("RGB Softening By Alpha", Range(0, 1)) = 0
        _HaloInteriorFill ("Halo fill from RGB when tex.a is low", Range(0, 1)) = 0.65
        _HaloLumaMin ("Halo luma min", Range(0, 0.5)) = 0.03
        _HaloLumaMax ("Halo luma max", Range(0.05, 1)) = 0.35
        _FaintRayFill ("Faint ray alpha fill", Range(0, 1)) = 0.35
        _FaintRayLumaMin ("Faint ray luma min", Range(0, 0.1)) = 0.005
        _FaintRayLumaMax ("Faint ray luma max", Range(0.01, 0.35)) = 0.16
        _FaintRayRgbLift ("Faint ray gray lift", Range(0, 0.5)) = 0.12
        _HistoryRgbAlphaFill ("Pixel history RGB alpha fill", Range(0, 1)) = 0.75
        _HistoryRgbAlphaPower ("Pixel history RGB alpha curve", Range(0.25, 2)) = 0.5
        _HistoryRgbAlphaMin ("Pixel history RGB black trim", Range(0, 0.1)) = 0.01
        _HistoryRgbBoost ("Pixel history RGB boost", Range(1, 4)) = 1.8

        [Header(World Soft Clip)]
        [Toggle] _RadialSoftMask ("Clip To Halo Circle (camera view)", Float) = 1
        _WorldMaskRadiusScale ("Halo Radius (x min quad size)", Range(0.2, 0.8)) = 0.48
        _RadialMaskSoftness ("Soft Edge (fraction of radius)", Range(0.05, 0.5)) = 0.22

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (show selected cell)", Float) = 0
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
                    "L2FxGpuInstancing" = "On"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "VampireBlink"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxEmitterSpawn.hlsl"
            #include "../../Common/L2FxFlipbook.hlsl"
            #include "../../Common/L2FxMeshParticleMotion.hlsl"
            #include "../../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../../Common/L2FxColorScaleSoft.hlsl"
            #include "../../Common/L2FxSpriteMultiSheet.hlsl"
            #include "../../Common/L2FxMeshFragment.hlsl"
            #include "../../Common/L2FxBrightenAlpha.hlsl"
            #include "../../Common/L2FxAtlasDebug.hlsl"

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
                float _Opacity;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _bAlphaBlend;
                float _ColorScaleRepeats;
                float _ColorScaleSmoothness;
                uint _ColorScaleCount;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                float4 _StartLocationOffset;
                float _SpawnUnitScale;
                float4 _SizeRangeX;
                float4 _SizeRangeY;
                float4 _SizeRangeZ;
                float _UniformSize;
                float _ApplyUuToStartSize;
                float4 _VelocityRangeZ;
                float _UseDirectionAsUp;
                float4 _ProjectionNormal;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _BlendBetweenSubdivisions;
                float _SubdivisionBlendStrength;
                float _AlphaPower;
                float _RgbAlphaModulate;
                float _HaloInteriorFill;
                float _HaloLumaMin;
                float _HaloLumaMax;
                float _FaintRayFill;
                float _FaintRayLumaMin;
                float _FaintRayLumaMax;
                float _FaintRayRgbLift;
                float _HistoryRgbAlphaFill;
                float _HistoryRgbAlphaPower;
                float _HistoryRgbAlphaMin;
                float _HistoryRgbBoost;
                float _RadialSoftMask;
                float _WorldMaskRadiusScale;
                float _RadialMaskSoftness;
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewAlpha;
                float _DebugAtlasPreviewBoost;
                float4 _DebugAtlasBackground;
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
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float4 tint : COLOR;
                nointerpolation float particleSeed : TEXCOORD2;
                float flipbookBlend : TEXCOORD3;
                float2 viewOffset : TEXCOORD4;
                nointerpolation float worldMaskRadius : TEXCOORD5;
            };

            float3 BlinkStartSizeUU(float seed)
            {
                float sx = L2Fx_RandomRange(_SizeRangeX.xy, seed, _StartTime, 41.0);
                float sy = L2Fx_RandomRange(_SizeRangeY.xy, seed, _StartTime, 43.0);
                float sz = L2Fx_RandomRange(_SizeRangeZ.xy, seed, _StartTime, 47.0);
                if (_UniformSize > 0.5)
                {
                    return float3(sx, sx, sx);
                }

                return float3(sx, sy, sz);
            }

            float3 BlinkResolveSizeM(float3 sizeUU)
            {
                if (_ApplyUuToStartSize > 0.5)
                {
                    return sizeUU * _SpawnUnitScale;
                }

                return sizeUU * L2Fx_ObjectWorldScale();
            }

            float3 BlinkBillboardWS(float3 centerWS, float3 quadOS, float3 sizeUU)
            {
                float3 sizeM = BlinkResolveSizeM(sizeUU);

                if (_UseDirectionAsUp > 0.5)
                {
                    return L2Fx_PtduUpCylindricalBillboardPositionWS(
                        centerWS,
                        quadOS,
                        sizeM,
                        L2Fx_SpriteYawRadiansFromObjectMatrix());
                }

                return L2Fx_CameraBillboardPositionWS(centerWS, quadOS, 0.0, _ApplyUuToStartSize);
            }

            L2Fx_BrightenAlphaTuning BlinkBrightenTuning()
            {
                L2Fx_BrightenAlphaTuning t;
                t.haloInteriorFill = _HaloInteriorFill;
                t.haloLumaMin = _HaloLumaMin;
                t.haloLumaMax = _HaloLumaMax;
                t.faintRayFill = _FaintRayFill;
                t.faintRayLumaMin = _FaintRayLumaMin;
                t.faintRayLumaMax = _FaintRayLumaMax;
                t.faintRayRgbLift = _FaintRayRgbLift;
                t.historyRgbAlphaFill = _HistoryRgbAlphaFill;
                t.historyRgbAlphaPower = _HistoryRgbAlphaPower;
                t.historyRgbAlphaMin = _HistoryRgbAlphaMin;
                t.historyRgbBoost = _HistoryRgbBoost;
                t.alphaPower = _AlphaPower;
                t.rgbAlphaModulate = _RgbAlphaModulate;
                return t;
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float ageNorm = saturate(age / max(lifetime, 1e-4));
                OUT.particleSeed = pSeed;

                float3 spawnOfs = float3(0, 0, 0);
                if (_ApplyUuToStartSize > 0.5)
                {
                    spawnOfs = L2Fx_UeVectorToUnity(_StartLocationOffset.xyz) * _SpawnUnitScale;
                    float velZ = L2Fx_RandomRange(_VelocityRangeZ.xy, pSeed, _StartTime, 107.0);
                    float3 velUe = float3(0, 0, velZ);
                    spawnOfs += L2Fx_UeVectorToUnity(velUe * age) * _SpawnUnitScale;
                }

                float3 sizeUU = BlinkStartSizeUU(pSeed);
                float3 sizeM = BlinkResolveSizeM(sizeUU);
                float3 quadOS = IN.positionOS.xyz;
                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = BlinkBillboardWS(centerWS, quadOS, sizeUU);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                L2Fx_SpriteViewOffsetAndMaskRadius(
                    centerWS,
                    posWS,
                    sizeM,
                    _WorldMaskRadiusScale,
                    OUT.viewOffset,
                    OUT.worldMaskRadius);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int cellA = clamp((int)_SubdivisionStart, 0, L2Fx_FlipbookCellCount(uSub, vSub) - 1);
                int cellB = clamp((int)_SubdivisionEnd, 0, L2Fx_FlipbookCellCount(uSub, vSub) - 1);

                OUT.uvAtlasA = L2Fx_FlipbookAtlasUV(quadUv, cellA, uSub, vSub);
                OUT.uvAtlasB = L2Fx_FlipbookAtlasUV(quadUv, cellB, uSub, vSub);
                OUT.flipbookBlend = (_BlendBetweenSubdivisions > 0.5 && cellA != cellB)
                    ? saturate(_SubdivisionBlendStrength)
                    : 0.0;

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

                OUT.tint = L2Fx_SampleColorScaleSoft(
                    ageNorm,
                    L2Fx_ColorScaleRepeatsParam(_ColorScaleRepeats),
                    _ColorScaleCount,
                    ctimes,
                    ccols,
                    _bAlphaBlend > 0.5,
                    _ColorScaleSmoothness);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pSeed = IN.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);

                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 tex = lerp(texA, texB, (half)IN.flipbookBlend);

                if (_DebugAtlasPreview > 0.5)
                {
                    float alphaRawPreview = L2Fx_BrightenAlphaRaw(
                        tex, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                    return L2Fx_AtlasDebugPreviewColor(
                        tex,
                        alphaRawPreview,
                        _DebugAtlasPreviewAlpha,
                        _DebugAtlasPreviewBoost,
                        _DebugAtlasBackground);
                }

                half4 col = tex * IN.tint;
                half3 rgb = col.rgb;

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                float alphaRaw = L2Fx_BrightenAlphaRaw(
                    tex, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);

                float alphaBlend;
                L2Fx_BrightenApplyTextureContribution(
                    tex, IN.tint, BlinkBrightenTuning(), alphaRaw, rgb, alphaBlend);

                float viewMask = _RadialSoftMask > 0.5
                    ? L2Fx_SpriteViewSoftMask(IN.viewOffset, IN.worldMaskRadius, _RadialMaskSoftness)
                    : 1.0;

                return L2Fx_BrightenFinalize(rgb, alphaBlend, IN.tint, _Opacity, lifeAlpha, viewMask);
            }

            ENDHLSL
        }
    }
}
