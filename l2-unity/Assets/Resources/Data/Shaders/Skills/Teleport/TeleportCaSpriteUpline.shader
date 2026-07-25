// e_u031_a SpriteEmitter2 "upline": fx_m_t0059, PTDU_Up, polar ring, One+One additive.
// SPIR-V FF decomp: out = sample(t0, uv) * in_Color0; consts.textureFactor read but unused.
// UV = PointCoord when sprite flag set, else in_Texcoord0.xy (flipbook atlas baked in VS here).
// Optional fog + alpha-test demote; blend One+One.
Shader "L2/Effects/TeleportCaSpriteUpline"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0059)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (3, 3, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (1.2, 1.2, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.05
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.3
        _FadeOutPower ("FadeOut Power (>1 faster early dim)", Range(0.25, 4)) = 1

        _Opacity ("Opacity (keep 1; brightness is in in_Color0 / ColorMult)", Range(0, 2)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1
        _RgbBoost ("RGB Boost (Unity tune; FF uses in_Color0 only)", Range(0, 4)) = 1
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        [Toggle] _UseAlphaTest ("D3D9 alpha test (demote)", Float) = 0
        _AlphaRef ("Alpha test reference", Range(0, 1)) = 0

        _ColorScaleCount ("ColorScale Count", Int) = 2
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 1
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)
        _ColorMultMin ("ColorMult Min (RGB, .uc ColorMultiplierRange)", Color) = (0.5, 0.5, 0.8, 1)
        _ColorMultMax ("ColorMult Max (RGB, .uc ColorMultiplierRange)", Color) = (0.7, 0.7, 1.0, 1)

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, -20, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (90, 90, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (20, 20, 0, 0)
        _SpawnUnitScale ("Spawn/velocity UU->Unity (0.01)", Float) = 0.01
        _UcPolarRadiusScale ("UC PolarRadius Scale", Float) = 1
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale", Float) = 1
        _UcVelocityScale ("UC StartVelocity Scale", Float) = 1
        _UcAccelerationScale ("UC Acceleration Scale", Float) = 1

        _VelocityRangeZ ("StartVelocityRange Z UU (Min,Max)", Vector) = (0, 30, 0, 0)
        _Acceleration ("Acceleration UE (X,Y,Z)", Vector) = (0, 0, 200, 0)
        _VelocityLossRangeZ ("VelocityLossRange Z UU (Min,Max)", Vector) = (1, 1, 0, 0)

        _SizeRangeX ("StartSizeRange X UU (Min,Max)", Vector) = (1, 3, 0, 0)
        _SizeRangeY ("StartSizeRange Y UU (Min,Max)", Vector) = (30, 30, 0, 0)
        _SizeRangeZ ("StartSizeRange Z UU (Min,Max)", Vector) = (30, 30, 0, 0)
        _L2FxEffectScale ("L2 Fx Effect Scale (runtime target)", Float) = 1
        _L2FxSpriteScale ("L2 Fx Sprite Scale (per-effect tune)", Float) = 1

        [Header(Unity RenderDoc Tune not UC)]
        _L2FxSpriteWidthScale ("Sprite Width Scale (RenderDoc vs L2)", Float) = 1
        _L2FxSpriteHeightScale ("Sprite Height Scale (RenderDoc vs L2)", Float) = 0.5

        [Toggle] _UniformSize ("Uniform Size", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.51
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 1
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 0.3

        [Toggle] _SpinParticles ("Spin Particles (L2 upline: off, UC beacon only)", Float) = 0
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 0, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 2
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 2
        _SubdivisionStart ("Subdivision Start", Float) = 2
        _SubdivisionEnd ("Subdivision End", Float) = 3
        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend (RenderDoc: One)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend (RenderDoc: One)", Float) = 1

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (_StartTime=0)", Float) = 0
        [Toggle] _DebugAtlasPreviewLoop ("Debug Preview Loop (flipbook/fade time)", Float) = 0
        [Toggle] _DebugAtlasPreviewRealSize ("Debug Preview Real UC Size (no x8)", Float) = 0
        [Toggle] _DebugAtlasPreviewMotion ("Debug Preview Motion (spawn+fly one streak)", Float) = 0
        _DebugAtlasPreviewAge ("Debug Preview Life (0-1 of Lifetime)", Range(0, 1)) = 0.25
        _DebugAtlasPreviewSizeScale ("Debug Preview Size Multiplier (ignored if Real Size)", Range(0.5, 32)) = 8
        _DebugAtlasPreviewAlpha ("Debug Preview Alpha (unused w/ One+One)", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 16)) = 8
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

        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "TeleportCaSpriteUpline"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _DEBUGSPAWNREGION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"
            #include "../Common/L2FxUcToUnityConvert.hlsl"
            #include "../Common/L2FxMeshEmitterVertex.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxSpriteMultiSheet.hlsl"
            #include "../Common/L2FxParticleAnim.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteColorFade.hlsl"
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
                float _FadeOutPower;
                float _Opacity;
                float _RgbBoost;
                float _EmitterAlpha;
                float _bAlphaBlend;
                float _UseAlphaTest;
                float _AlphaRef;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _SpawnUnitScale;
                float _UcPolarRadiusScale;
                float _UcStartLocationOffsetScale;
                float _UcVelocityScale;
                float _UcAccelerationScale;
                float4 _VelocityRangeZ;
                float4 _Acceleration;
                float4 _VelocityLossRangeZ;
                float4 _SizeRangeX;
                float4 _SizeRangeY;
                float4 _SizeRangeZ;
                float _L2FxEffectScale;
                float _L2FxSpriteScale;
                float _L2FxSpriteWidthScale;
                float _L2FxSpriteHeightScale;
                float _UniformSize;
                float _UseSizeScale;
                float _SizeScaleTime0;
                float _SizeScaleVal0;
                float _SizeScaleTime1;
                float _SizeScaleVal1;
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _BlendBetweenSubdivisions;
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewLoop;
                float _DebugAtlasPreviewRealSize;
                float _DebugAtlasPreviewMotion;
                float _DebugAtlasPreviewAge;
                float _DebugAtlasPreviewSizeScale;
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
            };

            float UplineSizeScaleScalar(float ageNorm)
            {
                return L2Fx_MeshBuiltin_SampleSizeScaleScalar(
                    ageNorm,
                    0.0,
                    1.0,
                    2u,
                    _UseSizeScale,
                    0.0,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    1.0, 1.0,
                    1.0, 1.0,
                    1.0, 1.0);
            }

            L2Fx_UcToUnitySpriteSpawnData UplineSpawnData()
            {
                L2Fx_UcToUnitySpriteSpawnData spawn;
                spawn.azimuthDegMinMax = _PolarAzimuthDeg.xy;
                spawn.polarPitchDegMinMax = _PolarPitchDeg.xy;
                spawn.radiusMinMax = _PolarRadius.xy;
                spawn.startLocationOffsetUe = _StartLocationOffset.xyz;
                spawn.startLocationRangeMinUe = float3(0.0, 0.0, 0.0);
                spawn.startLocationRangeMaxUe = float3(0.0, 0.0, 0.0);
                spawn.ucPolarRadiusScale = _UcPolarRadiusScale;
                spawn.ucStartLocationOffsetScale = _UcStartLocationOffsetScale;
                spawn.ucStartLocationRangeScale = 1.0;
                spawn.spawnUnitScale = _SpawnUnitScale;
                return spawn;
            }

            L2Fx_UcToUnitySpriteConvertData UplineSpriteConvertData()
            {
                L2Fx_UcToUnitySpriteConvertData data;
                data.effectScale = _L2FxEffectScale;
                data.spriteScale = _L2FxSpriteScale;
                return data;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                lifetime = max(lifetime, 1e-4);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float ageNorm = saturate(age / lifetime);
                OUT.particleSeed = pSeed;

                float scenePreview = L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime);
                float previewMotion = L2Fx_AtlasDebug_PreviewMotion(scenePreview, _DebugAtlasPreviewMotion);
                float previewRealSize = L2Fx_AtlasDebug_PreviewRealSize(scenePreview, _DebugAtlasPreviewRealSize);
                L2Fx_AtlasDebug_OverrideAgeNorm(
                    scenePreview,
                    _DebugAtlasPreviewLoop,
                    _DebugAtlasPreviewAge,
                    _Time.y,
                    lifetime,
                    age,
                    ageNorm);

                float unitScale = L2Fx_UcToUnityResolveSpawnUnitScale(_SpawnUnitScale);
                float3 spawnOfs = float3(0.0, 0.0, 0.0);

                if (L2Fx_AtlasDebug_UseRuntimeMotion(scenePreview, previewMotion) > 0.5)
                {
                    spawnOfs = L2Fx_UcToUnitySpriteSpawnOffset(
                        UplineSpawnData(),
                        pSeed,
                        _StartTime);

                    float velZ = L2Fx_RandomRange(
                        L2Fx_UcToUnityApplyScale2(_VelocityRangeZ.xy, _UcVelocityScale),
                        pSeed,
                        _StartTime,
                        107.0);
                    float3 velUe = float3(0.0, 0.0, velZ);
                    float3 accUe = L2Fx_UcToUnityApplyScale3(_Acceleration.xyz, _UcAccelerationScale);
                    float lossZ = L2Fx_RandomRange(_VelocityLossRangeZ.xy, pSeed, _StartTime, 109.0);
                    float3 vel = L2Fx_UeVectorToUnity(velUe) * unitScale;
                    float3 acc = L2Fx_UeVectorToUnity(accUe) * unitScale;
                    // VelocityLoss is a rate (1/s), NOT a distance -> no unitScale.
                    // dv/dt = a - k*v => speed converges to a/k (200 UU/s), no overshoot.
                    float3 lossRate = L2Fx_UeVectorToUnity(float3(0.0, 0.0, lossZ));
                    spawnOfs += L2Fx_DisplacementVelocityLossExp(vel, acc, lossRate, age);
                }

                float3 baseSizeM = L2Fx_UcToUnitySpriteStartSize(
                    _SizeRangeX.xy,
                    _SizeRangeY.xy,
                    _SizeRangeZ.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime,
                    UplineSpriteConvertData());
                float3 sizeM = L2Fx_AtlasDebug_ResolveSpriteSize(
                    scenePreview,
                    previewRealSize,
                    baseSizeM,
                    UplineSizeScaleScalar(ageNorm),
                    _DebugAtlasPreviewSizeScale,
                    0.12);

                // Skip RenderDoc tune only for static UC-size atlas inspect; play / motion use tune.
                if (scenePreview < 0.5 || previewMotion > 0.5 || previewRealSize < 0.5)
                {
                    sizeM = L2Fx_UcToUnitySpriteAnisotropicTune(
                        sizeM,
                        _L2FxSpriteWidthScale,
                        _L2FxSpriteHeightScale);
                }

                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = L2Fx_PtduUpMultiSheetPositionWS(
                    centerWS,
                    IN.positionOS.xyz,
                    sizeM,
                    L2Fx_SpriteYawRadiansFromObjectMatrix());

                OUT.positionHCS = TransformWorldToHClip(posWS);

                // L2 mesh (fx_m_t0059 upline): atlas V increases toward streak top (+Y); skip PtduUpQuadUv01 flip.
                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                float fBlend = 0.0;
                float2 uvA;
                float2 uvB;

                if (L2Fx_AtlasDebug_PinFlipbookToStart(scenePreview, _DebugAtlasPreviewLoop, previewMotion) > 0.5)
                {
                    int fi = clamp(s0, 0, L2Fx_FlipbookCellCount(uSub, vSub) - 1);
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
                    1.0, float4(1, 1, 1, 1),
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

                if (scenePreview > 0.5)
                {
                    // Editor atlas preview keeps the simple multiplicative dim for inspection.
                    float4 colorMult = lerp(
                        _ColorMultMin,
                        _ColorMultMax,
                        L2Fx_Hash11(pSeed * 19.0 + _StartTime));
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
                    OUT.tint = float4(cs.rgb * colorMult.rgb, cs.a * colorMult.a) * lifeAlpha;
                }
                else
                {
                    // Runtime: exact UE3 SpriteEmitter color pipeline (verified byte-for-byte
                    // vs Engine.dll). Per-channel random ColorMultiplier + SUBTRACTIVE fade
                    // (fade-to-black on all RGBA), so G/R zero out before B -> dies blue.
                    // e_u031_a ColorScale is white->white, so the white facade is exact here.
                    OUT.tint = L2Fx_SpriteColorFade_White(
                        _ColorMultMin.rgb, _ColorMultMax.rgb,
                        age, lifetime, _HasLifetime,
                        _FadeIn, _FadeInEndTime,
                        _Fadeout, _FadeoutStartTime,
                        pSeed, _StartTime);
                }

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 tex = lerp(texA, texB, (half)IN.flipbookBlend);

                float scenePreview = L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime);
                if (scenePreview > 0.5)
                {
                    return L2Fx_AtlasDebug_AdditiveOneOnePreview(
                        tex,
                        (half3)IN.tint.rgb,
                        (half)_DebugAtlasPreviewBoost);
                }

                // SPIR-V: out = sample(t0, uv) * in_Color0; One+One uses rgb only.
                half4 outColor = tex * IN.tint;

                if (_UseAlphaTest > 0.5 && outColor.a < (half)_AlphaRef)
                {
                    discard;
                }

                // One+One additive: no per-channel saturate (destroys ColorMult hue at high gain).
                // Do not saturate gain — _RgbBoost>1 must reach the framebuffer (L2 in_Color0 path).
                half gain = (half)max(0.0, _Opacity * _RgbBoost * _EmitterAlpha);
                half3 rgb = outColor.rgb * gain;
                return half4(rgb, 1.0);
            }

            ENDHLSL
        }
    }
}
