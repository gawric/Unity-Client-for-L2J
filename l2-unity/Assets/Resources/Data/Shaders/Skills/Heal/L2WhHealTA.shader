Shader "L2/Effects/Heal/WhHealTA"

{

    Properties

    {

        _MainTexture("Main Texture", 2D) = "white" {}

        _Alpha("Opacity", Range(0, 2)) = 0.6

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 5

        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10



        [Header(Lifetime)]

        _HasLifetime("Has Lifetime", Float) = 1

        _StartTime("Start Time", Float) = 0

        _InitialDelayRange("Initial Delay Range", Vector) = (0, 0, 0, 0)

        _LifetimeRange("Lifetime Range", Vector) = (2, 2.5, 0, 0)

        _FadeIn("Fade In", Float) = 1

        _FadeInEndTime("Fade In End Time", Float) = 0.63

        _Fadeout("Fade Out", Float) = 1

        _FadeoutStartTime("Fade Out Start Time", Float) = 1.5

        _Seed("Seed", Float) = 0



        [Header(Spawn)]

        _StartLocationOffset("Start Location Offset", Vector) = (0, 0, 0, 0)

        _StartLocationRangeX("Start Location Range X", Vector) = (0, 0, 0, 0)

        _StartLocationRangeY("Start Location Range Y", Vector) = (0, 0, 0, 0)

        _StartLocationRangeZ("Start Location Range Z", Vector) = (0, 0, 0, 0)

        _StartLocationPolarRangeX("Polar Azimuth", Vector) = (0, 0, 0, 0)

        _StartLocationPolarRangeY("Polar From Up", Vector) = (0, 0, 0, 0)

        _StartLocationPolarRangeZ("Polar Radius", Vector) = (0, 0, 0, 0)

        _StartVelocityRangeX("Velocity X", Vector) = (0, 0, 0, 0)

        _StartVelocityRangeY("Velocity Y", Vector) = (0, 0, 0, 0)

        _StartVelocityRangeZ("Velocity Z", Vector) = (0, 0, 0, 0)

        _VelocityLossRangeX("Velocity Loss X", Vector) = (0, 0, 0, 0)

        _VelocityLossRangeY("Velocity Loss Y", Vector) = (0, 0, 0, 0)

        _VelocityLossRangeZ("Velocity Loss Z", Vector) = (0, 0, 0, 0)



        [Header(Size And Motion)]

        _UseDirectionAs("Emitter Mode: 0 Sprite, 3 Beam", Float) = 0

        _Billboard("Billboard", Float) = 1

        _SpinParticles("Spin Particles", Float) = 1

        _UniformSize("Uniform Size", Float) = 1

        _UseSizeScale("Use Size Scale", Float) = 1

        _SizeRangeX("Start Size X", Vector) = (3, 5, 0, 0)

        _SizeRangeY("Start Size Y", Vector) = (3, 5, 0, 0)

        _SizeRangeZ("Start Size Z", Vector) = (3, 5, 0, 0)

        _BeamEndOffset("Beam End Offset", Vector) = (0, -190, 0, 0)

        _SizeScale0("Size Scale 0", Vector) = (0.07, 3, 0, 0)

        _SizeScale1("Size Scale 1", Vector) = (0.24, 6, 0, 0)

        _SizeScale2("Size Scale 2", Vector) = (0.52, 8.5, 0, 0)

        _SizeScale3("Size Scale 3", Vector) = (1, 10, 0, 0)

        _StartSpinRangeX("Start Spin X", Vector) = (0, 0, 0, 0)

        _StartSpinRangeY("Start Spin Y", Vector) = (0, 0, 0, 0)

        _StartSpinRangeZ("Start Spin Z", Vector) = (0, 0, 0, 0)

        _SpinsPerSecondRangeX("Spins Per Second X", Vector) = (0, 0, 0, 0)

        _SpinsPerSecondRangeY("Spins Per Second Y", Vector) = (0, 0, 0, 0)

        _SpinsPerSecondRangeZ("Spins Per Second Z", Vector) = (-0.2, 0.2, 0, 0)



        [Header(Color)]

        _UseColorScale("Use Color Scale", Float) = 1

        _ColorScale0Time("Color 0 Time", Float) = 0

        _ColorScale1Time("Color 1 Time", Float) = 0.539286

        _ColorScale2Time("Color 2 Time", Float) = 1

        _ColorScaleRepeats("Color Scale Repeats", Float) = 0

        _ColorScale3Time("Legacy Color Repeats", Float) = 20

        _ColorScale0Color("Color 0", Color) = (1, 1, 1, 1)

        _ColorScale1Color("Color 1", Color) = (0.5, 0.5, 0.5, 1)

        _ColorScale2Color("Color 2", Color) = (1, 1, 1, 1)

        _ColorMultiplierRangeR("Color Multiplier R", Vector) = (1, 1, 0, 0)

        _ColorMultiplierRangeG("Color Multiplier G", Vector) = (1, 1, 0, 0)

        _ColorMultiplierRangeB("Color Multiplier B", Vector) = (1, 1, 0, 0)

        _ColorIntensity("Color Intensity", Range(0, 8)) = 1

        _UseRgbAsAlpha("Use RGB Luminance As Alpha", Float) = 0

        _RgbAlphaThreshold("RGB Alpha Threshold", Range(0, 1)) = 0

        _AlphaPower("Alpha Power", Range(0.1, 8)) = 1

        _BeamEdgeFeather("Beam Edge Feather", Range(0, 0.5)) = 0

        _BeamEndFeather("Beam End Feather", Range(0, 0.5)) = 0

        _BeamCoreStrength("Beam Core Strength", Range(0, 4)) = 0

        _BeamCorePower("Beam Core Power", Range(0.1, 8)) = 2

        _BeamFootGlowStrength("Beam Foot Glow Strength", Range(0, 2)) = 0

        _BeamFootGlowPower("Beam Foot Glow Power", Range(0.2, 4)) = 1.15

        _BeamFootWarmTint("Beam Foot Warm Tint Add", Color) = (0.28, 0.1, 0.07, 0)



        [Header(Flipbook)]

        _TextureUSubdivisions("Texture U Subdivisions", Float) = 1

        _TextureVSubdivisions("Texture V Subdivisions", Float) = 1

        _SubdivisionStart("Subdivision Start", Float) = 0

        _SubdivisionEnd("Subdivision End", Float) = 0

    }



    SubShader

    {

        Tags

        {

            "RenderPipeline"="UniversalPipeline"

            "RenderType"="Transparent"

            "Queue"="Transparent"

            "IgnoreProjector"="True"

        }



        Blend [_SrcBlend] [_DstBlend]

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

            #include "../Common/L2FxParticleAnim.hlsl"

            #include "../Common/L2FxEmitterSpawn.hlsl"

            #include "../Common/L2FxFlipbook.hlsl"

            #include "../Common/L2FxMeshEmitter.hlsl"



            struct Attributes

            {

                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                float4 color : COLOR;

            };



            struct Varyings

            {

                float4 positionHCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                half4 color : COLOR;

                float normalizedAge : TEXCOORD1;

                nointerpolation float particleSeed : TEXCOORD2;

                nointerpolation float lifeAlpha : TEXCOORD3;

            };



            TEXTURE2D(_MainTexture);

            SAMPLER(sampler_MainTexture);



            CBUFFER_START(UnityPerMaterial)

                float4 _MainTexture_ST;

                float _Alpha;

                float _SrcBlend;

                float _DstBlend;

                float _HasLifetime;

                float _StartTime;

                float4 _InitialDelayRange;

                float4 _LifetimeRange;

                float _FadeIn;

                float _FadeInEndTime;

                float _Fadeout;

                float _FadeoutStartTime;

                float _Seed;

                float4 _StartLocationOffset;

                float4 _StartLocationRangeX;

                float4 _StartLocationRangeY;

                float4 _StartLocationRangeZ;

                float4 _StartLocationPolarRangeX;

                float4 _StartLocationPolarRangeY;

                float4 _StartLocationPolarRangeZ;

                float4 _StartVelocityRangeX;

                float4 _StartVelocityRangeY;

                float4 _StartVelocityRangeZ;

                float4 _VelocityLossRangeX;

                float4 _VelocityLossRangeY;

                float4 _VelocityLossRangeZ;

                float _UseDirectionAs;

                float _Billboard;

                float _SpinParticles;

                float _UniformSize;

                float _UseSizeScale;

                float4 _SizeRangeX;

                float4 _SizeRangeY;

                float4 _SizeRangeZ;

                float4 _BeamEndOffset;

                float4 _SizeScale0;

                float4 _SizeScale1;

                float4 _SizeScale2;

                float4 _SizeScale3;

                float4 _StartSpinRangeX;

                float4 _StartSpinRangeY;

                float4 _StartSpinRangeZ;

                float4 _SpinsPerSecondRangeX;

                float4 _SpinsPerSecondRangeY;

                float4 _SpinsPerSecondRangeZ;

                float _UseColorScale;

                float _ColorScale0Time;

                float _ColorScale1Time;

                float _ColorScale2Time;

                float _ColorScaleRepeats;

                float _ColorScale3Time;

                float4 _ColorScale0Color;

                float4 _ColorScale1Color;

                float4 _ColorScale2Color;

                float4 _ColorMultiplierRangeR;

                float4 _ColorMultiplierRangeG;

                float4 _ColorMultiplierRangeB;

                float _ColorIntensity;

                float _UseRgbAsAlpha;

                float _RgbAlphaThreshold;

                float _AlphaPower;

                float _BeamEdgeFeather;

                float _BeamEndFeather;

                float _BeamCoreStrength;

                float _BeamCorePower;

                float _BeamFootGlowStrength;

                float _BeamFootGlowPower;

                float4 _BeamFootWarmTint;

                float _TextureUSubdivisions;

                float _TextureVSubdivisions;

                float _SubdivisionStart;

                float _SubdivisionEnd;

            CBUFFER_END



            float L2WhHealTA_SizeScale4(float normalizedAge)

            {

                if (_UseSizeScale < 0.5)

                {

                    return 1.0;

                }



                float t0 = saturate(_SizeScale0.x);

                float t1 = max(t0 + 1e-4, saturate(_SizeScale1.x));

                float t2 = max(t1 + 1e-4, saturate(_SizeScale2.x));

                float t3 = max(t2 + 1e-4, saturate(_SizeScale3.x));



                if (normalizedAge <= t1)

                {

                    return lerp(_SizeScale0.y, _SizeScale1.y, saturate((normalizedAge - t0) / (t1 - t0)));

                }

                if (normalizedAge <= t2)

                {

                    return lerp(_SizeScale1.y, _SizeScale2.y, saturate((normalizedAge - t1) / (t2 - t1)));

                }

                return lerp(_SizeScale2.y, _SizeScale3.y, saturate((normalizedAge - t2) / (t3 - t2)));

            }



            float L2WhHealTA_DampedDistance(float velocity, float loss, float age)

            {

                loss = max(0.0, loss);

                if (loss < 1e-4)

                {

                    return velocity * age;

                }



                return velocity * (1.0 - exp(-loss * age)) / loss;

            }



            float3 L2WhHealTA_VelocityDisplacement(float3 velocity, float3 velocityLoss, float age)

            {

                return float3(

                    L2WhHealTA_DampedDistance(velocity.x, velocityLoss.x, age),

                    L2WhHealTA_DampedDistance(velocity.y, velocityLoss.y, age),

                    L2WhHealTA_DampedDistance(velocity.z, velocityLoss.z, age));

            }



            float3 L2WhHealTA_ColorMultiplier(float seed)

            {

                float r = L2Fx_RandomRange(_ColorMultiplierRangeR.xy, seed, _StartTime, 149.0);

                float g = L2Fx_RandomRange(_ColorMultiplierRangeG.xy, seed, _StartTime, 151.0);

                float b = L2Fx_RandomRange(_ColorMultiplierRangeB.xy, seed, _StartTime, 157.0);

                // UE omit ColorMultiplierRange => white (1,1,1). Serialized Unity mats often have min=max=0 → lerp=0;

                // with Blend One One (Brighten) that adds zero light and the mesh looks "textured but invisible".

                if (abs(_ColorMultiplierRangeR.x) < 1e-6 && abs(_ColorMultiplierRangeR.y) < 1e-6)

                    r = 1.0;

                if (abs(_ColorMultiplierRangeG.x) < 1e-6 && abs(_ColorMultiplierRangeG.y) < 1e-6)

                    g = 1.0;

                if (abs(_ColorMultiplierRangeB.x) < 1e-6 && abs(_ColorMultiplierRangeB.y) < 1e-6)

                    b = 1.0;

                return float3(r, g, b);

            }



            float3 L2WhHealTA_ObjectScale()

            {

                return float3(

                    length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20)),

                    length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21)),

                    length(float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22)));

            }



            float3 L2WhHealTA_UnscaledObjectToWorld(float3 positionOS)

            {

                float3 originWS = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);

                float3 axisX = normalize(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));

                float3 axisY = normalize(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                float3 axisZ = normalize(float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22));

                return originWS + axisX * positionOS.x + axisY * positionOS.y + axisZ * positionOS.z;

            }



            Varyings vert(Attributes IN)

            {

                Varyings OUT;



                float particleSeed = _Seed + IN.color.r * 37.13 + IN.color.g * 19.71 + IN.color.b * 11.17;

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, particleSeed, _StartTime, 3.0);

                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, particleSeed, _StartTime, 7.0);

                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);

                float normalizedAge = saturate(age / lifetime);

                bool isBeam = _UseDirectionAs > 2.5;



                float3 polar = L2Fx_SpawnOffsetPolarDegrees(

                    _StartLocationPolarRangeX.xy,

                    _StartLocationPolarRangeY.xy,

                    _StartLocationPolarRangeZ.xy,

                    particleSeed,

                    _StartTime);

                float3 boxOffset = L2Fx_SpawnOffsetBox(

                    _StartLocationRangeX.xy,

                    _StartLocationRangeY.xy,

                    _StartLocationRangeZ.xy,

                    particleSeed,

                    _StartTime);

                float3 centerOS = _StartLocationOffset.xyz + polar + boxOffset;



                float3 velocity = L2Fx_VelocityRandomBox(

                    _StartVelocityRangeX.xy,

                    _StartVelocityRangeY.xy,

                    _StartVelocityRangeZ.xy,

                    particleSeed,

                    _StartTime);

                float3 velocityLoss = float3(

                    L2Fx_RandomRange(_VelocityLossRangeX.xy, particleSeed, _StartTime, 163.0),

                    L2Fx_RandomRange(_VelocityLossRangeY.xy, particleSeed, _StartTime, 167.0),

                    L2Fx_RandomRange(_VelocityLossRangeZ.xy, particleSeed, _StartTime, 173.0));

                centerOS += L2WhHealTA_VelocityDisplacement(velocity, velocityLoss, age);



                float3 size = L2Fx_StartSize(_SizeRangeX.xy, _SizeRangeY.xy, _SizeRangeZ.xy, _UniformSize, particleSeed, _StartTime);

                if (!isBeam)

                {

                    size *= L2WhHealTA_SizeScale4(normalizedAge);

                }



                float3 quadOS;

                if (isBeam)

                {

                    float beamLength = max(1e-4, length(_BeamEndOffset.xyz));

                    float beamWidth = max(1e-4, size.x);

                    quadOS = float3(IN.positionOS.x * beamWidth, IN.positionOS.y * beamLength, 0.0);

                }

                else

                {

                    quadOS = IN.positionOS.xyz * size;

                    if (_SpinParticles > 0.5)

                    {

                        float3 angles = L2Fx_RotationAngles(

                            age,

                            _StartSpinRangeX.xy,

                            _StartSpinRangeY.xy,

                            _StartSpinRangeZ.xy,

                            _SpinsPerSecondRangeX.xy,

                            _SpinsPerSecondRangeY.xy,

                            _SpinsPerSecondRangeZ.xy,

                            particleSeed,

                            _StartTime);

                        quadOS = L2Fx_RotateX(quadOS, angles.x);

                        quadOS = L2Fx_RotateY(quadOS, angles.y);

                        quadOS = L2Fx_RotateZ(quadOS, angles.z);

                    }

                }



                float3 objectScale = L2WhHealTA_ObjectScale();

                float3 centerWS = L2WhHealTA_UnscaledObjectToWorld(centerOS);

                float3 posWS;

                if (_Billboard > 0.5)

                {

                    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;

                    float3 upRef = float3(0.0, 1.0, 0.0);



                    toCamera = dot(toCamera, toCamera) > 1e-8 ? normalize(toCamera) : float3(0.0, 0.0, 1.0);

                    float3 rightWS = cross(upRef, toCamera);

                    float rightLenSq = dot(rightWS, rightWS);

                    if (rightLenSq < 1e-10)

                    {

                        upRef = float3(0.0, 0.0, 1.0);

                        rightWS = cross(upRef, toCamera);

                        rightLenSq = dot(rightWS, rightWS);

                    }

                    rightWS = rightLenSq > 1e-10 ? rightWS * rsqrt(rightLenSq) : float3(1.0, 0.0, 0.0);

                    float3 upWS = cross(toCamera, rightWS);

                    upWS = dot(upWS, upWS) > 1e-10 ? normalize(upWS) : float3(0.0, 1.0, 0.0);

                    float beamTopAlign = isBeam ? (length(_BeamEndOffset.xyz) * 0.5 * objectScale.y) : 0.0;



                    posWS = centerWS

                        + rightWS * (quadOS.x * objectScale.x)

                        + upWS * ((quadOS.y * objectScale.y) - beamTopAlign)

                        + toCamera * (quadOS.z * objectScale.z);

                }

                else

                {

                    posWS = TransformObjectToWorld(centerOS + quadOS);

                }



                OUT.positionHCS = TransformWorldToHClip(posWS);



                float2 uv = TRANSFORM_TEX(IN.uv, _MainTexture);

                if (_TextureUSubdivisions > 1.5 || _TextureVSubdivisions > 1.5)

                {

                    uv = L2Fx_FlipbookSubDivisionUV_Random(

                        IN.uv,

                        particleSeed,

                        _StartTime,

                        max(1, (int)_TextureUSubdivisions),

                        max(1, (int)_TextureVSubdivisions),

                        (int)_SubdivisionStart,

                        (int)_SubdivisionEnd,

                        181.0);

                    uv = TRANSFORM_TEX(uv, _MainTexture);

                }



                OUT.uv = uv;

                OUT.color = (half4)IN.color;

                OUT.normalizedAge = normalizedAge;

                OUT.particleSeed = particleSeed;

                OUT.lifeAlpha = L2Fx_LifetimeAlpha(

                    _Time.y,

                    _HasLifetime,

                    _StartTime,

                    delay,

                    lifetime,

                    _FadeIn,

                    _FadeInEndTime,

                    _Fadeout,

                    _FadeoutStartTime);



                return OUT;

            }



            half4 frag(Varyings IN) : SV_Target

            {

                half4 tex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv);

                half3 colorScale = (half3)L2Fx_ColorScaleThreeKeysRepeating(

                    IN.normalizedAge,

                    _UseColorScale,

                    _ColorScale0Time,

                    _ColorScale1Time,

                    _ColorScale2Time,

                    _ColorScaleRepeats,

                    _ColorScale3Time,

                    _ColorScale0Color.rgb,

                    _ColorScale1Color.rgb,

                    _ColorScale2Color.rgb);

                half3 colorMul = (half3)L2WhHealTA_ColorMultiplier(IN.particleSeed);



                half rgbAlpha = dot(tex.rgb, half3(0.299h, 0.587h, 0.114h));

                rgbAlpha = saturate((rgbAlpha - (half)_RgbAlphaThreshold) / max(1.0h - (half)_RgbAlphaThreshold, 1e-4h));

                rgbAlpha = pow(rgbAlpha, max((half)_AlphaPower, 0.1h));

                half sourceAlpha = lerp(tex.a, rgbAlpha, saturate((half)_UseRgbAsAlpha));



                half rgbMask = lerp(1.0h, sourceAlpha, saturate((half)_UseRgbAsAlpha));

                half3 rgb = tex.rgb * rgbMask * IN.color.rgb * colorScale * colorMul * (half)_ColorIntensity;

                half alpha = sourceAlpha * IN.color.a * (half)_Alpha * (half)IN.lifeAlpha;

                if (_UseDirectionAs > 2.5)

                {

                    half edgeFeather = max((half)_BeamEdgeFeather, 1e-4h);

                    half endFeather = max((half)_BeamEndFeather, 1e-4h);

                    half edgeMask = smoothstep(0.0h, edgeFeather, (half)IN.uv.x)

                        * smoothstep(0.0h, edgeFeather, 1.0h - (half)IN.uv.x);

                    half endMask = smoothstep(0.0h, endFeather, (half)IN.uv.y)

                        * smoothstep(0.0h, endFeather, 1.0h - (half)IN.uv.y);

                    half coreMask = pow(saturate(1.0h - abs((half)IN.uv.x * 2.0h - 1.0h)), max((half)_BeamCorePower, 0.1h));

                    rgb *= 1.0h + coreMask * (half)_BeamCoreStrength;

                    half footGlow = pow(saturate((half)IN.uv.y), max((half)_BeamFootGlowPower, 0.1h));

                    rgb += footGlow * (half3)_BeamFootWarmTint.rgb * (half)_BeamFootGlowStrength;

                    alpha *= edgeMask * endMask;

                }

                clip(alpha - 0.001h);

                return half4(rgb, alpha);

            }

            ENDHLSL

        }

    }

}