// m_u004_a SpriteEmitter7: PTDS_Brighten, fx_m_t0005 4x4, polar spawn + box range,
// PTVD_StartPositionAndOwner velocity toward focal (or funnel ease-in fallback), ColorScale/SizeScale repeats.
Shader "L2/Effects/MightTaSprite"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0005)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (0.5, 0.5, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec, preview+play)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec, preview+play)", Float) = 0.5
        _FadeOutPower ("FadeOut Power (>1 faster early dim)", Range(0.25, 4)) = 1

        _Opacity ("Opacity", Range(0, 2)) = 1
        _TextureDilateTexels ("Texture Dilate Texels", Range(0, 24)) = 0
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        _PlasmaRgbScale ("Plasma RGB Scale (low luma only)", Range(0, 2)) = 1
        _PlasmaLumaMax ("Plasma Luma Max", Range(0.01, 1)) = 0.35
        _AlphaBoost ("Alpha Boost", Range(0, 16)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        [Toggle] _UseSoftLumaAlpha ("Soft luma alpha (preserve dim plasma)", Float) = 1
        _LumaAlphaPower ("Luma alpha power (<1 keeps dim fill)", Range(0.2, 2)) = 0.55
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorScaleRepeats ("ColorScale Repeats", Float) = 2
        _ColorScaleCount ("ColorScale Count", Int) = 4
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.089286
        _ColorScale1 ("ColorScale[1]", Color) = (0.992, 0.992, 0.992, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 0.467857
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleTime3 ("ColorScale Time[3]", Range(0, 1)) = 1
        _ColorScale3 ("ColorScale[3]", Color) = (1, 1, 1, 1)

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, 10, 0)
        _StartLocationRangeX ("StartLocationRange X UU (Min,Max)", Vector) = (-10, 10, 0, 0)
        _StartLocationRangeY ("StartLocationRange Y UU (Min,Max)", Vector) = (-10, 10, 0, 0)
        _StartLocationRangeZ ("StartLocationRange Z UU (Min,Max)", Vector) = (-10, 10, 0, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (75, 105, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (14, 14, 0, 0)
        [Toggle] _UseSphereRadius ("Use SphereRadiusRange", Float) = 0
        _SphereRadiusUU ("SphereRadiusRange UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpawnUnitScale ("Spawn/velocity UU->Unity (0.01)", Float) = 0.01
        [Toggle] _UseSpawnUnitScale ("Use Spawn Unit Scale", Float) = 0
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale (Might CA tune)", Float) = 1
        _UcStartLocationRangeScale ("UC StartLocationRange Scale (Might CA tune)", Float) = 1
        _UcPolarRadiusScale ("UC PolarRadius Scale (Might CA tune)", Float) = 1
        _UcSphereRadiusScale ("UC SphereRadius Scale (Might TA tune)", Float) = 1
        _UcVelocityScale ("UC StartVelocity Scale (Might CA tune)", Float) = 1
        _UcAccelerationScale ("UC Acceleration Scale (Might CA tune)", Float) = 1
        _OwnerWorldPos ("Owner World Pos (ParticleGroup)", Vector) = (0, 0, 0, 0)
        [Toggle] _UseExternalTargetPosition ("Use External Target Position", Float) = 0
        [Toggle] _UseOwnerFromShaderTarget ("Owner From Shader Target (CasterCenter)", Float) = 0
        _L2FxTargetWorldPos ("L2 Fx Target World Pos", Vector) = (0, 0, 0, 0)

        [Toggle] _UseVelocityTowardFocal ("Velocity Toward Focal (PTVD_StartPositionAndOwner)", Float) = 0
        [Toggle] _UseFull3DVelocityFromOwner ("Full 3D Velocity From Owner (UE PTVD pitch)", Float) = 0
        [Toggle] _UseVelocityMagnitude3D ("Use XYZ Velocity Magnitude", Float) = 0
        _VelocityRangeX ("StartVelocityRange X UU (Min,Max)", Vector) = (30, 30, 0, 0)
        _VelocityRangeY ("StartVelocityRange Y UU (Min,Max)", Vector) = (30, 30, 0, 0)
        _VelocityRangeZ ("StartVelocityRange Z UU (Min,Max)", Vector) = (0, 0, 0, 0)
        _VelocityDirectionSign ("Velocity Direction Sign (1=inward, -1=outward)", Range(-1, 1)) = 1
        _HorizontalOutwardWeight ("Horizontal Outward Weight", Range(0, 2)) = 0
        _Acceleration ("Acceleration UE (X,Y,Z)", Vector) = (0, 0, 100, 0)
        _ArcTangentialWeight ("Arc Tangential / Radial Speed (ring spiral)", Range(0, 1.5)) = 0
        _FocalConvergeStart ("Focal Converge Start (norm age, 0.85 = last 15%)", Range(0, 1)) = 1
        _FocalConvergePower ("Focal Converge Pull Power (>1 = tighter finish)", Range(0.5, 4)) = 2
        _FunnelEasePower ("Funnel Ease-In Power (>1 = slow start, funnel mode only)", Range(1, 5)) = 2.5
        _FunnelArcScale ("Funnel Arc From Accel (funnel mode only)", Range(0, 0.25)) = 0.06

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (4, 8, 0, 0)
        _L2FxEffectScale ("L2 Fx Effect Scale (runtime target)", Float) = 1
        _L2FxSpriteScale ("L2 Fx Sprite Scale (per-effect tune)", Float) = 1
        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 6
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.17
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.37
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.5
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 0.8
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 0.62
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 6
        _SubdivisionEnd ("Subdivision End", Float) = 8
        [Toggle] _UseRandomSubdivision ("Use Random Subdivision", Float) = 1
        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 0

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (_StartTime=0)", Float) = 0
        [Toggle] _DebugAtlasPreviewLoop ("Debug Preview Loop", Float) = 0
        _DebugAtlasPreviewAge ("Debug Preview Life (0-1 of Lifetime)", Range(0, 1)) = 0.25
        _DebugAtlasPreviewSizeScale ("Debug Preview Size Multiplier", Range(0.5, 32)) = 8
        _DebugAtlasPreviewAlpha ("Debug Preview Floor Alpha (unused w/ fade)", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Extra Boost", Range(0.25, 8)) = 1
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

        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MightTaSpriteBrighten"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _DEBUGSPAWNREGION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"
            #include "../Common/L2FxMotionEase.hlsl"
            #include "../Common/L2FxMeshParticleMotion.hlsl"
            #include "../Common/L2FxMeshEmitterVertex.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxAtlasDebug.hlsl"
            #include "../Common/L2FxSpawnRegionDebug.hlsl"
            #include "../Common/L2FxSpriteAutoScale.hlsl"
            #include "../Common/L2FxUcToUnityConvert.hlsl"
            #include "../Common/L2FxPlasmaParticleBlend.hlsl"

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
                float _TextureDilateTexels;
                float _RgbBoost;
                float _PlasmaRgbScale;
                float _PlasmaLumaMax;
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
                float _UseSphereRadius;
                float4 _SphereRadiusUU;
                float _SpawnUnitScale;
                float _UseSpawnUnitScale;
                float _UcStartLocationOffsetScale;
                float _UcStartLocationRangeScale;
                float _UcPolarRadiusScale;
                float _UcSphereRadiusScale;
                float _UcVelocityScale;
                float _UcAccelerationScale;
                float4 _OwnerWorldPos;
                float _UseExternalTargetPosition;
                float4 _L2FxTargetWorldPos;
                float _UseVelocityTowardFocal;
                float _UseFull3DVelocityFromOwner;
                float _UseVelocityMagnitude3D;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float4 _VelocityRangeZ;
                float _VelocityDirectionSign;
                float _HorizontalOutwardWeight;
                float4 _Acceleration;
                float _ArcTangentialWeight;
                float _FocalConvergeStart;
                float _FocalConvergePower;
                float _FunnelEasePower;
                float _FunnelArcScale;
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
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _SpinCCWorCW;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _UseRandomSubdivision;
                float _BlendBetweenSubdivisions;
                float _DebugAtlasPreview;
                float _DebugAtlasPreviewLoop;
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
                nointerpolation float focalVisibility : TEXCOORD4;
            };

            float MightTaSpawnUnitScale()
            {
                return _UseSpawnUnitScale > 0.5 ? _SpawnUnitScale : 1.0;
            }

            float3 MightTaSpawnStartLocationOffsetUe()
            {
                return L2Fx_UcToUnityApplyScale3(_StartLocationOffset.xyz, _UcStartLocationOffsetScale);
            }

            float3 MightTaSpawnOffsetUe(float pSeed)
            {
                float3 posUe = L2Fx_SpawnRegionOffsetUe(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    L2Fx_UcToUnityApplyScale2(_PolarRadius.xy, _UcPolarRadiusScale),
                    MightTaSpawnStartLocationOffsetUe(),
                    L2Fx_UcToUnityApplyScale3(
                        float3(_StartLocationRangeX.x, _StartLocationRangeY.x, _StartLocationRangeZ.x),
                        _UcStartLocationRangeScale),
                    L2Fx_UcToUnityApplyScale3(
                        float3(_StartLocationRangeX.y, _StartLocationRangeY.y, _StartLocationRangeZ.y),
                        _UcStartLocationRangeScale),
                    pSeed,
                    _StartTime);

                if (_UseSphereRadius > 0.5)
                {
                    float sphereRadiusUe = L2Fx_RandomRange(_SphereRadiusUU.xy, pSeed, _StartTime, 211.0)
                        * max(_UcSphereRadiusScale, 0.0);
                    posUe += L2Fx_SpawnRegionRandomOnSphereUe(pSeed, _StartTime, sphereRadiusUe, 221.0);
                }

                return posUe;
            }

            float3 MightTaFocalOffsetWS()
            {
                float3 offsetOs = L2Fx_UeVectorToUnity(
                    L2Fx_UcToUnityApplyScale3(_StartLocationOffset.xyz, _UcStartLocationOffsetScale)) * MightTaSpawnUnitScale();
                return TransformObjectToWorld(offsetOs) - TransformObjectToWorld(float3(0.0, 0.0, 0.0));
            }

            float MightTaUnitsToWorld()
            {
                return length(float3(UNITY_MATRIX_M[0][0], UNITY_MATRIX_M[1][0], UNITY_MATRIX_M[2][0]));
            }

            // Funnel apex: StartLocationOffset center (matches spawn wireframe top-center).
            float3 MightTaMotionFocalWS()
            {
                if (_UseExternalTargetPosition > 0.5)
                {
                    return _L2FxTargetWorldPos.xyz;
                }

                float3 focalOs = L2Fx_UeVectorToUnity(_StartLocationOffset.xyz);
                focalOs *= MightTaSpawnUnitScale();
                return TransformObjectToWorld(focalOs);
            }

            // PTVD_StartPositionAndOwner: inward radial to owner + ring tangent arc + UE accel bend.
            float3 MightTaArcVelocityDirWS(float3 spawnWS, float3 ownerWS)
            {
                float3 toOwnerWS = ownerWS - spawnWS;
                float3 radialDir;
                if (_UseFull3DVelocityFromOwner > 0.5)
                {
                    float radialLen = length(toOwnerWS);
                    radialDir = radialLen > 1e-5 ? (toOwnerWS / radialLen) : float3(0, 0, 1);
                }
                else
                {
                    float3 toOwnerH = float3(toOwnerWS.x, 0, toOwnerWS.z);
                    float radialLenH = length(toOwnerH);
                    radialDir = radialLenH > 1e-5 ? (toOwnerH / radialLenH) : float3(0, 0, 1);
                }

                float3 radialH = float3(radialDir.x, 0, radialDir.z);
                float radialLenH2 = length(radialH);
                float3 radialDirH = radialLenH2 > 1e-5 ? (radialH / radialLenH2) : float3(0, 0, 1);
                float tangentialSign = _SpinCCWorCW > 0.5 ? -1.0 : 1.0;
                float3 tangentDirH = float3(-radialDirH.z, 0, radialDirH.x) * tangentialSign;
                float3 velDir = radialDir + tangentDirH * _ArcTangentialWeight;
                if (length(velDir) > 1e-5)
                {
                    return normalize(velDir);
                }

                return radialDir;
            }

            void MightTaVelocityFocalArrival(
                float3 spawnWS,
                float3 focalWS,
                float3 vel,
                float stopDist,
                inout float3 centerWS,
                out float pathProgress,
                inout float visibility)
            {
                float3 toFocalWS = focalWS - spawnWS;
                float initialDist = length(toFocalWS);
                float distToFocal = length(centerWS - focalWS);
                pathProgress = saturate(1.0 - distToFocal / max(initialDist, 1e-5));

                float movingAwayFromFocal = dot(centerWS - focalWS, vel);
                bool reachedFocal = distToFocal <= stopDist;
                bool overshotFocal = movingAwayFromFocal > 0.0 && distToFocal < initialDist * 0.8;

                if (reachedFocal || overshotFocal)
                {
                    centerWS = focalWS;
                    pathProgress = 1.0;
                    if (overshotFocal)
                    {
                        visibility = 0.0;
                    }
                }
            }

            void MightTaResolveAtlasUvs(
                float2 quadUv,
                int uSub,
                int vSub,
                int s0,
                int s1,
                float ageNorm,
                float pSeed,
                out float2 uvA,
                out float2 uvB,
                out float fBlend)
            {
                fBlend = 0.0;

                if (_UseRandomSubdivision > 0.5)
                {
                    int fi = L2Fx_FlipbookSubDivisionRandomFrame(pSeed, _StartTime, s0, s1, 41.0);
                    if (L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime) > 0.5)
                    {
                        fi = s0;
                    }

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
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                // InitialDelay hides alpha in frag. Motion/size/spin also wait so the spark
                // appears on the spawn ring at full brightness, then flies inward while fading.
                float slotAge = max(0.0, _Time.y - _StartTime);
                float motionAge = max(0.0, slotAge - delay);
                motionAge = min(motionAge, lifetime);
                float age = motionAge;
                if (_StartTime <= 1e-4 && _DebugAtlasPreview < 0.5)
                {
                    age = min(age, max(_LifetimeRange.y, 1e-4));
                }
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

                // Spawn offset is always honest (polar + box + offset) — atlas preview only overrides age/size/flipbook.
                float3 posUe = MightTaSpawnOffsetUe(pSeed);
                float3 spawnOfs = L2Fx_UeVectorToUnity(posUe) * MightTaSpawnUnitScale();
                float3 baseSize;

                if (scenePreview > 0.5)
                {
                    // Fixed preview size — animated SizeScale looks like motion into camera.
                    baseSize = L2Fx_SpriteAutoScaleStartSize(
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _UniformSize > 0.5,
                        pSeed,
                        _StartTime,
                        1.0,
                        _L2FxSpriteScale) * max(_DebugAtlasPreviewSizeScale, 0.5);
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
                        1.0, 1.0);

                    baseSize = L2Fx_SpriteAutoScaleStartSize(
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _UniformSize > 0.5,
                        pSeed,
                        _StartTime,
                        _L2FxEffectScale,
                        _L2FxSpriteScale) * sizeMul;
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

                float3 spawnWS = TransformObjectToWorld(spawnOfs);
                float3 centerWS = spawnWS;
                float focalVisibility = 1.0;

                {
                    float uuToWorld = MightTaUnitsToWorld();
                    float pathProgress;
                    float3 focalWS = MightTaMotionFocalWS();

                    if (_UseVelocityTowardFocal > 0.5)
                    {
                        // PTVD_StartPositionAndOwner: speed along owner<-spawn arc, not straight to chest focal.
                        float3 ownerWS = _OwnerWorldPos.xyz;
                        float3 velDir = MightTaArcVelocityDirWS(spawnWS, ownerWS);
                        velDir *= (_VelocityDirectionSign < 0.0 ? -1.0 : 1.0);
                        float speedUe = L2Fx_RandomRange(
                            L2Fx_UcToUnityApplyScale2(_VelocityRangeX.xy, _UcVelocityScale),
                            pSeed,
                            _StartTime,
                            101.0);
                        if (_UseVelocityMagnitude3D > 0.5)
                        {
                            float3 velUe = float3(
                                L2Fx_RandomRange(L2Fx_UcToUnityApplyScale2(_VelocityRangeX.xy, _UcVelocityScale), pSeed, _StartTime, 101.0),
                                L2Fx_RandomRange(L2Fx_UcToUnityApplyScale2(_VelocityRangeY.xy, _UcVelocityScale), pSeed, _StartTime, 103.0),
                                L2Fx_RandomRange(L2Fx_UcToUnityApplyScale2(_VelocityRangeZ.xy, _UcVelocityScale), pSeed, _StartTime, 107.0));
                            speedUe = length(velUe);
                        }
                        float3 vel = velDir * speedUe * uuToWorld;
                        if (_HorizontalOutwardWeight > 0.001)
                        {
                            float3 outwardH = float3(spawnWS.x - ownerWS.x, 0.0, spawnWS.z - ownerWS.z);
                            float outwardLen = length(outwardH);
                            if (outwardLen > 1e-5)
                            {
                                vel += (outwardH / outwardLen) * speedUe * uuToWorld * _HorizontalOutwardWeight;
                            }
                        }
                        float3 acc = L2Fx_UeVectorToUnity(
                            L2Fx_UcToUnityApplyScale3(_Acceleration.xyz, _UcAccelerationScale)) * uuToWorld;
                        float3 disp = L2Fx_DisplacementConstantAccel(vel, acc, age);
                        centerWS = spawnWS + disp;

                        if (_FocalConvergeStart < 0.999)
                        {
                            float stopDist = max(2.0, _SizeRange.y * 0.35) * uuToWorld;
                            MightTaVelocityFocalArrival(
                                spawnWS,
                                focalWS,
                                vel,
                                stopDist,
                                centerWS,
                                pathProgress,
                                focalVisibility);

                            centerWS = L2Fx_EndFocalConverge(
                                centerWS,
                                focalWS,
                                ageNorm,
                                _FocalConvergeStart,
                                _FocalConvergePower);
                        }
                    }
                    else
                    {
                        // Funnel fallback: ease-in spawn→focal (slow start, fast finish) + optional accel arc.
                        centerWS = L2Fx_EaseInPathPosition(
                            spawnWS, focalWS, ageNorm, _FunnelEasePower, pathProgress);

                        centerWS += L2Fx_EaseInPathArcOffset(
                            L2Fx_UeVectorToUnity(
                                L2Fx_UcToUnityApplyScale3(_Acceleration.xyz, _UcAccelerationScale)) * uuToWorld,
                            age,
                            pathProgress,
                            _FunnelArcScale);
                    }

                    if (_UseVelocityTowardFocal <= 0.5)
                    {
                        L2Fx_FocalArrivalClamp(
                            focalWS,
                            pathProgress,
                            0.985,
                            max(2.0, _SizeRange.y * 0.35) * uuToWorld,
                            centerWS,
                            focalVisibility);
                    }
                }

                if (focalVisibility < 0.5)
                {
                    // Hard clip only on velocity overshoot — lifetime fade uses lifeAlpha in frag.
                    quadOS = 0.0;
                }

                OUT.focalVisibility = focalVisibility;

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
                float fBlend;
                float2 uvA;
                float2 uvB;
                MightTaResolveAtlasUvs(quadUv, uSub, vSub, s0, s1, ageNorm, pSeed, uvA, uvB, fBlend);

                OUT.uvAtlasA = uvA;
                OUT.uvAtlasB = uvB;
                OUT.flipbookBlend = fBlend;

                if (scenePreview > 0.5)
                {
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
                }
                else
                {
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
                }

                return OUT;
            }

            half4 MightTaDilatedSample(float2 uv)
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

            float MightTaSampleAlpha(half4 texColor)
            {
                return L2Fx_MeshFrag_SampleTextureAlphaSoft(
                    texColor,
                    _AlphaFromLuma,
                    _LumaAlphaFloor,
                    _LumaAlphaPower,
                    _UseSoftLumaAlpha,
                    _IgnoreMainTexAlpha);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pSeed = IN.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                lifetime = max(lifetime, 1e-4);

                half4 texA = MightTaDilatedSample(IN.uvAtlasA);
                half4 texB = MightTaDilatedSample(IN.uvAtlasB);
                half4 mixed = lerp(texA, texB, (half)IN.flipbookBlend);

                float scenePreview = L2Fx_AtlasDebug_IsScenePreviewActive(_DebugAtlasPreview, _StartTime);
                if (scenePreview > 0.5)
                {
                    float mask = MightTaSampleAlpha(mixed);
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
                    half alpha = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                    half3 previewRgb = lerp((half3)_DebugAtlasBackground.rgb, rgb, (half)saturate(mask));
                    return half4(saturate(previewRgb), alpha);
                }

                float mask = MightTaSampleAlpha(mixed);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                if (_Fadeout > 0.5)
                {
                    lifeAlpha = pow(saturate(lifeAlpha), max(_FadeOutPower, 0.0001));
                }

                // Brighten fx_m_t0005: RGB boost on full tex (incl. dim halo fill), mask gates alpha only.
                // FadeOut/FadeIn via lifeAlpha only — focalVisibility is overshoot clip in vert, not alpha fade.
                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)_RgbBoost;
                rgb = L2Fx_PlasmaParticle_ApplyLowLumaRgbScale(
                    rgb, mixed.rgb, _PlasmaRgbScale, _PlasmaLumaMax);
                half alpha = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}