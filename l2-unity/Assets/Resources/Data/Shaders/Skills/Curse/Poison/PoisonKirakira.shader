// UE bl_curse_poison_ta SpriteEmitter2 "kirakira": PTDS_Brighten, fx_m_t0000 4x4, cells 14..16.
Shader "L2/Effects/PoisonKirakira"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0000)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0.3, 0.3, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (0.4, 0.4, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.3

        _Opacity ("Opacity", Range(0, 2)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha (fx_m_t A=255)", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma (black = transparent)", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 4
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScale0 ("ColorScale[0]", Color) = (0.972549, 0.709804, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.646429
        _ColorScale1 ("ColorScale[1]", Color) = (0.972549, 0.709804, 1, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (0, 0, 0, 0)

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (0, 0, 0, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (50, 130, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (15, 15, 0, 0)
        _SphereRadiusUU ("SphereRadiusRange UU (Min,Max)", Vector) = (10, 10, 0, 0)
        _SpawnUnitScale ("Spawn/velocity UU→Unity (0.01)", Float) = 0.01
        _OwnerWorldPos ("Owner World Pos (set by ParticleGroup)", Vector) = (0, 0, 0, 0)

        _VelocityRangeX ("Velocity X UU (Min,Max)", Vector) = (-45, 45, 0, 0)
        _VelocityRangeY ("Velocity Y UU (Min,Max)", Vector) = (-45, 45, 0, 0)
        _VelocityRangeZ ("Velocity Z UU (Min,Max)", Vector) = (-45, 45, 0, 0)
        _Acceleration ("Acceleration UU (XYZ)", Vector) = (0, 0, 15, 0)

        _SizeRange ("Start Size UU (Min,Max)", Vector) = (2.25, 3.3, 0, 0)
        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0.028
        [Toggle] _UniformSize ("Uniform Size", Float) = 1

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _SpinsPerSecondRange ("Spins Per Second rev (Min,Max)", Vector) = (0, 0.1, 0, 0)
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)

        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4
        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 14
        _SubdivisionEnd ("Subdivision End", Float) = 16
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

        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Kirakira"
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
                uint _ColorScaleCount;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float4 _SphereRadiusUU;
                float _SpawnUnitScale;
                float4 _OwnerWorldPos;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float4 _VelocityRangeZ;
                float4 _Acceleration;
                float4 _SizeRange;
                float _BillboardScale;
                float _UniformSize;
                float _SpinParticles;
                float4 _SpinsPerSecondRange;
                float4 _StartSpinRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
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

            float3 KirakiraRandomOnSphereUU(float seed, float startTime, float radiusUU)
            {
                float u = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, 211.0);
                float v = L2Fx_RandomRange(float2(0.0, 1.0), seed, startTime, 223.0);
                float theta = 6.2831853 * u;
                float z = 1.0 - 2.0 * v;
                float r = sqrt(max(0.0, 1.0 - z * z));
                return float3(r * cos(theta), r * sin(theta), z) * radiusUU;
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

                float3 posUe = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarPitchDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                posUe += KirakiraRandomOnSphereUU(pSeed, _StartTime, _SphereRadiusUU.x);
                posUe += _StartLocationOffset.xyz;

                float motionComp = L2Fx_MotionCompensationForManualBillboardScale(_BillboardScale);
                float3 spawnLogical = L2Fx_UeVectorToUnity(posUe) * _SpawnUnitScale;
                float3 spawnOfs = spawnLogical * motionComp;

                // PTVD_StartPositionAndOwner: project random velocity onto spawn→owner (world space).
                float3 spawnWS = TransformObjectToWorld(spawnOfs);
                float3 dirWS = spawnWS - _OwnerWorldPos.xyz;
                float lenDir = length(dirWS);
                dirWS = lenDir > 1e-5 ? (dirWS / lenDir) : float3(0, 1, 0);
                float3 velUe = float3(
                    L2Fx_RandomRange(_VelocityRangeX.xy, pSeed, _StartTime, 101.0),
                    L2Fx_RandomRange(_VelocityRangeY.xy, pSeed, _StartTime, 103.0),
                    L2Fx_RandomRange(_VelocityRangeZ.xy, pSeed, _StartTime, 107.0));
                float3 vel = dirWS * length(velUe) * _SpawnUnitScale * motionComp;

                float3 acc = L2Fx_UeVectorToUnity(_Acceleration.xyz) * _SpawnUnitScale * motionComp;

                float3 baseSize = L2Fx_StartSize(
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _UniformSize > 0.5,
                    pSeed,
                    _StartTime);
                float3 quadOS = IN.positionOS.xyz * baseSize;

                if (_SpinParticles > 0.5)
                {
                    float startSpin = L2Fx_StartSpin(_StartSpinRange.xy, pSeed, _StartTime);
                    float sps = L2Fx_SpinsPerSecond(_SpinsPerSecondRange.xy, pSeed, _StartTime);
                    float angle = (startSpin + sps * age) * L2Fx_TwoPi;
                    float3 nrm = IN.normalOS;
                    L2Fx_ApplyMeshScalarSpin(quadOS, nrm, true, angle);
                }

                float3 disp = L2Fx_DisplacementLinearVelocityLoss(vel, acc, float3(0, 0, 0), age);
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

                if (_BlendBetweenSubdivisions > 0.5)
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
                OUT.tint = cs;

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

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                half3 rgb = mixed.rgb * (half3)IN.tint.rgb * (half)mask;
                half alpha = (half)saturate(mask * IN.tint.a * _Opacity * lifeAlpha);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}
