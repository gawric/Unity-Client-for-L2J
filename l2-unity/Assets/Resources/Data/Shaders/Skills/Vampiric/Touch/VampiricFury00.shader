// UE m_u041_vampiric MeshEmitter5 "Fury00": black_berserker00 + fx_m_t0032_A, PTDS_AlphaBlend.
Shader "L2/Effects/VampiricFury00"
{
    Properties
    {
        _MainTex ("Texture (mesh UV)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1
        _InitialDelayRange ("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Range (Min,Max)", Vector) = (1.6, 1.6, 0, 0)
        _Seed ("Seed", Float) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 0.224
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 0.784

        _ColorScale0 ("ColorScale[0]", Color) = (0.9, 0.9, 0.9, 1)
        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 0.5
        _ColorScale1 ("ColorScale[1]", Color) = (0.95, 0.95, 0.95, 1)
        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1.0
        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)
        _ColorScaleCount ("ColorScale Count", Int) = 3
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 1
        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _Opacity ("Opacity", Range(0, 1)) = 1
        _EmitterAlpha ("Emitter Alpha", Range(0, 1)) = 1.0
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma / ink mask", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        _AlphaEdgeFeather ("Mask edge feather", Range(0, 0.25)) = 0.014
        _AlphaPower ("Mask contrast", Range(0.5, 3)) = 1.2
        _InkDilateTexels ("Ink extraction radius (texels)", Range(0.5, 4)) = 4
        _InkShapeScale ("Red shape mask scale", Range(1, 12)) = 2
        _InkAlphaStrength ("Ink alpha strength", Range(0, 4)) = 2.2
        _EdgeGlowAlpha ("Soft red edge glow alpha", Range(0, 1)) = 0.075
        _EdgeGlowPower ("Soft red edge glow contrast", Range(0.5, 4)) = 0.5
        _EdgeGlowBlurTexels ("Soft red edge blur radius (texels)", Range(1, 8)) = 2
        [HDR] _EdgeGlowTint ("Soft red edge glow tint", Color) = (0.55, 0.12, 0.18, 1)
        _StreakLumaVariation ("Brightness variation from tex luma", Range(0, 0.5)) = 0
        [HDR] _StreakTint ("Ink texture tint", Color) = (1, 1, 1, 1)

        _StartSize ("Start Size (mesh vertex scale XYZ)", Vector) = (0.6, 1.107, 1.107, 0)
        [Toggle] _ApplyUuToStartSize ("StartSize × 0.01 (mesh verts in raw UE UU)", Float) = 0
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1.0
        [Toggle] _UseRegularSizeScale ("Regular SizeScale", Float) = 0.0
        _SizeScaleCount ("SizeScale Count", Int) = 4
        _SizeScaleRepeats ("SizeScale Repeats (cycles/lifetime)", Float) = 1.0
        _SizeScaleParam ("SizeScale Param", Float) = 0.0
        _SizeScaleTime0 ("SizeScale Time[0]", Range(0, 1)) = 0.0
        _SizeScaleVal0 ("SizeScale Value[0]", Float) = 0.5
        _SizeScaleTime1 ("SizeScale Time[1]", Range(0, 1)) = 0.2
        _SizeScaleVal1 ("SizeScale Value[1]", Float) = 1.0
        _SizeScaleTime2 ("SizeScale Time[2]", Range(0, 1)) = 0.45
        _SizeScaleVal2 ("SizeScale Value[2]", Float) = 1.02
        _SizeScaleTime3 ("SizeScale Time[3]", Range(0, 1)) = 1.0
        _SizeScaleVal3 ("SizeScale Value[3]", Float) = 1.03

        [Toggle] _SpinParticles ("Spin Particles", Float) = 1.0
        _StartSpinRange ("Start Spin Range rev (Min,Max)", Vector) = (1, 1, 0, 0)
        _SpinsPerSecond ("Spins Per Second", Float) = 0.0
        _SpinCCWorCW ("Spin CCW(0) / CW(1)", Range(0, 1)) = 0.0

        _StartLocationOffset ("StartLocationOffset (UU)", Vector) = (5, 0, -23, 0)
        _MeshYOffset ("Lift above ground (m)", Float) = 0
        [Toggle] _BillboardToCamera ("Billboard To Camera", Float) = 0
        _BillboardWorldUp ("Billboard World Up", Vector) = (0, 1, 0, 0)
        _BillboardEulerOffset ("Billboard Euler Offset XYZ", Vector) = (0, 0, 0, 0)
        _ClipDepthBias ("Pull toward camera (NDC z)", Range(0, 0.01)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "MeshEmitter5_Fury00"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "../../Common/L2FxMeshEmitterUrp.hlsl"

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
                float4 _ColorScale0;
                float _ColorScaleTime1;
                float4 _ColorScale1;
                float _ColorScaleTime2;
                float4 _ColorScale2;
                uint _ColorScaleCount;
                float _ColorScaleRepeats;
                float _bAlphaBlend;
                float _Opacity;
                float _EmitterAlpha;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _AlphaEdgeFeather;
                float _AlphaPower;
                float _InkDilateTexels;
                float _InkShapeScale;
                float _InkAlphaStrength;
                float _EdgeGlowAlpha;
                float _EdgeGlowPower;
                float _EdgeGlowBlurTexels;
                float4 _EdgeGlowTint;
                float _StreakLumaVariation;
                float4 _StreakTint;
                float4 _StartSize;
                float _ApplyUuToStartSize;
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
                float _SpinsPerSecond;
                float _SpinCCWorCW;
                float4 _StartLocationOffset;
                float _MeshYOffset;
                float _BillboardToCamera;
                float4 _BillboardWorldUp;
                float4 _BillboardEulerOffset;
                float _ClipDepthBias;
            CBUFFER_END

            float3 L2Fx_VampiricRotateEulerDegrees(float3 p, float3 degrees)
            {
                float3 r = radians(degrees);
                float sx, cx, sy, cy, sz, cz;
                sincos(r.x, sx, cx);
                sincos(r.y, sy, cy);
                sincos(r.z, sz, cz);

                p = float3(p.x, p.y * cx - p.z * sx, p.y * sx + p.z * cx);
                p = float3(p.x * cy + p.z * sy, p.y, -p.x * sy + p.z * cy);
                p = float3(p.x * cz - p.y * sz, p.x * sz + p.y * cz, p.z);
                return p;
            }

            float3 L2Fx_VampiricBillboardPositionWS(float3 posOS)
            {
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
                toCamera = dot(toCamera, toCamera) > 1e-8 ? normalize(toCamera) : float3(0, 0, 1);

                float3 upRef = _BillboardWorldUp.xyz;
                upRef = dot(upRef, upRef) > 1e-8 ? normalize(upRef) : float3(0, 1, 0);
                upRef = abs(dot(upRef, toCamera)) > 0.98 ? float3(0, 1, 0) : upRef;

                float3 rightWS = normalize(cross(upRef, toCamera));
                float3 upWS = normalize(cross(toCamera, rightWS));
                float3 objectScale = float3(
                    length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20)),
                    length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21)),
                    length(float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22)));

                float3 billboardOS = L2Fx_VampiricRotateEulerDegrees(posOS, _BillboardEulerOffset.xyz);
                return centerWS
                    + rightWS * (billboardOS.x * objectScale.x)
                    + upWS * (billboardOS.y * objectScale.y)
                    + toCamera * (billboardOS.z * objectScale.z);
            }

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
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float delay, lifetime, age, ageNorm;
                L2Fx_MeshBuiltin_ComputeTiming(
                    _Time.y, _InitialDelayRange, _LifetimeRange, _Seed, _StartTime,
                    delay, lifetime, age, ageNorm);
                OUT.ageNorm = ageNorm;

                float startSpinRev = L2Fx_RandomRange(_StartSpinRange.xy, _Seed, _StartTime, 91.0);

                float3 posOS = IN.positionOS.xyz;
                float3 nrmOS = IN.normalOS;
                L2Fx_MeshBuiltin_TransformVertexOS(
                    posOS, nrmOS,
                    _SpinParticles, startSpinRev, _SpinsPerSecond, _SpinCCWorCW, age, ageNorm,
                    _StartSize.xyz, _ApplyUuToStartSize,
                    _UseSizeScale, _UseRegularSizeScale,
                    _SizeScaleParam, _SizeScaleRepeats, _SizeScaleCount,
                    _SizeScaleTime0, _SizeScaleVal0,
                    _SizeScaleTime1, _SizeScaleVal1,
                    _SizeScaleTime2, _SizeScaleVal2,
                    _SizeScaleTime3, _SizeScaleVal3,
                    1.0, 1.0,
                    _StartLocationOffset.xyz, _MeshYOffset, 0.0);

                OUT.positionHCS = _BillboardToCamera > 0.5
                    ? TransformWorldToHClip(L2Fx_VampiricBillboardPositionWS(posOS))
                    : L2Fx_MeshUrp_ObjectToHClip(posOS, _ClipDepthBias);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float RedShape(float3 rgb)
            {
                return saturate((rgb.r - max(rgb.g, rgb.b)) * _InkShapeScale);
            }

            float DarkInkMask(float3 rgb)
            {
                float lum = dot(rgb, float3(0.299, 0.587, 0.114));
                float redFill = RedShape(rgb);
                return saturate((0.22 - lum) * 7.0) * (1.0 - redFill);
            }

            float InkCandidate(float4 s, float redBasis)
            {
                return DarkInkMask(s.rgb) * max(redBasis, RedShape(s.rgb));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float colorScaleTimeA = min(_ColorScaleTime1, _ColorScaleTime2);
                float colorScaleTimeB = max(_ColorScaleTime1, _ColorScaleTime2);
                half4 color = L2Fx_MeshBuiltin_SampleBaseTint(
                    IN.ageNorm,
                    _ColorScaleRepeats,
                    _ColorScaleCount,
                    _ColorScale0, colorScaleTimeA, _ColorScale1,
                    colorScaleTimeB, _ColorScale2,
                    _bAlphaBlend,
                    float3(1, 1, 1), float3(1, 1, 1),
                    _Opacity, _EmitterAlpha);

                float3 texRgb = texColor.rgb;
                float redShape = RedShape(texRgb);
                float2 texel = _MainTex_TexelSize.xy * _InkDilateTexels;
                half4 n0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(texel.x, 0));
                half4 n1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(texel.x, 0));
                half4 n2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, texel.y));
                half4 n3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(0, texel.y));

                float lum = saturate(dot(texRgb, float3(0.299, 0.587, 0.114)));
                float neighborShape = max(max(redShape, RedShape(n0.rgb)), max(max(RedShape(n1.rgb), RedShape(n2.rgb)), RedShape(n3.rgb)));
                float darkInk = max(max(DarkInkMask(texRgb) * neighborShape, InkCandidate(n0, redShape)), max(max(InkCandidate(n1, redShape), InkCandidate(n2, redShape)), InkCandidate(n3, redShape)));
                float mask = smoothstep(0.0, _AlphaEdgeFeather, darkInk);
                mask = pow(saturate(mask), max(_AlphaPower, 0.0001)) * _InkAlphaStrength;

                float lifeAlpha = L2Fx_LifetimeAlpha(
                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                float inkLifeAlpha = pow(saturate(lifeAlpha), 0.45) * 0.5;
                float inkAlpha = saturate(mask * color.a * inkLifeAlpha);

                float2 glowTexel = _MainTex_TexelSize.xy * _EdgeGlowBlurTexels;
                half4 g0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(glowTexel.x, 0));
                half4 g1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(glowTexel.x, 0));
                half4 g2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, glowTexel.y));
                half4 g3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(0, glowTexel.y));
                half4 g4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + glowTexel);
                half4 g5 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - glowTexel);
                half4 g6 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(glowTexel.x, -glowTexel.y));
                half4 g7 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-glowTexel.x, glowTexel.y));
                float blurredInk = max(max(max(darkInk, InkCandidate(g0, redShape)), max(InkCandidate(g1, redShape), InkCandidate(g2, redShape))), max(max(InkCandidate(g3, redShape), InkCandidate(g4, redShape)), max(max(InkCandidate(g5, redShape), InkCandidate(g6, redShape)), InkCandidate(g7, redShape))));
                float glowAlpha = pow(saturate(blurredInk - saturate(mask)), max(_EdgeGlowPower, 0.0001)) * _EdgeGlowAlpha * color.a * lifeAlpha;

                float lumaMul = 1.0 - _StreakLumaVariation + lum * _StreakLumaVariation;
                half3 inkRgb = (half3)(texRgb * 0.35 * _StreakTint.rgb * color.rgb * lumaMul);
                half3 glowRgb = (half3)(_EdgeGlowTint.rgb * color.rgb);
                float outAlpha = saturate(inkAlpha + glowAlpha * (1.0 - inkAlpha));
                half3 rgb = (inkRgb * (half)inkAlpha + glowRgb * (half)glowAlpha) / max(outAlpha, 1e-4);

                return half4(saturate(rgb), (half)outAlpha);
            }

            ENDHLSL
        }
    }
}
