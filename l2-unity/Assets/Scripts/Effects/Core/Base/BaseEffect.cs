using UnityEngine;
using System;
using System.Text;

public abstract class BaseEffect : MonoBehaviour
{
    protected MaterialPropertyBlock propBlock;
    protected Renderer targetRenderer;
    [SerializeField] protected Transform _owner;
    private float _scheduledDestroyAt = -1f;
    private float _scheduledLifeTime = -1f;
    private float _castStartTimeSnapshot = -1f;
    private float _castHitTimeSnapshot = -1f;
    private float _castFlightTimeSnapshot = -1f;
    private bool _deferDestroyUntilHomeArrival;
    private bool _lifetimeOwnedByProjectile;
    private bool _lifetimeOwnedByAuthoredStreams;
    private bool _lifetimeOwnedByHost;
    public virtual void Initialize()
    {
        propBlock = new MaterialPropertyBlock();
        targetRenderer = GetComponent<Renderer>();
    }

    public abstract void SetProgress(float normalizedTime); 
    public abstract void Play();

  
    public virtual void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        _owner = owner;
        if (castData != null)
        {
            _castStartTimeSnapshot = castData.StartTime;
            _castHitTimeSnapshot = castData.HitTime;
            _castFlightTimeSnapshot = castData.FlightTime;
        }
        else
        {
            _castStartTimeSnapshot = -1f;
            _castHitTimeSnapshot = -1f;
            _castFlightTimeSnapshot = -1f;
        }
    }

    public bool IsDestroyDeferredUntilHomeArrival => _deferDestroyUntilHomeArrival;

    public bool IsLifetimeOwnedByProjectile => _lifetimeOwnedByProjectile;

    public bool IsLifetimeOwnedByAuthoredStreams => _lifetimeOwnedByAuthoredStreams;

    public bool IsLifetimeOwnedByHost => _lifetimeOwnedByHost;

    /// <summary>
    /// NPC deco: do not DestoryEffect on Play. Owner (NpcDecoService) destroys the GO.
    /// </summary>
    public void BindLifetimeToHost()
    {
        _lifetimeOwnedByHost = true;
        CancelInvoke(nameof(BeginFadeOut));
    }

    /// <summary>
    /// Shot projectile: do not DestoryEffect on Play. ProjectileManager fires
    /// OnHitEffectProjectile then Destroys the GO.
    /// </summary>
    public void BindLifetimeToProjectile()
    {
        _lifetimeOwnedByProjectile = true;
        CancelInvoke(nameof(BeginFadeOut));
    }

    /// <summary>
    /// Skill _ta overlay (wind strike impact, heal target light): do not DestoryEffect
    /// on Play. ParticleGroupV2 plays its authored window, then the GO is destroyed.
    /// </summary>
    public void BindLifetimeToAuthoredStreams()
    {
        _lifetimeOwnedByAuthoredStreams = true;
        CancelInvoke(nameof(BeginFadeOut));
    }

    public void PrepareDestroyOnHomeArrival()
    {
        _deferDestroyUntilHomeArrival = true;
        CancelInvoke(nameof(BeginFadeOut));
    }

    public void BeginHomeArrivalFade(float destroyDelaySeconds)
    {
        _deferDestroyUntilHomeArrival = false;
        CancelInvoke(nameof(BeginFadeOut));
        DisableContinuousLoopOnChildren();
        BeginFadeOut();
        float delay = Mathf.Max(0.05f, destroyDelaySeconds);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[HOME_PROJECTILE] BeginHomeArrivalFade effect='{name}' destroyIn={delay:F3}s " +
            $"childGroups={GetComponentsInChildren<ParticleGroup>(true).Length} " +
            $"childSingles={GetComponentsInChildren<ParticleSingle>(true).Length}");
#endif

        Destroy(gameObject, delay);

        if (transform.parent != null && transform.parent.name == "HitPointProxy")
        {
            Destroy(transform.parent.gameObject, delay);
        }
    }

    public void CompleteHomeArrivalAndDestroy(float fadeSeconds = 0.35f)
    {
        BeginHomeArrivalFade(fadeSeconds);
    }

    public void DestroyHomeArrivalImmediate()
    {
        _deferDestroyUntilHomeArrival = false;
        CancelInvoke(nameof(BeginFadeOut));
        DisableContinuousLoopOnChildren();

        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null)
            {
                parts[i].StopPart();
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[HOME_PROJECTILE] DestroyHomeArrivalImmediate effect='{name}' " +
            $"renderers={renderers.Length} parts={parts.Length}");
