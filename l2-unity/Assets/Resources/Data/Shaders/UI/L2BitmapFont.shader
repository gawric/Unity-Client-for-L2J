Shader "L2/UI/BitmapFont"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        // 0 = world/object verts (lobby). 1 = verts already in clip NDC xy (world nameplates).
        _ClipSpace ("Clip Space", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "L2BitmapFont"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ClipSpace;

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
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                if (_ClipSpace > 0.5)
                {
                    // positionOS.xy = NDC (-1..1). Overlay — depth unused (ZTest Always).
                    o.positionCS = float4(v.positionOS.x, v.positionOS.y, UNITY_NEAR_CLIP_VALUE, 1.0);
                }
                else
                {
                    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                }
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return tex * i.color;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ClipSpace;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                if (_ClipSpace > 0.5)
                {
                    o.pos = float4(v.vertex.x, v.vertex.y, UNITY_NEAR_CLIP_VALUE, 1.0);
                }
                else
                {
                    o.pos = UnityObjectToClipPos(v.vertex);
                }
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
