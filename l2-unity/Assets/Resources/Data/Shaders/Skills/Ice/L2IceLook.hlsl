#ifndef L2_ICE_LOOK_INCLUDED
#define L2_ICE_LOOK_INCLUDED

// Shared visual model for Lineage-style ice meshes. Motion stays in each shader.
TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);
TEXTURE2D(_SpecMask);
SAMPLER(sampler_SpecMask);
TEXTURECUBE(_EnvCube);
SAMPLER(sampler_EnvCube);

struct L2IceLookInput
{
    float2 uvMain;
    float2 uvMask;
    float3 normalWS;
    float3 viewDirWS;
};

half4 L2IceLook_Fragment(
    L2IceLookInput input,
    half lifeAlpha,
    float4 tint,
    float alpha,
    float specMaskStrength,
    float fresnelPower,
    float fresnelStrength,
    float4 edgeColor,
    float4 envTint,
    float envStrength)
{
    half4 baseCol = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, input.uvMain);
    half4 maskTex = SAMPLE_TEXTURE2D(_SpecMask, sampler_SpecMask, input.uvMask);

    half mask = saturate(dot(maskTex.rgb, half3(0.3333h, 0.3333h, 0.3333h)) * specMaskStrength);
    half ndv = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
    half fresnel = pow(1.0h - ndv, max(0.1h, fresnelPower)) * fresnelStrength;
    float3 reflDir = reflect(-normalize(input.viewDirWS), normalize(input.normalWS));
    half3 envRgb = SAMPLE_TEXTURECUBE(_EnvCube, sampler_EnvCube, reflDir).rgb * envTint.rgb;

    half3 iceRgb = baseCol.rgb * tint.rgb;
    half3 edgeGlow = edgeColor.rgb * (fresnel * mask);
    half3 envSpec = envRgb * (mask * envStrength);
    half3 finalRgb = iceRgb + edgeGlow + envSpec;

    half finalA = saturate(baseCol.a * tint.a * alpha * lifeAlpha);
    return half4(finalRgb, finalA);
}

#endif
