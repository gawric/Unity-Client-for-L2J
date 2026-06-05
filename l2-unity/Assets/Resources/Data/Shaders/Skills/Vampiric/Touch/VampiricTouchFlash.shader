// UE m_u003_c SpriteEmitter0 "VampireFlash": PTDS_Brighten, fx_m_t0005 2x2, random subdiv 2..3.
// Small blue motes: delayed burst, additive blend, fast growth via SizeScale.
// Extended: optional ColorScaleRepeats, 3-key color, ColorMultiplierRange, SphereRadius,
//   StartLocationRange, PTVD_OwnerAndStartPosition velocity, VelocityLoss.
// All new features are opt-in via [Toggle] flags; default state = original VampireFlash behaviour.
Shader "L2/Effects/VampiricTouchFlash"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0005)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0.2, 0.2, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (0.8, 0.8, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.54

        _Opacity ("Opacity", Range(0, 2)) = 0.6
        _RgbBoost ("RGB Boost", Range(0, 8)) = 1
        _AlphaBoost ("Alpha Boost", Range(0, 8)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from RGB luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.015
        [Toggle] _UseSoftLumaAlpha ("Soft luma alpha (preserve dim plasma)", Float) = 1
        _LumaAlphaPower ("Luma alpha power (<1 keeps dim plasma)", Range(0.2, 2)) = 0.55

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)

        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count (2 or 3)", Int) = 2
        _ColorScaleRepeats ("ColorScale Repeats (0=legacy,>0=cycles/lifetime)", Float) = 0

        _ColorMultMin ("ColorMult Min (R,G,B)", Color) = (1, 1, 1, 1)
        _ColorMultMax ("ColorMult Max (R,G,B)", Color) = (1, 1, 1, 1)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (-7, 0, 2, 0)
        [Toggle] _ApplySpawnUnitScale ("Apply UE UU->Unity (0.01)", Float) = 1
        _SpawnUnitScale ("UE UU to Unity meters", Float) = 0.01
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (0, 180, 0, 0)
        _RadialSpeed ("Radial Speed UU/s (Min=Max, same speed)", Vector) = (18, 18, 0, 0)
        _PolarRadius ("Polar Radius (Min,Max)", Vector) = (0, 6, 0, 0)

        [Toggle] _UseSphereRadius ("Extra random on sphere surface", Float) = 0
        _SphereRadiusUU ("SphereRadiusRange UU (Min,Max)", Vector) = (16, 16, 0, 0)

        [Toggle] _UseStartLocationRange ("Random box offset from polar point", Float) = 0
        _StartLocationRangeXY ("StartLocationRange XY UU (minX,maxX,minY,maxY)", Vector) = (-5, 5, -5, 5)
        _StartLocationRangeZ ("StartLocationRange Z UU (minZ,maxZ)", Vector) = (-15, 15, 0, 0)

        [Toggle] _UseRadialVelocity ("Outward drift from spawn sphere", Float) = 1
        [Toggle] _UseCameraHorizontalPlane ("Spread on camera horizontal plane (screen L/R)", Float) = 0
        _Acceleration ("Acceleration (XYZ, UE units)", Vector) = (0, 0, 0, 0)

        [Toggle] _UseVelocityTowardOwner ("PTVD_OwnerAndStartPosition (toward owner)", Float) = 0
        _StartVelocityRange ("StartVelocityRange X,Y UU (velX_min,velX_max,velY_min,velY_max)", Vector) = (70, 70, 70, 70)
        _OwnerWorldPos ("Owner World Pos (set by MaterialPropertyBlock)", Vector) = (0, 0, 0, 0)

        [Toggle] _UseVelocityLoss ("Apply velocity loss (linear drag)", Float) = 0
        _VelocityLossRange ("VelocityLossRange UU (Min,Max)", Vector) = (2, 2, 0, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (2, 4, 0, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize x 0.01 (only if size is raw UE UU)", Float) = 0
        [Toggle] _UseMeshQuadBounds ("Use Unity Quad bounds (no shader resize)", Float) = 1
        _BillboardScale ("Manual Billboard Scale (0 = use quad transform scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScale0 ("SizeScale T0 / S0", Vector) = (0.0, 2.0, 0, 0)
        _SizeScale1 ("SizeScale T1 / S1", Vector) = (0.14, 5.0, 0, 0)
        _SizeScale2 ("SizeScale T2 / S2", Vector) = (0.28, 5.5, 0, 0)
        _SizeScale3 ("SizeScale T3 / S3", Vector) = (0.62, 6.0, 0, 0)
        _SizeScale4 ("SizeScale T4 / S4", Vector) = (1.0, 6.2, 0, 0)
        [Toggle] _UseMeshLifetimeScale ("Use shader mesh lifetime scale", Float) = 0
        _ExpansionEndSec ("Expansion End Sec", Float) = 0.6
        [Toggle] _ScaleAfterExpansion ("Scale after expansion", Float) = 0
        _PostBurstScaleSpeed ("Post Burst Scale Speed", Float) = 0

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 2
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 2
        _SubdivisionStart ("Subdivision Start", Float) = 2
        _SubdivisionEnd ("Subdivision End", Float) = 3
        [Toggle] _UseRandomSubdivision ("Random Subdiv [Start..End]", Float) = 1
        [Toggle] _BlendBetweenSubdivisions ("Blend Subdiv Over Age", Float) = 0
        _AtlasCellZoom ("Atlas Cell Zoom (crop scattered pixels)", Range(1, 8)) = 1
        _AtlasCellOffset ("Atlas Cell Offset XY", Vector) = (0, 0, 0, 0)
        [Toggle] _UseProceduralFlash ("Use compact procedural flash", Float) = 0
        [HDR] _FlashTint ("Flash Tint", Color) = (0.45, 0.72, 1.0, 1)
        _FlashCoreRadius ("Flash Core Radius", Range(0.005, 0.12)) = 0.018
        _FlashHaloRadius ("Flash Halo Radius", Range(0.02, 0.3)) = 0.095
        _FlashHaloPower ("Flash Halo Power", Range(0.5, 8)) = 3.5

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (show selected cell)", Float) = 0
        _DebugAtlasPreviewAlpha ("Debug Preview Alpha", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 2
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
            Name "VampiricFlash"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxEmitterSpawn.hlsl"
            #include "../../Common/L2FxFlipbook.hlsl"
            #include "../../Common/L2FxMeshParticleMotion.hlsl"
            #include "../../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../../Common/L2FxMeshLifetimeScale.hlsl"
            #include "../../Common/L2FxMeshFragment.hlsl"
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
                float _RgbBoost;
                float _AlphaBoost;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _UseSoftLumaAlpha;
                float _LumaAlphaPower;
                float _bAlphaBlend;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float4 _StartLocationOffset;
                float _ApplySpawnUnitScale;
                float _SpawnUnitScale;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _UseSphereRadius;
                float4 _SphereRadiusUU;
                float _UseStartLocationRange;
                float4 _StartLocationRangeXY;
                float4 _StartLocationRangeZ;
                float _UseRadialVelocity;
                float _UseCameraHorizontalPlane;
                float4 _RadialSpeed;
                float4 _Acceleration;
                float _UseVelocityTowardOwner;
                float4 _StartVelocityRange;
                float4 _OwnerWorldPos;
                float _UseVelocityLoss;
                float4 _VelocityLossRange;
                float4 _SizeRange;
                float _ApplyUuToStartSize;
                float _UseMeshQuadBounds;
                float _BillboardScale;
                float _UniformSize;
                float _UseSizeScale;
                float4 _SizeScale0;
                float4 _SizeScale1;
                float4 _SizeScale2;
                float4 _SizeScale3;
                float4 _SizeScale4;
                float _UseMeshLifetimeScale;
                float _ExpansionEndSec;
                float _ScaleAfterExpansion;
                float _PostBurstScaleSpeed;
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _UseRandomSubdivision;
                float _BlendBetweenSubdivisions;
                float _AtlasCellZoom;
                float4 _AtlasCellOffset;
                float _UseProceduralFlash;
                float4 _FlashTint;
                float _FlashCoreRadius;
                float _FlashHaloRadius;
                float _FlashHaloPower;
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
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float4 tint : COLOR;
                nointerpolation float particleSeed : TEXCOORD2;
                float flipbookBlend : TEXCOORD3;
                float2 quadUv : TEXCOORD4;
            };

            float FlashUnitScale()
            {
                return _ApplySpawnUnitScale > 0.5 ? _SpawnUnitScale : 1.0;
            }

            // ---- random-on-sphere helper for SphereRadius ----
            float3 FlashRandomOnSphereUE(float seed, float startTime, float radiusUU, float saltBase)
            {
                float u = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, saltBase);
                float v = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, saltBase + 1.0);
                float theta = 6.2831853 * u;
                float z = 1.0 - 2.0 * v;
                float r = sqrt(max(0.0, 1.0 - z * z));
                return float3(r * cos(theta), r * sin(theta), z) * radiusUU;
            }

            float3 FlashSpawnOffset(float pSeed)
            {
                float unitScale = FlashUnitScale();

                if (_UseCameraHorizontalPlane > 0.5)
                {
                    float azimuthDeg = L2Fx_RandomRange(_PolarAzimuthDeg.xy, pSeed, _StartTime, 71.0);
                    float radiusUU = L2Fx_RandomRange(_PolarRadius.xy, pSeed, _StartTime, 79.0);
                    float3 dirWS = L2Fx_CameraHorizontalUnitDirection(azimuthDeg);
                    float3 offsetWS = dirWS * (radiusUU * unitScale);

                    if (_UseStartLocationRange > 0.5)
                    {
                        float3 camRightH;
                        float3 camForwardH;
                        L2Fx_CameraHorizontalBasis(camRightH, camForwardH);
                        float boxRight = L2Fx_RandomRange(_StartLocationRangeXY.xy, pSeed, _StartTime, 231.0) * unitScale;
                        float boxForward = L2Fx_RandomRange(_StartLocationRangeXY.zw, pSeed, _StartTime, 233.0) * unitScale;
                        float boxUp = L2Fx_RandomRange(_StartLocationRangeZ.xy, pSeed, _StartTime, 239.0) * unitScale;
                        offsetWS += camRightH * boxRight + camForwardH * boxForward + float3(0.0, boxUp, 0.0);
                    }

                    offsetWS += L2Fx_UeVectorToUnity(_StartLocationOffset.xyz) * unitScale;

                    float3 pivotWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                    return TransformWorldToObject(pivotWS + offsetWS);
                }

                float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);

                // --- SphereRadius (extra random on sphere surface) ---
                if (_UseSphereRadius > 0.5)
                {
                    float sphereR = L2Fx_RandomRange(_SphereRadiusUU.xy, pSeed, _StartTime, 211.0);
                    posUe += FlashRandomOnSphereUE(pSeed, _StartTime, sphereR, 221.0);
                }

                // --- StartLocationRange (random box offset from polar point) ---
                if (_UseStartLocationRange > 0.5)
                {
                    posUe += float3(
                        L2Fx_RandomRange(_StartLocationRangeXY.xy, pSeed, _StartTime, 231.0),
                        L2Fx_RandomRange(_StartLocationRangeXY.zw, pSeed, _StartTime, 233.0),
                        L2Fx_RandomRange(_StartLocationRangeZ.xy, pSeed, _StartTime, 239.0));
                }

                posUe += _StartLocationOffset.xyz;
                return L2Fx_UeVectorToUnity(posUe) * unitScale;
            }

            float3 FlashRandomUnitDirection(float pSeed)
            {
                if (_UseCameraHorizontalPlane > 0.5)
                {
                    float azimuthDeg = L2Fx_RandomRange(_PolarAzimuthDeg.xy, pSeed, _StartTime, 71.0);
                    float3 dirWS = L2Fx_CameraHorizontalUnitDirection(azimuthDeg);
                    float3 dirOS = L2Fx_WorldVelocityToObject(dirWS);
                    float lenDir = length(dirOS);
                    return lenDir > 1e-5 ? (dirOS / lenDir) : float3(0.0, 1.0, 0.0);
                }

                // Direction for "explosion-like" velocity must not inherit StartLocationOffset bias.
                float3 dirUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    float2(1.0, 1.0),
                    pSeed,
                    _StartTime);
                float3 dir = L2Fx_UeVectorToUnity(dirUe);
                float lenDir = length(dir);
                return lenDir > 1e-5 ? (dir / lenDir) : float3(0.0, 1.0, 0.0);
            }

            // --- PTVD_OwnerAndStartPosition: horizontal outward burst (PoisonCloud-style XZ spread) ---
            float3 FlashVelocityOwnerAndStart(float3 spawnOffsetUnity, float pSeed)
            {
                float unitScale = FlashUnitScale();
                float hs = L2Fx_RandomRange(_StartVelocityRange.xy, pSeed, _StartTime, 113.0) * unitScale;

                if (_UseCameraHorizontalPlane > 0.5)
                {
                    float3 pivotWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                    float3 spawnWS = TransformObjectToWorld(spawnOffsetUnity);
                    float3 flatOffWS = float3(spawnWS.x - pivotWS.x, 0.0, spawnWS.z - pivotWS.z);

                    float3 camRightH;
                    float3 camForwardH;
                    L2Fx_CameraHorizontalBasis(camRightH, camForwardH);
                    float2 hCam = float2(dot(flatOffWS, camRightH), dot(flatOffWS, camForwardH));
                    float len = length(hCam);
                    float3 dirWS = len > 1e-5
                        ? (camRightH * (hCam.x / len) + camForwardH * (hCam.y / len))
                        : L2Fx_CameraHorizontalUnitDirection(
                            L2Fx_RandomRange(_PolarAzimuthDeg.xy, pSeed, _StartTime, 181.0));
                    return L2Fx_WorldVelocityToObject(dirWS * hs);
                }

                float2 hDir = L2Fx_OutwardDirectionXZ(
                    spawnOffsetUnity,
                    _PolarAzimuthDeg.xy,
                    pSeed,
                    _StartTime,
                    181.0);
                return float3(hDir.x * hs, 0.0, hDir.y * hs);
            }

            float2 FlashRotateUv(float2 uv, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);
                float2 p = uv - 0.5;
                return float2(p.x * c - p.y * s, p.x * s + p.y * c) + 0.5;
            }

            float2 FlashMote(float2 uv, float2 center, float coreRadius, float haloRadius, float haloPower)
            {
                float d = length(uv - center);
                float core = 1.0 - smoothstep(coreRadius, coreRadius + 0.018, d);
                float halo = pow(saturate(1.0 - d / max(haloRadius, 1e-4)), haloPower);
                return float2(core, halo);
            }

            float FlashTextureAlpha(half4 texColor)
            {
                if (_UseSoftLumaAlpha > 0.5 && _AlphaFromLuma > 0.5)
                {
                    float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                    float mask = saturate((lum - _LumaAlphaFloor) / max(1.0 - _LumaAlphaFloor, 1e-4));
                    return pow(mask, max(_LumaAlphaPower, 1e-4));
                }

                return L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
            }

            float3 FlashCameraFacingPositionWS(float3 centerWS, float3 quadOS)
            {
                float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
                toCamera = dot(toCamera, toCamera) > 1e-8 ? normalize(toCamera) : float3(0, 0, 1);

                float3 upRef = float3(0, 1, 0);
                if (abs(dot(upRef, toCamera)) > 0.98)
                {
                    upRef = float3(0, 0, 1);
                }

                float3 rightWS = normalize(cross(upRef, toCamera));
                float3 upWS = normalize(cross(toCamera, rightWS));

                float3 objectScale = L2Fx_ObjectWorldScale();
                return centerWS
                    + rightWS * (quadOS.x * objectScale.x)
                    + upWS * (quadOS.y * objectScale.y);
            }

            // --- ColorScale with optional repeats and 3 keys ---
            float4 FlashComputeColorTint(float ageNorm, float pSeed)
            {
                if (_ColorScaleRepeats <= 0.0 && _ColorScaleCount <= 2)
                {
                    // Legacy path: simple 2-key lerp (original VampiricFlash behaviour)
                    float4 tint = L2Fx_ColorScaleTwoKeys(
                        ageNorm, _ColorScale0, _ColorScale1, _ColorScaleTime1);
                    if (_bAlphaBlend > 0.5) tint.a = tint.r;
                    return tint;
                }

                // New path: SampleColorScale with repeats, 2-3 keys
                float ctimes[8];
                float4 ccols[8];
                [unroll]
                for (uint i = 0; i < 8; i++)
                {
                    ctimes[i] = 999.0;
                    ccols[i] = float4(1, 1, 1, 1);
                }
                ctimes[0] = 0.0;
                ccols[0] = _ColorScale0;
                if (_ColorScaleCount >= 2)
                {
                    ctimes[1] = _ColorScaleTime1;
                    ccols[1] = _ColorScale1;
                }
                if (_ColorScaleCount >= 3)
                {
                    ctimes[2] = _ColorScaleTime2;
                    ccols[2] = _ColorScale2;
                }

                float csParam = max(_ColorScaleRepeats, 1.0) - 1.0;
                float4 tint = L2Fx_SampleColorScale(
                    ageNorm, csParam, _ColorScaleCount,
                    ctimes, ccols, _bAlphaBlend > 0.5);

                // Apply ColorMultiplierRange (only when repeat path is active)
                tint.rgb = L2Fx_ApplyColorMultiplier(
                    tint.rgb, _ColorMultMin.rgb, _ColorMultMax.rgb, pSeed, _StartTime);

                return tint;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float ageNorm = saturate(age / max(lifetime, 1e-4));
                OUT.particleSeed = pSeed;

                float scalePathT = _UseMeshLifetimeScale > 0.5
                    ? L2Fx_MeshLifetimeScalePathT(age, _HasLifetime, _ExpansionEndSec, lifetime)
                    : ageNorm;
                float sizeMul = L2Fx_MeshLifetimeScaleMultiplier(
                    scalePathT,
                    age,
                    lifetime,
                    _UseSizeScale,
                    _SizeScale0,
                    _SizeScale1,
                    _SizeScale2,
                    _SizeScale3,
                    _SizeScale4,
                    _UseMeshLifetimeScale > 0.5 ? _ExpansionEndSec : lifetime,
                    _UseMeshLifetimeScale > 0.5 ? _ScaleAfterExpansion : 0.0,
                    _UseMeshLifetimeScale > 0.5 ? _PostBurstScaleSpeed : 0.0);
                float3 baseSize = L2Fx_StartSize(
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime);
                if (_ApplyUuToStartSize > 0.5)
                {
                    // SpriteEmitter0 StartSizeRange is authored in UE UU; convert to meters.
                    baseSize *= _SpawnUnitScale;
                }
                float3 quadOS = IN.positionOS.xyz;
                if (_UseMeshQuadBounds > 0.5)
                {
                    if (_UseMeshLifetimeScale > 0.5)
                    {
                        quadOS *= sizeMul;
                    }
                }
                else
                {
                    quadOS *= baseSize * sizeMul;
                }

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    L2Fx_ApplyMeshScalarSpin(quadOS, IN.normalOS, true, angle);
                }

                float3 spawnOfs = FlashSpawnOffset(pSeed);

                // --- Velocity ---
                if (_UseRadialVelocity > 0.5 || _UseVelocityTowardOwner > 0.5)
                {
                    float3 vel;
                    if (_UseVelocityTowardOwner > 0.5)
                    {
                        // PTVD_OwnerAndStartPosition: direction toward owner
                        vel = FlashVelocityOwnerAndStart(spawnOfs, pSeed);
                    }
                    else
                    {
                        // Original radial velocity: random direction
                        float3 dir = FlashRandomUnitDirection(pSeed + 17.0);
                        float radialSpeed = L2Fx_RandomRange(_RadialSpeed.xy, pSeed, _StartTime, 103.0) * FlashUnitScale();
                        vel = dir * radialSpeed;
                    }

                    float3 acc = L2Fx_UeVectorToUnity(_Acceleration.xyz) * FlashUnitScale();

                    if (_UseVelocityLoss > 0.5)
                    {
                        float loss = L2Fx_RandomRange(_VelocityLossRange.xy, pSeed, _StartTime, 197.0) * FlashUnitScale();
                        spawnOfs += L2Fx_DisplacementLinearHorizontalVelocityLoss(vel, acc, loss, age);
                    }
                    else
                    {
                        spawnOfs += L2Fx_DisplacementLinearVelocityLoss(vel, acc, float3(0.0, 0.0, 0.0), age);
                    }
                }

                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = _UseMeshQuadBounds > 0.5
                    ? FlashCameraFacingPositionWS(centerWS, quadOS)
                    : L2Fx_CameraBillboardPositionWS(centerWS, quadOS, _BillboardScale, _ApplyUuToStartSize);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.quadUv = quadUv;
                float zoom = max(_AtlasCellZoom, 1.0);
                float2 atlasUv = (quadUv - 0.5) / zoom + 0.5 + _AtlasCellOffset.xy;
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int subStart = (int)_SubdivisionStart;
                int subEnd = (int)_SubdivisionEnd;
                float fBlend = 0.0;
                float2 uvA;
                float2 uvB;

                if (_UseRandomSubdivision > 0.5)
                {
                    int cell = L2Fx_FlipbookSubDivisionRandomFrame(pSeed, _StartTime, subStart, subEnd, 191.0);
                    uvA = L2Fx_FlipbookAtlasUV(atlasUv, cell, uSub, vSub);
                    uvB = uvA;
                }
                else if (_BlendBetweenSubdivisions > 0.5)
                {
                    int fa;
                    int fb;
                    L2Fx_FlipbookBlendFrames(ageNorm, subStart, subEnd, fa, fb, fBlend);
                    uvA = L2Fx_FlipbookAtlasUV(atlasUv, fa, uSub, vSub);
                    uvB = L2Fx_FlipbookAtlasUV(atlasUv, fb, uSub, vSub);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, subStart, subEnd);
                    uvA = L2Fx_FlipbookAtlasUV(atlasUv, fi, uSub, vSub);
                    uvB = uvA;
                }

                OUT.uvAtlasA = uvA;
                OUT.uvAtlasB = uvB;
                OUT.flipbookBlend = fBlend;

                OUT.tint = FlashComputeColorTint(ageNorm, pSeed);

                // Legacy alpha-blend fallback (when not using repeats path)
                if (_ColorScaleRepeats <= 0.0 && _ColorScaleCount <= 2)
                {
                    if (_bAlphaBlend > 0.5)
                    {
                        OUT.tint.a = OUT.tint.r;
                    }
                }

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pSeed = IN.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);

                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 mixed = lerp(texA, texB, (half)IN.flipbookBlend);

                float mask = FlashTextureAlpha(mixed);

                if (_DebugAtlasPreview > 0.5)
                {
                    return L2Fx_AtlasDebugPreviewColor(
                        mixed,
                        mask,
                        _DebugAtlasPreviewAlpha,
                        _DebugAtlasPreviewBoost,
                        _DebugAtlasBackground);
                }

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                if (_UseProceduralFlash > 0.5)
                {
                    float angle = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime) * L2Fx_TwoPi;
                    float2 uv = FlashRotateUv(IN.quadUv, angle);

                    float2 m = 0.0;
                    m += FlashMote(uv, float2(0.16, 0.32), _FlashCoreRadius, _FlashHaloRadius, _FlashHaloPower);
                    m += FlashMote(uv, float2(0.20, 0.72), _FlashCoreRadius, _FlashHaloRadius, _FlashHaloPower);
                    m += FlashMote(uv, float2(0.78, 0.26), _FlashCoreRadius, _FlashHaloRadius, _FlashHaloPower);
                    m += FlashMote(uv, float2(0.84, 0.52), _FlashCoreRadius, _FlashHaloRadius, _FlashHaloPower);
                    m += FlashMote(uv, float2(0.74, 0.78), _FlashCoreRadius, _FlashHaloRadius, _FlashHaloPower);

                    float core = saturate(m.x);
                    float halo = saturate(m.y);
                    float flashAlpha = saturate(core + halo * 0.68) * IN.tint.a * _Opacity * lifeAlpha;
                    half3 flashRgb = (half3)(_FlashTint.rgb * (core * 1.8 + halo * 0.85) * _RgbBoost * IN.tint.rgb);
                    return half4(saturate(flashRgb), (half)flashAlpha);
                }

                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)_RgbBoost;
                half alpha = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}