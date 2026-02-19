using UnityEngine;

public class L2ChargeGroup : EffectPart
{
    [SerializeField] private L2Particle _owner;
    [SerializeField] private Renderer[] _particles;
    [SerializeField] private bool _hasFixedDuration = true;
    [SerializeField] private float _duration = 5f;
    public Vector3 SurfaceNormal { get; set; }
    private Transform _casterTransform;
    private Transform _followTarget;
    private Vector3 _ownerPosition;
    private int _particleIndex = 0;
    public int CountPerSecond { get => _countPerSecond; set => _countPerSecond = value; }
    public int MaxCount { get => _maxCount; set => _maxCount = value; }

    [Header("Spawning")]
    [SerializeField] private int _countPerSecond;
    [SerializeField] private int _maxCount;

    private bool _stopped;
    private float _lastEnable;
    private float _lastLoop;

    public Transform CasterTransform
    {
        get { return _casterTransform; }
        set { _casterTransform = value; }
    }

    public Vector3 OwnerPosition
    {
        get { return _ownerPosition; }
        set { _ownerPosition = value; }
    }


    public void FixedUpdate()
    {
        if (_stopped) return;

        if (_followTarget != null)
        {
            OwnerPosition = _followTarget.position;
            transform.position = _followTarget.position;
        }

        float now = Now();

        // Таймер завершения
        if (_hasFixedDuration && (now - _lastEnable > _duration))
        {
            _stopped = true;
            for (int i = 0; i < _particles.Length; i++) _particles[i].gameObject.SetActive(false);
            return;
        }

        // Обычный спаун (если мы НЕ активировали всё сразу в ResetTimer)
        if (_countPerSecond <= _maxCount && _countPerSecond > 0)
        {
            if (now - _lastLoop >= 1f / _countPerSecond)
            {
                _lastLoop = now;
                ActivateParticle(now);
            }
        }
    }


    public override void PlayPart()
    {
        throw new System.NotImplementedException();
    }

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        throw new System.NotImplementedException();
    }

    public override void StopPart()
    {
        throw new System.NotImplementedException();
    }


    public void ResetTimer(float duration, Transform target, Transform followTarget = null)
    {
        if (followTarget != null) _followTarget = followTarget;

        _lastEnable = Now();

        if (duration > 0.1f)
        {
            _duration = duration;
            _hasFixedDuration = true; 
        }


        if (_particles == null || _particles.Length == 0)
        {
            _particles = GetComponentsInChildren<Renderer>();
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            _particles[i].gameObject.SetActive(false);
        }



        if (_countPerSecond > _maxCount)
        {
            for (int i = 0; i < _maxCount; i++)
            {
                ActivateParticle(_lastEnable);
            }
        }

        _stopped = false;

    }

    private void ActivateParticle(float now)
    {
        if (_particleIndex >= _maxCount)
        {
            _particleIndex = 0;
        }

        _particles[_particleIndex].gameObject.SetActive(true);

        float seed = Random.Range(-100f, 100f);
        foreach (Material m in _particles[_particleIndex].materials)
        {
            m.SetFloat("_StartTime", now);
            m.SetFloat("_Seed", seed);
            if (SurfaceNormal != Vector3.zero)
                m.SetVector("_SurfaceNormals", SurfaceNormal);

            // --- НОВЫЙ КОД (Синхронизация времени жизни) ---
            // Получаем задержку из материала (если она есть)
            float initialDelay = m.GetVector("_InitialDelayRange").y;

            // Устанавливаем время жизни равным нашему duration (например, 0.4 для даггера)
            // Это заставит шейдер проиграть анимацию быстрее
            m.SetVector("_LifetimeRange", new Vector4(_duration + initialDelay, _duration + initialDelay, 0, 0));

            // Настраиваем плавное исчезновение (Fadeout) на 90% пути
            m.SetFloat("_FadeoutStartTime", (_duration + initialDelay) * 0.90f);
        }

        _particleIndex++;
    }
}
