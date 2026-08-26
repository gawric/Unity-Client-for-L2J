// e_u031_a MeshEmitter3 "CylUp": castleob03, fx_m_t0078, velocity + acceleration up.
// TODO: port full mesh emitter shader.
Shader "L2/Effects/TeleportCaCylUp"
{
    Properties
    {
        _MainTex ("Texture (fx_m_t0078)", 2D) = "white" {}
        _Tint ("Debug Tint", Color) = (0.7, 0.7, 1, 0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "L2FxGpuInstancing" = "On" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Resources/Data/Shaders/Skills/Teleport/_TeleportStubTemplate.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
            CBUFFER_END

            #include "../Common/L2FxInstancing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 c = L2Teleport_StubSample(input.uv, _Tint);
                c.rgb *= c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
