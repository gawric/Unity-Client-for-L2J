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
            part.StopPart();
        }
    }

}