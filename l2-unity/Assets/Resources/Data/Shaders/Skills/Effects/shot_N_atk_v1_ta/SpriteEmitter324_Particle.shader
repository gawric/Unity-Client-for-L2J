// shot_N_atk_v1_ta / SpriteEmitter324 "Particle"  (live e_u505_c layerIndex=0)
//
// UC: Polar + Offset X=2, Spin, SizeScale(0.2→1 .. 1→0.2), ColorScale×12,
//     FadeOutStart=0.66s, VelLoss=6, PTVD_OwnerAndStartPosition, atlas fx_m_t0084
//     4×8 sub 31..32. DrawStyle omitted → PTDS_Translucent (Blend One One).
//
// LIVE SpawnParticle (2026-07-22): shape@+0x174==2 → Offset+Polar only.
//   Do NOT call L2FxStartLocationRange (Range populated in UC but not sampled).
// LIVE UpdateParticles: PTVD2 + VelLoss, SizeScale late PASS, FadeOut abs 0.66s PASS,
//   ColorScale pulse under fade. Seeds: ParticleGroup _SpriteMotion/SpinRandStateBits.
//
// Skeleton: kirakira (Polar/PTVD2/Spin/Size/Color/Flipbook) + Upline drag motion.
Shader "L2/Effects/shot_N_atk_v1_ta/SpriteEmitter324_Particle"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0084)", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartLocationOffsetUc ("StartLocationOffset UE XYZ", Vector) = (2, 0, 0, 0)
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (90, 90, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (-140, 140, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (10, 20, 0, 0)
        _SizeRange ("Start Size UU Min Max", Vector) = (1, 4, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (300, 300, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (300, 300, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (300, 300, 0, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (0, 0, 0, 0)
        _VelocityLossRangeUc ("VelocityLoss per-axis 1/s", Vector) = (6, 6, 6, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012
        _StartTime ("Start Time", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.8, 2, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("Fade In End", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 0.66
        _ColorScaleCount ("Color Scale Keys", Float) = 3
        _ColorScaleParam ("Color Scale Repeats", Float) = 12
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        // UC ColorScale(1): Color=(B=146,G=71,R=73) → RGB
        _ColorKey1Time ("Color Scale 1 Time", Float) = 0.8
        _ColorKey1 ("Color Scale 1", Color) = (0.286275, 0.278431, 0.572549, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 1
        _ColorKey2 ("Color Scale 2", Color) = (1, 1, 1, 1)
        // UC ColorMultiplierRange Z=0.6 → RGB (1,1,0.6)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 1, 0.6, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 1, 0.6, 0)
        _Opacity ("Opacity", Range(0, 2)) = 1

        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0.1, 0.1, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0.2, 0.2, 0, 0)
        // UC SpinCCWorCW.X=1 → frand < 1 always flips SPS.X sign
        _SpriteSpinCcwOrCw ("Spin CCW/CW", Vector) = (1, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before Spin", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        // UC SizeScale(0) starts at RelativeTime=0.2 — not implicit key0=(0,0)
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.2, 1, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (1, 0.2, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 8
        _SubdivisionStart ("Subdivision Start", Float) = 31
        _SubdivisionEnd ("Subdivision End", Float) = 32

        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // RenderDoc: One One Add (PTDS_Translucent) — L2FxPTDS_DrawStyle
        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Particle"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpritePolar.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTVD_OwnerAndStartPosition.hlsl"
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
                float4 _StartLocationOffsetUc;
                float4 _PolarThetaRangeUc;
                float4 _PolarPhiRangeUc;
                float4 _PolarRadiusRangeUc;
                float4 _SizeRange;
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // LIVE SpawnParticle RNG scopes: Vel(+0x3A0) → Polar(+0x180) → …
                // (no +0x158 StartLocationRange). Spin uses separate spin-state bits.
                uint spawnState = asuint(_SpriteMotionRandStateBits);
                float3 rawVelocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
                    _StartVelocityRangeXUc.xy,
                    _StartVelocityRangeYUc.xy,
                    _StartVelocityRangeZUc.xy,
                    spawnState);
                float3 polarUe = L2Fx_SpritePolar_GetRandUe(
                    _PolarThetaRangeUc.xy,
                    _PolarPhiRangeUc.xy,
                    _PolarRadiusRangeUc.xy,
                    spawnState);
                float lifetimeSeconds = L2Fx_FRange_GetRand(_LifetimeRange.xy, spawnState);
                float spawnSizeUU = L2Fx_FRange_GetRand(_SizeRange.xy, spawnState);

                // Offset + Polar only (live shape=2). No StartLocationRange.
                float3 spawnPositionUe = _StartLocationOffsetUc.xyz + polarUe;
                float3 velocityBeforePtvdUe = rawVelocityUe + _AccelerationUc.xyz * _SpawnDeltaTime;
                // LIVE: velocity ≈ StartVel * normalize(spawn - owner) * e^(-VelLoss*t).
                float3 velocityUe = L2FxPTVD_OwnerAndStartPosition(
                    velocityBeforePtvdUe,
                    spawnPositionUe,
                    float3(0.0, 0.0, 0.0));

                float ageSeconds = max(0.0, _Time.y - _StartTime);
                float ageNorm = saturate(ageSeconds / max(lifetimeSeconds, 1e-4));

                // UC SizeScale(0)=(0.2,1) SizeScale(1)=(1,0.2). implicitKeyZero=false:
                // before t=0.2 SampleSizeScale holds ~1.0 (LIVE early size==startSize).
                float sizeMul = L2Fx_SpriteSizeScale_ScalarFromUniforms(
                    ageNorm,
                    _UseSizeScale,
                    _SizeScaleRepeats,
                    2u,
                    false,
                    false,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    1.0, 1.0,
                    1.0, 1.0,
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
                int frame = L2Fx_FlipbookFrameIndex(
                    ageNorm,
                    (int)_SubdivisionStart,
                    (int)_SubdivisionEnd);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
                OUT.ageSeconds = ageSeconds;
                OUT.lifetimeSeconds = lifetimeSeconds;
                OUT.seed = _SpriteMotionRandStateBits;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                // alphaBlend=0: PTDS_Translucent (ColorScale A forced to 1; fade owns A).
                // FadeOutStart=0.66 is absolute seconds (LIVE late A matches).
                float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
                    (uint)_ColorScaleCount,
                    _ColorScaleParam,
                    0.0,
                    _ColorKey0,
                    _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2,
                    1.0, _ColorKey2,
                    _ColorMulMin.xyz,
                    _ColorMulMax.xyz,
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
