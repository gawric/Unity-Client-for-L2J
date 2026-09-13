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
                return c;
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
    }
    FallBack Off
}
