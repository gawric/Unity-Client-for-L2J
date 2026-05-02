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
        _SizeScale0("Size Scale 0 (Time,Scale)", Vector) = (0.0, 1.0, 0, 0)
        _SizeScale1("Size Scale 1 (Time,Scale)", Vector) = (0.07, 1.1, 0, 0)
        _SizeScale2("Size Scale 2 (Time,Scale)", Vector) = (1.0, 1.0, 0, 0)
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
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);
            TEXTURE2D(_SpecMask);
            SAMPLER(sampler_SpecMask);
            TEXTURECUBE(_EnvCube);
            SAMPLER(sampler_EnvCube);

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
                float4 _SizeScale0;
                float4 _SizeScale1;
                float4 _SizeScale2;
                float  _SpecMaskStrength;
                float  _FresnelPower;
                float  _FresnelStrength;
                float4 _EdgeColor;
                float4 _EnvTint;
                float  _EnvStrength;
                float4 _UVScroll;
            CBUFFER_END

            float ResolveNormalizedAge()
            {
                float hasLifetime = step(0.5, _HasLifetime);
                if (hasLifetime < 0.5)
                {
                    return 1.0;
                }

                float age = _Time.y - _StartTime - max(0.0, _InitialDelayRange.x);
                float lifetime = max(0.0001, _LifetimeRange.x);
                return saturate(age / lifetime);
            }

            float ResolveSizeScale(float normalizedAge)
            {
                if (_UseSizeScale < 0.5)
                {
                    return 1.0;
                }

                float t0 = saturate(_SizeScale0.x);
                float t1 = saturate(_SizeScale1.x);
                float t2 = saturate(_SizeScale2.x);
                float s0 = _SizeScale0.y;
                float s1 = _SizeScale1.y;
                float s2 = _SizeScale2.y;

                if (normalizedAge <= t1)
                {
                    float denom01 = max(0.0001, t1 - t0);
                    return lerp(s0, s1, saturate((normalizedAge - t0) / denom01));
                }

                float denom12 = max(0.0001, t2 - t1);
                return lerp(s1, s2, saturate((normalizedAge - t1) / denom12));
            }

            float Hash11(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float ResolveRandomInRange(float2 minMax, float salt)
            {
                float t = Hash11((_Seed * 17.0) + (_StartTime * 31.0) + salt);
                return lerp(minMax.x, minMax.y, t);
            }

            float3 ResolveStartSize()
            {
                float sx = ResolveRandomInRange(_SizeRangeX.xy, 11.0);
                float sy = ResolveRandomInRange(_SizeRangeY.xy, 23.0);
                float sz = ResolveRandomInRange(_SizeRangeZ.xy, 37.0);

                if (_UniformSize > 0.5)
                {
                    sy = sx;
                    sz = sx;
                }

                return float3(max(0.0001, sx), max(0.0001, sy), max(0.0001, sz));
            }

            float3 RotateX(float3 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float3(p.x, p.y * c - p.z * s, p.y * s + p.z * c);
            }

            float3 RotateY(float3 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
            }

            float3 RotateZ(float3 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float3(p.x * c - p.y * s, p.x * s + p.y * c, p.z);
            }

            float ResolveAgeSeconds()
            {
                return max(0.0, _Time.y - _StartTime - max(0.0, _InitialDelayRange.x));
            }

            float3 ResolveRotationAngles(float ageSeconds)
            {
                float sx = ResolveRandomInRange(_StartSpinRangeX.xy, 41.0);
                float sy = ResolveRandomInRange(_StartSpinRangeY.xy, 43.0);
                float sz = ResolveRandomInRange(_StartSpinRangeZ.xy, 47.0);

                float vx = ResolveRandomInRange(_SpinsPerSecondRangeX.xy, 53.0);
                float vy = ResolveRandomInRange(_SpinsPerSecondRangeY.xy, 59.0);
                float vz = ResolveRandomInRange(_SpinsPerSecondRangeZ.xy, 61.0);

                float float2pi = 6.28318530718;
                return float3(sx, sy, sz) + (float3(vx, vy, vz) * ageSeconds * float2pi);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float normalizedAge = ResolveNormalizedAge();
                float ageSeconds = ResolveAgeSeconds();
                float sizeScale = ResolveSizeScale(normalizedAge);
                float3 baseSize = ResolveStartSize();
                float3 scaledPosOS = IN.positionOS.xyz * (baseSize * sizeScale);

                if (_SpinParticles > 0.5)
                {
                    float3 angles = ResolveRotationAngles(ageSeconds);
                    scaledPosOS = RotateX(scaledPosOS, angles.x);
                    scaledPosOS = RotateY(scaledPosOS, angles.y);
                    scaledPosOS = RotateZ(scaledPosOS, angles.z);
                }

                float3 posWS = TransformObjectToWorld(scaledPosOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTexture) + _UVScroll.xy * _Time.y;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(posWS));
                return OUT;
            }

            half ResolveLifetimeAlpha()
            {
                half hasLifetime = step(0.5h, (half)_HasLifetime);
                if (hasLifetime < 0.5h)
                {
                    return 1.0h;
                }

                float age = _Time.y - _StartTime - max(0.0, _InitialDelayRange.x);
                if (age <= 0.0)
                {
                    return 0.0h;
                }

                float lifetime = max(0.0001, _LifetimeRange.x);
                if (age >= lifetime)
                {
                    return 0.0h;
                }

                half fadeInMul = 1.0h;
                if (_FadeIn > 0.5)
                {
                    float fadeInEnd = max(0.0001, _FadeInEndTime);
                    fadeInMul = saturate((half)(age / fadeInEnd));
                }

                half fadeOutMul = 1.0h;
                if (_Fadeout > 0.5)
                {
                    float fadeStart = clamp(_FadeoutStartTime, 0.0, lifetime);
                    float fadeDuration = max(0.0001, lifetime - fadeStart);
                    float fadeT = saturate((age - fadeStart) / fadeDuration);
                    fadeOutMul = 1.0h - (half)fadeT;
                }

                return saturate(fadeInMul * fadeOutMul);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvMain = IN.uv;
                float2 uvMask = TRANSFORM_TEX(IN.uv, _SpecMask) + _UVScroll.xy * _Time.y;

                half4 baseCol = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, uvMain);
                half4 maskTex = SAMPLE_TEXTURE2D(_SpecMask, sampler_SpecMask, uvMask);

                half mask = saturate(dot(maskTex.rgb, half3(0.3333h, 0.3333h, 0.3333h)) * _SpecMaskStrength);
                half lifeAlpha = ResolveLifetimeAlpha();

                half ndv = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                half fresnel = pow(1.0h - ndv, max(0.1h, _FresnelPower)) * _FresnelStrength;
                float3 reflDir = reflect(-normalize(IN.viewDirWS), normalize(IN.normalWS));
                half3 envRgb = SAMPLE_TEXTURECUBE(_EnvCube, sampler_EnvCube, reflDir).rgb * _EnvTint.rgb;

                half3 iceRgb = baseCol.rgb * _Tint.rgb;
                half3 edgeGlow = _EdgeColor.rgb * (fresnel * mask);
                half3 envSpec = envRgb * (mask * _EnvStrength);
                half3 finalRgb = iceRgb + edgeGlow + envSpec;

                half finalA = saturate(baseCol.a * _Tint.a * _Alpha * lifeAlpha);
                return half4(finalRgb, finalA);
            }
            ENDHLSL
        }
    }
}
