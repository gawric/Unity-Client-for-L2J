using UnityEngine;

public class L2Particle : BaseEffect
{
    private const string LifetimeTraceEffectName = "el_wind_strike_ta";

    [Tooltip("Не вызывать DestoryEffect при Play — объект не удаляется по таймеру (отладка шейдера, Hold и т.п.). Выключено по умолчанию.")]
    [SerializeField] private bool _skipScheduledDestroyForDebug;

    [SerializeField] private Vector3 _surfaceNormal;
    [SerializeField] private PooledEffect _pooledEffect;
    [SerializeField] private EffectPart[] _particleGroups;
    private EffectSettings _settings;
    private MagicCastData _castData;
    private float _playStartedAt = -1f;
    public PooledEffect PooledEffect { get { return _pooledEffect; } }
    public Vector3 SurfaceNormal { get { return _surfaceNormal; } set { _surfaceNormal = value; } }

    private void Awake()
    {
        _pooledEffect.ResetTimerCallback = () =>
        {
            ResetTimer();
        };
    }

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {

        base.Setup(settings, castData, owner);

        _settings = settings;
        _castData = castData;


        if (castData != null)
        {
            //_settings.defaultLifeTime = castData.HitTime;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceLifetime())
        {
            Debug.Log(
                $"[TA_LIFETIME_SETUP] effect='{name}' now={Time.time:F3}s " +
                $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"hideTime={(_settings != null ? _settings.hideTime : -1f):F3}s " +
                $"castStart={(_castData != null ? _castData.StartTime : -1f):F3}s " +
                $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                $"castFlight={(_castData != null ? _castData.FlightTime : -1f):F3}s " +
                $"owner='{(_owner != null ? _owner.name : "null")}'.");
        }
#endif

    }


    public override void SetProgress(float normalizedTime)
    {
        throw new System.NotImplementedException();
    }

    public override void Play()
    {
        _playStartedAt = Time.time;
        ResetTimer();
        if (!_skipScheduledDestroyForDebug)
        {
            DestoryEffect(_settings, _castData);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_skipScheduledDestroyForDebug)
        {
            Debug.Log($"[L2Particle] SkipScheduledDestroyForDebug: '{name}' — DestoryEffect не вызывается.");
        }

        if (ShouldTraceLifetime())
        {
            Debug.Log(
                $"[TA_LIFETIME_PLAY] effect='{name}' playAt={_playStartedAt:F3}s " +
                $"scheduledLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"scheduledHide={(_settings != null ? _settings.hideTime : -1f):F3}s skipDestroy={_skipScheduledDestroyForDebug}.");
        }
#endif
    }

    public void ResetTimer()
    {
        if (_particleGroups == null || _particleGroups.Length == 0)
        {
            _particleGroups = GetComponentsInChildren<EffectPart>();
        }

        for (int i = 0; i < _particleGroups.Length; i++)
        {
            _particleGroups[i].Setup(_settings, _castData);

            if (_owner != null)
            {
                _particleGroups[i].OwnerTarget = _owner;
            }

            if(_settings == null)
            {
                Debug.Log("L2Particle>ResetTimer _settings is null");
                return ;
            }
            
            _particleGroups[i].FollowTarget = _settings.isFollowCaster ? _owner : null;

            if (!_settings.isFollowCaster)
            {
                _particleGroups[i].SurfaceNormal = _surfaceNormal;
            }

            _particleGroups[i].PlayPart();
        }
    }

    protected override void OnDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceLifetime())
        {
            float now = Time.time;
            float elapsed = _playStartedAt > 0f ? now - _playStartedAt : -1f;
            Debug.Log(
                $"[TA_LIFETIME_DESTROY] effect='{name}' now={now:F3}s elapsedSincePlay={elapsed:F3}s " +
                $"configuredLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s.");
        }
#endif
        base.OnDestroy();
    }

    private bool ShouldTraceLifetime()
    {
        return !string.IsNullOrEmpty(name) && name.IndexOf(LifetimeTraceEffectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

}
