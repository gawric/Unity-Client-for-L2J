// wh_cure_poison_ta MeshEmitter0 "LightSplash": lightcone00 + fx_m_t0005 atlas UVs, alpha blend.
// Mesh is authored in Unity scale (_ApplyUuToStartSize off). Size curve matches UE SizeScale keys.
Shader "L2/Effects/CurePoisonLightSplash"
{
    Properties
    {
        [Header(Lifetime)]
        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (1, 1.07, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (1.2, 1.2, 0, 0)
        _Seed ("Seed", Float) = 0

        [Header(Fade)]
        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End Time sec", Float) = 0.084
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start Time sec", Float) = 0.12

        [Header(ColorScale)]
        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.55
        _ColorScale1 ("ColorScale[1]", Color) = (0.788, 0.788, 0.788, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 10
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorMultMin ("ColorMult Min (R,G,B)", Color) = (1, 0.85, 0.85, 1)
        _ColorMultMax ("ColorMult Max (R,G,B)", Color) = (1, 0.85, 0.85, 1)
        _Opacity ("Opacity", Range(0, 2)) = 0.61
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1

        [Header(Size mesh Unity scale)]
        _StartSize ("Start Size multiplier XYZ", Vector) = (1, 1, 1, 0)
        [Toggle] _UniformSize ("Uniform Size", Float) = 1
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1
        _SizeScaleParam ("SizeScale Param", Float) = 0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.1
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.12
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.01
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.16
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1

        [Header(Spin)]
        [Toggle] _SpinParticles ("Spin Particles", Float) = 1
        _StartSpinRange ("Start Spin X rev (Min,Max)", Vector) = (0, 1, 0, 0)
        _StartSpinRangeZ ("Start Spin Z rev (Min,Max)", Vector) = (0, 0.15, 0, 0)
        _SpinsPerSecondRangeX ("Spin Per Sec X (Min,Max)", Vector) = (-0.15, 0.15, 0, 0)
        _SpinsPerSecondRangeY ("Spin Per Sec Y (Min,Max)", Vector) = (-0.01, 0.01, 0, 0)
        _SpinsPerSecondRangeZ ("Spin Per Sec Z (Min,Max)", Vector) = (-0.01, 0.01, 0, 0)
        _SpinMinorAxesScale ("Minor axes Y/Z strength", Range(0, 1)) = 1
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0

        [Header(Spawn)]
        _StartLocationOffset ("StartLocationOffset (UU, Z-up)", Vector) = (0, 0, 4, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001

        [Header(Texture D3D9)]
        _MainTex ("Atlas (fx_m_t0005)", 2D) = "white" {}
        [HDR] _TextureFactor ("Texture Factor (D3D9 TFACTOR)", Color) = (0.95, 0.75, 0.72, 1)
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha (fx_m_t A=255)", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from RGB luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0.02
        _AlphaEdgeFeather ("Alpha edge feather", Range(0, 0.25)) = 0.015
        _RgbBoost ("RGB Boost", Range(0.25, 3)) = 1

        [Header(Debug)]
        [Toggle] _DebugAtlasPreview ("Debug Mesh Texture Preview", Float) = 0
        _DebugAtlasPreviewAlpha ("Debug Preview Alpha", Range(0, 1)) = 0.85
        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 1
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

        // RenderDoc Pixel History EID 1299: TexAfter = TexBefore + ShaderOut (One One), not SrcAlpha*src.
        // ShaderOut ~ (0.42, 0.33, 0.33) on tan bg -> cream (1, 0.93, 0.81).
        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "LightSplash"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../Common/L2FxMeshEmitterUrp.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
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
                float4 _StartSize;
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
                float _SpinParticles;
                float4 _StartSpinRange;
                float4 _StartSpinRangeZ;
                float4 _SpinsPerSecondRangeX;
                float4 _SpinsPerSecondRangeY;
                float4 _SpinsPerSecondRangeZ;
                float _SpinMinorAxesScale;
                float _SpinCCWorCW;
                float4 _StartLocationOffset;
                float _MeshYOffset;
                float _ClipDepthBias;
                float4 _TextureFactor;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaEdgeFeather;
                float _RgbBoost;
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float ageNorm : TEXCOORD1;
                float4 vertexColor : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float delay, lifetime, age, ageNorm;
                L2Fx_MeshBuiltin_ComputeTiming(
                    _Time.y, _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, age, ageNorm);
                OUT.ageNorm = ageNorm;
                OUT.vertexColor = IN.color;

                float startSpinX = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);
                float startSpinZ = L2Fx_RandomRange(_StartSpinRangeZ.xy, _Seed, _StartTime, 93.0);
                float spsX = L2Fx_RandomRange(_SpinsPerSecondRangeX.xy, _Seed, _StartTime, 95.0);
                float spsY = L2Fx_RandomRange(_SpinsPerSecondRangeY.xy, _Seed, _StartTime, 97.0);
                float spsZ = L2Fx_RandomRange(_SpinsPerSecondRangeZ.xy, _Seed, _StartTime, 99.0);
                float minorSpin = _SpinMinorAxesScale;

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;

                if (_SpinParticles > 0.5)
                {
                    // Axis 1 (UE X): yaw around +Y — main spin.
                    float angleYaw = L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(startSpinX, spsX, age);
                    L2Fx_ApplyMeshSpinAroundY(posOS, nrmOS, true, angleYaw);

                    // Axis 2 (UE Z): mesh XY, .uc ±0.01 r/s + StartSpin Z.
                    float angleRoll = L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(
                        startSpinZ, spsZ * minorSpin, age);
                    L2Fx_ApplyMeshScalarSpin(posOS, nrmOS, true, angleRoll);

                    // Axis 3 (UE Y): pitch around +X, .uc ±0.01 r/s only (no StartSpin in .uc).
                    float anglePitch = L2Fx_ComputeSpinAngleRadiansMeshEmitterRevPerSec(
                        0.0, spsY * minorSpin, age);
                    posOS = L2Fx_RotateX(posOS, anglePitch);
                    nrmOS = L2Fx_RotateX(nrmOS, anglePitch);
                }

                L2Fx_MeshBuiltin_TransformVertexOS(
                    posOS, nrmOS,
                    0.0, 0.0, 0.0, 0.0, age, ageNorm,
                    _StartSize.xyz, 0.0,
                    _UseSizeScale, _UseRegularSizeScale,
                    _SizeScaleParam, _SizeScaleRepeats, _SizeScaleCount,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    _SizeScaleTime2, _SizeScaleVal2,
                    _SizeScaleTime3, _SizeScaleVal3,
                    1.0, 1.0,
                    _StartLocationOffset.xyz, _MeshYOffset, 0.0);

                OUT.positionHCS = L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);

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

                if (_DebugAtlasPreview > 0.5)
                {
                    float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                    float mask = max(texColor.a, lum);
                    return L2Fx_AtlasDebugPreviewColor(
                        texColor,
                        mask,
                        _DebugAtlasPreviewAlpha,
                        _DebugAtlasPreviewBoost,
                        _DebugAtlasBackground);
                }

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);

                half4 tint = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, _ColorScaleTime1, _ColorScale1,
                    _ColorScaleTime2, _ColorScale2,
                    _bAlphaBlend,
                    _ColorMultMin.rgb, _ColorMultMax.rgb,
                    _Opacity, _EmitterAlpha);

                // FF_FS: out = sample(t0) * textureFactor (in_Color0 not used in this variant).
                half4 factor = half4(
                    tint.rgb * _TextureFactor.rgb,
                    tint.a * _TextureFactor.a);
                half4 lit = half4(texColor.rgb * factor.rgb, texColor.a * factor.a);

                float mask = L2Fx_MeshFrag_SampleTextureAlpha(
                    texColor, _AlphaFromLuma, _LumaAlphaFloor, _IgnoreMainTexAlpha);
                mask = L2Fx_MeshFrag_AlphaFeather(mask, _AlphaEdgeFeather);

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);

                // Additive contribution; UE Opacity scales via tint.a (see SampleBaseTint).
                half3 rgb = lit.rgb * (half)(mask * lifeAlpha * tint.a) * (half)_RgbBoost;
                if (mask * lifeAlpha < 1e-4)
                {
                    rgb = half3(0, 0, 0);
                }

                return half4(saturate(rgb), 1);
            }

            ENDHLSL
        }
    }
}
