// m_u004_b MeshEmitter0 "supportenchant00": fx_m_t0005, spin, size scale, Z velocity + accel.
// m_u004_a MeshEmitter9 same mesh on ground: spin + size scale, no velocity/accel (params differ).
// m_u004_b MeshEmitter3 shares this shader with different material params.
//
// Axis: UE (X,Y,Z) -> Unity (X,Z,Y). Unity Y is up; UE Z maps to Unity Y.
// supportenchant00 FBX is already authored in Unity meters — keep _ApplyUuToStartSize off.
// Motion/accel/velocity from .uc stay in raw UE UU; shader converts axis + * _SpawnUnitScale.
Shader "L2/Effects/MightCaMesh"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0005)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (2, 2, 0, 0)
        _Seed ("Seed", Float) = 0
        _Hold ("Hold (0 = off, L2SkillEffect)", Range(0, 1)) = 0
        _HoldSizeReference ("Hold Size Reference (loop ref after release)", Range(0, 1)) = 0.413

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.02
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.78
        _FadeOutPower ("Fade Out strength (>1 = faster drop)", Range(1, 4)) = 1

        _ColorScale0 ("ColorScale[0]", Color) = (0.858824, 0.858824, 0.858824, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.5
        _ColorScale1 ("ColorScale[1]", Color) = (0.643137, 0.643137, 0.643137, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (0.858824, 0.858824, 0.858824, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 8
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 0

        _ColorMultMin ("ColorMult Min", Color) = (0.7, 0.7, 0.8, 1)
        _ColorMultMax ("ColorMult Max", Color) = (0.7, 0.7, 0.8, 1)
        _Opacity ("Opacity", Range(0, 2)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha (fx_m_t A=255)", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma (black = transparent)", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        _AlphaEdgeFeather ("Alpha edge feather", Range(0, 0.25)) = 0.02

        [Header(D3D9 Brighten FS supportenchant00)]
        [Toggle] _UseD3d9BrightenFs ("D3D9 FF: tex * textureFactor (no luma trim)", Float) = 0
        _RgbBoost ("RGB Boost (heads + base)", Range(0.25, 4)) = 1
        _PlasmaRgbScale ("Plasma RGB Scale (low luma only)", Range(0, 2)) = 1
        _PlasmaLumaMax ("Plasma Luma Max", Range(0.01, 1)) = 0.35
        _AlphaBoost ("Alpha Boost (SrcAlpha One)", Range(0.25, 3)) = 1
        _TailLift ("Tail RGB lift (additive, soft band only)", Range(0, 2)) = 0.35
        [Toggle] _D3d9FadeAlphaWithLife ("D3D9: multiply alpha by lifeAlpha", Float) = 0
        _SoftLumMin ("Tail band luma min", Range(0, 1)) = 0
        _SoftLumMax ("Tail band luma max", Range(0, 1)) = 0.45
        _LineLumMin ("Head luma min (excluded from tail lift)", Range(0, 1)) = 0.35
        _LineLumMax ("Head luma max", Range(0, 1)) = 1

        _StartSize ("Start Size UE order (X,Y,Z from .uc)", Vector) = (0.2, 0.2, 0.25, 0)
        [Toggle] _ApplyUuToStartSize ("Also x UU->m on size (off for Unity FBX)", Float) = 0
        _L2FxEffectScale ("L2 Fx Effect Scale (runtime target)", Float) = 1
        _L2FxMeshScale ("L2 Fx Mesh Scale (per-effect tune)", Float) = 1
        [Toggle] _UniformSize ("Uniform Size", Float) = 0
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.4
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.25
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.68
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.8
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 2.1
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 2.1

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 1, 0, 0)
        [Toggle] _UseStartSpin3Axis ("Start Spin XYZ (UE StartSpinRange)", Float) = 0
        _StartSpinRangeX ("Start Spin X rev (Min,Max)", Vector) = (0, 1, 0, 0)
        _StartSpinRangeY ("Start Spin Y rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRangeZ ("Start Spin Z rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpinsPerSecond ("Spins Per Second rev/s", Float) = 1.5
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 1
        _L2FxMeshSpinDirection ("L2 Fx Mesh Spin Direction (UC->Unity)", Float) = 1

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, 0, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001

        _Acceleration ("Acceleration (XYZ UE UU/s²)", Vector) = (0, 0, -11, 0)
        _StartVelocityRangeZ ("StartVelocity Z UU/s (Min,Max)", Vector) = (-23, -23, 0, 0)
        _SpawnUnitScale ("UE UU -> Unity meters", Float) = 0.01
        _UcStartSizeScale ("UC StartSize Scale", Float) = 1
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale", Float) = 1
        _UcVelocityScale ("UC StartVelocity Scale", Float) = 1
        _UcAccelerationScale ("UC Acceleration Scale", Float) = 1

        [Header(Scene Debug Preview)]
        [Toggle] _DebugMeshPreview ("Debug Mesh Preview (_StartTime=0)", Float) = 0
        [Toggle] _DebugMeshPreviewLoop ("Debug Preview Loop", Float) = 0
        _DebugMeshPreviewAge ("Debug Preview Age (sec, pause)", Range(0, 4)) = 0
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
            Name "MightCaMesh"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../Common/L2FxMeshDebug.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxMeshLifetimeAlpha.hlsl"
            #include "../Common/L2FxMeshBrightenD3d9.hlsl"
            #include "../Common/L2FxHold.hlsl"
            #include "../Common/L2FxMeshAutoScale.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _StartTime;
                float _HasLifetime;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _Seed;
                float _Hold;
                float _HoldSizeReference;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float _FadeOutPower;
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float _bAlphaBlend;
                float4 _ColorMultMin;
                float4 _ColorMultMax;
                float _Opacity;
                float _EmitterAlpha;
                float _SrcBlend;
                float _DstBlend;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaEdgeFeather;
                float _UseD3d9BrightenFs;
                float _RgbBoost;
                float _PlasmaRgbScale;
                float _PlasmaLumaMax;
                float _AlphaBoost;
                float _TailLift;
                float _D3d9FadeAlphaWithLife;
                float _SoftLumMin;
                float _SoftLumMax;
                float _LineLumMin;
                float _LineLumMax;
                float4 _StartSize;
                float _ApplyUuToStartSize;
                float _L2FxEffectScale;
                float _L2FxMeshScale;
                float _UniformSize;
                float _UseSizeScale;
                float _UseRegularSizeScale;
                uint _SizeScaleCount;
                float _SizeScaleRepeats;
                float _SizeScaleParam;
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
                float4 _StartSpinRange;
                float _UseStartSpin3Axis;
                float4 _StartSpinRangeX;
                float4 _StartSpinRangeY;
                float4 _StartSpinRangeZ;
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float _L2FxMeshSpinDirection;
                float4 _StartLocationOffset;
                float _MeshYOffset;
                float _ClipDepthBias;
                float4 _Acceleration;
                float4 _StartVelocityRangeZ;
                float _SpawnUnitScale;
                float _UcStartSizeScale;
                float _UcStartLocationOffsetScale;
                float _UcVelocityScale;
                float _UcAccelerationScale;
                float _DebugMeshPreview;
                float _DebugMeshPreviewLoop;
                float _DebugMeshPreviewAge;
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
                float2 uv : TEXCOORD0;
                float ageNorm : TEXCOORD1;
                float lifeAlpha : TEXCOORD2;
            };

            L2Fx_UcToUnityMeshConvertData MightCaMeshConvertData()
            {
                L2Fx_UcToUnityMeshConvertData data;
                data.applyUuToStartSize = _ApplyUuToStartSize;
                data.spawnUnitScale = _SpawnUnitScale;
                data.effectScale = _L2FxEffectScale;
                data.meshScale = _L2FxMeshScale;
                data.meshSpinDirection = _L2FxMeshSpinDirection;
                return data;
            }

            float3 MightCaMeshParticleMotion(float ageSeconds)
            {
                float velZUe = L2Fx_RandomRange(_StartVelocityRangeZ.xy, _Seed, _StartTime, 101.0);
                float3 velUe = L2Fx_UcToUnityApplyScale3(float3(0.0, 0.0, velZUe), _UcVelocityScale);
                float3 accUe = L2Fx_UcToUnityApplyScale3(_Acceleration.xyz, _UcAccelerationScale);
                float3 velUnity = L2Fx_UeVectorToUnity(velUe) * _SpawnUnitScale;
                float3 accUnity = L2Fx_UeVectorToUnity(accUe) * _SpawnUnitScale;
                return L2Fx_DisplacementConstantAccel(velUnity, accUnity, ageSeconds);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float delay, lifetime, elapsed, ageNormUnused;
                L2Fx_MeshDebug_ComputeTiming(
                    _DebugMeshPreview, _DebugMeshPreviewLoop, _DebugMeshPreviewAge,
                    _HasLifetime, _Time.y,
                    _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, elapsed, ageNormUnused);

                lifetime = max(lifetime, 1e-4);

                float motionAge = L2Fx_HoldMotionAgeStable(elapsed, lifetime, _Hold, _HoldSizeReference);
                float spinAge = L2Fx_HoldSpinAge(elapsed);
                float loopAgeNorm = L2Fx_HoldLoopAgeNorm(elapsed, lifetime, _Hold);
                float sizeAgeNorm = L2Fx_HoldSizeAgeNorm(elapsed, lifetime, _Hold, _HoldSizeReference);

                OUT.ageNorm = loopAgeNorm;

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;
                L2Fx_UcToUnityMeshConvertData convertData = MightCaMeshConvertData();
                float3 startSizeUe = L2Fx_UcToUnityApplyScale3(_StartSize.xyz, _UcStartSizeScale);
                float3 startSizeUnity = L2Fx_UcToUnityMeshSize(startSizeUe, convertData);
                float spinsPerSecondUnity = L2Fx_UcToUnityMeshSpinRate(_SpinsPerSecond, convertData);
                float3 startLocationOffsetUe = L2Fx_UcToUnityApplyScale3(
                    _StartLocationOffset.xyz, _UcStartLocationOffsetScale);
                float3 startLocationOffsetUnity = L2Fx_UcToUnityStartLocationOffset(
                    startLocationOffsetUe, convertData);

                if (_UseStartSpin3Axis >= 0.5)
                {
                    float sizeScale = L2Fx_MeshBuiltin_SampleSizeScaleScalar(
                        sizeAgeNorm,
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

                    float3 sizeMul = startSizeUnity * sizeScale;
                    float2 zeroSps = float2(0.0, 0.0);

                    // Spin + scale in mesh-local space, then translate (same as sprite billboard center).
                    // Offset-before-spin orbited vertices around object origin and shifted MeshEmitter3 sideways.
                    L2Fx_ApplyMeshParticleSpin(
                        posOS,
                        nrmOS,
                        _SpinParticles,
                        spinAge,
                        _StartSpinRangeX.xy,
                        _StartSpinRangeY.xy,
                        _StartSpinRangeZ.xy,
                        zeroSps,
                        zeroSps,
                        zeroSps,
                        _Seed,
                        _StartTime);
                    posOS *= sizeMul;
                    posOS += startLocationOffsetUnity;
                    posOS.y += _MeshYOffset;
                }
                else
                {
                    float startSpinRev = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);

                    L2Fx_MeshBuiltin_TransformVertexOS_SplitAgeUnityOffset(
                        posOS, nrmOS,
                        _SpinParticles,
                        startSpinRev,
                        spinsPerSecondUnity,
                        _SpinCCWorCW,
                        spinAge, sizeAgeNorm,
                        startSizeUnity,
                        _UseSizeScale, _UseRegularSizeScale,
                        _SizeScaleParam, _SizeScaleRepeats, _SizeScaleCount,
                        _SizeScaleTime0, _SizeScaleVal0,
                        _SizeScaleTime1, _SizeScaleVal1,
                        _SizeScaleTime2, _SizeScaleVal2,
                        _SizeScaleTime3, _SizeScaleVal3,
                        _SizeScaleTime4, _SizeScaleVal4,
                        startLocationOffsetUnity, _MeshYOffset, 0.0);
                }

                posOS += MightCaMeshParticleMotion(motionAge);

                OUT.positionHCS = L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);
                OUT.lifeAlpha = L2Fx_MeshLifetimeAlphaHold(
                    motionAge, elapsed, lifetime,
                    _Hold, _HasLifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                if (_FadeOutPower > 1.0001)
                {
                    OUT.lifeAlpha = pow(saturate(OUT.lifeAlpha), _FadeOutPower);
                }

                float2 uvMeshUnused;
                L2Fx_MeshBuiltin_ResolveUv(
                    IN.uv, IN.positionOS.xyz,
                    0.0, 0.5, 1.0, 0.0, float4(0.5, 0.5, 0, 0),
                    _MainTex_ST, 0.0, float4(0, 0, 1, 1),
                    OUT.uv, uvMeshUnused);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half4 factor = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, _ColorScaleTime1, _ColorScale1,
                    _ColorScaleTime2, _ColorScale2,
                    _bAlphaBlend,
                    _ColorMultMin.rgb, _ColorMultMax.rgb,
                    _Opacity, _EmitterAlpha);

                if (_UseD3d9BrightenFs >= 0.5)
                {
                    return L2Fx_MeshBrighten_D3d9TexFactor(
                        texColor, factor, (half)IN.lifeAlpha,
                        _TailLift,
                        _SoftLumMin, _SoftLumMax, _LineLumMin, _LineLumMax,
                        _RgbBoost, _PlasmaRgbScale, _PlasmaLumaMax,
                        _AlphaBoost, _IgnoreMainTexAlpha,
                        _D3d9FadeAlphaWithLife);
                }

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                mask = L2Fx_MeshFrag_AlphaFeather(mask, _AlphaEdgeFeather);

                half3 rgb = factor.rgb * texColor.rgb * (half)mask;
                half alpha = (half)saturate(factor.a * mask * IN.lifeAlpha);
                return half4(saturate(rgb), alpha);
            }

            ENDHLSL
        }
    }
}
