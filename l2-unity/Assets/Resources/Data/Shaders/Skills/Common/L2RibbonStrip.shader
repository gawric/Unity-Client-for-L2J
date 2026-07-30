// CPU-built URibbonEmitter strip (L2RibbonEmitter).
// UV.x = across (0=a3, 1=a2), UV.y = along trail (0=head, 1=tail).
// Formula reference: Decompile_Common/L2FxRibbonGetPoint.hlsl
// Blend: L2 RenderDoc One/One Add = PTDS_Translucent (L2FxPTDS_DrawStyle).
Shader "L2/Effects/Common/L2RibbonStrip"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 2)) = 1
        [Toggle] _UseVertexAlpha ("Use Vertex Alpha", Float) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("L2 Color Gamma→Linear (FX + sRGB tex OFF)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        // PTDS_Translucent — live D3D: Src=ONE Dest=ONE Op=ADD (see PTDS_DrawStyle_Reference).
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Name "L2RibbonStrip"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Decompile_Common/L2FxRibbonGetPoint.hlsl"
            #include "Decompile_Common/L2FxSpriteColorGammaLinear.hlsl"
            #include "Decompile_Common/L2FxPTDS_DrawStyle.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Opacity;
                float _UseVertexAlpha;
                float _RgbBoost;
                float _L2SpriteColorGammaToLinear;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                // Keep CPU strip UVs; ST still applies tiling/offset.
                float2 stripUv = L2FxRibbon_StripUv(input.uv.x, input.uv.y);
                o.uv = TRANSFORM_TEX(stripUv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half vertexA = _UseVertexAlpha > 0.5 ? input.color.a : 1.0h;

                // L2 display-gamma Tint → Unity Linear (same as sprite/mesh FX).
                float4 tint = L2Fx_SpriteColor_ApplyGammaToLinearIfEnabled(
                    _Color, _L2SpriteColorGammaToLinear);

                half4 col = tex * (half4)tint;
                // One/One ignores blend SrcAlpha — fold coverage + Opacity into RGB
                // (same rule as PTDS_Translucent / alphaBlend=0 color path).
                half a = col.a * vertexA * (half)_Opacity;
                col.rgb *= a;
                col.rgb *= (half)_RgbBoost;
                col.a = a;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
