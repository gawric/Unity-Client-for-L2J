// UE m_u003_c SpriteEmitter0 "VampireFlash": PTDS_Brighten, fx_m_t0005 2x2, random subdiv 2..3.
// Small blue motes: delayed burst, additive blend, fast growth via SizeScale.
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

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (-7, 0, 2, 0)
        [Toggle] _ApplySpawnUnitScale ("Apply UE UU→Unity (0.01)", Float) = 1
        _SpawnUnitScale ("UE UU to Unity meters", Float) = 0.01
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (0, 180, 0, 0)
        _RadialSpeed ("Radial Speed UU/s (Min=Max, same speed)", Vector) = (18, 18, 0, 0)
        _PolarRadius ("Polar Radius (Min,Max)", Vector) = (0, 6, 0, 0)

        [Toggle] _UseRadialVelocity ("Outward drift from spawn sphere", Float) = 1
        _Acceleration ("Acceleration (XYZ, UE units)", Vector) = (0, 0, 0, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (2, 4, 0, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize × 0.01 (only if size is raw UE UU)", Float) = 0
        [Toggle] _UseMeshQuadBounds ("Use Unity Quad bounds (no shader resize)", Float) = 1
        _BillboardScale ("Manual Billboard Scale (0 = use quad transform scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScale0 ("SizeScale T0 / S0", Vector) = (0.0, 2.0, 0, 0)
        _SizeScale1 ("SizeScale T1 / S1", Vector) = (0.14, 5.0, 0, 0)
        _SizeScale2 ("SizeScale T2 / S2", Vector) = (0.28, 5.5, 0, 0)
        _SizeScale3 ("SizeScale T3 / S3", Vector) = (0.62, 6.0, 0, 0)
        _SizeScale4 ("SizeScale T4 / S4", Vector) = (1.0, 6.2, 0, 0)

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
                float4 _StartLocationOffset;
                float _ApplySpawnUnitScale;
                float _SpawnUnitScale;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _UseRadialVelocity;
                float4 _RadialSpeed;
                float4 _Acceleration;
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

            float3 FlashSpawnOffset(float pSeed)
            {
                float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                posUe += _StartLocationOffset.xyz;
                return L2Fx_UeVectorToUnity(posUe) * FlashUnitScale();
            }

            float3 FlashRandomUnitDirection(float pSeed)
            {
                // Direction for "explosion-like" velocity must not inherit StartLocationOffset bias.
                float3 dirUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    float2(1.0, 1.0),
                    pSeed,
                    _StartTime);
                float3 dir = L2Fx_UeVectorToUnity(dirUe);
                float lenDir = length(dir);
                return lenDir > 1e-5 ? (dir / lenDir) : float3(0, 1, 0);
            }

            float FlashSizeScale(float ageNorm)
            {
                if (_UseSizeScale <= 0.5)
                {
                    return 1.0;
                }
                float t = saturate(ageNorm);
                float t0 = saturate(_SizeScale0.x);
                float t1 = saturate(_SizeScale1.x);
                float t2 = saturate(_SizeScale2.x);
                float t3 = saturate(_SizeScale3.x);
                float t4 = saturate(_SizeScale4.x);
                float s0 = _SizeScale0.y;
                float s1 = _SizeScale1.y;
                float s2 = _SizeScale2.y;
                float s3 = _SizeScale3.y;
                float s4 = _SizeScale4.y;

                if (t <= t1)
                {
                    return lerp(s0, s1, saturate((t - t0) / max(1e-5, t1 - t0)));
                }
                if (t <= t2)
                {
                    return lerp(s1, s2, saturate((t - t1) / max(1e-5, t2 - t1)));
                }
                if (t <= t3)
                {
                    return lerp(s2, s3, saturate((t - t2) / max(1e-5, t3 - t2)));
                }
                return lerp(s3, s4, saturate((t - t3) / max(1e-5, t4 - t3)));
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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float ageNorm = saturate(age / max(lifetime, 1e-4));
                OUT.particleSeed = pSeed;

                float sizeMul = FlashSizeScale(ageNorm);
                float3 baseSize = L2Fx_StartSize(
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime) * sizeMul;
                if (_ApplyUuToStartSize > 0.5)
                {
                    // SpriteEmitter0 StartSizeRange is authored in UE UU; convert to meters.
                    baseSize *= _SpawnUnitScale;
                }
                float3 quadOS = _UseMeshQuadBounds > 0.5
                    ? IN.positionOS.xyz
                    : IN.positionOS.xyz * baseSize;

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    L2Fx_ApplyMeshScalarSpin(quadOS, IN.normalOS, true, angle);
                }

                float3 spawnOfs = FlashSpawnOffset(pSeed);
                if (_UseRadialVelocity > 0.5)
                {
                    float3 dir = FlashRandomUnitDirection(pSeed + 17.0);
                    float radialSpeed = L2Fx_RandomRange(_RadialSpeed.xy, pSeed, _StartTime, 103.0) * FlashUnitScale();
                    float3 vel = dir * radialSpeed;
                    float3 acc = L2Fx_UeVectorToUnity(_Acceleration.xyz) * FlashUnitScale();
                    spawnOfs += L2Fx_DisplacementLinearVelocityLoss(vel, acc, float3(0, 0, 0), age);
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

                OUT.tint = L2Fx_ColorScaleTwoKeys(
                    ageNorm,
                    _ColorScale0,
                    _ColorScale1,
                    _ColorScaleTime1);

                if (_bAlphaBlend > 0.5)
                {
                    OUT.tint.a = OUT.tint.r;
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
