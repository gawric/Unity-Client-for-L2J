#ifndef L2_FX_PARTICLE_ANIM_INCLUDED
#define L2_FX_PARTICLE_ANIM_INCLUDED

// Shared L2-style particle timing: lifetime, fade in/out, size curve, seeded random, spin.
// Include after Core.hlsl. Call with your UnityPerMaterial uniforms (no globals inside this file).

static const float L2Fx_TwoPi = 6.28318530718;

float L2Fx_Hash11(float n)
{
    return frac(sin(n) * 43758.5453123);
}

float L2Fx_RandomRange(float2 minMax, float seed, float startTime, float salt)
{
    float t = L2Fx_Hash11((seed * 17.0) + (startTime * 31.0) + salt);
    return lerp(minMax.x, minMax.y, t);
}

float L2Fx_NormalizedAge(float timeY, float hasLifetime, float startTime, float initialDelay, float lifetime)
{
    float has = step(0.5, hasLifetime);
    if (has < 0.5)
    {
        return 1.0;
    }

    float age = timeY - startTime - max(0.0, initialDelay);
    float lt = max(0.0001, lifetime);
    return saturate(age / lt);
}

float L2Fx_AgeSeconds(float timeY, float startTime, float initialDelay)
{
    return max(0.0, timeY - startTime - max(0.0, initialDelay));
}

float L2Fx_SizeScale(float normalizedAge, float useSizeScale, float4 sizeScale0, float4 sizeScale1, float4 sizeScale2)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float t0 = saturate(sizeScale0.x);
    float t1 = saturate(sizeScale1.x);
    float t2 = saturate(sizeScale2.x);
    float s0 = sizeScale0.y;
    float s1 = sizeScale1.y;
    float s2 = sizeScale2.y;

    if (normalizedAge <= t1)
    {
        float denom01 = max(0.0001, t1 - t0);
        return lerp(s0, s1, saturate((normalizedAge - t0) / denom01));
    }

    float denom12 = max(0.0001, t2 - t1);
    return lerp(s1, s2, saturate((normalizedAge - t1) / denom12));
}

float3 L2Fx_StartSize(float2 rangeX, float2 rangeY, float2 rangeZ, float uniformSize, float seed, float startTime)
{
    float sx = L2Fx_RandomRange(rangeX, seed, startTime, 11.0);
    float sy = L2Fx_RandomRange(rangeY, seed, startTime, 23.0);
    float sz = L2Fx_RandomRange(rangeZ, seed, startTime, 37.0);

    if (uniformSize > 0.5)
    {
        sy = sx;
        sz = sx;
    }

    return float3(max(0.0001, sx), max(0.0001, sy), max(0.0001, sz));
}

float3 L2Fx_RotateX(float3 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float3(p.x, p.y * c - p.z * s, p.y * s + p.z * c);
}

float3 L2Fx_RotateY(float3 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
}

float3 L2Fx_RotateZ(float3 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float3(p.x * c - p.y * s, p.x * s + p.y * c, p.z);
}

float3 L2Fx_RotationAngles(
    float ageSeconds,
    float2 startSpinX,
    float2 startSpinY,
    float2 startSpinZ,
    float2 spinsPerSecondX,
    float2 spinsPerSecondY,
    float2 spinsPerSecondZ,
    float seed,
    float startTime)
{
    float sx = L2Fx_RandomRange(startSpinX, seed, startTime, 41.0);
    float sy = L2Fx_RandomRange(startSpinY, seed, startTime, 43.0);
    float sz = L2Fx_RandomRange(startSpinZ, seed, startTime, 47.0);

    float vx = L2Fx_RandomRange(spinsPerSecondX, seed, startTime, 53.0);
    float vy = L2Fx_RandomRange(spinsPerSecondY, seed, startTime, 59.0);
    float vz = L2Fx_RandomRange(spinsPerSecondZ, seed, startTime, 61.0);

    return float3(sx, sy, sz) + (float3(vx, vy, vz) * ageSeconds * L2Fx_TwoPi);
}

float L2Fx_LifetimeAlpha(
    float timeY,
    float hasLifetime,
    float startTime,
    float initialDelay,
    float lifetime,
    float fadeIn,
    float fadeInEndTime,
    float fadeout,
    float fadeoutStartTime)
{
    float has = step(0.5, hasLifetime);
    if (has < 0.5)
    {
        return 1.0;
    }

    float age = timeY - startTime - max(0.0, initialDelay);
    if (age <= 0.0)
    {
        return 0.0;
    }

    float lt = max(0.0001, lifetime);
    if (age >= lt)
    {
        return 0.0;
    }

    float fadeInMul = 1.0;
    if (fadeIn > 0.5)
    {
        float fadeInEnd = max(0.0001, fadeInEndTime);
        fadeInMul = saturate(age / fadeInEnd);
    }

    float fadeOutMul = 1.0;
    if (fadeout > 0.5)
    {
        float fadeStart = clamp(fadeoutStartTime, 0.0, lt);
        float fadeDuration = max(0.0001, lt - fadeStart);
        float fadeT = saturate((age - fadeStart) / fadeDuration);
        fadeOutMul = 1.0 - fadeT;
    }

    return saturate(fadeInMul * fadeOutMul);
}

#endif