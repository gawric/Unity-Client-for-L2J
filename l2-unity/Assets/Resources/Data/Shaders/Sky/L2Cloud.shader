// L2 sky clouds — D3D9 FF (RenderDoc FF_FS_424f0ae76ffe928c2d36cad61ff8e52ec7949c40):
//   tex = ImageSampleImplicitLod(t0, Texcoord0.xy)
//   out = tex * Color0 * textureFactor
// mesh_out_1.csv Color0 = (1,1,1,1). Day pixel history Shader Out ≈ tex (TF identity).
// Sampler: Repeat UV, mip bias -0.5 (texture importer). Blend SrcA / InvSrcA.
Shader "L2/Sky/Cloud"
{
    Properties
    {
        _MainTex ("Cloud", 2D) = "white" {}
        _TextureFactor ("textureFactor", Vector) = (1, 1, 1, 1)
        _SkyParams ("Sky Dist / Mesh Radius / LiftY", Vector) = (88, 1050, 0, 0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    HLSLINCLUDE
    #pragma target 3.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _TextureFactor;
        float4 _SkyParams;
    CBUFFER_END

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 color0 : COLOR;
    };

    Varyings vert(Attributes v)
    {
        Varyings o;
        float3 cam = GetCameraPositionWS();
        float3 offset = TransformObjectToWorld(v.positionOS.xyz) - cam;
        float skyZ = _SkyParams.x > 1.0 ? _SkyParams.x : (_ProjectionParams.z * 0.88);
        float nativeR = _SkyParams.y > 1.0 ? _SkyParams.y : 1050.0;
        float3 world = cam + offset * (skyZ / nativeR);
        world.y += _SkyParams.z;
        o.positionCS = TransformWorldToHClip(world);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.color0 = v.color;
        return o;
    }

    float4 frag(Varyings i) : SV_Target
    {
        // Same two muls as the D3D9 FF: do not fold into SAMPLE_TEXTURE2D (sample_b).
        float4 tex = _MainTex.Sample(sampler_MainTex, i.uv);
        float4 color0 = i.color0;
        float4 textureFactor = _TextureFactor;
        return tex * color0 * textureFactor;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-30"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Cloud"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            Lighting Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "CloudUNORM"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            Lighting Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
