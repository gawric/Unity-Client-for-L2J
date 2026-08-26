Shader "L2/Reference/BeamEmitterVerified"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _L2FxWorldCalibration ("World Calibration", Float) = 1.4
        _SizeRange ("StartSizeRange.X Min Max", Vector) = (5, 8.333, 0, 0)
        _StartLocationOffsetUc ("StartLocationOffset UE", Vector) = (0, 0, 150, 0)
        _StartLocationRangeXUc ("StartLocation X Min Max", Vector) = (-9.167, 9.167, 0, 0)
        _StartLocationRangeYUc ("StartLocation Y Min Max", Vector) = (-9.167, 9.167, 0, 0)
        _StartLocationRangeZUc ("StartLocation Z Min Max", Vector) = (-25, 25, 0, 0)
        [Toggle] _UsePolar ("PTLS_Polar", Float) = 0
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (0, 300, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (58.333, 91.667, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (10.833, 10.833, 0, 0)
        _BeamEndOffsetXUc ("BeamEnd Offset X", Vector) = (0, 0, 0, 0)
        _BeamEndOffsetYUc ("BeamEnd Offset Y", Vector) = (0, 0, 0, 0)
        _BeamEndOffsetZUc ("BeamEnd Offset Z", Vector) = (-190, -190, 0, 0)
        _BeamEndpointMode ("0 Offset, 1 Distance, 2 Absolute", Float) = 0
        _BeamDistanceRangeUc ("BeamDistance Min Max", Vector) = (0, 0, 0, 0)
        _SpriteMotionRandStateBits ("appRand State Before Spawn", Float) = 0
        _StartTime ("Start Time", Float) = 0
        _LifetimeRange ("Lifetime Min Max", Vector) = (2, 3, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        [Toggle] _FadeIn ("Fade In", Float) = 1
        _FadeInEndTime ("Fade In End", Float) = 0.5
        [Toggle] _Fadeout ("Fade Out", Float) = 1
        _FadeoutStartTime ("Fade Out Start", Float) = 1
        _ColorScaleCount ("Color Scale Keys", Float) = 3
        _ColorScaleParam ("Color Scale Repeats", Float) = 10
        _ColorKey0 ("Color Scale 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Scale 1 Time", Float) = 0.521429
        _ColorKey1 ("Color Scale 1", Color) = (0.905882, 0.905882, 0.905882, 1)
        _ColorKey2Time ("Color Scale 2 Time", Float) = 1
        _ColorKey2 ("Color Scale 2", Color) = (1, 1, 1, 1)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (0.5, 0.5, 0.5, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (0.704, 0.704, 0.704, 0)
        _Opacity ("Opacity", Range(0, 2)) = 0.33
        _OpacityRatio ("Opacity Ratio", Range(0, 1)) = 1
        _RgbBoost ("RGB Boost (debug only)", Range(0, 16)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "VerifiedBeamReference"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../L2FxEmitterSpawn.hlsl"
            #include "../L2FxCoreGeometryTest.hlsl"
            #include "L2FxAppRand.hlsl"
            #include "L2FxSpritePolar.hlsl"
            #include "L2FxStartLocationRange.hlsl"
            #include "L2FxBeamSegment.hlsl"
            #include "L2FxBeamColor.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float4 _StartLocationOffsetUc;
                float4 _StartLocationRangeXUc;
                float4 _StartLocationRangeYUc;
                float4 _StartLocationRangeZUc;
                float _UsePolar;
                float4 _PolarThetaRangeUc;
                float4 _PolarPhiRangeUc;
                float4 _PolarRadiusRangeUc;
                float4 _BeamEndOffsetXUc;
                float4 _BeamEndOffsetYUc;
                float4 _BeamEndOffsetZUc;
                float _BeamEndpointMode;
                float4 _BeamDistanceRangeUc;
                float _SpriteMotionRandStateBits;
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
                float _OpacityRatio;
                float _RgbBoost;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation float ageSeconds : TEXCOORD1;
                nointerpolation float lifetimeSeconds : TEXCOORD2;
                nointerpolation float seed : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                uint spawnState = asuint(_SpriteMotionRandStateBits);

                float3 spawnLocalUe;
                if (_UsePolar > 0.5)
                {
                    spawnLocalUe = L2Fx_SpritePolar_GetRandUe(
                        _PolarThetaRangeUc.xy,
                        _PolarPhiRangeUc.xy,
                        _PolarRadiusRangeUc.xy,
                        spawnState);
                }
                else
                {
                    spawnLocalUe = L2Fx_StartLocationRange_GetRandUe(
                        _StartLocationRangeXUc.xy,
                        _StartLocationRangeYUc.xy,
                        _StartLocationRangeZUc.xy,
                        spawnState);
                }
                float3 startUe = spawnLocalUe + _StartLocationOffsetUc.xyz;

                float3 endUe;
                if (_BeamEndpointMode < 0.5)
                {
                    endUe = L2Fx_Beam_PtepOffsetEndUe(
                        startUe,
                        _BeamEndOffsetXUc.xy,
                        _BeamEndOffsetYUc.xy,
                        _BeamEndOffsetZUc.xy,
                        spawnState);
                }
                else if (_BeamEndpointMode < 1.5)
                {
                    float distanceUe =
                        L2Fx_FRange_GetRand(_BeamDistanceRangeUc.xy, spawnState);
                    endUe = startUe + float3(0, 0, -distanceUe);
                }
                else
                {
                    endUe = L2Fx_FRangeVector_GetRandYawPitchRoll(
                        _BeamEndOffsetXUc.xy,
                        _BeamEndOffsetYUc.xy,
                        _BeamEndOffsetZUc.xy,
                        spawnState);
                }

                float lifetimeSeconds =
                    L2Fx_FRange_GetRand(_LifetimeRange.xy, spawnState);
                float sizeUU = L2Fx_FRange_GetRand(_SizeRange.xy, spawnState);
                float delay = L2Fx_RandomInitialDelay(
                    _InitialDelayRange.xy,
                    _SpriteMotionRandStateBits,
                    _StartTime,
                    3.0);
                float ageSeconds = max(0.0, _Time.y - _StartTime - delay);

                float3 startOS =
                    L2Fx_UcPositionToUnityMeters(startUe, _L2FxWorldCalibration);
                float3 endOS =
                    L2Fx_UcPositionToUnityMeters(endUe, _L2FxWorldCalibration);
                float3 startWS = TransformObjectToWorld(startOS);
                float3 endWS = TransformObjectToWorld(endOS);
                float widthMeters =
                    L2Fx_Beam_HalfWidthMeters(sizeUU, _L2FxWorldCalibration) * 2.0;

                float3 positionWS = L2Fx_Beam_BillboardPointWS(
                    startWS,
                    endWS,
                    input.positionOS.z,
                    input.positionOS.x,
                    widthMeters,
                    _WorldSpaceCameraPos.xyz);

                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.ageSeconds = ageSeconds;
                output.lifetimeSeconds = lifetimeSeconds;
                output.seed = _SpriteMotionRandStateBits;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 runtimeColor = L2Fx_Beam_RuntimeColorKeys(
                    (uint)_ColorScaleCount,
                    _ColorScaleParam,
                    _ColorKey0,
                    _ColorKey1Time,
                    _ColorKey1,
                    _ColorKey2Time,
                    _ColorKey2,
                    _ColorMulMin.xyz,
                    _ColorMulMax.xyz,
                    input.ageSeconds,
                    input.lifetimeSeconds,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime,
                    _Opacity,
                    _OpacityRatio,
                    input.seed,
                    _StartTime);

                // Keep raw runtime color for verified PTDS_Translucent Blend One One.
                half4 result = textureColor * (half4)runtimeColor;
                result.rgb *= (half)_RgbBoost;
                return result;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
