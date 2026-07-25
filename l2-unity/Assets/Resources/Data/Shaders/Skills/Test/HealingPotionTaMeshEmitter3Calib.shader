// it_healing_potion_ta / MeshEmitter3: supportenchant01.
// Mesh FBX is already imported in Unity meters; StartSize stays a mesh-scale multiplier.
Shader "L2/Effects/HealingPotionTaMeshEmitter3"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0005_A)", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Range Min Max sec", Vector) = (0, 0.1, 0, 0)
        _LifetimeRange ("Lifetime Range Min Max sec", Vector) = (1.5, 1.5, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 1.5)) = 0
        [Toggle] _LoopSizeScalePreview ("Loop SizeScale and Color Preview", Float) = 0

        _L2FxWorldCalibration ("Verified World Calibration K", Float) = 1.8
        _StartSize ("StartSizeRange X", Float) = 0.065
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0, 0.5, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.14, 3.5, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (0.37, 4.6, 0, 0)
        _SizeKey3 ("SizeScale 3 Time Size", Vector) = (0.81, 5.1, 0, 0)
        _SizeKey4 ("SizeScale 4 Time Size", Vector) = (1, 5.3, 0, 0)

        _StartLocationOffsetUU ("StartLocationOffset UE X Y Z", Vector) = (0, 0, 8, 0)

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpinRange X / runtime Yaw", Vector) = (0, 1, 0, 0)
        _StartSpinPitchRangeUc ("StartSpinRange Y / runtime Pitch", Vector) = (0, 1, 0, 0)
        _StartSpinRollRangeUc ("StartSpinRange Z / runtime Roll", Vector) = (0, 1, 0, 0)
        _StartSpinRandStateBits ("appRand TLS State before Roll/Z (uint bits)", Float) = 0

        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        _ColorKey2 ("ColorScale 2", Color) = (0.772549, 0.772549, 0.772549, 1)
        _ColorKey3 ("ColorScale 3", Color) = (0.737255, 0.737255, 0.737255, 1)
        _ColorKey4 ("ColorScale 4", Color) = (0.501961, 0.501961, 0.501961, 1)
        _ColorKey5 ("ColorScale 5", Color) = (0, 0, 0, 1)
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start Time sec", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "HealingPotionTaMeshEmitter3Calib"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Exact appRand needs 32-bit uint wraparound and bit shifts.
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxCoreGeometryTest.hlsl"
            #include "../Common/Decompile_Common/L2FxMeshSizeScale.hlsl"
            #include "../Common/Decompile_Common/L2FxMeshColorFade.hlsl"
            #include "../Common/Decompile_Common/L2FxMeshSpin.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _StartTime;
                float _Seed;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _UseManualAge;
                float _ManualAge;
                float _LoopSizeScalePreview;
                float _L2FxWorldCalibration;
                float _StartSize;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float4 _StartLocationOffsetUU;
                float _SpinParticles;
                float4 _StartSpinYawRangeUc;
                float4 _StartSpinPitchRangeUc;
                float4 _StartSpinRollRangeUc;
                float _StartSpinRandStateBits;
                float4 _ColorKey0;
                float4 _ColorKey1;
                float4 _ColorKey2;
                float4 _ColorKey3;
                float4 _ColorKey4;
                float4 _ColorKey5;
                float _FadeOut;
                float _FadeOutStartTime;
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
                half4 color : COLOR0;
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

            float ResolveSizeScale(float ageNorm)
            {
                if (_UseSizeScale < 0.5)
                {
                    return 1.0;
                }

                // MeshEmitter3 has no authored SizeScaleRepeats. param=1 makes
                // its explicit [0..1] curve run once during the particle lifetime.
                return L2Fx_MeshSizeScale_ScalarFromKeys5(
                    ageNorm,
                    1.0,
                    0.0,
                    1.0,
                    0.0,
                    5,
                    _SizeKey0.x, _SizeKey0.y,
                    _SizeKey1.x, _SizeKey1.y,
                    _SizeKey2.x, _SizeKey2.y,
                    _SizeKey3.x, _SizeKey3.y,
                    _SizeKey4.x, _SizeKey4.y);
            }

            float4 ResolveColor(float ageSeconds, float lifetime)
            {
                return L2Fx_MeshColorFade_FullKeys6(
                    ageSeconds,
                    lifetime,
                    0.0,
                    float3(1, 1, 1),
                    _FadeOut,
                    _FadeOutStartTime,
                    _ColorKey0,
                    0.15, _ColorKey1,
                    0.303571, _ColorKey2,
                    0.685714, _ColorKey3,
                    0.925, _ColorKey4,
                    1.0, _ColorKey5);
            }

            float ResolveParticleVisibility()
            {
                if (_UseManualAge > 0.5)
                {
                    return 1.0;
                }

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float rawAge = _StartTime > 0.0
                    ? _Time.y - _StartTime - delay
                    : _Time.y - delay;
                return rawAge >= 0.0 ? 1.0 : 0.0;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetime;
                float ageSeconds = ResolveAgeSeconds(lifetime);
                float ageNorm = _LoopSizeScalePreview > 0.5
                    ? frac(ageSeconds / lifetime)
                    : saturate(ageSeconds / lifetime);
                float sizeScale = ResolveSizeScale(ageNorm);

                // Verified MeshEmitter candidate:
                // finalScale = StartSize * SizeScale * K
                float finalScale = L2Fx_GetFinalMeshScale(_StartSize, sizeScale, _L2FxWorldCalibration);
                float3 localMeshOS = IN.positionOS.xyz * finalScale;
                if (_SpinParticles > 0.5)
                {
                    // m_u004_b / MeshEmitter3 is always replayed through the
                    // validated L2 appRand path. The runtime supplies a state
                    // immediately before the Roll/Z draw for this slot.
                    float3 startYawPitchRollUru =
                        L2Fx_MeshSpin_StartYawPitchRollUruFromAppRandState(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            asuint(_StartSpinRandStateBits));
                    float3 yawPitchRollUru = L2Fx_MeshSpin_EvaluateYawPitchRollUru(
                        startYawPitchRollUru,
                        float3(0.0, 0.0, 0.0),
                        ageSeconds);
                    float3 pitchYawRollRadians = L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(
                        L2Fx_MeshSpin_YawPitchRollUruToRadians(yawPitchRollUru));
                    localMeshOS = L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
                        localMeshOS,
                        pitchYawRollRadians);
                }

                float3 locationOffset = L2Fx_UcPositionToUnityMeters(
                    _StartLocationOffsetUU.xyz,
                    _L2FxWorldCalibration);
                float3 positionWS = TransformObjectToWorld(locationOffset + localMeshOS);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.color = (half4)(ResolveColor(ageNorm * lifetime, lifetime) * ResolveParticleVisibility());
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
