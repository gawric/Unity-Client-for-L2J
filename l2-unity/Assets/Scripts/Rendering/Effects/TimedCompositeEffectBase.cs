using System.Collections.Generic;
using UnityEngine;

public abstract class TimedCompositeEffectBase : BaseEffect
{
    protected EffectSettings _settings;
    protected MagicCastData _castData;
    protected EffectSettings _rootRuntimeSettings;

    private readonly List<EffectSettings> _runtimeSettings = new List<EffectSettings>();

    protected abstract string DebugPrefix { get; }

    protected void InitializeTimedComposite(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        _rootRuntimeSettings = CreateRuntimeSettings(settings);
    }

    protected EffectSettings CreateRuntimeSettings(EffectSettings sourceSettings)
    {
        if (sourceSettings == null)
        {
            return null;
        }

        if (_castData == null || _castData.HitTime <= 0f)
        {
            return sourceSettings;
        }

        EffectSettings runtime = Instantiate(sourceSettings);
        runtime.defaultLifeTime = _castData.HitTime;
        runtime.hideTime = Mathf.Min(runtime.hideTime, runtime.defaultLifeTime);
        _runtimeSettings.Add(runtime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Runtime settings cloned. Source='{sourceSettings.name}' " +
            $"hitTime={_castData.HitTime:F3}s hideTime={runtime.hideTime:F3}s.");
#endif

        return runtime;
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
}

