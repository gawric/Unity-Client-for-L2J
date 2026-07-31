using System.Collections.Generic;
using UnityEngine;

public class L2Particle : BaseEffect, IRegisteredBillboard
{
    private const string LifetimeTraceEffectName = "el_wind_strike_ta";

    [Tooltip("Не вызывать DestoryEffect при Play — объект не удаляется по таймеру (отладка шейдера, Hold и т.п.). Выключено по умолчанию.")]
    [SerializeField] private bool _skipScheduledDestroyForDebug;

    [SerializeField] private Vector3 _surfaceNormal;
    [Header("Billboard")]
    [SerializeField] private bool _billboardToCamera;
    [SerializeField] private Vector3 _billboardRotationOffsetEuler;
    [SerializeField] private PooledEffect _pooledEffect;
    [SerializeField] private EffectPart[] _particleGroups;
    private EffectSettings _settings;
    private MagicCastData _castData;
    private float _playStartedAt = -1f;
    public PooledEffect PooledEffect { get { return _pooledEffect; } }
    public Vector3 SurfaceNormal { get { return _surfaceNormal; } set { _surfaceNormal = value; } }

    private void OnEnable()
    {
        if (_billboardToCamera)
        {
            RegisteredBillboards.Register(this);
        }
    }

    private void OnDisable()
    {
        if (_billboardToCamera)
        {
            RegisteredBillboards.Unregister(this);
        }
    }

    private void Awake()
    {
        _pooledEffect.ResetTimerCallback = () =>
        {
            ResetTimer();
        };
    }

    public void OnCameraPreRender(Camera camera)
    {
        ApplyBillboardRotation(camera);
    }

    private void LateUpdate()
    {
        if (_billboardToCamera)
        {
            ApplyBillboardRotation(Camera.main);
        }
    }

    private void ApplyBillboardRotation(Camera camera)
    {
        if (!_billboardToCamera || camera == null)
        {
            return;
        }

        // Copy camera orientation so the whole mesh turns as an object, preserving its authored 3D shape.
        transform.rotation = camera.transform.rotation * Quaternion.Euler(_billboardRotationOffsetEuler);
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
        if (!_skipScheduledDestroyForDebug && !IsDestroyDeferredUntilHomeArrival)
        {
            DestoryEffect(_settings, _castData);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_skipScheduledDestroyForDebug)
        {
            Debug.Log($"[L2Particle] SkipScheduledDestroyForDebug: '{name}' — DestoryEffect не вызывается.");
        }

        if (ShouldTraceLifetime() || IsPowerStrikeEffect())
        {
            Debug.Log(
                $"[POWER_STRIKE_TIMING] FX_PLAY effect='{name}' playAt={_playStartedAt:F3}s " +
                $"scheduledLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"scheduledHide={(_settings != null ? _settings.hideTime : -1f):F3}s " +
                $"settingsAsset='{(_settings != null ? _settings.name : "null")}' " +
                $"shaderLife={BuildShaderLifetimeRangesSnapshot()} " +
                $"skipDestroy={_skipScheduledDestroyForDebug}.");
        }
#endif
    }

    public void ResetTimer()
    {
        RefreshParticleGroups();

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

    private void RefreshParticleGroups()
    {
        if (_particleGroups == null || _particleGroups.Length == 0)
        {
            _particleGroups = GetComponentsInChildren<EffectPart>();
            return;
        }

        List<EffectPart> refreshed = new List<EffectPart>(_particleGroups.Length + 1);
        for (int i = 0; i < _particleGroups.Length; i++)
        {
            if (_particleGroups[i] != null && !refreshed.Contains(_particleGroups[i]))
            {
                refreshed.Add(_particleGroups[i]);
            }
        }

        EffectPart[] children = GetComponentsInChildren<EffectPart>();
        for (int i = 0; i < children.Length; i++)
        {
            EffectPart part = children[i];
            if (part == null || refreshed.Contains(part))
            {
                continue;
            }

            if (part is ParticleGroup group && group.IsHomeFlightAnchor)
            {
                refreshed.Add(part);
            }
        }

        _particleGroups = refreshed.ToArray();
    }

    protected override void OnDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceLifetime() || IsPowerStrikeEffect())
        {
            float now = Time.time;
            float elapsed = _playStartedAt > 0f ? now - _playStartedAt : -1f;
            Debug.Log(
                $"[POWER_STRIKE_TIMING] FX_DESTROY effect='{name}' now={now:F3}s " +
                $"wallLived={elapsed:F3}s configuredLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s.");
        }
#endif
        base.OnDestroy();
    }

    private bool ShouldTraceLifetime()
    {
        return !string.IsNullOrEmpty(name) && name.IndexOf(LifetimeTraceEffectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsPowerStrikeEffect()
    {
        return !string.IsNullOrEmpty(name) &&
               name.IndexOf("power_striker", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string BuildShaderLifetimeRangesSnapshot()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return "no_renderers";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int logged = 0;
        for (int i = 0; i < renderers.Length && logged < 6; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
            {
                continue;
            }

            Material[] mats = r.sharedMaterials;
            if (mats == null)
            {
                continue;
            }

            for (int m = 0; m < mats.Length && logged < 6; m++)
            {
                Material mat = mats[m];
                if (mat == null || !mat.HasProperty("_LifetimeRange"))
                {
                    continue;
                }

                Vector4 life = mat.GetVector("_LifetimeRange");
                if (sb.Length > 0)
                {
                    sb.Append(" | ");
                }

                sb.Append(mat.name).Append(" life=(").Append(life.x.ToString("F3")).Append(',').Append(life.y.ToString("F3")).Append(')');
                logged++;
            }
        }

        return sb.Length > 0 ? sb.ToString() : "no_LifetimeRange";
    }

}
