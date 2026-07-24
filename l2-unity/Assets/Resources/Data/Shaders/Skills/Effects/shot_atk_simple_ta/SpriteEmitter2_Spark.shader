// shot_atk_simple_ta / SpriteEmitter2 "Spark"
//
// UC: UseDirectionAs=PTDU_Up, PTLS_Polar, Motion (ballistic, VelLoss=0),
//     SizeScale grow-then-shrink, ColorFade (fade-out only), PTDS_Brighten.
//     Offset X=0 Y=0 Z=0, Polar Theta -45..45 Phi 60..120 Radius 4..4,
//     Accel (0,0,-200), Vel X 50..120 Y -80..80 Z -80..80, VelLoss (0,0,0),
//     Size X/Z 0.2..0.4 Y 1.8..3.5, life=0.6, FadeOutStart=0.115, Opacity=1,
//     ColorMul 1/1/1, atlas 8x8 sub 17..20 UseRandomSubdivision=True.
//
// No GetVelocityDirectionFrom on Spark: do NOT include/call
// L2FxPTVD_OwnerAndStartPosition (that module is direction-only PTVD and is
// not part of this UC layer's spawn path).
//
// RNG (Polar shape — same draw order as Needle / Upline Polar path):
//   [0..2] Vel -> [3..5] Polar -> [6..12] mid burns -> [13..15] ColorMul ->
//   [16] Life -> [17] Delay -> [18] Radial -> [19..21] Size XYZ.
// Feed TLS via ParticleGroup _SpriteMotionRandStateBits (base + slot*28).
//
// Do NOT use camera billboard — PTDU_Up only (Decompile_Common).
// UseRandomSubdivision: pick ONE atlas cell per particle at spawn (Gawric
// SubDivision-style hash), not a time-animated flipbook — see Smog shader.
Shader "L2/Effects/shot_atk_simple_ta/SpriteEmitter2_Spark"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0061)", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartLocationOffsetUc ("StartLocationOffset UE XYZ", Vector) = (0, 0, 0, 0)
        // UC StartLocationPolarRange X=theta Y=phi Z=radius
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (-45, 45, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (60, 120, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (4, 4, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (50, 120, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (-80, 80, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (-80, 80, 0, 0)
        _AccelerationUc ("Acceleration UE XYZ", Vector) = (0, 0, -200, 0)
        _VelocityLossRangeUc ("VelocityLoss per-axis 1/s", Vector) = (0, 0, 0, 0)
        // PTDU_Up: X = sheet width, Y = streak length
        _SizeRangeXUc ("StartSize X UU Min Max", Vector) = (0.2, 0.4, 0, 0)
        _SizeRangeYUc ("StartSize Y UU Min Max", Vector) = (1.8, 3.5, 0, 0)
        _SizeRangeZUc ("StartSize Z UU Min Max", Vector) = (0.2, 0.4, 0, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed (unused for spawn RNG)", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (0.6, 0.6, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRadialRangeUc ("StartVelocityRadial", Vector) = (1, 1, 0, 0)

        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("Fade In End", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 0.115
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 1, 1, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 1, 1, 0)
        _Opacity ("Opacity", Range(0, 2)) = 1
        _RgbBoost ("RGB Boost", Range(0, 4)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear", Float) = 0

        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        // UC SizeScale(0)=(0,1.5) SizeScale(1)=(0.37,2) SizeScale(2)=(1,0.3) — explicit key 0 (not implicit zero).
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0, 1.5, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.37, 2, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (1, 0.3, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 8
        _TextureVSubdivisions ("Atlas V Cells", Float) = 8
        _SubdivisionStart ("Subdivision Start", Float) = 17
        _SubdivisionEnd ("Subdivision End", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        // PTDS_Brighten — Blend One OneMinusSrcColor (see Smog shader / L2FxPTDS_DrawStyle).
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Spark"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxAppRand.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpritePolar.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteMotion.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTDU_Up.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteSizeScale.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorFade.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTDS_DrawStyle.hlsl"
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
                float4 _StartVelocityRangeXUc;
                float4 _StartVelocityRangeYUc;
                float4 _StartVelocityRangeZUc;
                float4 _AccelerationUc;
                float4 _VelocityLossRangeUc;
                float4 _SizeRangeXUc;
                float4 _SizeRangeYUc;
                float4 _SizeRangeZUc;
                float _SpriteMotionRandStateBits;
                float _SpawnDeltaTime;
                float _StartTime;
                float _Seed;
                float4 _LifetimeRange;
                float4 _InitialDelayRange;
                float4 _StartVelocityRadialRangeUc;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float4 _ColorMulMin;
                float4 _ColorMulMax;
                float _Opacity;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
                float _UseSizeScale;
                float _SizeScaleRepeats;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
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
                nointerpolation float4 tint : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                uint spawnState = asuint(_SpriteMotionRandStateBits);

                // [0..2] StartVelocity
                float3 rawVelocityUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
                    _StartVelocityRangeXUc.xy,
                    _StartVelocityRangeYUc.xy,
                    _StartVelocityRangeZUc.xy,
                    spawnState);

                // [3..5] Polar (r→φ→θ)
                float3 polarUe = L2Fx_SpritePolar_GetRandUe(
                    _PolarThetaRangeUc.xy,
                    _PolarPhiRangeUc.xy,
                    _PolarRadiusRangeUc.xy,
                    spawnState);

                // [6..12] mid burns (scalar + unused vectors)
                [unroll] for (int rngSkip = 0; rngSkip < 7; ++rngSkip)
                {
                    L2Fx_AppRand(spawnState);
                }

                // [13..15] ColorMultiplierRange
                float3 colorMulRgb = L2Fx_FRangeVector_GetRandYawPitchRoll(
                    float2(_ColorMulMin.x, _ColorMulMax.x),
                    float2(_ColorMulMin.y, _ColorMulMax.y),
                    float2(_ColorMulMin.z, _ColorMulMax.z),
                    spawnState);

                // [16] Lifetime [17] Delay [18] Radial
                float lifetimeSeconds = L2Fx_FRange_GetRand(_LifetimeRange.xy, spawnState);
                L2Fx_FRange_GetRand(_InitialDelayRange.xy, spawnState);
                L2Fx_FRange_GetRand(_StartVelocityRadialRangeUc.xy, spawnState);

                // [19..21] StartSize XYZ (PTDU_Up uses X width, Y length)
                float3 sizeUu = L2Fx_FRangeVector_GetRandYawPitchRoll(
                    _SizeRangeXUc.xy,
                    _SizeRangeYUc.xy,
                    _SizeRangeZUc.xy,
                    spawnState);

                float3 spawnPositionUe = _StartLocationOffsetUc.xyz + polarUe;
                // No GetVelocityDirectionFrom on Spark — raw velocity + spawn accel only.
                float3 velocityUe = rawVelocityUe + _AccelerationUc.xyz * _SpawnDeltaTime;

                float ageSeconds = max(0.0, _Time.y - _StartTime);
                float lifetime = max(lifetimeSeconds, 1e-4);
                float ageNorm = saturate(ageSeconds / lifetime);

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

                float3 lossUe = _VelocityLossRangeUc.xyz;
                float3 dispUe = L2Fx_SpriteMotion_DisplacementUeWithDrag(
                    velocityUe, _AccelerationUc.xyz, lossUe, ageSeconds, 1.0);
                float prevAge = max(0.0, ageSeconds - max(_SpawnDeltaTime, 1e-4));
                float3 prevDispUe = L2Fx_SpriteMotion_DisplacementUeWithDrag(
                    velocityUe, _AccelerationUc.xyz, lossUe, prevAge, 1.0);

                float3 locUe = spawnPositionUe + dispUe;
                float3 oldUe = spawnPositionUe + prevDispUe;

                float worldK = _L2FxWorldCalibration;
                float3 locOS = L2Fx_UcPositionToUnityMeters(locUe, worldK);
                float3 oldOS = L2Fx_UcPositionToUnityMeters(oldUe, worldK);
                float sizeXM = L2Fx_GetFinalVertexSizeMeters(sizeUu.x * sizeMul, worldK);
                float sizeYM = L2Fx_GetFinalVertexSizeMeters(sizeUu.y * sizeMul, worldK);
                float3 camOS = TransformWorldToObject(GetCameraPositionWS());

                float3 cornerOS = L2FxPTDU_Up_PositionUnityFromQuadOs(
                    locOS, oldOS, camOS, sizeXM, sizeYM, IN.positionOS.xy);
                OUT.positionHCS = TransformObjectToHClip(cornerOS);

                // UseRandomSubdivision: one fixed atlas cell per particle (no time animation).
                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int lo = min((int)_SubdivisionStart, (int)_SubdivisionEnd);
                int hi = max((int)_SubdivisionStart, (int)_SubdivisionEnd);
                int frame = L2Fx_FlipbookSubDivisionRandomFrame(
                    _SpriteMotionRandStateBits, _StartTime, lo, hi, 53.0);
                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(quadUv, frame, uSub, vSub);

                float4 tint = L2Fx_SpriteColorFade_Apply(
                    float4(colorMulRgb, 1.0),
                    ageSeconds,
                    lifetime,
                    1.0,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime);
                // PTDS_Brighten: Opacity on RGB (not A-only) — see L2FxPTDS_DrawStyle.
                tint.rgb *= _Opacity;
                tint = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    tint, _L2SpriteColorGammaToLinear);
                OUT.tint = tint;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);
                half4 lit = tex * (half4)IN.tint;
                lit.rgb *= (half)_RgbBoost;
                return lit;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
