using UnityEngine;

/// <summary>
/// L2 _Hold math, cast-end fade, and composite loop override state.
/// </summary>
public class L2ShaderHoldController
{
    public struct Settings
    {
        public float ShaderHold;
        public bool ReleaseByCastProgress;
        public float ReleaseStartNormalized;
        public bool SmoothRelease;
    }

    public struct CastTimeline
    {
        public float Now;
        public float CastStartTime;
        public float SlotDuration;
        public EffectSettings Settings;
    }

    private bool _hasRuntimeLoopOverride;
    private bool _runtimeLoopOverrideValue;
    private bool _castEndFadeRequested;
    private float _baseEmitterAlpha = 1f;
    private bool _loggedReleaseStart;
    private bool _loggedReleaseResume;

    public bool CastEndFadeRequested => _castEndFadeRequested;
    public bool HasRuntimeLoopOverride => _hasRuntimeLoopOverride;
    public bool RuntimeLoopOverrideValue => _runtimeLoopOverrideValue;
    public float BaseEmitterAlpha => _baseEmitterAlpha;

    public bool CanApplyHoldUpdates(bool stopped)
    {
        if (stopped && !_castEndFadeRequested)
        {
            return false;
        }

        return _hasRuntimeLoopOverride || _castEndFadeRequested;
    }

    public void SetRuntimeLoopOverride(bool hasOverride, bool value)
    {
        _hasRuntimeLoopOverride = hasOverride;
        _runtimeLoopOverrideValue = value;
    }

    public void ResetReleaseLogs()
    {
        _loggedReleaseStart = false;
        _loggedReleaseResume = false;
    }

    public void ResetCastEndFade()
    {
        _castEndFadeRequested = false;
    }

    public void CaptureBaseEmitterAlpha(Material[] materials)
    {
        _baseEmitterAlpha = L2MaterialPropertyCopier.ReadFloatFromFirstMaterial(
            materials,
            L2MaterialPropertyCopier.EmitterAlphaId,
            1f);
    }

    public bool ShouldDeferStopForRelease(in Settings settings)
    {
        return settings.ShaderHold > 1e-4f
               && settings.ReleaseByCastProgress
               && _hasRuntimeLoopOverride
               && _runtimeLoopOverrideValue;
    }

    public bool TryBeginCastEndFadeDefer(in Settings settings)
    {
        if (!ShouldDeferStopForRelease(in settings))
        {
            return false;
        }

        _castEndFadeRequested = true;
        return true;
    }

    public void CompleteImmediateStop()
    {
        _hasRuntimeLoopOverride = true;
        _runtimeLoopOverrideValue = false;
        _castEndFadeRequested = false;
    }

    public float EvaluateHold(in Settings settings, in CastTimeline timeline)
    {
        if (_castEndFadeRequested && settings.ReleaseByCastProgress && settings.ShaderHold > 1e-4f)
        {
            return EvaluateHoldForCastNow(in settings, in timeline);
        }

        if (!_hasRuntimeLoopOverride || !_runtimeLoopOverrideValue)
        {
            return 0f;
        }

        return EvaluateHoldForCastNow(in settings, in timeline);
    }

