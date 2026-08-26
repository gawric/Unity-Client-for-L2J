Shader "L2/Effects/L2IceApprox"
{
    Properties
    {
        _MainTexture("Ice Base (fx_m_t0037)", 2D) = "white" {}
        _SpecMask("Spec Mask (fx_m_t0038)", 2D) = "white" {}
        _EnvCube("Environment Cubemap", Cube) = "" {}
        _Tint("Tint", Color) = (0.95, 0.97, 1.0, 1.0)
        _Alpha("Global Alpha", Range(0, 2)) = 1.0
        _HasLifetime("Has Lifetime", Float) = 1
        _StartTime("Start Time", Float) = 0
        _InitialDelayRange("Initial Delay Range (Min,Max)", Vector) = (0, 0, 0, 0)
        _LifetimeRange("Lifetime Range (Min,Max)", Vector) = (2, 2, 0, 0)
        _FadeIn("Fade In Enabled", Float) = 1
        _FadeInEndTime("Fade In End Time", Float) = 0.2
        _Fadeout("Fade Out Enabled", Float) = 1
        _FadeoutStartTime("Fade Out Start Time", Float) = 1.6
        _SizeRangeX("Start Size Range X (Min,Max)", Vector) = (0.0605, 0.099, 0, 0)
        _SizeRangeY("Start Size Range Y (Min,Max)", Vector) = (0.0605, 0.099, 0, 0)
        _SizeRangeZ("Start Size Range Z (Min,Max)", Vector) = (0.0605, 0.099, 0, 0)
        _ExtraRandomSizeMultiplier("Extra Random Size Multiplier (Min,Max)", Vector) = (0.75, 1.25, 0, 0)
        _UniformSize("Uniform Size", Float) = 1
        _UseSizeScale("Use Size Scale", Float) = 1
        _Seed("Seed", Float) = 0
        _SpinParticles("Spin Particles", Float) = 1
        _SpinsPerSecondRangeX("Spin Per Sec X (Min,Max)", Vector) = (-0.01, 0.01, 0, 0)
        _SpinsPerSecondRangeY("Spin Per Sec Y (Min,Max)", Vector) = (0.015, 0.015, 0, 0)
        _SpinsPerSecondRangeZ("Spin Per Sec Z (Min,Max)", Vector) = (-0.01, 0.01, 0, 0)
        _StartSpinRangeX("Start Spin X (Min,Max)", Vector) = (-1.0, 1.0, 0, 0)
        _StartSpinRangeY("Start Spin Y (Min,Max)", Vector) = (0.1, 0.4, 0, 0)
        _StartSpinRangeZ("Start Spin Z (Min,Max)", Vector) = (-1.0, 1.0, 0, 0)
        _ExtraRandomStartSpinZ("Extra Random Start Spin Z", Range(0, 3.14159)) = 3.14159
        _SizeScale0("Size Scale 0 (Time,Scale)", Vector) = (0.0, 0.35, 0, 0)
        _SizeScale1("Size Scale 1 (Time,Scale)", Vector) = (0.45, 0.75, 0, 0)
        _SizeScale2("Size Scale 2 (Time,Scale)", Vector) = (1.0, 1.0, 0, 0)
        _SizeScaleSpeed("Size Scale Speed", Range(0, 2)) = 0.21
        _SizeScaleRandomDelay("Size Scale Random Delay", Range(0, 1)) = 0.18
        _SpecMaskStrength("Spec Mask Strength", Range(0, 2)) = 0.6
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 4.0
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 0.25
        _EdgeColor("Edge Glow Color", Color) = (0.9, 0.93, 1.0, 1.0)
        _EnvTint("Env Tint", Color) = (1, 1, 1, 1)
        _EnvStrength("Env Strength", Range(0, 4)) = 1.25
        _UVScroll("UV Scroll XY", Vector) = (0.0, 0.0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
                    "L2FxGpuInstancing" = "On"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Common/L2FxParticleAnim.hlsl"
            #include "L2IceLook.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexture_ST;
                float4 _SpecMask_ST;
                float4 _Tint;
                float  _Alpha;
                float  _HasLifetime;
                float  _StartTime;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float  _FadeIn;
                float  _FadeInEndTime;
                float  _Fadeout;
                float  _FadeoutStartTime;
                float4 _SizeRangeX;
                float4 _SizeRangeY;
                float4 _SizeRangeZ;
                float4 _ExtraRandomSizeMultiplier;
                float  _UniformSize;
                float  _UseSizeScale;
                float  _Seed;
                float  _SpinParticles;
                float4 _SpinsPerSecondRangeX;
                float4 _SpinsPerSecondRangeY;
                float4 _SpinsPerSecondRangeZ;
                float4 _StartSpinRangeX;
                float4 _StartSpinRangeY;
                float4 _StartSpinRangeZ;
                float  _ExtraRandomStartSpinZ;
                float4 _SizeScale0;
                float4 _SizeScale1;
                float4 _SizeScale2;
                float  _SizeScaleSpeed;
                float  _SizeScaleRandomDelay;
                float  _SpecMaskStrength;
                float  _FresnelPower;
                float  _FresnelStrength;
                float4 _EdgeColor;
                float4 _EnvTint;
                float  _EnvStrength;
                float4 _UVScroll;
            CBUFFER_END

            #include "../Common/L2FxInstancing.hlsl"

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                float normalizedAge = L2Fx_NormalizedAge(_Time.y, _HasLifetime, _StartTime, _InitialDelayRange.x, _LifetimeRange.x);
                float ageSeconds = L2Fx_AgeSeconds(_Time.y, _StartTime, _InitialDelayRange.x);
                float sizeDelay = L2Fx_RandomRange(float2(0.0, _SizeScaleRandomDelay), _Seed, _StartTime, 157.0);
                float sizeScaleAge = saturate(max(0.0, normalizedAge - sizeDelay) * _SizeScaleSpeed);
                float sizeScale = L2Fx_SizeScale(sizeScaleAge, _UseSizeScale, _SizeScale0, _SizeScale1, _SizeScale2);
                float3 baseSize = L2Fx_StartSize(_SizeRangeX.xy, _SizeRangeY.xy, _SizeRangeZ.xy, _UniformSize, _Seed, _StartTime);
                float extraSize = L2Fx_RandomRange(_ExtraRandomSizeMultiplier.xy, _Seed, _StartTime, 151.0);
                baseSize *= max(0.001, extraSize);
                float3 scaledPosOS = IN.positionOS.xyz * (baseSize * sizeScale);

                if (_SpinParticles > 0.5)
                {
                    float3 angles = L2Fx_RotationAngles(
                        ageSeconds,
                        _StartSpinRangeX.xy,
                        _StartSpinRangeY.xy,
                        _StartSpinRangeZ.xy,
                        _SpinsPerSecondRangeX.xy,
                        _SpinsPerSecondRangeY.xy,
                        _SpinsPerSecondRangeZ.xy,
                        _Seed,
                        _StartTime);
                    angles.z += L2Fx_RandomRange(
                        float2(-_ExtraRandomStartSpinZ, _ExtraRandomStartSpinZ),
                        _Seed,
                        _StartTime,
                        149.0);
                    scaledPosOS = L2Fx_RotateX(scaledPosOS, angles.x);
                    scaledPosOS = L2Fx_RotateY(scaledPosOS, angles.y);
                    scaledPosOS = L2Fx_RotateZ(scaledPosOS, angles.z);
                }

                float3 posWS = TransformObjectToWorld(scaledPosOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTexture) + _UVScroll.xy * _Time.y;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(posWS));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half lifeAlpha = (half)L2Fx_LifetimeAlpha(
                    _Time.y,
                    _HasLifetime,
                    _StartTime,
                    _InitialDelayRange.x,
                    _LifetimeRange.x,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime);

                L2IceLookInput lookInput;
                lookInput.uvMain = IN.uv;
                lookInput.uvMask = TRANSFORM_TEX(IN.uv, _SpecMask) + _UVScroll.xy * _Time.y;
                lookInput.normalWS = IN.normalWS;
                lookInput.viewDirWS = IN.viewDirWS;
                return L2IceLook_Fragment(
                    lookInput,
                    lifeAlpha,
                    _Tint,
                    _Alpha,
                    _SpecMaskStrength,
                    _FresnelPower,
                    _FresnelStrength,
                    _EdgeColor,
                    _EnvTint,
                    _EnvStrength);
            }
            ENDHLSL
        }
    }
}
