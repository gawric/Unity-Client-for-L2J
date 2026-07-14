// SpriteEmitter2 minimal isolation pass:
// StartSizeRange + StartLocationOffset + World K + runtime-timed flipbook.

Shader "L2/Effects/Calib/HealingPotionTaSpriteEmitter2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.4
        _SizeRange ("Start Size UU Min Max", Vector) = (5.5, 5.5, 0, 0)
        _StartLocationOffsetUU ("StartLocationOffset UE X Y Z", Vector) = (0, 0, 8, 0)

        [Header(ParticleSingle Timing)]
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _LifetimeRange ("Lifetime Range Min Max sec", Vector) = (2, 2, 0, 0)
        _InitialDelayRange ("Initial Delay Range Min Max sec", Vector) = (0, 0.01, 0, 0)
        [Toggle] _LoopSizeScalePreview ("Loop Flipbook Preview", Float) = 1

        [Header(Color and Fade m_u004_b)]
        _HasLifetime ("Has Lifetime", Float) = 1
        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End Time sec", Float) = 0.049
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start Time sec", Float) = 0.154

        [Header(Sprite Motion m_u004_b)]
        _StartVelocityRangeXUc ("StartVelocityRange X UU/s Min Max", Vector) = (-0.001, 0.001, 0, 0)
        _StartVelocityRangeYUc ("StartVelocityRange Y UU/s Min Max", Vector) = (-0.001, 0.001, 0, 0)
        _StartVelocityRangeZUc ("StartVelocityRange Z UU/s Min Max", Vector) = (0.004, 0.004, 0, 0)
        _SpriteMotionRandStateBits ("appRand State Before StartVelocity", Float) = 0

        [Header(Flipbook)]
        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 4
        _SubdivisionEnd ("Subdivision End", Float) = 7

        [Header(SizeScale m_u004_b)]
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0, 0.6, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.07, 1.8, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (0.14, 2.6, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (0.34, 3, 0, 0)
        _SizeKey4 ("Size Key 4 Time Size", Vector) = (1, 3.4, 0, 0)

        [Header(SpriteSpin StartSpin and SPS)]
        _SpriteSpinStartRangeUc ("StartSpinRange.X UC Min Max", Vector) = (0, 360, 0, 0)
        _SpriteSpinSpsRangeUc ("SpinsPerSecondRange.X UC Min Max", Vector) = (0, 0.3, 0, 0)
        _SpriteSpinCcwOrCw ("SpinCCWorCW X Y Z", Vector) = (0, 0, 0, 0)
        _SpriteSpinRandStateBits ("appRand State Before StartSpin", Float) = 0
        _TestSpinRadians ("Test Spin Radians", Float) = 0

        _RgbBoost ("RGB Boost", Range(0, 16)) = 7
        _LumaAlphaFloor ("Luma Alpha Floor", Range(0, 0.25)) = 0.003
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "HealingPotionTaCalibSE2"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxEmitterSpawn.hlsl" // L2FxSpriteEmitterVertex dependency: L2Fx_DegToRad.
            #include "../Common/L2FxCoreGeometryTest.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteSizeScale.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteSpin.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteColorFade.hlsl"
            #include "../Common/Decompile_Common/L2FxSpriteMotion.hlsl"
            #include "../Common/L2FxSpriteEmitterVertex.hlsl"
            #include "../Common/L2FxFlipbook.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float4 _StartLocationOffsetUU;
                float _StartTime;
                float _Seed;
                float4 _LifetimeRange;
                float4 _InitialDelayRange;
                float _LoopSizeScalePreview;
                float _HasLifetime;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float _SpriteMotionRandStateBits;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float4 _SpriteSpinStartRangeUc;
                float4 _SpriteSpinSpsRangeUc;
                float4 _SpriteSpinCcwOrCw;
                float _SpriteSpinRandStateBits;
                float _TestSpinRadians;
                float _RgbBoost;
                float _LumaAlphaFloor;
            CBUFFER_END

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

            float ResolveParticleAgeNorm()
            {
                float delay = L2Fx_RandomInitialDelay(
                    _InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(
                    _LifetimeRange.xy, _Seed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                if (_StartTime <= 0.0)
                {
                    age = _Time.y;
                }

                float normalizedAge = age / max(lifetime, 1e-4);
                return _LoopSizeScalePreview > 0.5
                    ? frac(normalizedAge)
                    : saturate(normalizedAge);
            }

            float ResolveParticleAgeSeconds()
            {
                float delay = L2Fx_RandomInitialDelay(
                    _InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                return _StartTime <= 0.0 ? _Time.y : age;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float flipBlend : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float ageNorm = ResolveParticleAgeNorm();
                float sizeMul = L2Fx_SpriteSizeScale_ScalarFromUniforms(
                    ageNorm,
                    1.0,
                    0.0,
                    5,
                    false,
                    false,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y);
                float sizeUU = ResolveStartSizeUU() * sizeMul;
                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);
                float3 spawnOffsetOS = L2Fx_UcPositionToUnityMeters(
                    _StartLocationOffsetUU.xyz,
                    _L2FxWorldCalibration);
                float3 startVelocityUe =
                    L2Fx_SpriteMotion_StartVelocityUeFromAppRandState(
                        _StartVelocityRangeXUc.xy,
                        _StartVelocityRangeYUc.xy,
                        _StartVelocityRangeZUc.xy,
                        asuint(_SpriteMotionRandStateBits));
                float3 motionOffsetOS = L2Fx_SpriteMotion_DisplacementUnityCalibrated(
                    startVelocityUe,
                    float3(0.0, 0.0, 0.0),
                    ResolveParticleAgeSeconds(),
                    1.0,
                    _L2FxWorldCalibration);
                float3 offsetOS = spawnOffsetOS + motionOffsetOS;
                float startSpinSlotFloat;
                float spinsPerSecondSlotFloat;
                L2Fx_SpriteSpin_SpawnSlotFloatsFromAppRandState(
                    _SpriteSpinStartRangeUc.xy,
                    _SpriteSpinSpsRangeUc.xy,
                    _SpriteSpinCcwOrCw.xyz,
                    asuint(_SpriteSpinRandStateBits),
                    startSpinSlotFloat,
                    spinsPerSecondSlotFloat);
                float spinRadians = L2Fx_SpriteSpin_EvaluateRadians(
                    startSpinSlotFloat,
                    spinsPerSecondSlotFloat,
                    ResolveParticleAgeSeconds());
                float2 rotatedQuadOffset = L2Fx_SpriteSpin_RotateBillboardOffset(
                    IN.positionOS.xy * sizeM,
                    spinRadians);
                float3 quadOffsetOS = float3(
                    rotatedQuadOffset.x,
                    rotatedQuadOffset.y,
                    IN.positionOS.z * sizeM);
                float3 centerWS = TransformObjectToWorld(offsetOS);
                float3 positionWS = L2Fx_CameraBillboardPositionWS(
                    centerWS,
                    quadOffsetOS,
                    0.0,
                    0.0);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                L2Fx_FlipbookAtlasUVBlend(
                    IN.uv,
                    ageNorm,
                    uSub,
                    vSub,
                    (int)_SubdivisionStart,
                    (int)_SubdivisionEnd,
                    OUT.uvAtlasA,
                    OUT.uvAtlasB,
                    OUT.flipBlend);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 tex = lerp(texA, texB, (half)IN.flipBlend);
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float mask = saturate((lum - _LumaAlphaFloor) / max(1.0 - _LumaAlphaFloor, 1e-4));
                float4 particleColor = L2Fx_SpriteColorFade_White(
                    float3(1.0, 1.0, 1.0),
                    float3(1.0, 1.0, 1.0),
                    ResolveParticleAgeSeconds(),
                    _LifetimeRange.y,
                    _HasLifetime,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime,
                    _Seed,
                    _StartTime);
                half3 rgb = tex.rgb * (half)_RgbBoost * (half)mask * particleColor.rgb;
                return half4(saturate(rgb), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
