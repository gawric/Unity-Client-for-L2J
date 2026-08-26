// UE m_u003_b SpriteEmitter2: PTDS_Brighten, fx_m_t0000 4x4, random subdiv 14..16.
// PTCS_Independent trail sparks (home projectile tail); not attached to SpriteEmitter5 yet.
Shader "L2/Effects/VampiricTouchSpark"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0000)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (0.333, 0.333, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.155

        _Opacity ("Opacity", Range(0, 2)) = 1
        _TextureDilateTexels ("Texture Dilate Texels", Range(0, 24)) = 0
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        _AlphaBoost ("Alpha Boost", Range(0, 16)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha from RGB luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1
        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (90, 90, 0, 0)
        _PolarRadius ("Polar Radius (Min,Max)", Vector) = (0, 0, 0, 0)
        [Toggle] _ApplySpawnUnitScale ("Apply UE UU→Unity (0.01)", Float) = 0
        _SpawnUnitScale ("UE UU to Unity meters", Float) = 0.01
        _OwnerWorldPos ("Owner World Pos (ParticleGroup)", Vector) = (0, 0, 0, 0)

        [Toggle] _UseTrailVelocity ("Use provider trail velocity", Float) = 1
        [Toggle] _UseTrailPathFade ("Fade alpha along trail (MPB _TrailPathFadeT)", Float) = 0
        _TrailPathFadeHead ("Trail fade at head", Range(0, 1)) = 1
        _TrailPathFadeTail ("Trail fade at tail", Range(0, 1)) = 0
        _TrailPathFadeT ("Trail position 0=head 1=tail (per renderer)", Range(0, 1)) = 0
        _VelocityRangeX ("Radial Velocity X (Min,Max)", Vector) = (0, 0, 0, 0)
        _VelocityRangeY ("Radial Velocity Y (Min,Max)", Vector) = (0, 0, 0, 0)
        _VelocityRangeZ ("Radial Velocity Z (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeX ("Trail Velocity X (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeY ("Trail Velocity Y (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeZ ("Trail Velocity Z (Min,Max)", Vector) = (0, 0, 0, 0)
        _Acceleration ("Acceleration (XYZ)", Vector) = (0, 0, 0, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (2, 2, 0, 0)
        _BillboardScale ("Manual Billboard Scale (0 = use quad transform scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScale0 ("SizeScale T0 / S0", Vector) = (0, 1.5, 0, 0)
        _SizeScale1 ("SizeScale T1 / S1", Vector) = (0.07, 1.2, 0, 0)
        _SizeScale2 ("SizeScale T2 / S2", Vector) = (1, 1.2, 0, 0)

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 14
        _SubdivisionEnd ("Subdivision End", Float) = 16
        [Toggle] _UseRandomSubdivision ("Random Subdiv 14..16 (UE UseRandomSubdivision)", Float) = 1
        [Toggle] _BlendBetweenSubdivisions ("Blend Subdiv Over Age", Float) = 0
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
            Name "VampiricSpark"
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
            #include "../../Common/L2FxMeshFragment.hlsl"

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
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _ApplySpawnUnitScale;
                float _SpawnUnitScale;
                float4 _OwnerWorldPos;
                float _UseTrailVelocity;
                float _UseTrailPathFade;
                float _TrailPathFadeHead;
                float _TrailPathFadeTail;
                float _TrailPathFadeT;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float4 _VelocityRangeZ;
                float4 _StartVelocityRangeX;
                float4 _StartVelocityRangeY;
                float4 _StartVelocityRangeZ;
                float4 _Acceleration;
                float4 _SizeRange;
                float _BillboardScale;
                float _UniformSize;
                float _UseSizeScale;
                float4 _SizeScale0;
                float4 _SizeScale1;
                float4 _SizeScale2;
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _UseRandomSubdivision;
                float _BlendBetweenSubdivisions;
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
            };

            float SparkMotionUnitScale()
            {
                return _ApplySpawnUnitScale > 0.5 ? _SpawnUnitScale : 1.0;
            }

            float3 SparkSpawnOffset(float pSeed)
            {
                if (_ApplySpawnUnitScale > 0.5)
                {
                    float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                        _PolarAzimuthDeg.xy,
                        _PolarPitchDeg.xy,
                        _PolarRadius.xy,
                        pSeed,
                        _StartTime);
                    posUe += _StartLocationOffset.xyz;
                    return L2Fx_UeVectorToUnity(posUe) * SparkMotionUnitScale();
                }

                float3 pos = L2Fx_SpawnOffsetPolarYDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                return pos + _StartLocationOffset.xyz;
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

                float unitScale = SparkMotionUnitScale();
                float motionComp = _ApplySpawnUnitScale > 0.5
                    ? L2Fx_MotionCompensationForManualBillboardScale(_BillboardScale)
                    : 1.0;

                float sizeMul = L2Fx_SizeScale(
                    ageNorm,
                    _UseSizeScale,
                    _SizeScale0,
                    _SizeScale1,
                    _SizeScale2);

                float3 baseSize;
                if (_ApplySpawnUnitScale > 0.5)
                {
                    baseSize = L2Fx_StartSize(
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _SizeRange.xy,
                        _UniformSize > 0.5,
                        pSeed,
                        _StartTime) * sizeMul;
                }
                else
                {
                    // Quad mesh is already in Unity units; only apply SizeScale curve.
                    baseSize = float3(1, 1, 1) * sizeMul;
                }

                float3 quadOS = IN.positionOS.xyz * baseSize;

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    L2Fx_ApplyMeshScalarSpin(quadOS, IN.normalOS, true, angle);
                }

                float3 centerWS;
                if (_UseTrailVelocity > 0.5)
                {
                    // HomeProjectileTrailVelocityProvider moves each quad along path history.
                    // Do not add spawn polar offset or velocity integration here (gizmo stays at transform, sprite shears away).
                    centerWS = TransformObjectToWorld(float3(0, 0, 0));
                }
                else
                {
                    float3 spawnLogical = SparkSpawnOffset(pSeed);
                    float3 spawnOfs = spawnLogical * motionComp;

                    float speed = L2Fx_RandomRange(_VelocityRangeX.xy, pSeed, _StartTime, 101.0) * unitScale;
                    float2 hDir = spawnLogical.xz;
                    float hLen = length(hDir);
                    hDir = hLen > 1e-5 ? (hDir / hLen) : float2(1, 0);
                    float3 vel = float3(hDir.x, 0, hDir.y) * speed * motionComp;

                    float3 acc = _ApplySpawnUnitScale > 0.5
                        ? L2Fx_UeVectorToUnity(_Acceleration.xyz) * unitScale
                        : _Acceleration.xyz;

                    spawnOfs += L2Fx_DisplacementLinearVelocityLoss(vel, acc, float3(0, 0, 0), age);
                    centerWS = TransformObjectToWorld(spawnOfs);
                }
                float3 posWS = L2Fx_CameraBillboardPositionWS(
                    centerWS,
                    quadOS,
                    _BillboardScale,
                    0.0);

                OUT.positionHCS = TransformWorldToHClip(posWS);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int subStart = (int)_SubdivisionStart;
                int subEnd = (int)_SubdivisionEnd;
                float fBlend = 0.0;
                float2 uvA;
                float2 uvB;

                if (_UseRandomSubdivision > 0.5)
                {
                    int cell = L2Fx_FlipbookSubDivisionRandomFrame(
                        pSeed, _StartTime, subStart, subEnd, 191.0);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, cell, uSub, vSub);
                    uvB = uvA;
                }
                else if (_BlendBetweenSubdivisions > 0.5)
                {
                    int fa;
                    int fb;
                    L2Fx_FlipbookBlendFrames(ageNorm, subStart, subEnd, fa, fb, fBlend);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fa, uSub, vSub);
                    uvB = L2Fx_FlipbookAtlasUV(quadUv, fb, uSub, vSub);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, subStart, subEnd);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fi, uSub, vSub);
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

            half4 SparkDilatedSample(float2 uv)
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

                half4 texA = SparkDilatedSample(IN.uvAtlasA);
                half4 texB = SparkDilatedSample(IN.uvAtlasB);
                half4 mixed = lerp(texA, texB, (half)IN.flipbookBlend);

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    mixed, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                float pathFadeMul = 1.0;
                if (_UseTrailPathFade > 0.5)
                {
                    float pathT = saturate(_TrailPathFadeT);
                    pathFadeMul = lerp(_TrailPathFadeHead, _TrailPathFadeTail, pathT);
                }

                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)(mask * _RgbBoost);
                half baseAlpha = (half)saturate(mask * _AlphaBoost * IN.tint.a * _Opacity * lifeAlpha);
                half alpha = (half)(baseAlpha * pathFadeMul);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}
