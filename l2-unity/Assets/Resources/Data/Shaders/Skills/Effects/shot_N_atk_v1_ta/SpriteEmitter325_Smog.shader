// shot_N_atk_v1_ta / SpriteEmitter325 "smog"  (live e_u505_c layerIndex=1)
//
// LIVE SpawnParticle (2026-07-22): FadeOut=0.32 FadeInEnd=0.12 Opacity=0.6
//   shape@+0x174==0 → LocRange GetRand fires but min=max=0 (no-op).
//   Polar@+0x180 populated in UC but NOT sampled (shape≠Polar).
//   spawn at (0,0,0); no PTVD (mode=0); Accel=(20,0,30).
// LIVE rngStream (28 FRange draws): Vel → LocRange → +0x2FC → 2×zeroVec →
//   ColorMul → Life → Delay → Radial → Size(vector) → Spin/SPS (@draw22).
//   Replay via L2FxSpriteSpawnParticle (do NOT short-circuit mid draws).
// LIVE runtime vs UC: Vel X=130 YZ=+/-100, Size 12..18 (UC 50/+/-80, 10..16).
//
// Do NOT call L2FxSpritePolar / L2FxPTVD_*.
Shader "L2/Effects/shot_N_atk_v1_ta/SpriteEmitter325_Smog"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0067)", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        // LIVE StartSizeRange XYZ (UniformSize → billboard uses .x after 3 draws)
        _SizeRange ("Start Size UU Min Max", Vector) = (12, 18, 0, 0)
        // LIVE StartVelocityRange (not raw UC 50/+/-80)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (130, 130, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (-100, 100, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (-100, 100, 0, 0)
        // LIVE LocRange@+0x158 min=max=0 — still consumes 3 appRand draws
        _StartLocationRangeXUc ("StartLocationRange X", Vector) = (0, 0, 0, 0)
        _StartLocationRangeYUc ("StartLocationRange Y", Vector) = (0, 0, 0, 0)
        _StartLocationRangeZUc ("StartLocationRange Z", Vector) = (0, 0, 0, 0)
        _StartVelocityRadialRangeUc ("StartVelocityRadial", Vector) = (1, 1, 0, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (20, 0, 30, 0)
        _VelocityLossRangeUc ("VelocityLoss per-axis 1/s", Vector) = (8, 8, 8, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012
        _StartTime ("Start Time", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.486, 1.3, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End", Float) = 0.12
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 0.32
        _ColorScaleCount ("Color Scale Keys", Float) = 2
        _ColorScaleParam ("Color Scale Repeats", Float) = 0
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Scale 1 Time", Float) = 1
        _ColorKey1 ("Color Scale 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 1
        _ColorKey2 ("Color Scale 2", Color) = (1, 1, 1, 1)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (0.716, 0.830, 0.784, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (0.716, 0.830, 0.784, 0)
        _Opacity ("Opacity", Range(0, 2)) = 0.6

        // UC StartSpinRange.X Max=1 → [0,1] rev; SPS 0.05..0.1
        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0, 1, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0.05, 0.1, 0, 0)
        _SpriteSpinCcwOrCw ("Spin CCW/CW", Vector) = (0, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before Spin", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        // UC SizeScale(0..3); first key @0.07 — implicitKeyZero=false (hold~1 then ramp)
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.07, 1.6, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.24, 2.0, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (0.53, 2.3, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (1.0, 2.5, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 2
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 2
        _SubdivisionEnd ("Subdivision End", Float) = 4

        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Brighten — Blend One OneMinusSrcColor.
        // Opacity must use FullKeys alphaBlend=0 (RGB *= Opacity), not A-only.
        // Contract: L2FxPTDS_DrawStyle.hlsl + L2FxSpriteColorFade ApplyOpacity.
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Smog"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteSpawnParticle.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteMotion.hlsl"
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
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float4 _StartLocationRangeXUc;
                float4 _StartLocationRangeYUc;
                float4 _StartLocationRangeZUc;
                float4 _StartVelocityRadialRangeUc;
                float4 _AccelerationUc;
                float4 _VelocityLossRangeUc;
                float _SpriteMotionRandStateBits;
                float _SpawnDeltaTime;
                float _StartTime;
                float4 _LifetimeRange;
                float4 _InitialDelayRange;
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
                nointerpolation float3 colorMulRgb : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Exact LIVE SpawnSoulShotSmogCapture rngStream (draws 0..21).
                // Spin uses _SpriteSpinRandStateBits = motionState + 22 draws.
                uint spawnState = asuint(_SpriteMotionRandStateBits);
                float3 rawVelocityUe;
                float3 locationUe;
                float3 colorMulRgb;
                float lifetimeSeconds;
                float initialDelaySeconds;
                float velocityRadial;
                float3 sizeUu;
                L2Fx_SpriteSpawnParticle_SampleShape0ThroughSize(
                    _StartVelocityRangeXUc.xy,
                    _StartVelocityRangeYUc.xy,
                    _StartVelocityRangeZUc.xy,
                    _StartLocationRangeXUc.xy,
                    _StartLocationRangeYUc.xy,
                    _StartLocationRangeZUc.xy,
                    float2(_ColorMulMin.x, _ColorMulMax.x),
                    float2(_ColorMulMin.y, _ColorMulMax.y),
                    float2(_ColorMulMin.z, _ColorMulMax.z),
                    _LifetimeRange.xy,
                    _InitialDelayRange.xy,
                    _StartVelocityRadialRangeUc.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    _SizeRange.xy,
                    spawnState,
                    rawVelocityUe,
                    locationUe,
                    colorMulRgb,
                    lifetimeSeconds,
                    initialDelaySeconds,
                    velocityRadial,
                    sizeUu);

                // LocRange zeros + Offset 0 → spawn at origin. No PTVD.
                // Delay/Radial consumed for TLS parity (smog ranges are no-ops).
                float3 spawnPositionUe = locationUe;
                float ageSeconds = max(0.0, _Time.y - _StartTime - initialDelaySeconds);
                float3 velocityUe = rawVelocityUe + _AccelerationUc.xyz * _SpawnDeltaTime;
                // UniformSize: billboard diameter from Size.X after full vector GetRand.
                float spawnSizeUU = sizeUu.x;

                float ageNorm = saturate(ageSeconds / max(lifetimeSeconds, 1e-4));

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
                float sizeUU = spawnSizeUU * sizeMul;
                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);

                float3 spawnOffsetOS = L2Fx_UcPositionToUnityMeters(
                    spawnPositionUe, _L2FxWorldCalibration);
                float3 dispUe = L2Fx_SpriteMotion_DisplacementUeWithDrag(
                    velocityUe,
                    _AccelerationUc.xyz,
                    _VelocityLossRangeUc.xyz,
                    ageSeconds,
                    1.0);
                float3 motionOffsetOS = L2Fx_UcPositionToUnityMeters(
                    dispUe, _L2FxWorldCalibration);

                float startSpin;
                float spinsPerSecond;
                L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
                    _SpriteSpinStartRangeUc.xy,
                    _SpriteSpinSpsRangeUc.xy,
                    _SpriteSpinCcwOrCw.xyz,
                    asuint(_SpriteSpinRandStateBits),
                    startSpin,
                    spinsPerSecond);
                float2 rotatedQuad = L2Fx_SpriteSpin_RotateBillboardOffset(
                    IN.positionOS.xy * sizeM,
                    L2Fx_SpriteSpin_EvaluateRadians(startSpin, spinsPerSecond, ageSeconds));
                float3 positionWS = L2Fx_CameraBillboardPositionWS(
                    TransformObjectToWorld(spawnOffsetOS + motionOffsetOS),
                    float3(rotatedQuad, IN.positionOS.z * sizeM),
                    0.0,
                    0.0);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int lo = min((int)_SubdivisionStart, (int)_SubdivisionEnd);
                int hi = max((int)_SubdivisionStart, (int)_SubdivisionEnd);
                // UC UseRandomSubdivision=True, Sub 2..4
                int frame = L2Fx_FlipbookSubDivisionRandomFrame(
                    _SpriteMotionRandStateBits, _StartTime, lo, hi, 19.0);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
                OUT.ageSeconds = ageSeconds;
                OUT.lifetimeSeconds = lifetimeSeconds;
                OUT.seed = _SpriteMotionRandStateBits;
                OUT.colorMulRgb = colorMulRgb;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                // alphaBlend=0: Brighten — Opacity scales RGB (live A8), not A.
                float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
                    (uint)_ColorScaleCount,
                    _ColorScaleParam,
                    0.0,
                    _ColorKey0,
                    _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2,
                    1.0, _ColorKey2,
                    IN.colorMulRgb,
                    IN.colorMulRgb,
                    IN.ageSeconds,
                    IN.lifetimeSeconds,
                    1.0,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime,
                    _Opacity,
                    1.0,
                    IN.seed,
                    _StartTime);
                colorFade = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    colorFade, _L2SpriteColorGammaToLinear);

                half4 lit = tex * (half4)colorFade;
                lit.rgb *= (half)_RgbBoost;
                return lit;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
