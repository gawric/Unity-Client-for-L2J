// URP: Lineage 2 Interlude SpriteEmitter Steam + dual texture smoke (lerp by COLOR.a, flipbook blend).
// Per-particle variation: pack a stable value in vertex COLOR (e.g. random per particle).
// Tune _SpawnUnitScale to match UE units vs Unity scene scale. MaxParticles / spawn rate = particle system, not this shader.
Shader "L2/Effects/SteamInterlude"
{
    Properties
    {
        [Header(Textures)]
        _MainTex("Smoke A (fx_m_t0035)", 2D) = "white" {}
        _Tex1("Smoke B", 2D) = "white" {}

        [Header(Smoke dual texture blend)]
        _Opacity("Opacity", Range(0, 2)) = 0.2
        _SmokeBrightness("Smoke Brightness", Range(0, 4)) = 1.5

        [Header(Lifetime)]
        _HasLifetime("Has Lifetime", Float) = 1
        _StartTime("Start Time", Float) = 0
        _InitialDelayRange("Initial Delay Range (Min,Max)", Vector) = (0.2, 0.2, 0, 0)
        _LifetimeRange("Lifetime Range (Min,Max)", Vector) = (1, 1, 0, 0)
        _FadeIn("Fade In Enabled", Float) = 1
        _FadeInEndTime("Fade In End Time", Float) = 0.15
        _Fadeout("Fade Out Enabled", Float) = 1
        _FadeoutStartTime("Fade Out Start Time", Float) = 1.0

        [Header(Seed)]
        _Seed("Seed", Float) = 0

        [Header(Spawn)]
        _StartLocationOffset("Start Location Offset", Vector) = (0, 0, 1, 0)
        _StartLocationRangeX("Start Location Range X (Min,Max)", Vector) = (-25, 25, 0, 0)
        _StartLocationRangeY("Start Location Range Y (Min,Max)", Vector) = (-25, 25, 0, 0)
        _StartLocationRangeZ("Start Location Range Z (Min,Max)", Vector) = (0, 0, 0, 0)
        _PolarAzimuthDeg("Polar Azimuth Deg (Min,Max)", Vector) = (0, 360, 0, 0)
        _PolarFromZDeg("Polar From +Z Deg (Min,Max)", Vector) = (85, 95, 0, 0)
        _PolarRadius("Polar Radius (Min,Max)", Vector) = (16, 16, 0, 0)
        _SpawnUnitScale("Spawn Unit Scale UE to Unity", Float) = 0.01

        [Header(Velocity OS)]
        _VelocityRangeX("Velocity X (Min,Max)", Vector) = (10, 10, 0, 0)
        _VelocityRangeY("Velocity Y (Min,Max)", Vector) = (10, 10, 0, 0)
        _VelocityRangeZ("Velocity Z (Min,Max)", Vector) = (-40, -20, 0, 0)
        _OwnerWorldPos("Owner World Pos (optional)", Vector) = (0, 0, 0, 0)
        _UseVelocityTowardOwner("Use Velocity Toward Owner", Float) = 0
        _TowardOwnerSpeed("Toward Owner Speed (Min,Max)", Vector) = (1, 5, 0, 0)
        _BillboardToOwner("Billboard To Camera", Float) = 1
        _BillboardWorldUp("Billboard World Up", Vector) = (0, 0, 1, 0)

        [Header(Size)]
        _SizeRangeX("Start Size X (Min,Max)", Vector) = (12, 12, 0, 0)
        _SizeRangeY("Start Size Y (Min,Max)", Vector) = (12, 12, 0, 0)
        _SizeRangeZ("Start Size Z (Min,Max)", Vector) = (12, 12, 0, 0)
        _UniformSize("Uniform Size", Float) = 1
        _UseSizeScale("Use Size Scale", Float) = 1
        _SizeScaleTimeEnd("Size Scale Relative Time End", Float) = 1.0
        _SizeScaleValueEnd("Size Scale Relative Size End", Float) = 1.2

        // Spin is applied before shader billboard. X/Y tilt the quad off the billboard plane; use Z for in-plane twist only.
        [Header(Spin)]
        _SpinParticles("Spin Particles", Float) = 1
        _SpinsPerSecondRangeX("Spin Per Sec X (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpinsPerSecondRangeY("Spin Per Sec Y (Min,Max)", Vector) = (0, 0, 0, 0)
        _SpinsPerSecondRangeZ("Spin Per Sec Z (Min,Max)", Vector) = (0.05, 0.1, 0, 0)
        _StartSpinRangeX("Start Spin X (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRangeY("Start Spin Y (Min,Max)", Vector) = (0, 0, 0, 0)
        _StartSpinRangeZ("Start Spin Z (Min,Max)", Vector) = (0, 0, 0, 0)

        [Header(Flipbook)]
        _TextureUSubdivisions("Texture U Subdivisions", Float) = 8
        _TextureVSubdivisions("Texture V Subdivisions", Float) = 8
        _SubdivisionStart("Subdivision Start", Float) = 4
        _SubdivisionEnd("Subdivision End", Float) = 15
        _BlendBetweenSubdivisions("Blend Between Subdivisions", Float) = 1
        _RandomizeSubdivisionPhase("Randomize Subdivision Phase", Float) = 1

        [Header(Color scale)]
        _Color0("Color Scale 0", Color) = (1, 1, 1, 1)
        _Color1("Color Scale 1", Color) = (1, 1, 1, 1)
        _ColorScaleTime1("Color Scale Relative Time 1", Float) = 1.0
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

        Blend SrcAlpha One
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
            #include "../L2SteamIncludes.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float normalizedAge : TEXCOORD1;
                nointerpolation float particleSeed : TEXCOORD2;
                nointerpolation float flipbookAge : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Tex1);
            SAMPLER(sampler_Tex1);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tex1_ST;
                float _Opacity;
                float _SmokeBrightness;
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
                float4 _PolarAzimuthDeg;
                float4 _PolarFromZDeg;
                float4 _PolarRadius;
                float _SpawnUnitScale;
                float4 _VelocityRangeX;
                float4 _VelocityRangeY;
                float4 _VelocityRangeZ;
                float4 _OwnerWorldPos;
                float _UseVelocityTowardOwner;
                float4 _TowardOwnerSpeed;
                float _BillboardToOwner;
                float4 _BillboardWorldUp;
                float4 _SizeRangeX;
                float4 _SizeRangeY;
                float4 _SizeRangeZ;
                float _UniformSize;
                float _UseSizeScale;
                float _SizeScaleTimeEnd;
                float _SizeScaleValueEnd;
                float _SpinParticles;
                float4 _SpinsPerSecondRangeX;
                float4 _SpinsPerSecondRangeY;
                float4 _SpinsPerSecondRangeZ;
                float4 _StartSpinRangeX;
                float4 _StartSpinRangeY;
                float4 _StartSpinRangeZ;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _BlendBetweenSubdivisions;
                float _RandomizeSubdivisionPhase;
                float4 _Color0;
                float4 _Color1;
                float _ColorScaleTime1;
            CBUFFER_END

            #include "../../../Common/L2FxInstancing.hlsl"

            float ParticleSeed(float4 vertexColor, float globalSeed)
            {
                return globalSeed + vertexColor.r * 31.917 + vertexColor.g * 11.713;
            }

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float pSeed = ParticleSeed(v.color, _Seed);
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float life = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                float nAge = saturate(age / max(life, 1e-4));

                float3 polar = L2Fx_SpawnOffsetPolarDegrees(
                    _PolarAzimuthDeg.xy,
                    _PolarFromZDeg.xy,
                    _PolarRadius.xy,
                    pSeed,
                    _StartTime);
                float3 boxOfs = L2Fx_SpawnOffsetBox(_StartLocationRangeX.xy, _StartLocationRangeY.xy, _StartLocationRangeZ.xy, pSeed, _StartTime);
                float3 spawnOfs = L2Fx_CombineSpawnOffsets(_StartLocationOffset.xyz * _SpawnUnitScale, polar * _SpawnUnitScale, boxOfs * _SpawnUnitScale);
                float3 particleCenterOS = spawnOfs;

                float3 baseSize = L2Fx_StartSize(_SizeRangeX.xy, _SizeRangeY.xy, _SizeRangeZ.xy, _UniformSize, pSeed, _StartTime);
                float sizeMul = L2Fx_SizeScaleImplicitStartOneKey(nAge, _UseSizeScale, _SizeScaleTimeEnd, _SizeScaleValueEnd);
                float3 quadOS = v.positionOS.xyz * baseSize * sizeMul;

                if (_SpinParticles > 0.5)
                {
                    float3 ang = L2Fx_RotationAngles(
                        age,
                        _StartSpinRangeX.xy,
                        _StartSpinRangeY.xy,
                        _StartSpinRangeZ.xy,
                        _SpinsPerSecondRangeX.xy,
                        _SpinsPerSecondRangeY.xy,
                        _SpinsPerSecondRangeZ.xy,
                        pSeed,
                        _StartTime);
                    quadOS = L2Fx_RotateX(quadOS, ang.x);
                    quadOS = L2Fx_RotateY(quadOS, ang.y);
                    quadOS = L2Fx_RotateZ(quadOS, ang.z);
                }

                float3 vel = L2Fx_VelocityRandomBox(_VelocityRangeX.xy, _VelocityRangeY.xy, _VelocityRangeZ.xy, pSeed, _StartTime);
                if (_UseVelocityTowardOwner > 0.5)
                {
                    float3 spawnWS = TransformObjectToWorld(particleCenterOS);
                    float3 own = _OwnerWorldPos.xyz;
                    vel = L2Fx_VelocityTowardOwner(spawnWS, own, _TowardOwnerSpeed.xy, pSeed, _StartTime);
                }

                particleCenterOS += L2Fx_DisplacementFromVelocity(vel * _SpawnUnitScale, age);

                float3 posWS;
                if (_BillboardToOwner > 0.5)
                {
                    float3 centerWS = TransformObjectToWorld(particleCenterOS);
                    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
                    toCamera = dot(toCamera, toCamera) > 1e-8 ? normalize(toCamera) : float3(0.0, 1.0, 0.0);

                    float3 upRef = _BillboardWorldUp.xyz;
                    upRef = dot(upRef, upRef) > 1e-8 ? normalize(upRef) : float3(0.0, 0.0, 1.0);
                    upRef = abs(dot(upRef, toCamera)) > 0.98 ? float3(0.0, 1.0, 0.0) : upRef;

                    float3 rightWS = normalize(cross(upRef, toCamera));
                    float3 upWS = normalize(cross(toCamera, rightWS));
                    float3 objectScale = L2Fx_ObjectWorldScale();
                    posWS = centerWS
                        + rightWS * (quadOS.x * objectScale.x)
                        + upWS * (quadOS.y * objectScale.y)
                        + toCamera * (quadOS.z * objectScale.z);
                }
                else
                {
                    posWS = TransformObjectToWorld(particleCenterOS + quadOS);
                }
                o.positionHCS = TransformWorldToHClip(posWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.normalizedAge = nAge;
                o.particleSeed = pSeed;
                o.flipbookAge = nAge;
                if (_RandomizeSubdivisionPhase > 0.5)
                {
                    float phase = L2Fx_RandomRange(float2(0.0, 1.0), pSeed, _StartTime, 131.0);
                    o.flipbookAge = frac(o.flipbookAge + phase);
                }
                return o;
            }

            half4 frag(Varyings v) : SV_Target
            {
                float pSeed = v.particleSeed;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);
                float life = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);

                half lifeA = (half)L2Fx_LifetimeAlpha(
                    _Time.y,
                    _HasLifetime,
                    _StartTime,
                    delay,
                    life,
                    _FadeIn,
                    _FadeInEndTime,
                    _Fadeout,
                    _FadeoutStartTime);

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                float flipbookAge = v.flipbookAge;

                half4 tex0;
                half4 tex1Mixed;
                if (_BlendBetweenSubdivisions > 0.5)
                {
                    float2 uvA, uvB;
                    float fBlend;
                    L2Fx_FlipbookAtlasUVBlend(v.uv, flipbookAge, uSub, vSub, s0, s1, uvA, uvB, fBlend);
                    half4 t0a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvA);
                    half4 t0b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB);
                    half4 t1a = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, uvA);
                    half4 t1b = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, uvB);
                    tex0 = lerp(t0a, t0b, (half)fBlend);
                    tex1Mixed = lerp(t1a, t1b, (half)fBlend);
                }
                else
                {
                    int fi = L2Fx_FlipbookFrameIndex(flipbookAge, s0, s1);
                    float2 atlasUv = L2Fx_FlipbookAtlasUV(v.uv, fi, uSub, vSub);
                    tex0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUv);
                    tex1Mixed = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, atlasUv);
                }

                // Steam port: lerp(texA, texB, vertex alpha); RGB tinted by vertex color.
                half4 mixed = lerp(tex0, tex1Mixed, (half)v.color.a);
                half3 cs = L2Fx_ColorScaleTwoKeys(v.normalizedAge, _Color0, _Color1, _ColorScaleTime1).rgb;
                half3 rgb = mixed.rgb * v.color.rgb * cs * (half)_SmokeBrightness;
                half a = mixed.a * (half)_Opacity * lifeA;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
