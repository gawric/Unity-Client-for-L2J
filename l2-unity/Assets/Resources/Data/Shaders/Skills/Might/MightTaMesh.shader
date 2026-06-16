// m_u004_a mesh emitters: MeshEmitter9 (supportenchant00), MeshEmitter1 (supportenchant02).
// TODO: port UC physics/color/size/spin from m_u004_a.uc
Shader "L2/Effects/MightTaMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Debug Tint", Color) = (1, 0.85, 0.4, 0.75)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                return L2Might_StubSample(input.uv, _Tint);
            }
            ENDHLSL
        }
    }
}
