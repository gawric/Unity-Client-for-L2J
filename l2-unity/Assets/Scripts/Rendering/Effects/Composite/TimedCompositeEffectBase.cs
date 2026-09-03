using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class TimedCompositeEffectBase : BaseEffect
{
    protected EffectSettings _settings;
    protected MagicCastData _castData;
    protected EffectSettings _rootRuntimeSettings;

    private readonly List<EffectSettings> _runtimeSettings = new List<EffectSettings>();

    protected abstract string DebugPrefix { get; }
    protected virtual float RuntimeLifeTimeTailSeconds => 0f;

    protected void InitializeTimedComposite(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        _rootRuntimeSettings = CreateRuntimeSettings(settings);
    }

    protected EffectSettings CreateRuntimeSettings(EffectSettings sourceSettings, bool applyTimedLifetime = true)
    {
        if (sourceSettings == null)
        {
            return null;
        }

        EffectSettings runtime = Instantiate(sourceSettings);
        _runtimeSettings.Add(runtime);

        if (!applyTimedLifetime || _castData == null)
        {
            return runtime;
        }

        float timedLife = ResolveCastTimedLifetimeSeconds();
        if (timedLife <= 0f)
        {
            return runtime;
        }

        runtime.defaultLifeTime = timedLife;
        runtime.hideTime = Mathf.Min(runtime.hideTime, runtime.defaultLifeTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Runtime settings cloned. Source='{sourceSettings.name}' " +
            $"timed={applyTimedLifetime} " +
            $"hitTime={_castData.HitTime:F3}s animDur={_castData.SkillAnimationDuration:F3}s " +
            $"tail={RuntimeLifeTimeTailSeconds:F3}s " +
            $"lifeTime={runtime.defaultLifeTime:F3}s hideTime={runtime.hideTime:F3}s.");
#endif

        return runtime;
    }

    protected virtual float ResolveCastTimedLifetimeSeconds()
    {
        if (_castData == null || _castData.HitTime <= 0f)
        {
            return -1f;
        }

        return _castData.HitTime + Mathf.Max(0f, RuntimeLifeTimeTailSeconds);
    }

    protected EffectSettings SelectLifetimeSettings()
    {
        return _rootRuntimeSettings != null ? _rootRuntimeSettings : _settings;
    }

    protected void CleanupRuntimeSettings()
    {
        for (int i = 0; i < _runtimeSettings.Count; i++)
        {
            if (_runtimeSettings[i] != null)
            {
                Destroy(_runtimeSettings[i]);
            }
        }

        _runtimeSettings.Clear();
        _rootRuntimeSettings = null;
    }

    protected void StopAndClearCoroutine(ref Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
        routine = null;
    }

    protected void UnsubscribeShootEventSources(
        List<AnimationEventsBase> shootEventSources,
        Action<string> onAnimationShootHandler,
        ref AnimationEventsBase animationEvents,
        ref bool isSubscribedToAnyShoot)
    {
        CompositePlaybackSubscriptions.UnsubscribeShoot(
            shootEventSources,
            onAnimationShootHandler,
            ref isSubscribedToAnyShoot);
        animationEvents = null;
    }

    protected virtual void OnTimedCompositeDestroy()
    {
    }

    protected override void OnDestroy()
    {
        OnTimedCompositeDestroy();
        CleanupRuntimeSettings();
        base.OnDestroy();
    }
}
