// e_u031_a MeshEmitter1 "MC": etc_spawn01, fx_m_t0053.
// Decompiled FS (RenderDoc): out = sample(t0, in_Texcoord0.xy) * textureFactor; blend One+One.
// Black texels * factor = 0 (no additive contribution) - no luma mask needed on RGB path.
// textureFactor = ColorScale * ColorMult (consts.textureFactor in D3D9FixedFunctionPS).
Shader "L2/Effects/TeleportCaMesh"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0053)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (30, 30, 0, 0)
        _Seed ("Seed", Float) = 0
        _Hold ("Hold (0 = off, L2SkillEffect)", Range(0, 1)) = 0
        _HoldSizeReference ("Hold Size Reference (loop ref after release)", Range(0, 1)) = 0.75

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.6
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 30
        _FadeOutPower ("Fade Out strength (>1 = faster drop)", Range(1, 4)) = 1

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.725
        _ColorScale1 ("ColorScale[1]", Color) = (0.717647, 0.717647, 0.717647, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 300
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 0

        _ColorMultMin ("ColorMult Min", Color) = (0.4, 0.5, 0.9, 1)
        _ColorMultMax ("ColorMult Max", Color) = (0.4, 0.5, 0.9, 1)
        _Opacity ("Opacity", Range(0, 2)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend (RenderDoc: One)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend (RenderDoc: One)", Float) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Luma mask (off for D3D9 FF path)", Float) = 0
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        _AlphaEdgeFeather ("Alpha edge feather", Range(0, 0.25)) = 0
        _RgbBoost ("RGB Boost", Range(0.25, 4)) = 1
        [Toggle] _UseAlphaTest ("D3D9 alpha test", Float) = 0
        _AlphaRef ("Alpha test reference", Range(0, 1)) = 0

        [Toggle] _SplitByLuma ("Split core vs plasma by luma", Float) = 0
        _LineLumMin ("Core (white line) luma min", Range(0, 1)) = 0.38
        _LineLumMax ("Core (white line) luma max", Range(0, 1)) = 0.72
        _SoftLumMin ("Soft fringe luma min", Range(0, 1)) = 0.04
        _SoftLumMax ("Soft fringe luma max", Range(0, 1)) = 0.38
        _SoftRgbBoost ("Soft fringe hue lift", Range(0, 2)) = 0.35
        _PlasmaRgbScale ("Plasma RGB scale (low luma only)", Range(0, 2)) = 1
        _PlasmaLumaMax ("Plasma luma max", Range(0.01, 1)) = 0.22
        _PlasmaLowLumaRgbGain ("Plasma tune (R=dim, B=trim)", Color) = (0.82, 1, 0.68, 0)

        _StartSize ("Start Size UE order (X,Y,Z from .uc)", Vector) = (0.6, 0.6, 0.6, 0)
        [Toggle] _ApplyUuToStartSize ("Also x UU->m on size (off for Unity FBX)", Float) = 0
        _L2FxEffectScale ("L2 Fx Effect Scale (runtime target)", Float) = 1
        _L2FxMeshScale ("L2 Fx Mesh Scale (per-effect tune)", Float) = 1
        [Toggle] _UniformSize ("Uniform Size", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 6
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.8
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.5
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.3
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.95
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.3
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 0.98
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1
        _SizeScaleTime4 ("SizeScale Time[4]", Range(0, 1)) = 0.99
        _SizeScaleVal4 ("SizeScale Value[4]", Float) = 0.7
        _SizeScaleTime5 ("SizeScale Time[5]", Range(0, 1)) = 1
        _SizeScaleVal5 ("SizeScale Value[5]", Float) = 0.2

        _StartLocationOffset ("StartLocationOffset UE (X,Y,Z)", Vector) = (0, 0, 7, 0)
        _PolarAzimuthDeg ("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarPitchDeg ("Polar Pitch from +Z Deg (Min,Max)", Vector) = (60, 120, 0, 0)
        _PolarRadius ("Polar Radius UU (Min,Max)", Vector) = (10, 10, 0, 0)
        _UcStartLocationOffsetScale ("UC StartLocationOffset Scale", Float) = 1
        _UcPolarRadiusScale ("UC PolarRadius Scale", Float) = 1
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
            Name "TeleportCaMesh"
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
            #include "../Common/L2FxMeshAutoScale.hlsl"
            #include "../Common/L2FxUcToUnityConvert.hlsl"
            #include "../Common/L2FxMeshBrightenD3d9.hlsl"
            #include "../Common/L2FxPlasmaParticleBlend.hlsl"

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
                float _RgbBoost;
                float _UseAlphaTest;
                float _AlphaRef;
                float _SplitByLuma;
                float _LineLumMin;
                float _LineLumMax;
                float _SoftLumMin;
                float _SoftLumMax;
                float _SoftRgbBoost;
                float _PlasmaRgbScale;
                float _PlasmaLumaMax;
                float4 _PlasmaLowLumaRgbGain;
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
                float _SizeScaleTime5;
                float _SizeScaleVal5;
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarPitchDeg;
                float4 _PolarRadius;
                float _UcStartLocationOffsetScale;
                float _UcPolarRadiusScale;
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

            L2Fx_UcToUnityMeshConvertData TeleportCaMeshConvertData()
            {
                L2Fx_UcToUnityMeshConvertData data;
                data.applyUuToStartSize = _ApplyUuToStartSize;
                data.spawnUnitScale = _SpawnUnitScale;
                data.effectScale = _L2FxEffectScale;
                data.meshScale = _L2FxMeshScale;
                data.meshSpinDirection = 1.0;
                return data;
            }

            L2Fx_UcToUnitySpriteSpawnData TeleportCaMeshSpawnData()
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

            float TeleportCaMesh_SizeScale(float ageNorm)
            {
                if (_UseSizeScale < 0.5)
                {
                    return 1.0;
                }

                float stimes[8];
                float3 svals[8];
                [unroll]
                for (uint si = 0; si < 8; si++)
                {
                    stimes[si] = 999.0;
                    svals[si] = float3(1.0, 1.0, 1.0);
                }
                stimes[0] = _SizeScaleTime0;
                svals[0] = float3(_SizeScaleVal0, _SizeScaleVal0, _SizeScaleVal0);
                stimes[1] = _SizeScaleTime1;
                svals[1] = float3(_SizeScaleVal1, _SizeScaleVal1, _SizeScaleVal1);
                stimes[2] = _SizeScaleTime2;
                svals[2] = float3(_SizeScaleVal2, _SizeScaleVal2, _SizeScaleVal2);
                stimes[3] = _SizeScaleTime3;
                svals[3] = float3(_SizeScaleVal3, _SizeScaleVal3, _SizeScaleVal3);
                stimes[4] = _SizeScaleTime4;
                svals[4] = float3(_SizeScaleVal4, _SizeScaleVal4, _SizeScaleVal4);
                stimes[5] = _SizeScaleTime5;
                svals[5] = float3(_SizeScaleVal5, _SizeScaleVal5, _SizeScaleVal5);

                float3 ss = L2Fx_SampleSizeScale(
                    ageNorm,
                    _SizeScaleParam,
                    _SizeScaleRepeats,
                    _SizeScaleCount,
                    stimes,
                    svals,
                    (_UseRegularSizeScale > 0.5));

                return ss.x;
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
                float loopAgeNorm = L2Fx_HoldLoopAgeNorm(elapsed, lifetime, _Hold);
                float sizeAgeNorm = L2Fx_HoldSizeAgeNorm(elapsed, lifetime, _Hold, _HoldSizeReference);

                OUT.ageNorm = loopAgeNorm;

                float3 posOS = IN.positionOS.xyz;
                L2Fx_UcToUnityMeshConvertData convertData = TeleportCaMeshConvertData();
                float3 startSizeUe = L2Fx_UcToUnityApplyScale3(_StartSize.xyz, _UcStartSizeScale);
                float3 startSizeUnity = L2Fx_UcToUnityMeshSize(startSizeUe, convertData);
                float sizeScale = TeleportCaMesh_SizeScale(sizeAgeNorm);
                float3 spawnOfs = L2Fx_UcToUnitySpriteSpawnOffset(
                    TeleportCaMeshSpawnData(),
                    _Seed,
                    _StartTime);

                posOS *= startSizeUnity * sizeScale;
                posOS += spawnOfs;

                posOS.y += _MeshYOffset;

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

                // textureFactor: ColorScale curve * ColorMultiplierRange (D3D9 consts.textureFactor).
                half4 factor = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, _ColorScaleTime1, _ColorScale1,
                    _ColorScaleTime2, _ColorScale2,
                    _bAlphaBlend,
                    _ColorMultMin.rgb, _ColorMultMax.rgb,
                    _Opacity, _EmitterAlpha);

                // FF_FS: _123 = sample(t0, uv0) * textureFactor
                half4 outColor = texColor * factor;

                if (_UseAlphaTest > 0.5 && outColor.a < (half)_AlphaRef)
                {
                    discard;
                }

                half3 rgb = outColor.rgb;

                // RenderDoc tune: dim + mild B trim on low-luma plasma (white lines at high tex luma unchanged).
                float texLuma = dot((float3)texColor.rgb, float3(0.2126, 0.7152, 0.0722));
                float plasmaW = 1.0 - smoothstep(_PlasmaLumaMax * 0.5, _PlasmaLumaMax, texLuma);
                half plasmaMask = (half)saturate(plasmaW);
                half plasmaDim = (half)lerp(1.0, _PlasmaLowLumaRgbGain.r, plasmaMask);
                half blueScale = (half)lerp(1.0, _PlasmaLowLumaRgbGain.b, plasmaMask);
                rgb *= plasmaDim;
                rgb.b *= blueScale;

                if (_SplitByLuma > 0.5)
                {
                    float luma = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                    float coreW = smoothstep(_LineLumMin, _LineLumMax, luma);
                    rgb = lerp(outColor.rgb, texColor.rgb, (half)coreW);

                    float softW = L2Fx_MeshBrighten_SoftTailWeight(
                        texColor.rgb, _SoftLumMin, _SoftLumMax, _LineLumMin, _LineLumMax);
                    half3 hueTint = L2Fx_MeshBrighten_TexHueTint(texColor.rgb);
                    rgb += hueTint * factor.rgb * (half)softW * (half)_SoftRgbBoost;

                    rgb = L2Fx_PlasmaParticle_ApplyLowLumaRgbScale(
                        rgb, texColor.rgb, _PlasmaRgbScale, _PlasmaLumaMax);
                }

                if (_AlphaFromLuma > 0.5)
                {
                    float vis = L2Fx_MeshFrag_SampleTextureAlpha(
                        texColor, 1.0, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                    vis = L2Fx_MeshFrag_AlphaFeather(vis, _AlphaEdgeFeather);
                    rgb *= (half)vis;
                }

                rgb *= (half)_RgbBoost * (half)IN.lifeAlpha;
                return half4(saturate(rgb), 1.0);
            }

            ENDHLSL
        }
    }
}
