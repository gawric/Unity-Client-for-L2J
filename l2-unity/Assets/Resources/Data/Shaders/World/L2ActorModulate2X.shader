// Skinned actor body. Unlit: the vertex color is the L2 light, not Unity PBR.
// Draw monster_008 (keltir_t00): Cull None, ZWrite, blend off, alpha GREATER 127,
// MODULATE2X, linear fog 4096..12288. Lighting is per vertex.
// Colors and the sun vector come from the day globals. No hour is baked in.
// c251 = _L2ActorAmbientHalf
// c250 = _L2HsvActorLight.rgb * _L2ActorDirectionalScale
// _L2HsvActorLight.a is brightness/255 and is not multiplied again.
Shader "L2/World/ActorModulate2X"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.498039216
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

    // Not in the shared include: terrain and static mesh must not see these.
    float4 _L2ActorAmbientHalf;
    float4 _L2HsvActorLight;
    float _L2ActorDirectionalScale;

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        float _Cull;
        float _Cutoff;
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

    void L2_ActorClip(float alpha)
    {
        // _Cutoff 0 is not alpha-test off: clip(0) still exists in the shader.
        // The keyword removes the instruction. Keltir keeps it, the guild face does not.
#if defined(_ALPHATEST_ON)
        clip(alpha - _Cutoff);
#endif
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "L2ActorForwardOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma shader_feature_local_fragment _ALPHATEST_ON
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
                OUT.color0 = L2_ActorColor0(TransformObjectToWorldNormal(IN.normalOS), posInputs.positionWS);
                OUT.viewAbsZ = abs(posInputs.positionVS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float alpha = tex.a * _BaseColor.a;
                L2_ActorClip(alpha);
                float3 rgb = L2_LinearFog(L2_Modulate2X(tex.rgb * _BaseColor.rgb, IN.color0), IN.viewAbsZ);
                return half4(rgb, saturate(alpha * _ActorAlpha));
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct DepthAttrs
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttrs IN)
            {
                DepthVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                OUT.uv = L2_ActorUv(IN.uv);
                return OUT;
            }

            half DepthFrag(DepthVaryings IN) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                L2_ActorClip(alpha);
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct DNAttrs
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DNVaryings DepthNormalsVert(DNAttrs IN)
            {
                DNVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = L2_ActorUv(IN.uv);
                return OUT;
            }

            half4 DepthNormalsFrag(DNVaryings IN) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                L2_ActorClip(alpha);
                float3 n = normalize(IN.normalWS);
                return half4(n * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
