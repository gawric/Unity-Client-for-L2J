Shader "L2/UI/BitmapFontScreenSpace"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "L2BitmapFontScreenSpace"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Matches NameplatesManager.GlyphVertex (32-byte stride).
            struct GlyphVertex
            {
                float2 ScreenPos;
                float Depth;
                float Pad0;
                float2 UV;
                uint Color;
                uint Pad1;
            };

            StructuredBuffer<GlyphVertex> _GlyphBuffer;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            half4 UnpackColor(uint c)
            {
                return half4(
                    (c & 255u) / 255.0h,
                    ((c >> 8) & 255u) / 255.0h,
                    ((c >> 16) & 255u) / 255.0h,
                    ((c >> 24) & 255u) / 255.0h);
            }

            Varyings vert(uint vertexID : SV_VertexID)
            {
                GlyphVertex v = _GlyphBuffer[vertexID];

                Varyings o;
                o.uv = v.UV;
                o.color = UnpackColor(v.Color);

                // Screen pixels (Unity bottom-up Y) → NDC. L2 canvas path: no world re-project.
                float2 ndc;
                ndc.x = (v.ScreenPos.x / _ScreenParams.x) * 2.0 - 1.0;
                ndc.y = (v.ScreenPos.y / _ScreenParams.y) * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                ndc.y = -ndc.y;
                #endif

                // Depth unused for ZTest Always; keep near for stable overlay.
                o.positionCS = float4(ndc, UNITY_NEAR_CLIP_VALUE, 1.0);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return tex * i.color;
            }
            ENDHLSL
        }
    }
}
