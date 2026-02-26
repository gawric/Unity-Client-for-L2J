using UnityEngine;

public abstract class BaseEffectGroup : EffectPart
{
    [Header("Base Spawning")]
    [SerializeField] protected EffectInstance[] _effectInstances;

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

        if (_effectInstances == null || _effectInstances.Length == 0)
        {
            return;
        }

        foreach (var inst in _effectInstances) inst.Deactivate();

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
            transform.SetPositionAndRotation(_followTarget.position, _followTarget.rotation);
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
        if (_effectInstances == null || _effectInstances.Length == 0) return;
        if (_particleIndex >= _maxCount) _particleIndex = 0;

        float seed = UnityEngine.Random.Range(-100f, 100f);

        // Просто вызываем метод обертки
        _effectInstances[_particleIndex].Activate(now, seed , ApplyShaderParams);

        _particleIndex++;
    }

    protected abstract void ApplyShaderParams(Component source, float now, float seed);

    protected void StopEffect()
    {
        _stopped = true;
        if (_effectInstances == null) return;
        foreach (var inst in _effectInstances) inst.Deactivate();
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
