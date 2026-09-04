#ifndef L2_FX_HE_VECTOR_SCALE_INCLUDED
#define L2_FX_HE_VECTOR_SCALE_INCLUDED

// Engine_essens_high_elves.dll, UParticleEmitter::UpdateParticles:
//   phase = frac((Repeats + 1) * lifeNorm)
//   key   = { float RelativeTime; FVector RelativeValue; } // 16 bytes
// The first interpolation starts at the native default vector (1,1,1).
// VelocityScale: emitter +0x418 bit0, keys +0x41C, repeats +0x428.
// RevolutionScale: emitter +0x1FC bit5, keys +0x230, repeats +0x23C.

void L2FxHE_VectorScale_BuildKeys7(
    float4 key0,
    float4 key1,
    float4 key2,
    float4 key3,
    float4 key4,
    float4 key5,
    float4 key6,
    out float times[7],
    out float3 values[7])
{
    times[0] = key0.x; values[0] = key0.yzw;
    times[1] = key1.x; values[1] = key1.yzw;
    times[2] = key2.x; values[2] = key2.yzw;
    times[3] = key3.x; values[3] = key3.yzw;
    times[4] = key4.x; values[4] = key4.yzw;
    times[5] = key5.x; values[5] = key5.yzw;
    times[6] = key6.x; values[6] = key6.yzw;
}

float3 L2FxHE_VectorScale_SamplePhase(
    float phase,
    uint keyCount,
    float times[7],
    float3 values[7])
{
    uint count = min(keyCount, 7u);
    if (count == 0u)
        return float3(1.0, 1.0, 1.0);

    float p = saturate(phase);
    uint nextIndex = 0u;
    [loop]
    while (nextIndex < count && times[nextIndex] < p)
        ++nextIndex;

    if (nextIndex >= count)
        return values[count - 1u];

    float3 nextValue = values[nextIndex];
    float nextTime = times[nextIndex];
    if (nextIndex == 0u)
    {
        // Native takes the authored key immediately when RelativeTime is zero.
        if (abs(nextTime) <= 1e-6)
            return nextValue;
        return lerp(float3(1.0, 1.0, 1.0), nextValue, p / nextTime);
    }

    float previousTime = times[nextIndex - 1u];
    float3 previousValue = values[nextIndex - 1u];
    float duration = nextTime - previousTime;
    if (abs(duration) <= 1e-6)
        return nextValue;
    return lerp(previousValue, nextValue, (p - previousTime) / duration);
}

float3 L2FxHE_VectorScale_Sample(
    float lifeNorm,
    float useScale,
    float repeats,
    uint keyCount,
    float times[7],
    float3 values[7])
{
    if (useScale < 0.5 || keyCount == 0u)
        return float3(1.0, 1.0, 1.0);

    float phase = frac((repeats + 1.0) * saturate(lifeNorm));
    return L2FxHE_VectorScale_SamplePhase(phase, keyCount, times, values);
}

bool L2FxHE_MaxAbsWouldClamp(
    float3 startVelocityUe,
    float3 accelerationUe,
    float3 velocityLossPerSecond,
    float3 maxAbsVelocityUe,
    float ageSeconds,
    float motionMode)
{
    float t = max(ageSeconds, 0.0);
    float3 endVelocity = startVelocityUe + accelerationUe * t;
    if (motionMode > 1.5)
    {
        [unroll]
        for (uint axis = 0u; axis < 3u; ++axis)
        {
            float loss = velocityLossPerSecond[axis];
            if (loss > 1e-6)
            {
                float terminal = accelerationUe[axis] / loss;
                endVelocity[axis] = terminal +
                    (startVelocityUe[axis] - terminal) * exp(-loss * t);
            }
        }
    }

    [unroll]
    for (uint axis = 0u; axis < 3u; ++axis)
    {
        float limit = maxAbsVelocityUe[axis];
        if (limit != 0.0 &&
            max(abs(startVelocityUe[axis]), abs(endVelocity[axis])) > abs(limit))
        {
            return true;
        }
    }
    return false;
}

// GPU particles reconstruct position directly from age instead of retaining the
// native per-tick slot. Midpoint integration preserves the native meaning:
// every tick moves by currentVelocity * sampledScale * dt.
float3 L2FxHE_VectorScale_IntegrateVelocityMidpoint16(
    float3 startVelocityUe,
    float3 accelerationUe,
    float3 velocityLossPerSecond,
    float3 maxAbsVelocityUe,
    float ageSeconds,
    float lifetimeSeconds,
    float motionMode,
    float useVelocityScale,
    float velocityScaleRepeats,
    uint velocityScaleCount,
    float times[7],
    float3 values[7])
{
    float age = max(ageSeconds, 0.0);
    if (age <= 0.0)
        return float3(0.0, 0.0, 0.0);

    const uint stepCount = 16u;
    float dt = age / (float)stepCount;
    float3 displacement = float3(0.0, 0.0, 0.0);

    [unroll]
    for (uint step = 0u; step < stepCount; ++step)
    {
        float t = ((float)step + 0.5) * dt;
        float3 velocity = startVelocityUe + accelerationUe * t;
        if (motionMode > 1.5)
        {
            float3 loss = velocityLossPerSecond;
            [unroll]
            for (uint axis = 0u; axis < 3u; ++axis)
            {
                if (loss[axis] > 1e-6)
                {
                    float terminal = accelerationUe[axis] / loss[axis];
                    velocity[axis] = terminal +
                        (startVelocityUe[axis] - terminal) * exp(-loss[axis] * t);
                }
            }
        }

        // Native: MaxAbs==0 skips that axis.
        [unroll]
        for (uint axis = 0u; axis < 3u; ++axis)
        {
            float limit = maxAbsVelocityUe[axis];
            if (limit != 0.0)
                velocity[axis] = clamp(velocity[axis], -limit, limit);
        }

        float lifeNorm = t / max(lifetimeSeconds, 1e-4);
        float3 scale = L2FxHE_VectorScale_Sample(
            lifeNorm,
            useVelocityScale,
            velocityScaleRepeats,
            velocityScaleCount,
            times,
            values);
        displacement += velocity * scale * dt;
    }

    return displacement;
}

// Integral of the RevolutionScale multiplier in seconds. Revolution consumes
// this value as integral(multiplier(t) dt), independently for X/Y/Z.
float3 L2FxHE_VectorScale_IntegrateMultiplierMidpoint32(
    float ageSeconds,
    float lifetimeSeconds,
    float useScale,
    float repeats,
    uint keyCount,
    float times[7],
    float3 values[7])
{
    float age = max(ageSeconds, 0.0);
    if (useScale < 0.5 || keyCount == 0u)
        return age.xxx;

    const uint stepCount = 32u;
    float dt = age / (float)stepCount;
    float3 integral = float3(0.0, 0.0, 0.0);
    [unroll]
    for (uint step = 0u; step < stepCount; ++step)
    {
        float t = ((float)step + 0.5) * dt;
        integral += L2FxHE_VectorScale_Sample(
            t / max(lifetimeSeconds, 1e-4),
            useScale,
            repeats,
            keyCount,
            times,
            values) * dt;
    }
    return integral;
}

#endif // L2_FX_HE_VECTOR_SCALE_INCLUDED
