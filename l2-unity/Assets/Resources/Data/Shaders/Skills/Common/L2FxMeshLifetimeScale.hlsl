#ifndef L2_FX_MESH_LIFETIME_SCALE_INCLUDED
#define L2_FX_MESH_LIFETIME_SCALE_INCLUDED

// Shader-side uniform scale path for mesh-backed particle quads.
// It keeps the fast expansion timeline separate from the particle lifetime/fade timeline.
float L2Fx_MeshLifetimeScalePathT(
    float ageSeconds,
    float hasLifetime,
    float expansionEndSec,
    float lifetimeSeconds)
{
    if (hasLifetime < 0.5)
    {
        return 1.0;
    }

    float fallbackEnd = max(lifetimeSeconds, 0.0001);
    float endSec = expansionEndSec > 0.0 ? expansionEndSec : fallbackEnd;
    return saturate(ageSeconds / max(endSec, 0.0001));
}

float L2Fx_MeshLifetimeScaleCurve5(
    float pathT,
    float useSizeScale,
    float4 sizeScale0,
    float4 sizeScale1,
    float4 sizeScale2,
    float4 sizeScale3,
    float4 sizeScale4)
{
    if (useSizeScale < 0.5)
    {
        return 1.0;
    }

    float t = saturate(pathT);
    float t0 = saturate(sizeScale0.x);
    float t1 = saturate(sizeScale1.x);
    float t2 = saturate(sizeScale2.x);
    float t3 = saturate(sizeScale3.x);
    float s0 = sizeScale0.y;
    float s1 = sizeScale1.y;
    float s2 = sizeScale2.y;
    float s3 = sizeScale3.y;
    float s4 = sizeScale4.y;

    if (t <= t1)
    {
        return lerp(s0, s1, saturate((t - t0) / max(0.00001, t1 - t0)));
    }
    if (t <= t2)
    {
        return lerp(s1, s2, saturate((t - t1) / max(0.00001, t2 - t1)));
    }
    if (t <= t3)
    {
        return lerp(s2, s3, saturate((t - t2) / max(0.00001, t3 - t2)));
    }

    return lerp(s3, s4, saturate((t - t3) / max(0.00001, saturate(sizeScale4.x) - t3)));
}

float L2Fx_MeshLifetimePostBurstScale(
    float ageSeconds,
    float expansionEndSec,
    float lifetimeSeconds,
    float scaleAfterExpansion,
    float postBurstScaleSpeed)
{
    if (scaleAfterExpansion < 0.5)
    {
        return 0.0;
    }

    float expansionEnd = max(0.0, expansionEndSec);
    float lifetime = max(expansionEnd, lifetimeSeconds);
    float driftAge = max(0.0, ageSeconds - expansionEnd);
    float driftLimit = max(0.0, lifetime - expansionEnd);
    return min(driftAge, driftLimit) * max(0.0, postBurstScaleSpeed);
}

float L2Fx_MeshLifetimeScaleMultiplier(
    float pathT,
    float ageSeconds,
    float lifetimeSeconds,
    float useSizeScale,
    float4 sizeScale0,
    float4 sizeScale1,
    float4 sizeScale2,
    float4 sizeScale3,
    float4 sizeScale4,
    float expansionEndSec,
    float scaleAfterExpansion,
    float postBurstScaleSpeed)
{
    float scaleMul = L2Fx_MeshLifetimeScaleCurve5(
        pathT,
        useSizeScale,
        sizeScale0,
        sizeScale1,
        sizeScale2,
        sizeScale3,
        sizeScale4);
    scaleMul += L2Fx_MeshLifetimePostBurstScale(
        ageSeconds,
        expansionEndSec,
        lifetimeSeconds,
        scaleAfterExpansion,
        postBurstScaleSpeed);

    return max(0.01, scaleMul);
}

#endif // L2_FX_MESH_LIFETIME_SCALE_INCLUDED
