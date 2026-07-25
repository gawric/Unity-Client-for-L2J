// m_u004_b SpriteEmitter2: fx_m_t0033, subdiv 4..7, flash ring.
// TODO: port flash ring sprite shader (alpha blend + subdiv animation).
Shader "L2/Effects/MightCaFlash"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0033)", 2D) = "white" {}
        _Tint ("Debug Tint", Color) = (1, 1, 1, 0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Resources/Data/Shaders/Skills/Might/_MightStubTemplate.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 c = L2Might_StubSample(input.uv, _Tint);
                c.rgb *= c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
