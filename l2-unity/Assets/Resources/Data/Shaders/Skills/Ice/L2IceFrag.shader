Shader "L2/Effects/L2IceFrag"
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
        _LifetimeRange("Lifetime Range (Min,Max)", Vector) = (1, 1, 0, 0)
        _FadeIn("Fade In Enabled", Float) = 0
        _FadeInEndTime("Fade In End Time", Float) = 0
        _Fadeout("Fade Out Enabled", Float) = 1
        _FadeoutStartTime("Fade Out Start Time", Float) = 0.4

        _StartLocationOffset("Start Location Offset XYZ", Vector) = (0, 4, 0, 0)
        _PolarAzimuthDeg("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarFromYDeg("Polar From +Y Deg (Min,Max)", Vector) = (30, 150, 0, 0)
        _PolarRadius("Polar Radius (Min,Max)", Vector) = (10, 10, 0, 0)
        _SpawnUnitScale("UE Unit Scale", Float) = 0.01904762

        _OutwardSpeed("Outward Speed (Min,Max)", Vector) = (200, 200, 0, 0)
        _UpVelocity("Up Velocity (Min,Max)", Vector) = (150, 240, 0, 0)
        _Acceleration("Acceleration XYZ", Vector) = (0, -350, 0, 0)
        _VelocityLossRange("Velocity Loss (Min,Max)", Vector) = (2, 2, 0, 0)
        _OwnerWorldPos("Owner World Pos", Vector) = (0, 0, 0, 0)

        _SizeRangeX("Start Size Range X (Min,Max)", Vector) = (0.06, 0.19, 0, 0)
        _SizeRangeY("Start Size Range Y (Min,Max)", Vector) = (0.03, 0.15, 0, 0)
        _SizeRangeZ("Start Size Range Z (Min,Max)", Vector) = (0.03, 0.15, 0, 0)
        _ExtraRandomSizeMultiplier("Extra Random Size Multiplier (Min,Max)", Vector) = (0.9, 1.1, 0, 0)
        _UniformSize("Uniform Size", Float) = 1
        _UseSizeScale("Use Size Scale", Float) = 0
        _SizeScale0("Size Scale 0 (Time,Scale)", Vector) = (0.0, 1.0, 0, 0)
        _SizeScale1("Size Scale 1 (Time,Scale)", Vector) = (1.0, 1.0, 0, 0)
        _SizeScale2("Size Scale 2 (Time,Scale)", Vector) = (1.0, 1.0, 0, 0)

        _Seed("Seed", Float) = 0
        _SpinParticles("Spin Particles", Float) = 1
        _SpinsPerSecondRangeX("Spin Per Sec X (Min,Max)", Vector) = (0, 1, 0, 0)
        _SpinsPerSecondRangeY("Spin Per Sec Y (Min,Max)", Vector) = (1, 2, 0, 0)
        _SpinsPerSecondRangeZ("Spin Per Sec Z (Min,Max)", Vector) = (1, 2, 0, 0)
        _StartSpinRangeX("Start Spin X (Min,Max)", Vector) = (0, 1, 0, 0)
        _StartSpinRangeY("Start Spin Y (Min,Max)", Vector) = (0, 1, 0, 0)
        _StartSpinRangeZ("Start Spin Z (Min,Max)", Vector) = (0, 1, 0, 0)

        _SpecMaskStrength("Spec Mask Strength", Range(0, 2)) = 1.026
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 0.1
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 0.852
        _EdgeColor("Edge Glow Color", Color) = (0.986, 0.993, 1.0, 1.0)
        _EnvTint("Env Tint", Color) = (1, 1, 1, 1)
        _EnvStrength("Env Strength", Range(0, 4)) = 0.83
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
            #include "../Common/L2FxMeshParticleMotion.hlsl"
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
                float4 _StartLocationOffset;
                float4 _PolarAzimuthDeg;
                float4 _PolarFromYDeg;
                float4 _PolarRadius;
                float  _SpawnUnitScale;
                float4 _OutwardSpeed;
                float4 _UpVelocity;
                float4 _Acceleration;
                float4 _VelocityLossRange;
                float4 _OwnerWorldPos;
                float4 _SizeRangeX;
                float4 _SizeRangeY;
                float4 _SizeRangeZ;
                float4 _ExtraRandomSizeMultiplier;
                float  _UniformSize;
                float  _UseSizeScale;
                float4 _SizeScale0;
                float4 _SizeScale1;
                float4 _SizeScale2;
                float  _Seed;
                float  _SpinParticles;
                float4 _SpinsPerSecondRangeX;
                float4 _SpinsPerSecondRangeY;
                float4 _SpinsPerSecondRangeZ;
                float4 _StartSpinRangeX;
                float4 _StartSpinRangeY;
                float4 _StartSpinRangeZ;
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

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float normalizedAge = saturate(age / max(lifetime, 1e-4));

                float3 baseSize = L2Fx_StartSize(_SizeRangeX.xy, _SizeRangeY.xy, _SizeRangeZ.xy, _UniformSize, _Seed, _StartTime);
                float extraSize = L2Fx_RandomRange(_ExtraRandomSizeMultiplier.xy, _Seed, _StartTime, 151.0);
                float sizeScale = L2Fx_SizeScale(normalizedAge, _UseSizeScale, _SizeScale0, _SizeScale1, _SizeScale2);
                float3 localPos = IN.positionOS.xyz * (baseSize * max(0.001, extraSize) * sizeScale);
                float3 localNormal = IN.normalOS;

                L2Fx_ApplyMeshParticleSpin(
                    localPos,
                    localNormal,
                    _SpinParticles,
                    age,
                    _StartSpinRangeX.xy,
                    _StartSpinRangeY.xy,
                    _StartSpinRangeZ.xy,
                    _SpinsPerSecondRangeX.xy,
                    _SpinsPerSecondRangeY.xy,
                    _SpinsPerSecondRangeZ.xy,
                    _Seed,
                    _StartTime);

                float3 spawn = _StartLocationOffset.xyz;
                spawn += L2Fx_SpawnOffsetPolarYDegrees(_PolarAzimuthDeg.xy, _PolarFromYDeg.xy, _PolarRadius.xy, _Seed, _StartTime);
                spawn *= _SpawnUnitScale;

                float2 horizontalDir = L2Fx_OutwardDirectionXZ(spawn, _PolarAzimuthDeg.xy, _Seed, _StartTime, 181.0);
                float outwardSpeed = L2Fx_RandomRange(_OutwardSpeed.xy, _Seed, _StartTime, 191.0);
                float upVelocity = L2Fx_RandomRange(_UpVelocity.xy, _Seed, _StartTime, 193.0);
                float velocityLoss = L2Fx_RandomRange(_VelocityLossRange.xy, _Seed, _StartTime, 197.0);
                float3 velocity = float3(horizontalDir.x * outwardSpeed, upVelocity, horizontalDir.y * outwardSpeed) * _SpawnUnitScale;
                float3 acceleration = _Acceleration.xyz * _SpawnUnitScale;
                float3 displacement = L2Fx_DampedDisplacement(velocity, acceleration, velocityLoss, age);

                float3 posWS = TransformObjectToWorld(localPos + spawn + displacement);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTexture) + _UVScroll.xy * _Time.y;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(localNormal));
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(posWS));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, _Seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, _Seed, _StartTime, 7.0);
                half lifeAlpha = (half)L2Fx_LifetimeAlpha(
                    _Time.y,
                    _HasLifetime,
                    _StartTime,
                    delay,
                    lifetime,
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
