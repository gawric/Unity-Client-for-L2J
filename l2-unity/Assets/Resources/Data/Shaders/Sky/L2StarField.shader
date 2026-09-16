// L2 StarField: L2sky_sp04 + StarField_smallStar01 / StarField_largeStar02.
// Client: L2_Skies.Shaders.StarField01/02, ColorModifier StarField_Final01/02.
// RenderDoc high_elf_moon.rdc EID 1408 (small) + 1414 (large):
//   out = tex2D(StarField, Texcoord0.xy) * Color0 * textureFactor
// Live blend: One / One / Add (black sky is not darkened).
// Drawn on L2Sky with sun/moon: UNORM after scene copy, fxGain 1.4.
// Night black sky stays in the scene copy and is not boosted.
Shader "L2/Sky/StarField"
{
    Properties
    {
        _MainTex ("StarField", 2D) = "black" {}
        _StarColor ("Star Tint (textureFactor)", Vector) = (1, 1, 1, 1)
        _SkyDistance ("Sky Distance", Float) = 88
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-10"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "StarField"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _FxGain;
            float _L2FxD3D9CompositorActive;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _StarColor;
                float _SkyDistance;
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float3 cam = GetCameraPositionWS();
                float3 dir = world - cam;
                float len = length(dir);
                dir = len > 1.0e-8 ? dir / len : float3(0, 1, 0);
                float skyZ = _SkyDistance > 1.0 ? _SkyDistance : (_ProjectionParams.z * 0.88);
                float3 sky = cam + dir * skyZ;
                o.positionCS = TransformWorldToHClip(sky);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 c = tex * _StarColor;
                if (_L2FxD3D9CompositorActive > 0.5)
                    c.rgb *= _FxGain;
                return c;
            }
            ENDHLSL
        }
    }
}
