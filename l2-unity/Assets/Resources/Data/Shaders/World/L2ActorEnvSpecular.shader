// Guard NPC body. Same actor vertex light as keltir, different pixel combine.
// Engine.Shader on a_guard_MHuman: Diffuse * Color0 * 2, then add
// SpecularityMask.a * Cube.gold. Output alpha is 1. Alpha test is off.
// Cube.gold is sampled with the view-space reflection vector's XY.
// VS Out writes that unit vector to TEXCOORD2. There is no 0.5 bias.
// Diffuse and the specularity mask share the mesh UV.
Shader "L2/World/ActorEnvSpecular"
{
    Properties
    {
        [MainTexture] _BaseMap ("Diffuse", 2D) = "white" {}
        _SpecMask ("Specularity Mask", 2D) = "black" {}
        _EnvMap ("Env", 2D) = "black" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _ActorAlpha ("Actor Alpha", Range(0, 1)) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
        [HideInInspector] _MainTex ("BaseMap", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "L2Modulate2XLighting.hlsl"
    #include "L2ActorPointLight.hlsl"

    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    TEXTURE2D(_SpecMask);
    SAMPLER(sampler_SpecMask);
    TEXTURE2D(_EnvMap);
    SAMPLER(sampler_EnvMap);

    float4 _L2ActorAmbientHalf;
    float4 _L2HsvActorLight;
    float _L2ActorDirectionalScale;

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        float _Cull;
        float _ActorAlpha;
    CBUFFER_END

    float2 L2_ActorUv(float2 uv)
    {
        float4 st = _BaseMap_ST;
        if (abs(st.x) + abs(st.y) < 0.0001)
            st = float4(1, 1, 0, 0);
        return uv * st.xy + st.zw;
    }

    float3 L2_ActorColor0(float3 worldNormal, float3 worldPos)
    {
        float3 n = normalize(worldNormal);
        float3 sunDir = _L2StaticMeshLightDirection.xyz;
        float lenSq = dot(sunDir, sunDir);
        float ndotl = lenSq > 1e-8 ? saturate(-dot(n, sunDir * rsqrt(lenSq))) : 0.0;
        float3 sunColor = _L2HsvActorLight.rgb * _L2ActorDirectionalScale;
        float3 localSum = L2_ActorPointSum(worldPos, n);
        return saturate(_L2ActorAmbientHalf.rgb + sunColor * ndotl + localSum);
    }

    float2 L2_CameraEnvUv(float3 positionVS, float3 worldNormal)
    {
        float3 incident = normalize(positionVS);
        float3 nVS = normalize(TransformWorldToViewDir(worldNormal));
        float3 reflection = reflect(incident, nVS);
        return reflection.xy;
    }

    float3 L2_ActorEnvRgb(float2 uv, float2 envUv, float3 color0)
    {
        float3 diffuse = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
        float mask = SAMPLE_TEXTURE2D(_SpecMask, sampler_SpecMask, uv).a;
        float3 env = SAMPLE_TEXTURE2D(_EnvMap, sampler_EnvMap, envUv).rgb;
        float3 base = L2_Modulate2X(diffuse, color0);
        return saturate(base + mask * env);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "L2ActorEnvForwardOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 color0 : COLOR;
                float viewAbsZ : TEXCOORD1;
                float3 positionVS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.uv = L2_ActorUv(IN.uv);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color0 = L2_ActorColor0(normalWS, posInputs.positionWS);
                OUT.viewAbsZ = abs(posInputs.positionVS.z);
                OUT.positionVS = posInputs.positionVS;
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float2 envUv = L2_CameraEnvUv(IN.positionVS, IN.normalWS);
                float3 rgb = L2_LinearFog(L2_ActorEnvRgb(IN.uv, envUv, IN.color0), IN.viewAbsZ);
                return half4(rgb, saturate(_ActorAlpha));
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 2.0
            #pragma multi_compile_instancing

            struct DepthAttrs
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttrs IN)
            {
                DepthVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                return OUT;
            }

            half DepthFrag(DepthVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma target 2.0
            #pragma multi_compile_instancing

            struct DNAttrs
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DNVaryings DepthNormalsVert(DNAttrs IN)
            {
                DNVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFrag(DNVaryings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                return half4(n * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
