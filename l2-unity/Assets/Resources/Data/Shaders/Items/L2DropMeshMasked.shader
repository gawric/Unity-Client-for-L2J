// Ground-drop mesh for items whose Abstractgrp textures are gold-on-black
// (cbui24/25). PTDS_Regular look (Blend One Zero + clip black).
// Do NOT use Queue=Geometry + UniversalForward: this project's URP is Deferred
// (RenderingMode=2, DepthPriming=1). Unlit Geometry/Forward is skipped; FX
// CoinJunk already uses Transparent + UniversalForwardOnly for the same mesh.
Shader "L2/Items/DropMeshMasked"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _MainTex_ST ("MainTex ST", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Unlit"
        }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend One Zero

        Pass
        {
            Name "DropMeshForwardOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float4 st = _MainTex_ST;
                if (abs(st.x) + abs(st.y) < 0.0001)
                    st = float4(1, 1, 0, 0);
                OUT.uv = IN.uv * st.xy + st.zw;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(max(tex.r, max(tex.g, tex.b)) - 0.06h);
                return tex;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
