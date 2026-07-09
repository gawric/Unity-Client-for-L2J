using UnityEngine;

/// <summary>
/// Spawn timing, slot duration, and continuous-loop state for ParticleSingle.
/// </summary>
public class ParticleSingleLifetimeTracker
{
    public float LastEnable { get; set; }
    public float LastLoop { get; set; }
    public float SpawnedAt { get; set; }
    public int SpawnedCount { get; set; }
    public bool Active { get; set; }
    public bool Stopped { get; set; }
    public float Duration { get; set; }
    public float BaseShaderLifetime { get; set; }
    public bool RuntimeContinuousLoop { get; set; }

    public void ResetForPlayPart(float now)
    {
        LastEnable = now;
        LastLoop = 0f;
        SpawnedAt = 0f;
        SpawnedCount = 0;
        Active = false;
        Stopped = false;
    }

    public void MarkSpawned(float now)
    {
        Active = true;
        SpawnedAt = now;
    }

    public bool IsBeforeStartDelay(float now, float startDelay)
    {
        return now - LastEnable < startDelay;
    }

    public bool IsSlotExpired(float now, bool preserveShaderTimeInContinuousLoop)
    {
        return Active && !preserveShaderTimeInContinuousLoop && now - SpawnedAt >= Duration;
    }

    public bool ShouldLoopContinuously(bool forceContinuousSpawning)
    {
        return forceContinuousSpawning || RuntimeContinuousLoop;
    }

    public void ResolveDurationFromSetup(
        ref float duration,
        bool hasFixedDuration,
        EffectSettings settings,
        MagicCastData castData,
        out float legacyHitDuration)
    {
        duration = EffectCastDurationResolver.Resolve(
            duration,
            hasFixedDuration,
            settings,
            castData,
            out legacyHitDuration,
            out _);
    }

    public void PrepareForPlay(float fallbackDuration, bool hasFixedDuration, bool hasLoopOverride, bool loopOverrideValue)
    {
        if (Duration < 0.01f)
        {
            Duration = fallbackDuration;
        }

        BaseShaderLifetime = Mathf.Max(0.01f, fallbackDuration);
        RuntimeContinuousLoop = !hasFixedDuration && Duration > BaseShaderLifetime + 0.05f;
        if (hasLoopOverride)
        {
            RuntimeContinuousLoop = loopOverrideValue;
        }
    }

    public float ReadLifetimeFromSharedMaterials(Renderer renderer, string groupName, L2Particle owner, Transform transform, float fallback)
    {
        if (renderer == null || renderer.sharedMaterials == null)
        {
            ParticleSingleLifetimeDebug.LogLifetimeFallback(
                groupName,
                owner,
                transform,
                fallback,
                "no_renderer_or_materials rendererNull=" + (renderer == null));
            return fallback;
        }

        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            Material mat = renderer.sharedMaterials[i];
            if (mat != null && mat.HasProperty(L2MaterialPropertyCopier.LifetimeRangeId))
            {
                float lifetime = mat.GetVector(L2MaterialPropertyCopier.LifetimeRangeId).y;
                ParticleSingleLifetimeDebug.LogLifetimeFallback(
                    groupName,
                    owner,
                    transform,
                    fallback,
                    string.Empty,
                    mat.name,
                    i,
                    lifetime);
                return lifetime;
            }
        }

        ParticleSingleLifetimeDebug.LogLifetimeFallback(
            groupName,
            owner,
            transform,
            0.5f,
            "no _LifetimeRange on shared materials — this matches ~500ms shader fade if used as slot duration.");
        return 0.5f;
    }
}
