// L2 haze-boundary myst clouds (sun_start_v2 EID 1566).
//   rgb = saturate(t0 + t2 * t1.a)
//   out = float4(rgb, t1.a) * textureFactor
// Blend One / One. Default URP transparent — not L2Haze UNORM.
Shader "L2/Sky/CloudMyst"
{
    Properties
    {
        _MainTex ("Color (t0)", 2D) = "black" {}
        _AlphaTex ("Alpha (t1)", 2D) = "black" {}
        _NoiseTex ("Color noise (t2)", 2D) = "gray" {}
        _CloudColor ("Tint (textureFactor)", Vector) = (1, 1, 1, 1)
        _NoiseTiling ("Noise Tiling", Vector) = (5, 2.5, 0, 0)
        _NoisePan ("Noise Pan (UV/sec)", Vector) = (0.05, 0.02, 0, 0)
        _SkyParams ("Sky Distance", Vector) = (88, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-22"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "CloudMyst"
            Tags { "LightMode" = "UniversalForward" }

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
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _AlphaTex_ST;
                float4 _NoiseTex_ST;
                float4 _CloudColor;
                float4 _NoiseTiling;
                float4 _NoisePan;
                float4 _SkyParams;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uvNoise : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float3 cam = GetCameraPositionWS();
                float3 dir = world - cam;
                float len = length(dir);
                dir = len > 1.0e-8 ? dir / len : float3(0, 1, 0);
                float skyZ = _SkyParams.x > 1.0 ? _SkyParams.x : (_ProjectionParams.z * 0.88);
                o.positionCS = TransformWorldToHClip(cam + dir * skyZ);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv.y = 1.0 - uv.y;
                o.uv0 = uv;
                o.uvNoise = uv * _NoiseTiling.xy + _Time.y * _NoisePan.xy;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 t0 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv0, 0);
                float4 t1 = SAMPLE_TEXTURE2D_LOD(_AlphaTex, sampler_AlphaTex, i.uv0, 0);
                float4 t2 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, i.uvNoise, 0);
                // PNG dump keeps the mask in .a (sparse). BC1 import set .a=1
                // everywhere and showed the whole t2 noise as a magenta wall.
                float a = t1.a;
                if (a > 0.999)
                    a = max(t1.r, max(t1.g, t1.b));
                float3 rgb = saturate(t0.rgb + t2.rgb * a);
                float4 outc = float4(rgb, a) * _CloudColor;
                outc.a *= _SkyParams.x > 0.0 ? 1.0 : 1.0;
                return outc;
            }
            ENDHLSL
        }
    }
}
