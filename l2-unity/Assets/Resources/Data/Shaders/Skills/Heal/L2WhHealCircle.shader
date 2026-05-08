Shader "L2/Effects/Heal/WhHealCircle_URP"
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

        [Header(Size And Motion)]
        _UseDirectionAs("Emitter Mode: 0 Sprite, 3 Beam", Float) = 3
        _ColorIntensity("Color Intensity", Range(0, 8)) = 1
        
        [Header(Beam Settings)]
        _BeamEdgeFeather("Beam Edge Feather", Range(0, 0.5)) = 0.1
        _BeamEndFeather("Beam End Feather", Range(0, 0.5)) = 0.1
        _BeamCoreStrength("Beam Core Strength", Range(0, 4)) = 1
        _BeamCorePower("Beam Core Power", Range(0.1, 8)) = 2

        [Header(Color)]
        _UseColorScale("Use Color Scale", Float) = 1
        _ColorScale0Color("Color 0", Color) = (1, 1, 1, 1)
        _ColorScale1Color("Color 1", Color) = (0.5, 0.5, 0.5, 1)
        _ColorScale2Color("Color 2", Color) = (1, 1, 1, 1)
        _ColorScale0Time("Time 0", Float) = 0
        _ColorScale1Time("Time 1", Float) = 0.5
        _ColorScale2Time("Time 2", Float) = 1
        _ColorScaleRepeats("Repeats", Float) = 0
        
        _ColorMultiplierRangeR("R Multiplier", Vector) = (1,1,0,0)
        _ColorMultiplierRangeG("G Multiplier", Vector) = (1,1,0,0)
        _ColorMultiplierRangeB("B Multiplier", Vector) = (1,1,0,0)

        _UseRgbAsAlpha("Use RGB As Alpha", Float) = 0
        _RgbAlphaThreshold("Alpha Threshold", Range(0, 1)) = 0
        _AlphaPower("Alpha Power", Range(0.1, 8)) = 1
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
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // �������� ������� ����������. 
            // �����: ���� ���� ��������, Unity ������ ������ "cannot open source file"
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
                float lifeAlpha : TEXCOORD3;
                float particleSeed : TEXCOORD4;
            };

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexture_ST;
                float _Alpha;
                float _StartTime;
                float _HasLifetime;
                float _Seed;
                float4 _InitialDelayRange;
                float4 _LifetimeRange;
                float _FadeIn;
                float _FadeInEndTime;
                float _Fadeout;
                float _FadeoutStartTime;
                float _UseDirectionAs;
                float _ColorIntensity;
                float _BeamEdgeFeather;
                float _BeamEndFeather;
                float _BeamCoreStrength;
                float _BeamCorePower;
                float _UseColorScale;
                float _ColorScale0Time;
                float _ColorScale1Time;
                float _ColorScale2Time;
                float _ColorScaleRepeats;
                float4 _ColorScale0Color;
                float4 _ColorScale1Color;
                float4 _ColorScale2Color;
                float4 _ColorMultiplierRangeR;
                float4 _ColorMultiplierRangeG;
                float4 _ColorMultiplierRangeB;
                float _UseRgbAsAlpha;
                float _RgbAlphaThreshold;
                float _AlphaPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // ������ ������� ����� ������� (��� �������� ������������)
                float seed = _Seed + IN.color.r * 37.13;
                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, seed, _StartTime, 3.0);
                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, seed, _StartTime, 7.0);
                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);
                
                // ������� ���������: Object Space -> Clip Space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                // UV � ������ Tiling/Offset � ���������
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTexture);
                
                OUT.color = IN.color;
                OUT.normalizedAge = saturate(age / lifetime);
                OUT.particleSeed = seed;
                
                // ������� �� ����� ���������� ��� ���������
                OUT.lifeAlpha = L2Fx_LifetimeAlpha(_Time.y, _HasLifetime, _StartTime, delay, lifetime, 
                                                   _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ������ ��������
                half4 tex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv);
                
                // �������� ����� (��� � L2)
                half3 colorScale = (half3)L2Fx_ColorScaleThreeKeysRepeating(
                    IN.normalizedAge, _UseColorScale, _ColorScale0Time, _ColorScale1Time, _ColorScale2Time,
                    _ColorScaleRepeats, 20.0, _ColorScale0Color.rgb, _ColorScale1Color.rgb, _ColorScale2Color.rgb);
                
                // Random per-channel multiplier; (0,0) min/max => UE default white (see L2WhHealTA).
                float cr = L2Fx_RandomRange(_ColorMultiplierRangeR.xy, IN.particleSeed, _StartTime, 149.0);
                float cg = L2Fx_RandomRange(_ColorMultiplierRangeG.xy, IN.particleSeed, _StartTime, 151.0);
                float cb = L2Fx_RandomRange(_ColorMultiplierRangeB.xy, IN.particleSeed, _StartTime, 157.0);
                if (abs(_ColorMultiplierRangeR.x) < 1e-6 && abs(_ColorMultiplierRangeR.y) < 1e-6) cr = 1.0;
                if (abs(_ColorMultiplierRangeG.x) < 1e-6 && abs(_ColorMultiplierRangeG.y) < 1e-6) cg = 1.0;
                if (abs(_ColorMultiplierRangeB.x) < 1e-6 && abs(_ColorMultiplierRangeB.y) < 1e-6) cb = 1.0;
                half3 colorMul = half3(cr, cg, cb);
                
                // Respect UseRgbAsAlpha for smoke-like textures that store mask in RGB.
                half rgbAlpha = dot(tex.rgb, half3(0.299h, 0.587h, 0.114h));
                rgbAlpha = saturate((rgbAlpha - (half)_RgbAlphaThreshold) / max(1.0h - (half)_RgbAlphaThreshold, 1e-4h));
                rgbAlpha = pow(rgbAlpha, max((half)_AlphaPower, 0.1h));
                half sourceAlpha = lerp(tex.a, rgbAlpha, saturate((half)_UseRgbAsAlpha));

                half rgbMask = lerp(1.0h, sourceAlpha, saturate((half)_UseRgbAsAlpha));
                half3 rgb = tex.rgb * rgbMask * IN.color.rgb * colorScale * colorMul * (half)_ColorIntensity;
                
                // ����� �����
                half alpha = sourceAlpha * IN.color.a * (half)_Alpha * (half)IN.lifeAlpha;

                // ������ ���� (Beam)
                if (_UseDirectionAs > 2.5)
                {
                    // �������� ����� �� ����������� (X)
                    half edgeMask = smoothstep(0.0h, (half)_BeamEdgeFeather, IN.uv.x) * smoothstep(0.0h, (half)_BeamEdgeFeather, 1.0h - IN.uv.x);
                    
                    // �������� ����� �� ��������� (Y)
                    half endMask = smoothstep(0.0h, (half)_BeamEndFeather, IN.uv.y) * smoothstep(0.0h, (half)_BeamEndFeather, 1.0h - IN.uv.y);
                    
                    // ����������� ���� (Core)
                    half core = pow(saturate(1.0h - abs(IN.uv.x * 2.0h - 1.0h)), max((half)_BeamCorePower, 0.1h));
                    
                    rgb *= (1.0h + core * (half)_BeamCoreStrength);
                    alpha *= (edgeMask * endMask);
                }

                // �������� ��������� ������� ��� ������������������
                clip(alpha - 0.001h);
                
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}