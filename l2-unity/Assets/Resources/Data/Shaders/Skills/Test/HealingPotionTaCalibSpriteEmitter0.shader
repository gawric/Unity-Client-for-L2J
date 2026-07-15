// it_healing_potion_ta / m_u004_b SpriteEmitter0 - calib shader (L2FxCoreGeometryTest.hlsl).
Shader "L2/Effects/Calib/HealingPotionTaSpriteEmitter0"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.4
        _SizeRange ("Start Size UU Min Max", Vector) = (3, 6, 0, 0)
        _StartLocationOffsetUU ("StartLocationOffset UE X Y Z", Vector) = (0, 0, 7, 0)
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (0, 360, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (10, 105, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (2.4, 2.4, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (60, 60, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (60, 60, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (-18, 1, 0, 0)
        _AccelerationUc ("Acceleration UE X Y Z", Vector) = (0, 0, -40, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012
        _StartTime ("Start Time", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (1, 1.8, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0.1, 0, 0)
        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End", Float) = 0.09
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 0.954
        _ColorScaleCount ("Color Scale Keys", Float) = 3
        _ColorScaleParam ("Color Scale Repeats", Float) = 9
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Scale 1 Time", Float) = 0.5
        _ColorKey1 ("Color Scale 1", Color) = (0.62745, 0.62745, 0.62745, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 1
        _ColorKey2 ("Color Scale 2", Color) = (1, 1, 1, 1)
        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0, 360, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0, 0.1, 0, 0)
        _SpriteSpinCcwOrCw ("Spin CCW/CW", Vector) = (0, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before Spin", Float) = 0

        _TestSizeScaleAge ("SizeScale Age 0-1", Range(0, 1)) = 0.5
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 6
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.17, 1, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.75, 0.8, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (1, 1, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (1, 1, 0, 0)
        _SizeKey4 ("Size Key 4 Time Size", Vector) = (1, 1, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _ManualFrameIndex ("Manual Frame Index", Float) = 6
        _SubdivisionStart ("Subdivision Start", Float) = 6
        _SubdivisionEnd ("Subdivision End", Float) = 8

        _RgbBoost ("RGB Boost", Range(0, 16)) = 0.7
        _PlasmaRgbScale ("Plasma RGB Scale (low luma only)", Range(0, 2)) = 1.032
        _PlasmaLumaMax ("Plasma Luma Max", Range(0.01, 1)) = 0.215
        [Toggle] _L2MotionReplayEnabled ("Debug: Replay captured L2 SE0 slot 0", Float) = 0
        _AlphaBoost ("Alpha Boost", Range(0, 16)) = 1
        _Opacity ("Opacity", Range(0, 2)) = 1
        [Toggle] _IgnoreMainTexAlpha ("Ignore texture alpha", Float) = 1
        [Toggle] _AlphaFromLuma ("Alpha from luma", Float) = 1
        _LumaAlphaFloor ("Luma alpha trim", Range(0, 0.25)) = 0
        [Toggle] _UseSoftLumaAlpha ("Soft luma alpha (preserve dim plasma)", Float) = 1
        _LumaAlphaPower ("Luma alpha power (<1 keeps dim fill)", Range(0.2, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "HealingPotionTaCalibSE0"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl"
            #include "../Common/L2FxCoreGeometryTest.hlsl"
            #include "../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../Common/Decompile_Common/L2FxSpritePolar.hlsl"
            #include "../Common/Decompile_Common/L2FxPTVD_StartPositionAndOwner.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteMotion.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteSpin.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteColorFade.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"
            #include "../Common/L2FxMeshFragment.hlsl"
            #include "../Common/L2FxPlasmaParticleBlend.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float4 _StartLocationOffsetUU;
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
                float4 _SpriteSpinStartRangeUc;
                float4 _SpriteSpinSpsRangeUc;
                float4 _SpriteSpinCcwOrCw;
                float _SpriteSpinRandStateBits;
                float _TestSizeScaleAge;
                float _SizeScaleRepeats;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _ManualFrameIndex;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _RgbBoost;
                float _PlasmaRgbScale;
                float _PlasmaLumaMax;
                float _AlphaBoost;
                float _Opacity;
                float _IgnoreMainTexAlpha;
                float _AlphaFromLuma;
                float _LumaAlphaFloor;
                float _UseSoftLumaAlpha;
                float _LumaAlphaPower;
            CBUFFER_END

            float EvaluateDynamicSizeScale(float progress)
            {
                float phase = frac(progress * _SizeScaleRepeats);

                float4 keys[5] = { _SizeKey0, _SizeKey1, _SizeKey2, _SizeKey3, _SizeKey4 };

                if (keys[0].x > 0.0 && phase < keys[0].x)
                {
                    return lerp(1.0, keys[0].y, phase / max(keys[0].x, 1e-6));
                }

                int idx = 0;
                while (idx < 4 && phase > keys[idx + 1].x)
                {
                    idx++;
                }

                float t0 = keys[idx].x;
                float s0 = keys[idx].y;
                float t1 = keys[idx + 1].x;
                float s1 = keys[idx + 1].y;

                if (abs(t1 - t0) < 1e-6)
                {
                    return s0;
                }

                float u = (phase - t0) / (t1 - t0);
                return lerp(s0, s1, saturate(u));
            }

            float ResolveStartSizeUU()
            {
                float minUU = _SizeRange.x;
                float maxUU = _SizeRange.y;
                if (maxUU < minUU)
                {
                    float t = minUU;
                    minUU = maxUU;
                    maxUU = t;
                }
                return (minUU + maxUU) * 0.5;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvAtlas : TEXCOORD0;
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
                // Captured SE0 stream: draws 6..15 are presently unnamed; preserve
                // their LCG positions before consuming Lifetime (16) and StartSize (19).
                [unroll] for (int rngSkip = 0; rngSkip < 10; ++rngSkip)
                {
                    L2Fx_AppRand(spawnState);
                }
                float lifetimeSeconds = L2Fx_FRange_GetRand(_LifetimeRange.xy, spawnState);
                L2Fx_AppRand(spawnState);
                L2Fx_AppRand(spawnState);
                float spawnSizeUU = L2Fx_FRange_GetRand(_SizeRange.xy, spawnState);
                L2Fx_AppRand(spawnState);
                L2Fx_AppRand(spawnState);
                float3 spawnPositionUe = _StartLocationOffsetUU.xyz + polarUe;
                float3 velocityBeforePtvdUe = rawVelocityUe + _AccelerationUc.xyz * _SpawnDeltaTime;
                float3 velocityUe = L2FxPTVD_StartPositionAndOwner(
                    velocityBeforePtvdUe,
                    spawnPositionUe,
                    float3(0.0, 0.0, 0.0));
                float ageSeconds = max(0.0, _Time.y - _StartTime);
                float ageNorm = saturate(ageSeconds / max(lifetimeSeconds, 1e-4));
                float sizeUU = spawnSizeUU * EvaluateDynamicSizeScale(ageNorm);
                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
                // L2 advances velocity and position in discrete emitter ticks.
                // This shader intentionally uses continuous ballistic displacement instead:
                // p = v0*t + 0.5*a*t². It is visually stable and avoids per-slot CPU
                // updates, but its vertical speed can differ from L2 by roughly
                // acceleration * SpawnDeltaTime immediately after spawn.
                float3 spawnOffsetOS = L2Fx_UcPositionToUnityMeters(
                    spawnPositionUe, _L2FxWorldCalibration);
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
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                int lo = min(s0, s1);
                int hi = max(s0, s1);
                int frame = L2Fx_FlipbookSubDivisionRandomFrame(
                    _SpriteMotionRandStateBits, _StartTime, lo, hi, 19.0);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                float mask = L2Fx_MeshFrag_SampleTextureAlphaSoft(
                    tex,
                    _AlphaFromLuma,
                    _LumaAlphaFloor,
                    _LumaAlphaPower,
                    _UseSoftLumaAlpha,
                    _IgnoreMainTexAlpha);
                float ageSeconds = max(0.0, _Time.y - _StartTime);
                float4 colorFade = L2Fx_SpriteColorFade_FullKeys(
                    (uint)_ColorScaleCount, _ColorScaleParam, 1.0,
                    _ColorKey0, _ColorKey1Time, _ColorKey1,
                    _ColorKey2Time, _ColorKey2, 1.0, _ColorKey2,
                    float3(1.0, 1.0, 1.0), float3(1.0, 1.0, 1.0),
                    ageSeconds, _LifetimeRange.y, 1.0,
                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime,
                    1.0, 1.0, _SpriteMotionRandStateBits, _StartTime);
                half3 rgb = tex.rgb * (half3)colorFade.rgb * (half)_RgbBoost;
                rgb = L2Fx_PlasmaParticle_ApplyLowLumaRgbScale(
                    rgb, tex.rgb, _PlasmaRgbScale, _PlasmaLumaMax);
                half alpha = (half)saturate(mask * _AlphaBoost * colorFade.a * _Opacity);
                return half4(saturate(rgb), alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