#endif

        Destroy(gameObject);

        if (transform.parent != null && transform.parent.name == "HitPointProxy")
        {
            Destroy(transform.parent.gameObject);
        }
    }

    private void DisableContinuousLoopOnChildren()
    {
        ParticleGroup[] groups = GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null)
            {
                groups[i].SetRuntimeContinuousLoopOverride(true, false);
            }
        }

        ParticleSingle[] singles = GetComponentsInChildren<ParticleSingle>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            if (singles[i] != null)
            {
                singles[i].SetRuntimeContinuousLoopOverride(true, false);
            }
        }

        ParticleEmitterV2.StopAll(this);
    }

    public void DestoryEffect(EffectSettings settings, MagicCastData castData = null)
    {
        float fadeOutTime = Mathf.Max(0, settings.defaultLifeTime - settings.hideTime);
        _scheduledLifeTime = settings.defaultLifeTime;
        _scheduledDestroyAt = Time.time + _scheduledLifeTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Time.time;
        float globalSinceCastStart = _castStartTimeSnapshot > 0f ? now - _castStartTimeSnapshot : -1f;
        Debug.Log(
            $"[EffectDestroySchedule] effect='{name}' owner='{(_owner != null ? _owner.name : "null")}' " +
            $"now={now:F3}s globalSinceCastStart={globalSinceCastStart:F3}s " +
            $"lifeTime={settings.defaultLifeTime:F3}s hideTime={settings.hideTime:F3}s " +
            $"fadeStartIn={fadeOutTime:F3}s castHit={_castHitTimeSnapshot:F3}s " +
            $"castFlight={_castFlightTimeSnapshot:F3}s shader={BuildLifetimeShaderSnapshot()}\n" +
            $"scheduleStack={Environment.StackTrace}");
#endif

        Invoke(nameof(BeginFadeOut), fadeOutTime);
        
        // Удаляем сам эффект через заданное время
        Destroy(gameObject, settings.defaultLifeTime);
        
        // Проверяем, есть ли родитель с именем HitPointProxy и удаляем его тоже
        if (transform.parent != null && transform.parent.name == "HitPointProxy")
        {
            Destroy(transform.parent.gameObject, settings.defaultLifeTime);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DropAdenaDestroyLog.Event(
            "SCHEDULE_DESTROY",
            this,
            $"lifeTime={settings.defaultLifeTime:F3}s hideTime={settings.hideTime:F3}s " +
            $"fadeStartIn={fadeOutTime:F3}s destroyAt={_scheduledDestroyAt:F3}s " +
            $"alsoDestroyHitPointProxy={(transform.parent != null && transform.parent.name == "HitPointProxy")}",
            includeStack: true);
#endif
    }

    private string BuildLifetimeShaderSnapshot()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return "no_renderers";
        }

        StringBuilder sb = new StringBuilder();
        int maxRenderersToLog = 12;
        for (int i = 0; i < renderers.Length && i < maxRenderersToLog; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            Material mat = renderer.sharedMaterial;
            string hasLifetime = TryReadFloat(mat, "_HasLifeTime", "hasLifeTime");
            if (hasLifetime == "n/a")
            {
                hasLifetime = TryReadFloat(mat, "_HasLifetime", "hasLifetime");
            }

            string lifeTimeRange = TryReadVector(mat, "_LifeTimeRange", "lifeTimeRange");
            if (lifeTimeRange == "n/a")
            {
                lifeTimeRange = TryReadFloat(mat, "_LifeTimeRange", "lifeTimeRangeScalar");
            }
            if (lifeTimeRange == "n/a")
            {
                lifeTimeRange = TryReadVector(mat, "_LifetimeRange", "lifetimeRange");
            }
            if (lifeTimeRange == "n/a")
            {
                lifeTimeRange = TryReadFloat(mat, "_LifetimeRange", "lifetimeRangeScalar");
            }

            if (sb.Length > 0)
            {
                sb.Append(" | ");
            }

            sb.Append($"{renderer.name}:{mat.name}({hasLifetime},{lifeTimeRange})");
        }

        if (renderers.Length > maxRenderersToLog)
        {
            sb.Append($" | ... +{renderers.Length - maxRenderersToLog} renderers");
        }

        return sb.Length > 0 ? sb.ToString() : "no_material_data";
    }

    private string TryReadFloat(Material mat, string property, string label)
    {
        if (mat.HasProperty(property))
        {
            return $"{label}={mat.GetFloat(property):F3}";
        }

        return "n/a";
    }

    private string TryReadVector(Material mat, string property, string label)
    {
        if (mat.HasProperty(property))
        {
            Vector4 v = mat.GetVector(property);
            return $"{label}=({v.x:F3},{v.y:F3},{v.z:F3},{v.w:F3})";
        }

        return "n/a";
    }

    protected virtual void OnDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DropAdenaDestroyLog.Event(
            "ON_DESTROY",
            this,
            $"scheduledAt={_scheduledDestroyAt:F3}s scheduledLife={_scheduledLifeTime:F3}s " +
            $"castHit={_castHitTimeSnapshot:F3}s owner='{(_owner != null ? _owner.name : "null")}'",
            includeStack: true);
        if (_scheduledDestroyAt <= 0f)
        {
            return;
        }

        float now = Time.time;
        float globalSinceCastStart = _castStartTimeSnapshot > 0f ? now - _castStartTimeSnapshot : -1f;
        Debug.Log(
            $"[EffectDestroyed] effect='{name}' now={now:F3}s scheduledAt={_scheduledDestroyAt:F3}s " +
            $"globalSinceCastStart={globalSinceCastStart:F3}s castHit={_castHitTimeSnapshot:F3}s castFlight={_castFlightTimeSnapshot:F3}s " +
            $"deltaToCastHit={(globalSinceCastStart >= 0f && _castHitTimeSnapshot >= 0f ? globalSinceCastStart - _castHitTimeSnapshot : -1f):F3}s " +
            $"scheduledLifeTime={_scheduledLifeTime:F3}s shader={BuildLifetimeShaderSnapshot()}");
#endif
    }

    protected void BeginFadeOut()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Time.time;
        float globalSinceCastStart = _castStartTimeSnapshot > 0f ? now - _castStartTimeSnapshot : -1f;
        Debug.Log(
            $"[BeginFadeOut] effect='{name}' now={now:F3}s globalSinceCastStart={globalSinceCastStart:F3}s " +
            $"castHit={_castHitTimeSnapshot:F3}s castFlight={_castFlightTimeSnapshot:F3}s owner='{(_owner != null ? _owner.name : "null")}'.\n" +
            $"stack={Environment.StackTrace}");
#endif
        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);

        foreach (var part in parts)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DropAdenaDestroyLog.Event(
                "BEGIN_FADEOUT_STOPPART",
                part,
                $"fromEffect='{name}'",
                includeStack: false);
#endif
            part.StopPart();
        }
    }

}