// Trader body, Engine.Shader inside FinalBlend. Two hardware draws.
// Pass 1 is FF_FS_8aae5a43: saturate(diffuse * color0 * 2), alpha is the texture alpha,
// GREATER than 160, then SrcAlpha / OneMinusSrcAlpha. World fog.
// Pass 2 is FF_FS_056bc5a7: rgb = n3gold01 at the camera reflection xy, alpha = spec mask.
// No color0 and no *2. Blend SrcAlpha One. Fog on this pass goes to black.
// Both passes are TwoSided on a_traderB_MHuman_re.
Shader "L2/World/ActorEnvAdd"
{
    Properties
    {
        [MainTexture] _BaseMap ("Diffuse", 2D) = "white" {}
        _SpecMask ("Specularity Mask", 2D) = "black" {}
        _EnvMap ("Env", 2D) = "black" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 1
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.627451
        _ActorAlpha ("Actor Alpha", Range(0, 1)) = 1
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

    float2 L2_CameraEnvUv(float3 positionVS, float3 worldNormal)
    {
        float3 incident = normalize(positionVS);
        float3 nVS = normalize(TransformWorldToViewDir(worldNormal));
        float3 reflection = reflect(incident, nVS);
        return reflection.xy;
    }

    void L2_ActorClip(float alpha)
    {
#if defined(_ALPHATEST_ON)
        clip(alpha - _Cutoff);
#endif
    }

    // Specular pass fog color in the capture is 0,0,0. The mask alpha is not fogged.
    float3 L2_BlackFog(float3 rgb, float viewAbsZ)
    {
        if (_L2FogEnable > 0.5)
        {
            float fogEnd = _L2FogEnd > 0.0 ? _L2FogEnd : (12288.0 / 52.5);
            float fogScale = _L2FogScale > 0.0 ? _L2FogScale : (52.5 / 8192.0);
            float f = saturate((fogEnd - viewAbsZ) * fogScale);
            rgb = rgb * f;
        }
        return rgb;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "L2ActorEnvAddBase"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                float alpha = tex.a * _BaseColor.a * _ActorAlpha;
                L2_ActorClip(tex.a * _BaseColor.a);
                float3 rgb = L2_LinearFog(L2_Modulate2X(tex.rgb * _BaseColor.rgb, IN.color0), IN.viewAbsZ);
                return half4(rgb, saturate(alpha));
            }
            ENDHLSL
        }

        Pass
        {
            Name "L2ActorEnvAddSpec"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

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
                float2 envUv : TEXCOORD1;
                float viewAbsZ : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.uv = L2_ActorUv(IN.uv);
                OUT.envUv = L2_CameraEnvUv(posInputs.positionVS, normalWS);
                OUT.viewAbsZ = abs(posInputs.positionVS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 env = SAMPLE_TEXTURE2D(_EnvMap, sampler_EnvMap, IN.envUv).rgb;
                float mask = SAMPLE_TEXTURE2D(_SpecMask, sampler_SpecMask, IN.uv).a;
                float3 rgb = L2_BlackFog(env, IN.viewAbsZ);
                return half4(rgb, saturate(mask * _ActorAlpha));
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
