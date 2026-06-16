#ifndef L2_FX_HOLD_INCLUDED
#define L2_FX_HOLD_INCLUDED

// _Hold from ParticleSingle: fraction of particle lifetime (0.413 = 41.3% of 2s).
// Split ages when hold > 0:
// - motionAge:  capped at holdRef during full hold; resumes on release (hold < holdRef)
// - spinAge:    raw elapsed
// - loopAgeNorm: color loop tail via fmod after hold cap
// - sizeAgeNorm: SizeScale linear to holdRef, frozen on hold loop, then continues to curve end on release

float L2Fx_HoldCapSeconds(float lifetime, float hold)
{
    lifetime = max(lifetime, 1e-4);
    return lifetime * max(0.0, hold);
}

// 0 = full hold at holdRef, 1 = hold fully released (hold -> 0).
float L2Fx_HoldReleaseT(float hold, float holdSizeReference)
{
    if (holdSizeReference <= 1e-4)
    {
        return hold <= 1e-4 ? 1.0 : 0.0;
    }

    if (hold <= 1e-4)
    {
        return 1.0;
    }

    if (hold >= holdSizeReference - 1e-4)
    {
        return 0.0;
    }

    return 1.0 - hold / holdSizeReference;
}

float L2Fx_HoldMotionAge(float elapsed, float lifetime, float hold)
{
    lifetime = max(lifetime, 1e-4);
    elapsed = max(0.0, elapsed);

    if (hold <= 0.0)
    {
        return elapsed;
    }

    return min(elapsed, L2Fx_HoldCapSeconds(lifetime, hold));
}

// Motion: rise to holdRef cap, hold, then resume path when C# lowers _Hold for release.
float L2Fx_HoldMotionAgeStable(float elapsed, float lifetime, float hold, float holdSizeReference)
{
    lifetime = max(lifetime, 1e-4);
    elapsed = max(0.0, elapsed);

    if (holdSizeReference <= 1e-4)
    {
        return L2Fx_HoldMotionAge(elapsed, lifetime, hold);
    }

    float capSec = L2Fx_HoldCapSeconds(lifetime, holdSizeReference);
    float releaseT = L2Fx_HoldReleaseT(hold, holdSizeReference);

    if (elapsed < capSec)
    {
        return elapsed;
    }

    if (releaseT <= 1e-4)
    {
        return capSec;
    }

    return lerp(capSec, elapsed, releaseT);
}

float L2Fx_HoldSpinAge(float elapsed)
{
    return max(0.0, elapsed);
}

float L2Fx_HoldLoopAgeNorm(float elapsed, float lifetime, float hold)
{
    lifetime = max(lifetime, 1e-4);
    elapsed = max(0.0, elapsed);

    if (hold <= 0.0)
    {
        return saturate(elapsed / lifetime);
    }

    float holdSec = L2Fx_HoldCapSeconds(lifetime, hold);
    float holdNorm = saturate(hold);

    if (elapsed <= holdSec)
    {
        return saturate(elapsed / lifetime);
    }

    float tailDuration = max(1e-4, lifetime - holdSec);
    float tailPhase = fmod(elapsed - holdSec, tailDuration) / tailDuration;
    return saturate(holdNorm + tailPhase * (1.0 - holdNorm));
}

// SizeScale: linear to holdRef, hold loop freeze, release continues toward curve end (norm 1).
float L2Fx_HoldSizeAgeNorm(float elapsed, float lifetime, float hold, float holdSizeReference)
{
    lifetime = max(lifetime, 1e-4);
    elapsed = max(0.0, elapsed);
    float linearNorm = saturate(elapsed / lifetime);

    if (holdSizeReference <= 1e-4)
    {
        return linearNorm;
    }

    float capSec = L2Fx_HoldCapSeconds(lifetime, holdSizeReference);
    float releaseT = L2Fx_HoldReleaseT(hold, holdSizeReference);

    if (elapsed < capSec)
    {
        return linearNorm;
    }

    if (releaseT <= 1e-4)
    {
        return saturate(holdSizeReference);
    }

    float endNorm = max(holdSizeReference, linearNorm);
    return lerp(holdSizeReference, endNorm, releaseT);
}

#endif
