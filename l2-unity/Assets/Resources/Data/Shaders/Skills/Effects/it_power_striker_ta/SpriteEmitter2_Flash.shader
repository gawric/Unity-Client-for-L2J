// it_power_striker_ta / SpriteEmitter2 (s_u002_a)
// UC: fx_m_t0002, MaxParticles=1, UniformSize StartSize=4.18,
// SpinParticles StartSpin X Max=1 (no SPS), SizeScale 4 keys (t=0.25..1),
// ColorScale 4 keys + Repeats=20, Opacity=0.8, FadeIn/Out, ForcedFade,
// no velocity/polar (billboard at emitter), no DrawStyle → PTDS_Translucent.
// Libs: SpriteSpin, SpriteSizeScale, SpriteColorFade, GeometryTest, Flipbook (1x1).
Shader "L2/Effects/it_power_striker_ta/SpriteEmitter2_Flash"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0002)", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _SizeRange ("Start Size UU Min Max", Vector) = (4.18, 4.18, 0, 0)
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (1.5, 1.5, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 2)) = 0

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End", Float) = 0.435
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 1.2
        _ColorScaleCount ("Color Scale Keys", Float) = 4
        _ColorScaleParam ("Color Scale Repeats", Float) = 20
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Scale 1 Time", Float) = 0.3
        _ColorKey1 ("Color Scale 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 0.857143
        _ColorKey2 ("Color Scale 2", Color) = (0.364706, 0.364706, 0.364706, 1)
        _ColorKey3Time ("Color Scale 3 Time", Float) = 1
        _ColorKey3 ("Color Scale 3", Color) = (1, 1, 1, 1)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 1, 1, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 1, 1, 0)
        _Opacity ("Opacity", Range(0, 2)) = 0.8

        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0, 1, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0, 0, 0, 0)
        _SpriteSpinCcwOrCw ("Spin CCW/CW", Vector) = (0, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before Spin", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.25, 1.4, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.65, 1.7, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (0.9, 1.9, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (1, 1.9, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 1
        _TextureVSubdivisions ("Atlas V Cells", Float) = 1
        _SubdivisionStart ("Subdivision Start", Float) = 0
        _SubdivisionEnd ("Subdivision End", Float) = 0

        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "SpriteEmitter2_Flash"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxEmitterSpawn.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteSpin.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteSizeScale.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorFade.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTDS_DrawStyle.hlsl"
            #include "../../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../../Common/L2FxFlipbook.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float _StartTime;
                float _Seed;
                float4 _LifetimeRange;
                float4 _InitialDelayRange;
                float _UseManualAge;
                float _ManualAge;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float _ColorScaleCount;
                float _ColorScaleParam;
                float4 _ColorKey0;
                float _ColorKey1Time;
                float4 _ColorKey1;
                float _ColorKey2Time;
                float4 _ColorKey2;
                float _ColorKey3Time;
                float4 _ColorKey3;
                float4 _ColorMulMin;
                float4 _ColorMulMax;
                float _Opacity;
                float4 _SpriteSpinStartRangeUc;
                float4 _SpriteSpinSpsRangeUc;
                float4 _SpriteSpinCcwOrCw;
                float _SpriteSpinRandStateBits;
                float _UseSizeScale;
                float _SizeScaleRepeats;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvAtlas : TEXCOORD0;
                nointerpolation float ageSeconds : TEXCOORD1;
                nointerpolation float lifetimeSeconds : TEXCOORD2;
                nointerpolation float seed : TEXCOORD3;
            };

            float ResolveAgeSeconds(out float lifetime)
            {
                lifetime = max(L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0), 1e-4);
                if (_UseManualAge > 0.5)
                {
                    return _ManualAge;
                }

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                return _StartTime > 0.0
                    ? L2Fx_AgeSeconds(_Time.y, _StartTime, delay)
                    : max(0.0, _Time.y - delay);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetimeSeconds;
                float ageSeconds = ResolveAgeSeconds(lifetimeSeconds);
                float ageNorm = saturate(ageSeconds / lifetimeSeconds);

                // UC SizeScale(0) at RelativeTime=0.25 — not t=0.
                // Use authored keys (implicitKeyZero=false), same as kirakira.
                float sizeMul = L2Fx_SpriteSizeScale_ScalarFromUniforms(
                    ageNorm,
                    _UseSizeScale,
                    _SizeScaleRepeats,
                    4u,
                    false,
                    false,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    1.0, 1.0);
                float spawnSizeUU = L2Fx_RandomRange(_SizeRange.xy, _Seed, _StartTime, 11.0);
                float sizeM = L2Fx_GetFinalVertexSizeMeters(spawnSizeUU * sizeMul, _L2FxWorldCalibration);

                float startSpin;
                float spinsPerSecond;
                // Preview path: mapped seed → UC slot floats (same StartSlot/Sps helpers).
                // Live parity: ParticleGroup feeds _SpriteSpinRandStateBits (TLS).
                uint spinState = asuint(_SpriteSpinRandStateBits);
                if (spinState != 0u)
                {
                    L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
                        _SpriteSpinStartRangeUc.xy, _SpriteSpinSpsRangeUc.xy,
                        _SpriteSpinCcwOrCw.xyz, spinState,
                        startSpin, spinsPerSecond);
                }
                else
                {
                    float startUc = L2Fx_RandomRange(_SpriteSpinStartRangeUc.xy, _Seed, _StartTime, 167.0);
                    float spsUc = L2Fx_RandomRange(_SpriteSpinSpsRangeUc.xy, _Seed, _StartTime, 173.0);
                    float directionSign = _SpriteSpinCcwOrCw.x == 0.0 ? -1.0 : 1.0;
                    startSpin = L2Fx_SpriteSpin_StartSlotFloatFromUc(startUc);
                    spinsPerSecond = L2Fx_SpriteSpin_SpsSlotFloatFromUc(spsUc, directionSign);
                }

                float2 rotatedQuad = L2Fx_SpriteSpin_RotateBillboardOffset(
                    IN.positionOS.xy * sizeM,
                    L2Fx_SpriteSpin_EvaluateRadians(startSpin, spinsPerSecond, ageSeconds));
                float3 positionWS = L2Fx_CameraBillboardPositionWS(
                    TransformObjectToWorld(float3(0, 0, 0)),
                    float3(rotatedQuad, IN.positionOS.z * sizeM), 0.0, 0.0);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int frame = clamp((int)_SubdivisionStart, 0, uSub * vSub - 1);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
                OUT.ageSeconds = ageSeconds;
                OUT.lifetimeSeconds = lifetimeSeconds;
                OUT.seed = _Seed;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                // alphaBlend=0 → Opacity to RGB (Translucent / non-AlphaBlend path).
                float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
                    (uint)_ColorScaleCount, _ColorScaleParam, 0.0,
                    _ColorKey0, _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2, _ColorKey3Time, _ColorKey3,
                    _ColorMulMin.xyz, _ColorMulMax.xyz,
                    IN.ageSeconds, IN.lifetimeSeconds, 1.0,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime,
                    _Opacity, 1.0, IN.seed, _StartTime);

                colorFade = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    colorFade, _L2SpriteColorGammaToLinear);

                float visible = (_UseManualAge > 0.5
                    || (IN.ageSeconds >= 0.0 && IN.ageSeconds < IN.lifetimeSeconds)) ? 1.0 : 0.0;
                half4 lit = tex * (half4)(colorFade * visible);
                lit.rgb *= (half)_RgbBoost;
                return lit;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
