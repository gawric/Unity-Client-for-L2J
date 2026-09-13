// L2 map fire haze (FX_E_T.LightGlowSet.LIGHT_GLOW01).
// FF: color = tex * Color0. Blend OB_Brighten.
//
// Original RenderDoc: Depth Disabled — haze paints over the fire basin.
// Hardware ZTest would clip on that decor (Unity screenshot). Hardware Always
// would also paint through castle walls.
//
// Compromise: ZTest Always + scene-depth compare. Occluders closer than
// _WallOccludeStart (pedestal, nearby floor) are ignored. A wall many meters
// in front of the fire fades the haze out.
Shader "L2/Maps/DeFireHaze"
{
    Properties
    {
        _MainTex ("LIGHT_GLOW01", 2D) = "black" {}
        _Color ("Color0 (L2 bytes)", Vector) = (1, 1, 1, 1)
        _HazeScale ("Haze Size vs Flame", Range(0.5, 8)) = 3.2
        _HazeRefSize ("Wall-torch Height (m)", Range(0.25, 8)) = 1.75
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        _WallOccludeStart ("Ignore Nearby Occluders (m)", Range(0.1, 8)) = 2
        _WallOccludeEnd ("Hide Behind Walls (m)", Range(0.2, 16)) = 4.5
        [Toggle] _Billboard ("Camera Billboard", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        // L2 OB_Brighten: RGB and A both One / OneMinusSrcColor.
        // Unity's short form quietly uses OneMinusSrcAlpha for A.
        Blend One OneMinusSrcColor, One OneMinusSrcColor

        Pass
        {
            Name "DeFireHaze"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Skills/Common/Decompile_Common/L2FxD3d9ColorPath.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _HazeScale;
                float _HazeRefSize;
                float _RgbBoost;
                float _WallOccludeStart;
                float _WallOccludeEnd;
                float _Billboard;
            CBUFFER_END

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 DeFireObjectScale()
            {
                return float3(
                    length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                    length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)),
                    length(float3(UNITY_MATRIX_M._m02, UNITY_MATRIX_M._m12, UNITY_MATRIX_M._m22)));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 scale = DeFireObjectScale();
                // 3.2x is the wall-torch look (height ~1.75m → ~5.5m disc).
                // The same ratio on a 5.7m brazier is an 18m camera-facing
                // disc and fills the view. Keep the extra as a world pad.
                float flameSize = max(scale.x, scale.y);
                float ratio = max(_HazeScale, 0.001);
                float refSize = max(_HazeRefSize, 0.001);
                float pad = min(flameSize, refSize) * max(ratio - 1.0, 0.0);
                float hazeSize = flameSize + pad;
                float3 pivot = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 worldPos;

                if (_Billboard < 0.5)
                {
                    worldPos = TransformObjectToWorld(input.positionOS.xyz);
                }
                else
                {
                    float3 camRight = normalize(GetViewToWorldMatrix()._m00_m10_m20);
                    float3 camUp = normalize(GetViewToWorldMatrix()._m01_m11_m21);
                    worldPos = pivot
                        + camRight * (input.positionOS.x * hazeSize)
                        + camUp * (input.positionOS.y * hazeSize);
                }

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float DeFireHazeWallVisibility(float4 positionCS)
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(positionCS);
                float sceneRaw = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float hazeEye = LinearEyeDepth(positionCS.z, _ZBufferParams);
                float occludeMeters = hazeEye - sceneEye;
                float startM = min(_WallOccludeStart, _WallOccludeEnd);
                float endM = max(_WallOccludeEnd, startM + 0.01);
                return 1.0 - smoothstep(startM, endM, occludeMeters);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 color = tex * _Color;
                color.rgb *= L2Fx_D3d9EffectiveRgbBoost(_RgbBoost);
                color.rgb *= DeFireHazeWallVisibility(input.positionCS);
                return float4(saturate(color.rgb), color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
