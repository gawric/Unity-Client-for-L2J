// UE bl_curse_poison_ta SpriteEmitter1 "PoisonClaud": PTDS_AlphaBlend, fx_m_t0089 2x2 flipbook.
// Fragment matches L2 D3D9 PS: lerp(tex0@uvA, tex1@uvB, COLOR.a); rgb=COLOR.rgb*mixed.rgb; a=mixed.a.
Shader "L2/Effects/PoisonPoisonCloud"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0089)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0.1, 0.1, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (1.2, 2, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.87

        _Opacity ("Opacity", Range(0, 2)) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        _AlphaPower ("Luma alpha power", Range(0.25, 4)) = 1.8
        _AlphaStrength ("Luma alpha strength", Range(0, 2)) = 0.55
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 0

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        _ColorScaleCount ("ColorScale Count", Int) = 5
        _ColorScale0 ("ColorScale[0]", Color) = (0.576, 0.290, 0.541, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.303571
        _ColorScale1 ("ColorScale[1]", Color) = (0.490, 0.329, 0.494, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 0.725
        _ColorScale2 ("ColorScale[2]", Color) = (0.153, 0.145, 0.212, 0.392)
        _ColorScaleTime3 ("ColorScale Time[3]", Range(0, 1)) = 0.917857
        _ColorScale3 ("ColorScale[3]", Color) = (0.094, 0.114, 0.161, 0.196)
        _ColorScaleTime4 ("ColorScale Time[4]", Range(0, 1)) = 1
        _ColorScale4 ("ColorScale[4]", Color) = (0.114, 0.063, 0.212, 1)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, -5, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (60, 120, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (10, 10, 0, 0)
        [Toggle] _ApplyUuToStartSize ("Legacy: size × UU scale instead of transform scale", Float) = 0
        _SpawnUnitScale ("Spawn/velocity UU→Unity (0.01)", Float) = 0.01

        _VelocityRangeX ("Velocity X UU (Min,Max)", Vector) = (30, 60, 0, 0)
        _VelocityRangeY ("Velocity Y UU (Min,Max)", Vector) = (30, 60, 0, 0)
        _HorizontalSpreadBoost ("Horizontal spread boost", Range(0.5, 3)) = 1.08
        _DownwardVelocityScale ("Initial sink from Y range", Range(0, 1)) = 0.42
        _Acceleration ("Acceleration UU (XYZ)", Vector) = (0, 0, -30, 0)
        _VelocityLoss ("Velocity loss UU (Min,Max)", Vector) = (1.5, 1.5, 0, 0)
        _OwnerWorldPos ("Owner World Pos", Vector) = (0, 0, 0, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (12, 16, 0, 0)
        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0
        [Toggle] _UniformSize ("Uniform Size", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.08
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1.1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.18
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.25
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.5
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.35
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1.4

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0.08, 0.12, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)

        _TextureUSubdivisions ("Texture U Submotion", Float) = 2
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 2
        _SubdivisionStart ("Subdivision Start", Float) = 0
        _SubdivisionEnd ("Subdivision End", Float) = 1
        [Toggle] _UsePoisonTopRowAtlas ("Legacy: force top row cells", Float) = 0
        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "PoisonClaud"
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
            #include "../../Common/L2FxMeshEmitterVertex.hlsl"
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
                float _Opacity;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaPower;
                float _AlphaStrength;
                float _IgnoreMainTexAlpha;
                float _bAlphaBlend;
                uint _ColorScaleCount;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                float _ColorScaleTime3;
                float4 _ColorScale3;
                float _ColorScaleTime4;
                float4 _ColorScale4;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _ApplyUuToStartSize;
                float _SpawnUnitScale;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float _HorizontalSpreadBoost;
                float _DownwardVelocityScale;
                float4 _Acceleration;
                float4 _VelocityLoss;
                float4 _OwnerWorldPos;
                float4 _SizeRange;
                float _BillboardScale;
                float _UniformSize;
                float _UseSizeScale;
                float _UseRegularSizeScale;
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
                float _UsePoisonTopRowAtlas;
                float _BlendBetweenSubdivisions;
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

            int L2PoisonCloudFrameToAtlasCell(int frameIndex, int uSub, int vSub)
            {
                if (_UsePoisonTopRowAtlas > 0.5 && uSub == 2 && vSub == 2)
                {
                    // Debug fallback only. RenderDoc for SpriteEmitter1 shows the real effect blends left-column cells.
                    return (frameIndex & 1) == 0 ? 0 : 2;
                }

                return frameIndex;
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

                // Spawn_StartLocationShape_Polar (CSV row 19): UE axes, pitch from +Z
                float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                posUe += _StartLocationOffset.xyz;

                float motionComp = L2Fx_MotionCompensationForManualBillboardScale(_BillboardScale);

                float3 spawnLogical = L2Fx_UeVectorToUnity(posUe) * _SpawnUnitScale;
                float3 vel = L2Fx_VelocityFogSpreadHorizontal(
                    spawnLogical,
                    _VelocityRangeX.xy,
                    _VelocityRangeY.xy,
                    _DownwardVelocityScale,
                    _HorizontalSpreadBoost,
                    _PolarAzimuthDeg.xy,
                    pSeed,
                    _StartTime) * _SpawnUnitScale;

                float3 accUe = _Acceleration.xyz;
                float3 acc = float3(0.0, L2Fx_UeVectorToUnity(accUe).y, 0.0) * _SpawnUnitScale;
                float velLossUe = L2Fx_RandomRange(_VelocityLoss.xy, pSeed, _StartTime, 197.0);
                float horizLoss = velLossUe * _SpawnUnitScale;

                float3 spawnOfs = spawnLogical * motionComp;
                vel *= motionComp;
                acc *= motionComp;
                horizLoss *= motionComp;

                float3 baseSize = L2Fx_StartSize(
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime);
                if (_ApplyUuToStartSize > 0.5)
                {
                    baseSize *= _SpawnUnitScale;
                }

                float stimes[8];
                float3 svals[8];
                [unroll]
                for (uint si = 0; si < 8; si++)
                {
                    stimes[si] = 999.0;
                    svals[si] = float3(1, 1, 1);
                }
                stimes[0] = _SizeScaleTime0;
                svals[0] = float3(_SizeScaleVal0, _SizeScaleVal0, _SizeScaleVal0);
                stimes[1] = _SizeScaleTime1;
                svals[1] = float3(_SizeScaleVal1, _SizeScaleVal1, _SizeScaleVal1);
                stimes[2] = _SizeScaleTime2;
                svals[2] = float3(_SizeScaleVal2, _SizeScaleVal2, _SizeScaleVal2);
                stimes[3] = _SizeScaleTime3;
                svals[3] = float3(_SizeScaleVal3, _SizeScaleVal3, _SizeScaleVal3);

                float sizeMul = L2Fx_SampleSizeScale(
                    ageNorm,
                    0.0,
                    1.0,
                    4,
                    stimes,
                    svals,
                    _UseRegularSizeScale > 0.5);
                sizeMul = lerp(1.0, sizeMul, step(0.5, _UseSizeScale));

                float3 quadOS = IN.positionOS.xyz * baseSize * sizeMul;

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    float3 nrm = IN.normalOS;
                    L2Fx_ApplyMeshScalarSpin(quadOS, nrm, true, angle);
                }

                spawnOfs += L2Fx_DisplacementFogFall(vel, acc, horizLoss, age);

                float3 centerWS = TransformObjectToWorld(spawnOfs);
                float3 posWS = L2Fx_CameraBillboardPositionWS(
                    centerWS,
                    quadOS,
                    _BillboardScale,
                    _ApplyUuToStartSize);

                OUT.positionHCS = TransformWorldToHClip(posWS);

                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                float fBlend = 0.0;
                float2 uvA;
                float2 uvB;

                if (_BlendBetweenSubdivisions > 0.5)
                {
                    int fa;
                    int fb;
                    L2Fx_FlipbookBlendFrames(ageNorm, s0, s1, fa, fb, fBlend);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, L2PoisonCloudFrameToAtlasCell(fa, uSub, vSub), uSub, vSub);
                    uvB = L2Fx_FlipbookAtlasUV(quadUv, L2PoisonCloudFrameToAtlasCell(fb, uSub, vSub), uSub, vSub);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, s0, s1);
                    uvA = L2Fx_FlipbookAtlasUV(quadUv, L2PoisonCloudFrameToAtlasCell(fi, uSub, vSub), uSub, vSub);
                    uvB = uvA;
                }

                OUT.uvAtlasA = uvA;
                OUT.uvAtlasB = uvB;

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
                float4 cs = L2Fx_SampleColorScale(
                    ageNorm,
                    0.0,
                    _ColorScaleCount,
                    ctimes,
                    ccols,
                    false);
                OUT.tint = cs;
                OUT.flipbookBlend = fBlend;

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

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    mixed, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                mask = L2Fx_MeshFrag_ApplyAlphaPowerStrength(
                    mask, _AlphaFromLuma, _AlphaPower, _AlphaStrength);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                half3 rgb = mixed.rgb * (half3)IN.tint.rgb;
                half alpha = (half)(mask * mixed.a * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), saturate(alpha));
            }

            ENDHLSL
        }
    }
}
