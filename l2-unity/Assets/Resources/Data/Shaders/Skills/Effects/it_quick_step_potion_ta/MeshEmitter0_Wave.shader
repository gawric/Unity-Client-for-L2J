// it_quick_step_potion_ta / MeshEmitter0 "Wave"
// Same skeleton as healing Wave; UC: etcpotion00, ColorMul (1,1,0.6), Opacity=0.5,
// Size/Loc/Vel ranges smaller than healing. PTDS_Brighten, MeshSpin (not PTRS_Actor).
Shader "L2/Effects/it_quick_step_potion_ta/MeshEmitter0_Wave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (1, 1, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Range(0, 1)) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSizeXY ("StartSize X/Y", Float) = 0.132
        _StartSizeZRange ("StartSize Z Min Max", Vector) = (-0.0408, 0.0408, 0, 0)
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 1
        _SizeKey0 ("SizeScale 0 Time Size", Vector) = (0, 0.2, 0, 0)
        _SizeKey1 ("SizeScale 1 Time Size", Vector) = (0.17, 1.3, 0, 0)
        _SizeKey2 ("SizeScale 2 Time Size", Vector) = (0.37, 1.65, 0, 0)
        _SizeKey3 ("SizeScale 3 Time Size", Vector) = (0.75, 2, 0, 0)
        _SizeKey4 ("SizeScale 4 Time Size", Vector) = (1, 2.2, 0, 0)

        _StartLocationZRangeUU ("StartLocation Z Min Max UU", Vector) = (-3.6, 3.6, 0, 0)
        _StartVelocityZRangeUU ("StartVelocity Z Min Max UU", Vector) = (-3.6, 3.6, 0, 0)
        // TLS appRand before StartVelocity GetRand. Non-zero enables L2FxMeshSpawnParticle.
        _MeshSpawnRandStateBits ("appRand TLS before StartVelocity (uint bits)", Float) = 0

        [Toggle] _SpinParticles ("SpinParticles", Float) = 1
        _StartSpinYawRangeUc ("StartSpin X / slot c0", Vector) = (0, 1, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Y / slot c1", Vector) = (0, 0.01, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Z / slot c2", Vector) = (0, 0, 0, 0)
        _SpsYawPitchRollUc ("SpinsPerSecond c0 c1 c2", Vector) = (0.3, 0, 0, 0)
        _SpinCCWorCW ("SpinCCWorCW X Y Z", Vector) = (0, 0, 0, 0)
        _StartSpinRandStateBits ("appRand TLS before StartSpin (uint bits)", Float) = 0
        // Isolation: multiply slot (c0,c1,c2)=(Yaw,Pitch,Roll) before same FRotator.
        // (1,1,1)=full; (0,1,1)=no Yaw; (1,0,1)=no Pitch; (1,1,0)=no Roll.
        // _SpinAxisEnable ("DEBUG enable c0 c1 c2", Vector) = (1, 1, 1, 0)

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 0.774, 0.6, 0)
        _ColorKey0 ("ColorScale 0", Color) = (1, 1, 1, 1)
        _ColorKey1 ("ColorScale 1", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 1
        _FadeInEndTime ("FadeIn End sec", Float) = 0.06
        [Toggle] _FadeOut ("FadeOut", Float) = 1
        _FadeOutStartTime ("FadeOut Start sec", Float) = 0.21
        _Opacity ("Opacity", Range(0, 2)) = 0.5
        // Unity-side gain (same knob as Might SpriteEmitter2). Not from UC.
        _RgbBoost ("RGB Boost", Range(0, 16)) = 4
        // ON when atlas sRGB=OFF and ColorMul looks too bright in Linear.
        // Lib: Decompile_Common/L2FxSpriteColorGammaLinear.hlsl (kirakira + this Wave).
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 1

        // RenderDoc mesh-out (force-kept in PS; _DebugMeshOut must be 1):
        //   TEXCOORD0.xy = UV, .zw = localPostSpin.xy
        //   TEXCOORD1 = localPreSpin.xyz + age
        //   TEXCOORD2 = localPostSpin.xyzw (w=spinOn)
        //   TEXCOORD3 = world.xyz + motion.y
        [Toggle] _DebugMeshOut ("DEBUG MeshOut TEXCOORD0.zw+1-3", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Wave"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../Common/L2FxCoreGeometryTest.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSizeScale.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshColorFade.hlsl"
            #include "../../Common/Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSpin.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshMotion.hlsl"
            #include "../../Common/Decompile_Common/L2FxMeshSpawnParticle.hlsl"
            #include "../../Common/Decompile_Common/L2FxPTDS_DrawStyle.hlsl"

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
                float _L2FxWorldCalibration;
                float _StartSizeXY;
                float4 _StartSizeZRange;
                float _UseSizeScale;
                float4 _SizeKey0;
                float4 _SizeKey1;
                float4 _SizeKey2;
                float4 _SizeKey3;
                float4 _SizeKey4;
                float4 _StartLocationZRangeUU;
                float4 _StartVelocityZRangeUU;
                float _MeshSpawnRandStateBits;
                float _SpinParticles;
                float4 _StartSpinYawRangeUc;
                float4 _StartSpinPitchRangeUc;
                float4 _StartSpinRollRangeUc;
                float4 _SpsYawPitchRollUc;
                float4 _SpinCCWorCW;
                float _StartSpinRandStateBits;
                // float4 _SpinAxisEnable;
                float4 _ColorMultiplier;
                float4 _ColorKey0;
                float4 _ColorKey1;
                float _FadeIn;
                float _FadeInEndTime;
                float _FadeOut;
                float _FadeOutStartTime;
                float _Opacity;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
                float _DebugMeshOut;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                // xy=UV; zw=localPostSpin.xy — packed so RenderDoc "TEXCOORD" shows debug even if 1..3 strip.
                float4 uvDbg : TEXCOORD0;
                half4 color : COLOR0;
                // RenderDoc: export these (meters, Unity OS / WS).
                nointerpolation float4 dbgLocalPreSpin : TEXCOORD1;
                nointerpolation float4 dbgLocalPostSpin : TEXCOORD2;
                nointerpolation float4 dbgWorld : TEXCOORD3;
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
                return L2Fx_MeshSizeScale_ScalarFromKeys5(
                    ageNorm,
                    _UseSizeScale,
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
                float4 color = L2Fx_MeshColorFade_FullKeys6(
                    ageSeconds,
                    lifetime,
                    0.0,
                    _ColorMultiplier.xyz,
                    _FadeIn,
                    _FadeInEndTime,
                    _FadeOut,
                    _FadeOutStartTime,
                    _Opacity,
                    _ColorKey0,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1,
                    1.0, _ColorKey1);
                return L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    color, _L2SpriteColorGammaToLinear);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float lifetime;
                float ageSeconds = ResolveAgeSeconds(lifetime);
                float ageNorm = saturate(ageSeconds / lifetime);
                float sizeScale = ResolveSizeScale(ageNorm);

                float startSizeZ;
                float startLocZ;
                float startVelZ;
                uint meshSpawnState = asuint(_MeshSpawnRandStateBits);
                if (meshSpawnState != 0u)
                {
                    L2Fx_MeshSpawnParticle_SampleLocVelSizeZ(
                        _StartLocationZRangeUU.xy,
                        _StartVelocityZRangeUU.xy,
                        _StartSizeZRange.xy,
                        float2(_StartSizeXY, _StartSizeXY),
                        float2(_ColorMultiplier.x, _ColorMultiplier.x),
                        float2(_ColorMultiplier.y, _ColorMultiplier.y),
                        float2(_ColorMultiplier.z, _ColorMultiplier.z),
                        _LifetimeRange.xy,
                        _InitialDelayRange.xy,
                        float2(1.0, 1.0),
                        meshSpawnState,
                        startLocZ,
                        startVelZ,
                        startSizeZ);
                }
                else
                {
                    startSizeZ = L2Fx_RandomRange(_StartSizeZRange.xy, _Seed, _StartTime, 11.0);
                    startLocZ = L2Fx_RandomRange(_StartLocationZRangeUU.xy, _Seed, _StartTime, 13.0);
                    startVelZ = L2Fx_RandomRange(_StartVelocityZRangeUU.xy, _Seed, _StartTime, 17.0);
                }

                float scaleXY = L2Fx_GetFinalMeshScale(
                    _StartSizeXY, sizeScale, _L2FxWorldCalibration);
                float scaleZ = L2Fx_GetFinalMeshScale(
                    startSizeZ, sizeScale, _L2FxWorldCalibration);
                // Remapped mesh: UE FinalSize (XY,XY,Z) -> Unity scale (XY,Z,XY).
                // Same bridge as L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll.
                float3 localPreSpinOS = IN.positionOS.xyz * float3(scaleXY, scaleZ, scaleXY);
                float3 localMeshOS = localPreSpinOS;
                float spinOn = 0.0;

                if (_SpinParticles > 0.5)
                {
                    spinOn = 1.0;
                    uint appRandState = asuint(_StartSpinRandStateBits);
                    float3 startYawPitchRollUru = appRandState != 0u
                        ? L2Fx_MeshSpin_StartYawPitchRollUruFromAppRandState(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            appRandState)
                        : L2Fx_MeshSpin_StartYawPitchRollUruFromMappedRanges(
                            _StartSpinYawRangeUc.xy,
                            _StartSpinPitchRangeUc.xy,
                            _StartSpinRollRangeUc.xy,
                            _Seed,
                            _StartTime);

                    // SpinCCWorCW==0 => negate (matches L2Fx_ApplySpinCCWorCW_Scalar /
                    // Wave live spinRate mostly -19660.5).
                    float3 directionSign = float3(
                        _SpinCCWorCW.x == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.y == 0.0 ? -1.0 : 1.0,
                        _SpinCCWorCW.z == 0.0 ? -1.0 : 1.0);
                    float3 spinRateC012 = L2Fx_MeshSpin_VelocityYawPitchRollUruPerSecond(
                        _SpsYawPitchRollUc.xyz,
                        directionSign);

                    float3 yawPitchRollUru = L2Fx_MeshSpin_EvaluateYawPitchRollUru(
                        startYawPitchRollUru,
                        spinRateC012,
                        ageSeconds);
                    float3 pitchYawRollRadians = L2Fx_MeshSpin_YawPitchRollToPitchYawRoll(
                        L2Fx_MeshSpin_YawPitchRollUruToRadians(yawPitchRollUru));
                    localMeshOS = L2Fx_MeshSpin_RotateUnityLocalPositionPitchYawRoll(
                        localMeshOS,
                        pitchYawRollRadians);
                }
                float3 locUe = L2Fx_MeshMotion_EvaluatePositionUe(
                    float3(0.0, 0.0, startLocZ),
                    float3(0.0, 0.0, startVelZ),
                    float3(0.0, 0.0, 0.0),
                    ageSeconds);
                float3 motionOS = L2Fx_UcPositionToUnityMeters(locUe, _L2FxWorldCalibration);

                float3 positionWS = TransformObjectToWorld(motionOS + localMeshOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                float2 uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                // Pack post-spin xy into TEXCOORD0.zw (always visible in RenderDoc as TEXCOORD.zw).
                OUT.uvDbg = float4(uv, localMeshOS.xy);

                float visible = (_UseManualAge > 0.5 || (ageSeconds >= 0.0 && ageSeconds < lifetime)) ? 1.0 : 0.0;
                OUT.color = (half4)(ResolveColor(ageSeconds, lifetime) * visible);

                OUT.dbgLocalPreSpin = float4(localPreSpinOS, ageSeconds);
                OUT.dbgLocalPostSpin = float4(localMeshOS, spinOn);
                OUT.dbgWorld = float4(positionWS, motionOS.y);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvDbg.xy);
                // Do not saturate — _RgbBoost>1 must reach Brighten framebuffer (Might SE2 path).
                half4 col = half4(
                    tex.rgb * IN.color.rgb * (half)_RgbBoost,
                    tex.a * IN.color.a);

                // Non-eliminable use: uniform*_tiny keeps TEXCOORD0.zw + 1..3 in the PS signature.
                // (_DebugMeshOut==1 on the material; visual delta ~1e-10).
                float3 live = IN.dbgLocalPreSpin.xyz
                    + IN.dbgLocalPostSpin.xyz
                    + IN.dbgWorld.xyz
                    + float3(IN.uvDbg.zw, IN.dbgLocalPostSpin.w);
                col.rgb += (half3)(live * (_DebugMeshOut * 1e-10));

                // Impossible with normal toggle (0/1); stops DXC from DCE'ing the path.
                if (_DebugMeshOut > 1.0e6)
                {
                    col = half4(IN.dbgLocalPreSpin + IN.dbgLocalPostSpin + IN.dbgWorld);
                }
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
