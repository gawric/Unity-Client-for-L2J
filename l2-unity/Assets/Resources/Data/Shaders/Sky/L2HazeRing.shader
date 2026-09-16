// L2 Haze Ring: WhiteRing on L2sky_Cylinder.
// Client: L2_Skies.Shaders.HazeRing Diffuse+Opacity=WhiteRing,
// ColorModifier HazeRing_Final, blend SrcAlpha / InvSrcAlpha / Add.
// RenderDoc high_elf_soon_clouds_6_00 EID 1661 SPIR-V:
//   out = tex2D(WhiteRing, Texcoord0.xy) * Color0
//   blend SrcAlpha / InvSrcAlpha / Add. No blur, no look curve.
// Soft top of the wash is WhiteRing alpha (mesh V 0.99 at horizon → 0.01 at +21°).
//
// Drawn in the D3D9 compositor UNORM buffer (layer L2Haze) after the scene copy.
// Skill post applies the same fxGain (1.4) + bloom as SkillEffect / sun-moon.
// WhiteRing sRGB OFF. No pow look-curves in this shader.
Shader "L2/Sky/HazeRing"
{
    Properties
    {
        _MainTex ("WhiteRing", 2D) = "white" {}
        _HazeColor ("Haze Tint (GetHazeColor bytes)", Vector) = (0.737, 0.796, 0.878, 1)
        _SkyDistance ("Sky Distance", Float) = 88
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HazeRing"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            // Camera sits inside the tube. Cull Front + outward winding hid every
            // triangle; celestial discs use Cull Off for the same reason.
            Cull Off
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _HazeColor;
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
                // EID 1661 SPIR-V: out = ImageSample(t0, Texcoord0.xy) * Color0.
                // No extra fade math. Soft top is WhiteRing alpha + SrcAlpha blend.
                // D3D/Vulkan V=0 is the top of WhiteRing; Unity V=0 is the bottom.
                float2 uv = float2(i.uv.x, 1.0 - i.uv.y);
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return tex * _HazeColor;
            }
            ENDHLSL
        }
    }
}
