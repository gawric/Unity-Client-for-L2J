// it_healing_potion_ta / SpriteEmitter7 "kirakira"
// UC: Polar, Spin, SizeScale, ColorScale+Repeats, Flipbook RandomSubdivision 6..8,
//     PTVD_OwnerAndStartPosition, PTDS_Brighten
// Size: L2Fx_GetFinalVertexSizeMeters (L2FxCoreGeometryTest) — same as SE0/SE2 calib.
// SizeScale: ScalarFromUniforms, implicitKeyZero=false (UC first key at t=0.37).
// Frag (RenderDoc FF PS): out = tex * particleColor; Brighten — no luma mask on RGB.
Shader "L2/Effects/it_healing_potion_ta/SpriteEmitter7_Kirakira"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0005_A)", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.4
        _SizeRange ("Start Size UU Min Max", Vector) = (4.8, 6.6, 0, 0)
        _StartLocationRangeUU ("StartLocationRange UE XYZ half-extents", Vector) = (12, 12, 12, 0)
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (0, 360, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (85, 95, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (9, 9, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (10, 10, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (10, 10, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (10, 10, 0, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (0, 0, 0, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012
        _StartTime ("Start Time", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (1, 1.4, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)

        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End", Float) = 0.1
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 0.2
        _ColorScaleCount ("Color Scale Keys", Float) = 3
        _ColorScaleParam ("Color Scale Repeats", Float) = 16
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Scale 1 Time", Float) = 0.425
        _ColorKey1 ("Color Scale 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 1
        _ColorKey2 ("Color Scale 2", Color) = (0, 0, 0, 0)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 0.553, 0.12, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 0.553, 0.12, 0)
        _Opacity ("Opacity", Range(0, 2)) = 1

        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0, 1, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0.12, 0.12, 0, 0)
        _SpriteSpinCcwOrCw ("Spin CCW/CW", Vector) = (0, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before Spin", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.37, 0.9, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.75, 0.2, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (1, 0.1, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 6
        _SubdivisionEnd ("Subdivision End", Float) = 8

        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        // ON when atlas sRGB=OFF and ColorMul/ColorScale look too white in Linear.
        // Lib: Decompile_Common/L2FxSpriteColorGammaLinear.hlsl (verified kirakira).
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        [Toggle] _UseSoftLumaAlpha ("Soft luma alpha", Float) = 1
        _LumaAlphaPower ("Luma alpha power", Range(0.2, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcColor
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
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxEmitterSpawn.hlsl"
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
            #include "../../Common/L2FxMeshFragment.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float4 _StartLocationRangeUU;
                float4 _PolarThetaRangeUc;
                float4 _PolarPhiRangeUc;
                float4 _PolarRadiusRangeUc;
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float4 _AccelerationUc;
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
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _UseSoftLumaAlpha;
                float _LumaAlphaPower;
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
                float3 spawnPositionUe = polarUe;
                float3 velocityBeforePtvdUe = rawVelocityUe + _AccelerationUc.xyz * _SpawnDeltaTime;
                // UC GetVelocityDirectionFrom=PTVD_OwnerAndStartPosition (mode 2):
                // +vel * normalize(spawn - owner) → outward from polar ring.
                // Do NOT use L2FxPTVD_StartPositionAndOwner (mode 1, negated = inward).
                float3 velocityUe = L2FxPTVD_OwnerAndStartPosition(
                    velocityBeforePtvdUe,
                    spawnPositionUe,
                    float3(0.0, 0.0, 0.0));

                float ageSeconds = max(0.0, _Time.y - _StartTime);
                float ageNorm = saturate(ageSeconds / max(lifetimeSeconds, 1e-4));
                // UC SizeScale(0) starts at RelativeTime=0.37, RelativeSize=0.9 — not at t=0.
                // Do NOT use L2Fx_SpriteSizeScale_FullKeys (implicit key0=(0,0)): that collapses
                // size to ~0 for the first ~37% of life. SampleSizeScale with first key t>0
                // lerps from 1.0 → first RelativeSize (same as SE0/SE2 calib).
                float sizeMul = L2Fx_SpriteSizeScale_ScalarFromUniforms(
                    ageNorm,
                    _UseSizeScale,
                    _SizeScaleRepeats,
                    3u,
                    false,
                    false,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    1.0, 1.0,
                    1.0, 1.0);
                float sizeUU = spawnSizeUU * sizeMul;
                // Verified sprite path (same as SE0/SE2 calib): diameter ≈ sizeUU/52.5 * K.
                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);

                float3 spawnOffsetOS = L2Fx_UcPositionToUnityMeters(spawnPositionUe, _L2FxWorldCalibration);
                float3 motionOffsetOS = L2Fx_SpriteMotion_DisplacementUnityCalibrated(
                    velocityUe, _AccelerationUc.xyz, ageSeconds, 1.0, _L2FxWorldCalibration);

                float startSpin;
                float spinsPerSecond;
                L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
                    _SpriteSpinStartRangeUc.xy, _SpriteSpinSpsRangeUc.xy,
                    _SpriteSpinCcwOrCw.xyz, asuint(_SpriteSpinRandStateBits),
                    startSpin, spinsPerSecond);
                float2 rotatedQuad = L2Fx_SpriteSpin_RotateBillboardOffset(
                    IN.positionOS.xy * sizeM,
                    L2Fx_SpriteSpin_EvaluateRadians(startSpin, spinsPerSecond, ageSeconds));
                float3 positionWS = L2Fx_CameraBillboardPositionWS(
                    TransformObjectToWorld(spawnOffsetOS + motionOffsetOS),
                    float3(rotatedQuad, IN.positionOS.z * sizeM), 0.0, 0.0);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int lo = min((int)_SubdivisionStart, (int)_SubdivisionEnd);
                int hi = max((int)_SubdivisionStart, (int)_SubdivisionEnd);
                int frame = L2Fx_FlipbookSubDivisionRandomFrame(
                    _SpriteMotionRandStateBits, _StartTime, lo, hi, 19.0);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
                OUT.ageSeconds = ageSeconds;
                OUT.lifetimeSeconds = lifetimeSeconds;
                OUT.seed = _SpriteMotionRandStateBits;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // RenderDoc L2 FF PS (Brighten kirakira): out = tex * vertexColor.
                // Soft core/gel is already in tex.rgb — do not multiply RGB by luma mask.
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
                    (uint)_ColorScaleCount, _ColorScaleParam, 1.0,
                    _ColorKey0, _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2, 1.0, _ColorKey2,
                    _ColorMulMin.xyz, _ColorMulMax.xyz,
                    IN.ageSeconds, IN.lifetimeSeconds, 1.0,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime,
                    _Opacity, 1.0, IN.seed, _StartTime);

                // Optional: L2 ColorMul/ColorScale gamma→linear (mat toggle).
                // See L2FxSpriteColorGammaLinear.hlsl — Fade math stays untouched.
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
