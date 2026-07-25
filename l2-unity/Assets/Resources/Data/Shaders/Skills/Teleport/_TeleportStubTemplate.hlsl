#ifndef L2_TELEPORT_STUB_INCLUDED
#define L2_TELEPORT_STUB_INCLUDED

// Placeholder helpers for Teleport effect shaders (e_u031_a).
// Replace with full L2Fx vertex/fragment logic when porting each emitter.

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

half4 L2Teleport_StubSample(float2 uv, half4 tint)
{
    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    return tex * tint;
}

#endif
