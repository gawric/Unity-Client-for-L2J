// Unity built-in Skybox/Cubemap vertex path (MIT).
// Source: https://github.com/TwoTailsGames/Unity-Built-in-Shaders
//   DefaultResourcesExtra/Skybox-Cubed.shader
// Camera.RenderSkybox uses built-in clip matrices (UnityObjectToClipPos /
// UnityCG.cginc). URP TransformWorldToHClip does not, which left the color
// target uncleared → trails. Fragment is L2 GetSkyBoxColor (flat LUT).
Shader "L2/Sky/Flat"
{
    Properties
    {
        [HDR] _GradientColor1 ("Sky Color", Color) = (0.384, 0.678, 0.918, 1)
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            half4 _GradientColor1;
            half4 _Tint;
            half _Exposure;
            float _Rotation;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 c = _GradientColor1.rgb * _Tint.rgb;
                c *= _Exposure;
                return half4(c, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
