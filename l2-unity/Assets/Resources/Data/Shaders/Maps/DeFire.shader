// L2 map torch fire (FX_E_T.Flameset series A).
// Drawn by L2Fx D3D9 Compositor: layer SkillEffect + LightMode UniversalForwardOnly
// + transparent queue + atlas/glow sRGB OFF (same path as SkillEffect).
// Flame: Default_Flame_Final — OB_Brighten, flipbook de_fire_0000..0015, 25-30 FPS.
// Glow:  de_fire_glow * FadeColor0 * 2. LIGHT_GLOW01 haze is not in this shader.
Shader "L2/Maps/DeFire"
{
    Properties
    {
        _FlameAtlas ("Flame Atlas (4x4 de_fire_0000-0015)", 2D) = "black" {}
        _GlowTex ("Glow (de_fire_glow)", 2D) = "black" {}

        _FramesCount ("Frames Count", Float) = 16
        _AtlasColumns ("Atlas Columns", Float) = 4
        _AtlasRows ("Atlas Rows", Float) = 4
        _FramesPerSecond ("Frames Per Second", Range(1, 60)) = 27.5

        _GlowColor1 ("FadeColor Color1 (L2 bytes)", Vector) = (0.1921569, 0.1058824, 0.08235294, 1)
        _GlowColor2 ("FadeColor Color2 (L2 bytes)", Vector) = (0.4862745, 0.2627451, 0.1803922, 1)
        _GlowFadePeriod ("FadeColor Period", Float) = 0.4
        _GlowFadePhase ("FadeColor Phase", Float) = 1
        _GlowModulate2X ("Glow Modulate2X", Float) = 2
        _GlowScale ("Glow Size", Range(0.5, 3)) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1

        [Toggle] _Billboard ("Face Camera (yaw only)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        // L2 OB_Brighten: RGB and A both One / OneMinusSrcColor.
        Blend One OneMinusSrcColor, One OneMinusSrcColor

        Pass
        {
            Name "DeFire"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Skills/Common/Decompile_Common/L2FxD3d9ColorPath.hlsl"

            TEXTURE2D(_FlameAtlas);
            SAMPLER(sampler_FlameAtlas);
            TEXTURE2D(_GlowTex);
            SAMPLER(sampler_GlowTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FlameAtlas_ST;
                float4 _GlowTex_ST;
                float _FramesCount;
                float _AtlasColumns;
                float _AtlasRows;
                float _FramesPerSecond;
                float4 _GlowColor1;
                float4 _GlowColor2;
                float _GlowFadePeriod;
                float _GlowFadePhase;
                float _GlowModulate2X;
                float _GlowScale;
                float _RgbBoost;
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

                float3 worldPos;
                if (_Billboard < 0.5)
                {
                    worldPos = TransformObjectToWorld(input.positionOS.xyz);
                }
                else
                {
                    // Cylindrical billboard: pivot stays on the torch, flame stays world-up.
                    // Spherical (camera up) was sliding the quad out of the bowl when the camera moved.
                    float3 pivot = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                    float3 scale = DeFireObjectScale();
                    float3 worldUp = float3(0.0, 1.0, 0.0);
                    float3 toCamera = _WorldSpaceCameraPos - pivot;
                    toCamera.y = 0.0;
                    float3 right = float3(1.0, 0.0, 0.0);
                    float toCameraLenSq = dot(toCamera, toCamera);
                    if (toCameraLenSq > 1e-8)
                        right = normalize(cross(worldUp, toCamera * rsqrt(toCameraLenSq)));
                    worldPos = pivot
                        + right * (input.positionOS.x * scale.x)
                        + worldUp * (input.positionOS.y * scale.y);
                }

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                return output;
            }

            float2 DeFireAtlasUV(float2 uv)
            {
                float frames = max(_FramesCount, 1.0);
                float cols = max(_AtlasColumns, 1.0);
                float rows = max(_AtlasRows, 1.0);
                float fps = max(_FramesPerSecond, 0.001);
                float frame = floor(fmod(_Time.y * fps, frames));
                float col = fmod(frame, cols);
                float row = floor(frame / cols);
                float2 cell = float2(1.0 / cols, 1.0 / rows);
                float rowFromBottom = (rows - 1.0) - row;
                return (uv + float2(col, rowFromBottom)) * cell;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 flameUV = DeFireAtlasUV(TRANSFORM_TEX(input.uv, _FlameAtlas));
                float4 flame = SAMPLE_TEXTURE2D(_FlameAtlas, sampler_FlameAtlas, flameUV);

                float2 glowUV = (TRANSFORM_TEX(input.uv, _GlowTex) - 0.5) / max(_GlowScale, 0.001) + 0.5;
                float4 glow = SAMPLE_TEXTURE2D(_GlowTex, sampler_GlowTex, glowUV);

                float period = max(_GlowFadePeriod, 0.0001);
                float wave = 0.5 + 0.5 * sin((_Time.y / period + _GlowFadePhase) * TWO_PI);
                float3 fade = lerp(_GlowColor1.rgb, _GlowColor2.rgb, wave);
                float3 glowRgb = glow.rgb * fade * _GlowModulate2X;
                float glowAlpha = glow.a;
                if (glowAlpha < 1.0 / 255.0)
                    glowAlpha = dot(glow.rgb, float3(0.299, 0.587, 0.114));

                float3 rgb = flame.rgb + glowRgb * saturate(glowAlpha);
                rgb *= L2Fx_D3d9EffectiveRgbBoost(_RgbBoost);
                return float4(saturate(rgb), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
