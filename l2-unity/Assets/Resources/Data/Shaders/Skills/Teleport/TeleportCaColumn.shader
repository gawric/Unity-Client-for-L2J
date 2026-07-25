// e_u031_a MeshEmitter2 "Column": etc_spawn00, fx_m_t0053.
// RenderDoc: out = sample(t0) * textureFactor; blend One+One; textureFactor ≈ ColorMult * ColorScale * Opacity.
Shader "L2/Effects/TeleportCaColumn"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0053)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (2, 2, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (28, 28, 0, 0)
        _Seed ("Seed", Float) = 0
        _Hold ("Hold (0 = off, L2SkillEffect)", Range(0, 1)) = 0
        _HoldSizeReference ("Hold Size Reference (loop ref after release)", Range(0, 1)) = 0.75

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 1.68
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 28
        _FadeOutPower ("Fade Out strength (>1 = faster drop)", Range(1, 4)) = 1

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.685714
        _ColorScale1 ("ColorScale[1]", Color) = (0.654902, 0.654902, 0.654902, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 300
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 0

        _ColorMultMin ("ColorMult Min", Color) = (0.2, 0.25, 0.25, 1)
        _ColorMultMax ("ColorMult Max", Color) = (0.2, 0.25, 0.25, 1)
        _Opacity ("Opacity", Range(0, 2)) = 0.39
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend (RenderDoc: One)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend (RenderDoc: One)", Float) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        _RgbBoost ("RGB Boost", Range(0.25, 4)) = 1
        [Toggle] _UseAlphaTest ("D3D9 alpha test", Float) = 0
        _AlphaRef ("Alpha test reference", Range(0, 1)) = 0

        _StartSize ("Start Size UE order (X,Y,Z from .uc)", Vector) = (0.8, 0.8, 0.6, 0)
        [Toggle] _ApplyUuToStartSize ("Also x UU->m on size (off for Unity FBX)", Float) = 0
        _L2FxEffectScale ("L2 Fx Effect Scale (runtime target)", Float) = 1
        _L2FxMeshScale ("L2 Fx Mesh Scale (per-effect tune)", Float) = 1
        [Toggle] _UniformSize ("Uniform Size", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 2
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.92
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 1
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 0.1
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 1
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 0.1
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 0.1
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 1
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 0.1

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _StartSpinRange ("Start Spin rev (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpinsPerSecond ("Spins Per Second rev/s", Float) = 0.05
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 1
        _L2FxMeshSpinDirection ("L2 Fx Mesh Spin Direction (UC->Unity)", Float) = 1

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, -6, 0)
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale", Float) = 1
        _SpawnUnitScale ("UE UU -> Unity meters", Float) = 0.01
        _UcStartSizeScale ("UC StartSize Scale", Float) = 1

        _MeshYOffset ("Lift above ground (m)", Float) = 0
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001

        [Header(Scene Debug Preview)]
        [Toggle] _DebugMeshPreview ("Debug Mesh Preview (_StartTime=0)", Float) = 0
        [Toggle] _DebugMeshPreviewLoop ("Debug Preview Loop", Float) = 0
        _DebugMeshPreviewAge ("Debug Preview Age (sec, pause)", Range(0, 32)) = 0
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
            Name "TeleportCaColumn"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../Common/L2FxMeshDebug.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxMeshLifetimeAlpha.hlsl"
            #include "../Common/L2FxHold.hlsl"
            #include "../Common/L2FxUcToUnityConvert.hlsl"

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
                float _IgnoreMainTexAlpha;
                float _RgbBoost;
                float _UseAlphaTest;
                float _AlphaRef;
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
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float _L2FxMeshSpinDirection;
                float4 _StartLocationOffset;
                float _UcStartLocationOffsetScale;
                float _SpawnUnitScale;
                float _UcStartSizeScale;
                float _MeshYOffset;
                float _ClipDepthBias;
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

            L2Fx_UcToUnityMeshConvertData TeleportCaColumnConvertData()
            {
                L2Fx_UcToUnityMeshConvertData data;
                data.applyUuToStartSize = _ApplyUuToStartSize;
                data.spawnUnitScale = _SpawnUnitScale;
                data.effectScale = _L2FxEffectScale;
                data.meshScale = _L2FxMeshScale;
                data.meshSpinDirection = _L2FxMeshSpinDirection;
                return data;
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
                L2Fx_UcToUnityMeshConvertData convertData = TeleportCaColumnConvertData();
                float3 startSizeUe = L2Fx_UcToUnityApplyScale3(_StartSize.xyz, _UcStartSizeScale);
                float3 startSizeUnity = L2Fx_UcToUnityMeshSize(startSizeUe, convertData);
                float spinsPerSecondUnity = L2Fx_UcToUnityMeshSpinRate(_SpinsPerSecond, convertData);
                float startSpinRev = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);
                float3 startLocationOffsetUe = L2Fx_UcToUnityApplyScale3(
                    _StartLocationOffset.xyz, _UcStartLocationOffsetScale);
                float3 startLocationOffsetUnity = L2Fx_UcToUnityStartLocationOffset(
                    startLocationOffsetUe, convertData);

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

                half4 outColor = texColor * factor;

                if (_UseAlphaTest > 0.5 && outColor.a < (half)_AlphaRef)
                {
                    discard;
                }

                if (_IgnoreMainTexAlpha > 0.5)
                {
                    outColor.rgb = texColor.rgb * factor.rgb;
                    outColor.a = factor.a;
                }

                half3 rgb = outColor.rgb * (half)_RgbBoost * (half)IN.lifeAlpha;

                // One+One additive: dim rgb by opacity * emitter (alpha channel ignored by blend).
                half emitterVis = (half)saturate(_Opacity * _EmitterAlpha);
                rgb *= emitterVis;

                half alpha = outColor.a * (half)IN.lifeAlpha;
                return half4(saturate(rgb), saturate(alpha));
            }

            ENDHLSL
        }
    }
}