    public bool ApplyToMaterials(
        Material[] materials,
        in Settings settings,
        in CastTimeline timeline,
        string debugGroupName,
        float spawnedAt,
        float baseShaderLifetime)
    {
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        float holdValue = EvaluateHold(in settings, in timeline);
        L2MaterialPropertyCopier.SetFloatOnMaterials(materials, L2MaterialPropertyCopier.HoldId, holdValue);

        Material holdLogMat = null;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].HasProperty(L2MaterialPropertyCopier.HoldId))
            {
                holdLogMat = materials[i];
                break;
            }
        }

        ApplyCastEndEmitterFade(materials, in timeline);
        ParticleSingleLifetimeDebug.TryLogHoldReleaseTransition(
            debugGroupName,
            in settings,
            in timeline,
            holdValue,
            holdLogMat,
            spawnedAt,
            baseShaderLifetime,
            timeline.SlotDuration,
            _hasRuntimeLoopOverride,
            _runtimeLoopOverrideValue,
            _castEndFadeRequested,
            ref _loggedReleaseStart,
            ref _loggedReleaseResume);

        return TryCompleteCastEndFade(materials, in settings, in timeline, holdValue, debugGroupName, spawnedAt, baseShaderLifetime);
    }

    public void ApplySpawnHoldSpecs(Material runtimeMat, in Settings settings, float holdValue)
    {
        if (runtimeMat == null)
        {
            return;
        }

        if (runtimeMat.HasProperty(L2MaterialPropertyCopier.HoldSizeReferenceId))
        {
            runtimeMat.SetFloat(L2MaterialPropertyCopier.HoldSizeReferenceId, settings.ShaderHold);
        }

        if (runtimeMat.HasProperty(L2MaterialPropertyCopier.HoldId))
        {
            runtimeMat.SetFloat(L2MaterialPropertyCopier.HoldId, holdValue);
        }
    }

    private void ApplyCastEndEmitterFade(Material[] materials, in CastTimeline timeline)
    {
        if (!_castEndFadeRequested || materials == null)
        {
            return;
        }

        float fadeMul = ComputeCastEndFadeMul(in timeline);
        L2MaterialPropertyCopier.SetFloatOnMaterials(
            materials,
            L2MaterialPropertyCopier.EmitterAlphaId,
            _baseEmitterAlpha * fadeMul);
    }

    private bool TryCompleteCastEndFade(
        Material[] materials,
        in Settings settings,
        in CastTimeline timeline,
        float holdValue,
        string debugGroupName,
        float spawnedAt,
        float baseShaderLifetime)
    {
        if (!_castEndFadeRequested)
        {
            return false;
        }

        float fadeMul = ComputeCastEndFadeMul(in timeline);
        float castDur = ResolveCastDuration(in timeline);
        float elapsed = ResolveCastElapsed(in timeline);

        bool castTimelineEnded = elapsed >= castDur - 0.02f;
        bool fadeAndHoldDone = holdValue <= 0.02f && fadeMul <= 0.02f;
        if (!castTimelineEnded && !fadeAndHoldDone)
        {
            return false;
        }

        ParticleSingleLifetimeDebug.EnsureHoldReleaseResumeLogged(
            debugGroupName,
            in settings,
            in timeline,
            holdValue,
            fadeMul,
            materials,
            spawnedAt: spawnedAt,
            baseShaderLifetime: baseShaderLifetime,
            slotDuration: timeline.SlotDuration,
            ref _loggedReleaseResume);

        ParticleSingleLifetimeDebug.LogHoldReleaseDone(debugGroupName, in timeline);
        CompleteImmediateStop();
        return true;
    }

    private float ComputeCastEndFadeMul(in CastTimeline timeline)
    {
        float castDur = ResolveCastDuration(in timeline);
        float elapsed = ResolveCastElapsed(in timeline);
        float fadeWindow = ResolveCastEndFadeWindow(in timeline);
        float fadeStart = Mathf.Max(0f, castDur - fadeWindow);
        return elapsed <= fadeStart
            ? 1f
            : 1f - Mathf.Clamp01((elapsed - fadeStart) / Mathf.Max(1e-4f, fadeWindow));
    }

    private float EvaluateHoldForCastNow(in Settings settings, in CastTimeline timeline)
    {
        if (!settings.ReleaseByCastProgress)
        {
            return settings.ShaderHold;
        }

        float dur = ResolveCastDuration(in timeline);
        float u = Mathf.Clamp01(ResolveCastElapsed(in timeline) / dur);
        return EvaluateHoldForNormalizedCast(u, in settings);
    }

    private static float EvaluateHoldForNormalizedCast(float u, in Settings settings)
    {
        if (!settings.ReleaseByCastProgress)
        {
            return settings.ShaderHold;
        }

        u = Mathf.Clamp01(u);
        float start = Mathf.Clamp(settings.ReleaseStartNormalized, 0f, 0.999f);
        if (u < start)
        {
            return settings.ShaderHold;
        }

        if (!settings.SmoothRelease)
        {
            return 0f;
        }

        if (start >= 1f - 1e-4f)
        {
            return 0f;
        }

        float t = Mathf.InverseLerp(start, 1f, u);
        return Mathf.Lerp(settings.ShaderHold, 0f, t);
    }

    public static float ResolveCastDuration(in CastTimeline timeline)
    {
        if (timeline.Settings != null && timeline.Settings.defaultLifeTime > 1e-4f)
        {
            return timeline.Settings.defaultLifeTime;
        }

        return Mathf.Max(1e-4f, timeline.SlotDuration);
    }

    public static float ResolveCastElapsed(in CastTimeline timeline)
    {
        return Mathf.Max(0f, timeline.Now - timeline.CastStartTime);
    }

    public static float ResolveCastEndFadeWindow(in CastTimeline timeline)
    {
        if (timeline.Settings != null && timeline.Settings.hideTime > 1e-4f)
        {
            return timeline.Settings.hideTime;
        }

        return 0.5f;
    }
}
