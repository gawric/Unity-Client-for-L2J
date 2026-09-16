Shader "Hidden/L2/FxPostBloomContrast"
{
    Properties
    {
        _FxGain ("Fx Gain", Float) = 2
        _BloomThreshold ("Bloom Threshold", Float) = 0.2
        _BloomPreGain ("Bloom Pre Gain", Float) = 1
        _BloomIntensity ("Bloom Intensity", Float) = 0.95
        _KawaseOffset ("Kawase Offset", Float) = 1
        _TransferMode ("Transfer Mode", Float) = 0
    }
    // UNORM skill post. No look-curve pow/gamma.
    // Working recipe: R8G8B8A8_UNorm sRGB=off, D3D9 blends on the scene,
    // RGB Boost on the skill delta, bloom from max(color-scene, 0).
    // SrgbToLinear below is only the Unity camera handshake, not a skill look.
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_SceneTex);
        TEXTURE2D_X(_BloomTex);

        float _BloomThreshold;
        float _BloomPreGain;
        float _BloomIntensity;
        float _FxGain;
        float _KawaseOffset;
        float4 _KawaseTexelSize;
        float _TransferMode;

        float3 L2Fx_SrgbToLinear(float3 c)
        {
            // Same handshake as Hidden/L2/FxColorTransfer. Not a skill look curve.
            float3 lo = c / 12.92;
            float3 hi = pow((c + 0.055) / 1.055, 2.4);
            return float3(
                c.r <= 0.04045 ? lo.r : hi.r,
                c.g <= 0.04045 ? lo.g : hi.g,
                c.b <= 0.04045 ? lo.b : hi.b);
        }

        half3 L2Fx_BrightExtract(half3 c)
        {
            half luma = dot(c, half3(0.333333, 0.333333, 0.333333));
            half knee = max(1e-4h, (half)(1.0 - _BloomThreshold));
            half w = saturate((luma - (half)_BloomThreshold) / knee);
            return c * w * (half)_BloomPreGain;
        }
        ENDHLSL

        Pass
        {
            Name "ExtractDownsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragExtract

            half4 FragExtract(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half3 withFx = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                half3 scene = SAMPLE_TEXTURE2D_X(_SceneTex, sampler_LinearClamp, uv).rgb;
                half3 added = max(withFx - scene, 0);
                return half4(L2Fx_BrightExtract(added), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "KawaseBlur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragKawase

            half4 FragKawase(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 d = _KawaseTexelSize.xy * _KawaseOffset;
                half4 s = half4(0, 0, 0, 0);
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,  d.y));
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,  d.y));
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x, -d.y));
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, -d.y));
                return s * 0.25h;
            }
            ENDHLSL
        }

        Pass
        {
            Name "CompositeDecode"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            half4 FragComposite(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half3 withFx = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                half3 scene = SAMPLE_TEXTURE2D_X(_SceneTex, sampler_LinearClamp, uv).rgb;
                half3 bloom = SAMPLE_TEXTURE2D_X(_BloomTex, sampler_LinearClamp, uv).rgb;

                half3 lit = saturate(withFx + bloom * (half)_BloomIntensity);
                half3 delta = lit - scene;
                half3 composed = saturate(scene + delta * (half)_FxGain);

                if (_TransferMode > 2.5)
                    return half4(composed, 1);
                if (_TransferMode > 1.5)
                    composed = (half3)L2Fx_SrgbToLinear(composed);
                return half4(composed, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "CopyDebug"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDebug

            half4 FragDebug(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                if (_TransferMode > 1.5)
                    c = (half3)L2Fx_SrgbToLinear(c);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
