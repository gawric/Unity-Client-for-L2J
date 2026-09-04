#ifndef L2_FX_HE_WARMUP_INCLUDED
#define L2_FX_HE_WARMUP_INCLUDED

// WarmupTicksPerSecond / RelativeWarmupTime.
//
// Not present in Decompile_Common. Not in the HE particle dump (config offsets
// for these floats were not found). Bind from UC.
//
// 1147: u_mon_fire1_fl Flame
//   WarmupTicksPerSecond=2, RelativeWarmupTime=0.2, Lifetime=0.35
//
// Not in HE UpdateParticles / SpawnParticle / Initialize (IDA Initialize
// dump is truncated). Keep the standard UE2 warmup loop:
//   tickCount = WarmupTicksPerSecond * RelativeWarmupTime
//   dt        = 1 / WarmupTicksPerSecond
//   total simulated age = RelativeWarmupTime seconds
// Zero ticks or zero relative time → no warmup.

float L2FxHE_Warmup_AgeOffsetSeconds(float warmupTicksPerSecond, float relativeWarmupTime)
{
    if (warmupTicksPerSecond <= 1e-6 || relativeWarmupTime <= 1e-6)
    {
        return 0.0;
    }

    return relativeWarmupTime;
}

float L2FxHE_Warmup_EffectiveAge(
    float ageSeconds,
    float warmupTicksPerSecond,
    float relativeWarmupTime)
{
    return max(ageSeconds, 0.0) + L2FxHE_Warmup_AgeOffsetSeconds(
        warmupTicksPerSecond,
        relativeWarmupTime);
}

float L2FxHE_Warmup_EffectiveAgeNorm(
    float ageSeconds,
    float lifetimeSeconds,
    float warmupTicksPerSecond,
    float relativeWarmupTime)
{
    float life = max(lifetimeSeconds, 1e-4);
    float age = L2FxHE_Warmup_EffectiveAge(
        ageSeconds,
        warmupTicksPerSecond,
        relativeWarmupTime);
    return saturate(age / life);
}

#endif
