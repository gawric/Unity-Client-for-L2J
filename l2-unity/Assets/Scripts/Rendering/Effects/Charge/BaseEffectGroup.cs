using UnityEngine;

public abstract class BaseEffectGroup : EffectPart
{
    [Header("Base Spawning")]
    [SerializeField] protected Renderer[] _particles;
    [SerializeField] protected int _countPerSecond = 20;
    [SerializeField] protected int _maxCount = 20;
    [SerializeField] protected float _duration = 1f;

    protected bool _stopped = true;
    protected float _lastEnable;
    protected float _lastLoop;
    protected int _particleIndex = 0;
    protected Transform _followTarget;

    public virtual void ResetTimer(float duration, Transform followTarget = null)
    {
        _duration = duration;
        _followTarget = followTarget;
        _lastEnable = Now();
        _lastLoop = 0;
        _particleIndex = 0;
        _stopped = false;

        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<Renderer>(true);

        foreach (var p in _particles) p.gameObject.SetActive(false);


        if (_countPerSecond > _maxCount)
        {
            for (int i = 0; i < _maxCount; i++) ActivateParticle(_lastEnable);
        }
    }

    protected virtual void LateUpdate()
    {
        if (_stopped) return;


        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
            transform.rotation = _followTarget.rotation;
        }

        float now = Now();
        if (now - _lastEnable > _duration)
        {
            StopEffect();
            return;
        }

        if (_countPerSecond <= _maxCount && _countPerSecond > 0)
        {
            if (now - _lastLoop >= 1f / _countPerSecond)
            {
                _lastLoop = now;
                ActivateParticle(now);
            }
        }
    }

    protected virtual void ActivateParticle(float now)
    {
        if (_particleIndex >= _maxCount) _particleIndex = 0;

        GameObject go = _particles[_particleIndex].gameObject;
        go.SetActive(true);

        float seed = Random.Range(-100f, 100f);
        foreach (Material m in _particles[_particleIndex].materials)
        {
            ApplyShaderParams(m, now, seed);
        }
        _particleIndex++;
    }

    // Этот метод переопределяем в наследниках для специфичных данных
    protected abstract void ApplyShaderParams(Material m, float now, float seed);

    protected void StopEffect()
    {
        _stopped = true;
        foreach (var p in _particles) p.gameObject.SetActive(false);
    }

    protected float Now() => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;


    public override void PlayPart() => _stopped = false;

    public override void StopPart() => StopEffect();

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        if (settings != null) _duration = settings.defaultLifeTime;
    }
}
