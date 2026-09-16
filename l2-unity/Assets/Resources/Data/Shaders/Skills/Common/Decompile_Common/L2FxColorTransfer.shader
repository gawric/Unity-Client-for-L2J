Shader "Hidden/L2/FxColorTransfer"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        // Pass 0: camera → L2 UNORM (optional Linear→sRGB encode)
        Pass
        {
            Name "EncodeToL2Unorm"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragEncode
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _TransferMode; // 0 copy, 1 LinearToSRGB

            float3 L2Fx_LinearToSrgb(float3 c)
            {
                float3 lo = c * 12.92;
                float3 hi = 1.055 * pow(max(c, 0.0), 1.0 / 2.4) - 0.055;
                return float3(
                    c.r <= 0.0031308 ? lo.r : hi.r,
                    c.g <= 0.0031308 ? lo.g : hi.g,
                    c.b <= 0.0031308 ? lo.b : hi.b);
            }

            half4 FragEncode(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_TransferMode > 0.5)
                    c.rgb = (half3)L2Fx_LinearToSrgb(c.rgb);
                // D3D9 Colour Pass is opaque. Cloud SrcA/InvSrcA needs dst.A = 1
                // (original pixel history Tex Before A=1.00). Camera color often has A=0.
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }

        // Pass 1: L2 UNORM → camera (optional sRGB→Linear decode)
        Pass
        {
            Name "DecodeFromL2Unorm"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDecode
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _TransferMode; // 0 copy, 2 SrgbToLinear, 3 raw debug

            float3 L2Fx_SrgbToLinear(float3 c)
            {
                float3 lo = c / 12.92;
                float3 hi = pow((c + 0.055) / 1.055, 2.4);
                return float3(
                    c.r <= 0.04045 ? lo.r : hi.r,
                    c.g <= 0.04045 ? lo.g : hi.g,
                    c.b <= 0.04045 ? lo.b : hi.b);
            }

            half4 FragDecode(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_TransferMode > 2.5)
                    return half4(c.rgb, 1);
                if (_TransferMode > 1.5)
                    c.rgb = (half3)L2Fx_SrgbToLinear(c.rgb);
                return c;
            }
            ENDHLSL
        }

        // Pass 2: sky scratch → UNORM. No pow.
        // RGB Boost only on bright sky (day blue). Night LUT ~#283146 stays as drawn.
        Pass
        {
            Name "CopySkyOverUnorm"
            Blend One OneMinusSrcAlpha
            ZTest Always
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCopySky
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _FxGain;
            float _SkyBoostStart;
            float _SkyBoostFull;

            half4 FragCopySky(Varyings input) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                if (c.a < 0.01h)
                    return half4(0, 0, 0, 0);

                half peak = max(c.r, max(c.g, c.b));
                half start = (half)_SkyBoostStart;
                half full = max(start + 1e-3h, (half)_SkyBoostFull);
                half w = smoothstep(start, full, peak);
                half g = lerp(1.0h, (half)_FxGain, w);
                return half4(saturate(c.rgb * g), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
