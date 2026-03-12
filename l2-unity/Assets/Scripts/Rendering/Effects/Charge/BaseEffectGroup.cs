using System;
using UnityEngine;

public abstract class BaseEffectGroup : EffectPart
{
    [Header("Base Spawning")]
    [SerializeField] protected EffectInstance[] _effectInstances;

    [SerializeField] protected int _countPerSecond = 20;
    [SerializeField] protected int _maxCount = 20;
    [SerializeField] protected float _duration = 1f;
    [SerializeField] protected bool useFixedLifetime = false;
    [SerializeField] protected float minLifeTime = 0;
    [SerializeField] protected float _startDelay = 0f; // Задержка перед началом спавна (в секундах)

    protected bool _stopped = true;
    protected float _lastEnable;
    protected float _lastLoop;
    protected int _particleIndex = 0;

    public override void PlayPart()
    {
        _lastEnable = Now();
        _lastLoop = 0;
        _particleIndex = 0;
        _stopped = false;

        if (_effectInstances == null || _effectInstances.Length == 0)
        {
            return;
        }

        foreach (var inst in _effectInstances) inst.Deactivate();

        // Если нужно заспавнить всё сразу (Burst) И задержки нет, спавним сразу.
        // Иначе это будет обработано в LateUpdate после истечения задержки.
        if (_countPerSecond > _maxCount && _startDelay <= 0f)
        {
            for (int i = 0; i < _maxCount; i++) ActivateParticle(_lastEnable);
            // Если мы заспавнили сразу, то countPerSecond будет > maxCount, и в LateUpdate ничего происходить не будет
        }
    }

    protected virtual void LateUpdate()
    {
        if (_stopped) return;

        if (FollowTarget != null)
        {
            transform.SetPositionAndRotation(FollowTarget.position, FollowTarget.rotation);
        }

        float now = Now();
        float timeSinceEnable = now - _lastEnable;

        // Если прошло меньше времени, чем задержка, просто выходим
        if (timeSinceEnable < _startDelay) return;

        // Корректируем время жизни, чтобы оно считалось ПОСЛЕ задержки
        if (timeSinceEnable - _startDelay > _duration)
        {
            StopEffect();
            return;
        }

        if (_countPerSecond > _maxCount && _startDelay > 0f)
        {
            // Здесь обрабатываем Burst-спавн, если была задержка
            if (timeSinceEnable >= _startDelay && _particleIndex == 0)
            {
                for (int i = 0; i < _maxCount; i++) ActivateParticle(now);
            }
        }
        else if (_countPerSecond <= _maxCount && _countPerSecond > 0)
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
        try
        {
            if (_effectInstances == null || _effectInstances.Length == 0) return;
            if (_particleIndex >= _maxCount) _particleIndex = 0;

            float seed = UnityEngine.Random.Range(-100f, 100f);

            Debug.Log($"ActivateParticle: {_particleIndex}");
            _effectInstances[_particleIndex].Activate(now, seed, ApplyShaderParams);

            _particleIndex++;
        }
        catch(IndexOutOfRangeException ex)
        {
            Debug.LogWarning("BaseEffectGroup>ActivateParticle: ошибка не смогли активировать частицу " + ex.Message);
        }

    }

    protected abstract void ApplyShaderParams(Component source, float now, float seed);

    protected void StopEffect()
    {
        _stopped = true;
        if (_effectInstances == null) return;
        foreach (var inst in _effectInstances) inst.Deactivate();
    }

    protected float Now() => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
    public override void StopPart() => StopEffect();

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        //if (settings != null) _duration = settings.defaultLifeTime;
    }
}
