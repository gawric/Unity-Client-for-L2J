// Calibration ONLY: it_healing_potion_ta / m_u004_b SpriteEmitter2
// Self-contained: flipbook + size. No L2Fx library includes.
// DrawScale (.uc): ONLY root Transform (DrawScale=0.05). NOT in shader/material.
// Child quads: localScale=(1,1,1).
// final(m) = StartSize(UU) * 0.01 * K_world * lossyScale(root DrawScale)
Shader "L2/Effects/Calib/HealingPotionTaSpriteEmitter2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _L2FxWorldCalibration ("World Calibration K", Float) = 0.7
        _SizeRange ("Start Size UU Min Max", Vector) = (5.5, 5.5, 0, 0)

        _TextureUSubdivisions ("Atlas U Cells", Float) = 4
        _TextureVSubdivisions ("Atlas V Cells", Float) = 4
        _SubdivisionStart ("Subdivision Start", Float) = 4
        _SubdivisionEnd ("Subdivision End", Float) = 7
        _TestFlipbookAge ("Flipbook Age 0-1", Range(0, 1)) = 0

        _RgbBoost ("RGB Boost", Range(0, 16)) = 7
        _LumaAlphaFloor ("Luma Alpha Floor", Range(0, 0.25)) = 0.003
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "HealingPotionTaCalibSE2"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _L2FxWorldCalibration;
                float4 _SizeRange;
                float _TextureUSubdivisions;
                float _TextureVSubdivisions;
                float _SubdivisionStart;
                float _SubdivisionEnd;
                float _TestFlipbookAge;
                float _RgbBoost;
                float _LumaAlphaFloor;
            CBUFFER_END

            float2 AtlasUV(float2 uv01, int cellIndex, int uSub, int vSub)
            {
                uSub = max(uSub, 1);
                vSub = max(vSub, 1);
                int tiles = uSub * vSub;
                cellIndex = clamp(cellIndex, 0, tiles - 1);
                float du = 1.0 / (float)uSub;
                float dv = 1.0 / (float)vSub;
                int u = cellIndex / vSub;
                int v = (vSub - 1) - (cellIndex % vSub);
                float2 cellSize = float2(du, dv);
                float2 origin = float2((float)u * du, (float)v * dv);
                return origin + saturate(uv01) * cellSize;
            }

            void ResolveBlendFrames(
                float ageNorm,
                int subStart,
                int subEnd,
                out int frameA,
                out int frameB,
                out float blend)
            {
                int span = max(subEnd - subStart, 1);
                float t = saturate(ageNorm);
                float f = (float)subStart + t * (float)span;
                frameA = (int)floor(f);
                frameB = frameA + 1;
                frameA = clamp(frameA, subStart, subEnd);
                frameB = clamp(frameB, subStart, subEnd);
                blend = saturate(f - (float)frameA);
            }

            float ResolveStartSizeUU()
            {
                float minUU = _SizeRange.x;
                float maxUU = _SizeRange.y;
                if (maxUU < minUU)
                {
                    float t = minUU;
                    minUU = maxUU;
                    maxUU = t;
                }
                return (minUU + maxUU) * 0.5;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvAtlasA : TEXCOORD0;
                float2 uvAtlasB : TEXCOORD1;
                float flipBlend : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float k = _L2FxWorldCalibration > 0.0 ? _L2FxWorldCalibration : 1.0;
                float sizeM = ResolveStartSizeUU() * 0.01 * k;
                float3 quadOS = IN.positionOS.xyz * sizeM;
                OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));

                int uSub = max(1, (int)_TextureUSubdivisions);
                int vSub = max(1, (int)_TextureVSubdivisions);
                int s0 = (int)_SubdivisionStart;
                int s1 = (int)_SubdivisionEnd;
                int fa;
                int fb;
                float blend;
                ResolveBlendFrames(_TestFlipbookAge, s0, s1, fa, fb, blend);
                OUT.uvAtlasA = AtlasUV(IN.uv, fa, uSub, vSub);
                OUT.uvAtlasB = AtlasUV(IN.uv, fb, uSub, vSub);
                OUT.flipBlend = blend;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);
                half4 tex = lerp(texA, texB, (half)IN.flipBlend);
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float mask = saturate((lum - _LumaAlphaFloor) / max(1.0 - _LumaAlphaFloor, 1e-4));
                half3 rgb = tex.rgb * (half)_RgbBoost * (half)mask;
                return half4(saturate(rgb), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
