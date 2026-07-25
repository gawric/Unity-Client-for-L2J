using UnityEngine;

/// <summary>
/// Resolves emitter duration from cast timing vs static EffectSettings.
/// Server HitTime wins over asset defaultLifeTime when both are present.
/// </summary>
public static class EffectCastDurationResolver
{
    public static float Resolve(
        float prefabDuration,
        bool hasFixedDuration,
        EffectSettings settings,
        MagicCastData castData,
        out float legacyHitDuration,
        out bool serverHitOverridesSettings)
    {
        legacyHitDuration = 0f;
        serverHitOverridesSettings = false;

        if (hasFixedDuration)
        {
            return prefabDuration;
        }

        float castHitDuration = castData != null && castData.HitTime > 0f ? castData.HitTime : 0f;
        float settingsDuration = settings != null && settings.defaultLifeTime > 0f ? settings.defaultLifeTime : 0f;

        if (castHitDuration <= 0f && settingsDuration <= 0f && EffectSkillsmanager.Instance != null)
        {
            float legacyHitTimeMs = EffectSkillsmanager.Instance.HitTime();
            if (legacyHitTimeMs > 0f)
            {
                legacyHitDuration = legacyHitTimeMs / 1000f;
            }
        }

        float authoritativeDuration;
        if (castHitDuration > 0f)
        {
            authoritativeDuration = castHitDuration;
            serverHitOverridesSettings = settingsDuration > castHitDuration + 0.01f;
        }
        else if (settingsDuration > 0f)
        {
            authoritativeDuration = settingsDuration;
        }
        else
        {
            authoritativeDuration = legacyHitDuration;
        }

        return Mathf.Max(prefabDuration, authoritativeDuration);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static void LogMismatchIfNeeded(
        string context,
        string groupName,
        float castHit,
        float settingsLife,
        float resolvedDuration,
        bool serverHitOverridesSettings)
    {
        if (castHit <= 0f || settingsLife <= 0f)
        {
            return;
        }

        if (serverHitOverridesSettings)
        {
            Debug.LogWarning(
                $"[EFFECT_LIFETIME_MISMATCH] {context} group='{groupName}' " +
                $"serverHit={castHit:F3}s settingsDefaultLife={settingsLife:F3}s " +
                $"resolvedDuration={resolvedDuration:F3}s — using server HitTime, not asset defaultLifeTime.");
            return;
        }

        if (Mathf.Abs(resolvedDuration - castHit) > 0.05f && Mathf.Abs(resolvedDuration - settingsLife) < 0.05f)
        {
            Debug.LogWarning(
                $"[EFFECT_LIFETIME_MISMATCH] {context} group='{groupName}' " +
                $"resolvedDuration={resolvedDuration:F3}s differs from serverHit={castHit:F3}s " +
                $"(settingsLife={settingsLife:F3}s).");
        }
    }
#endif
}
