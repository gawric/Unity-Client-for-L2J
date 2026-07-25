#ifndef L2_MIGHT_STUB_INCLUDED
#define L2_MIGHT_STUB_INCLUDED

// Placeholder helpers for Might effect shaders (m_u004_a / m_u004_b).
// Replace with full L2Fx vertex/fragment logic when porting each emitter.

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

half4 L2Might_StubSample(float2 uv, half4 tint)
{
    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    return tex * tint;
}

#endif
