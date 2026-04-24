
using UnityEngine;

public class ParticleGroup : EffectPart
{
    [SerializeField] private L2Particle _owner;
    [SerializeField] private Renderer[] _particles;
    [Header("Spawning (Настройки появления)")]
    [SerializeField] private float _startDelay = 0f;    // ЗАДЕРЖКА ПЕРЕД СТАРТОМ (в сек)
    [SerializeField] private int _countPerSecond = 15;
    [SerializeField] private int _maxCount = 2;
    [SerializeField] private bool _forceContinuousSpawning;

    [Space(10)]
    [SerializeField] private bool _isBurstSpawning;    // Мгновенный выстрел (после задержки)
    [SerializeField] private float _relativeWarmupTime; // Прогрев (для колец)

    [Header("Loop & Timing")]
    [SerializeField] private float _duration = 0.2f;    // Индивидуальная жизнь частицы
    [SerializeField] private bool _hasFixedDuration = true;
    [SerializeField] private bool _instantKillAtCastEnd;
    [SerializeField] private bool _fitToBounds;

    // Prevent auto-running in scene before explicit PlayPart/Setup.
    private bool _stopped = true;
    private float _lastEnable;
    private float _lastLoop;
    private int _particleIndex = 0;
    private int _spawnedCount = 0;
    private float _debugPlayStartedAt;

    private float[] _particleSpawnTimes;
    private bool[] _isParticleActive;
    private float _baseShaderLifetime = -1f;
    private bool _runtimeContinuousLoop;
    private bool _hasRuntimeContinuousLoopOverride;
    private bool _runtimeContinuousLoopOverrideValue;

    public void FixedUpdate()
    {
        if (_stopped) return;

        float now = Now();
        float timeSinceEnable = now - _lastEnable;

        // 1. ПРОВЕРКА ЗАДЕРЖКИ СТАРТА ГРУППЫ
        if (timeSinceEnable < _startDelay) return;

        // 2. КОНТРОЛЬ СМЕРТИ ЧАСТИЦ (Индивидуально)
        bool anyActive = false;
        if (_particles != null && _isParticleActive != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_isParticleActive[i])
                {
                    anyActive = true;
                    if (now - _particleSpawnTimes[i] >= _duration)
                    {
                        //Debug.Log($"<color=orange>[Particle DIE]</color> {gameObject.name} слот [{i}] выключен.");
                        _particles[i].gameObject.SetActive(false);
                        _isParticleActive[i] = false;
                    }
                }
            }
        }

        // 3. ЛОГИКА СПАВНА
        bool shouldLoopContinuously = _forceContinuousSpawning || _runtimeContinuousLoop;
        if (_spawnedCount < _maxCount || shouldLoopContinuously)
        {
            if (_isBurstSpawning && !shouldLoopContinuously)
            {
                // Если это Burst - выстреливаем всё сразу ОДИН РАЗ после задержки
                //Debug.Log($"<color=yellow>[Burst SPAWN]</color> {gameObject.name}: Мгновенный запуск {_maxCount} частиц через {_startDelay}с.");
                for (int i = 0; i < _maxCount; i++)
                {
                    ActivateParticle(now);
                    _spawnedCount++;
                }
            }
            else
            {
                // Обычная очередь (для колец)
                float spawnInterval = 1f / _countPerSecond;
                if (now - _lastLoop >= spawnInterval)
                {
                    _lastLoop = now;
                    ActivateParticle(now);
                    _spawnedCount++;
                }
            }
        }
        else if (!anyActive)
        {
            _stopped = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float elapsed = now - _debugPlayStartedAt;
            Debug.Log(
                $"[ParticleGroupStop] group='{name}' elapsed={elapsed:F3}s duration={_duration:F3}s " +
                $"spawned={_spawnedCount}/{_maxCount} forceContinuous={_forceContinuousSpawning} " +
                $"burst={_isBurstSpawning} castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"effectRoot='{(_owner != null ? _owner.name : "null")}' settings='{(_settings != null ? _settings.name : "null")}'.");
#endif
        }
    }

    private void ActivateParticle(float now)
    {
        if (_particles == null || _particles.Length == 0) return;
        if (_particleIndex >= _particles.Length) _particleIndex = 0;

        if (_particleSpawnTimes == null || _particleSpawnTimes.Length != _particles.Length)
        {
            _particleSpawnTimes = new float[_particles.Length];
            _isParticleActive = new bool[_particles.Length];
        }

        GameObject pObj = _particles[_particleIndex].gameObject;
        pObj.SetActive(true);

        _particleSpawnTimes[_particleIndex] = now;
        _isParticleActive[_particleIndex] = true;

        // Эмуляция прогрева (Warmup)
        float shaderStartTime = now - _relativeWarmupTime;

        //Debug.Log($"<color=green>[Particle SPAWN]</color> {gameObject.name} слот [{_particleIndex}] в {now:F3}с.");

        float seed = Random.Range(-100f, 100f);
        Material[] runtimeMaterials = _particles[_particleIndex].materials;
        Material[] sharedMaterials = _particles[_particleIndex].sharedMaterials;
        for (int materialIndex = 0; materialIndex < runtimeMaterials.Length; materialIndex++)
        {
            Material m = runtimeMaterials[materialIndex];
            if (m == null)
            {
                continue;
            }

            // Keep alpha exactly as configured in shared material.
            if (m.HasProperty("_Alpha") && sharedMaterials != null && materialIndex < sharedMaterials.Length)
            {
                Material shared = sharedMaterials[materialIndex];
                if (shared != null && shared.HasProperty("_Alpha"))
                {
                    m.SetFloat("_Alpha", shared.GetFloat("_Alpha"));
                }
            }

            // Debug.Log("Set Start Time " + shaderStartTime + " Seed " + seed + "name " + m.name);
            m.SetFloat("_StartTime", shaderStartTime);
            m.SetFloat("_Seed", seed);
            if (SurfaceNormal != Vector3.zero)
            {
                m.SetVector("_SurfaceNormals", SurfaceNormal);
            }
        }

        _particleIndex++;
    }

    public override void PlayPart()
    {
        if (_fitToBounds && OwnerTarget != null) FitToOwnerWidth(OwnerTarget);

        _lastEnable = Now();
        _lastLoop = 0;
        _particleIndex = 0;
        _spawnedCount = 0;
        _stopped = false;
        _debugPlayStartedAt = _lastEnable;

        if (_duration < 0.01f) _duration = GetLifeTimeFromMaterial();

        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<Renderer>(true);

        _particleSpawnTimes = new float[_particles.Length];
        _isParticleActive = new bool[_particles.Length];
        _baseShaderLifetime = Mathf.Max(0.01f, GetLifeTimeFromMaterial());
        _runtimeContinuousLoop = !_hasFixedDuration && _duration > _baseShaderLifetime + 0.05f;
        if (_hasRuntimeContinuousLoopOverride)
        {
            _runtimeContinuousLoop = _runtimeContinuousLoopOverrideValue;
        }

        for (int i = 0; i < _particles.Length; i++)
            if (_particles[i] != null) _particles[i].gameObject.SetActive(false);

        //Debug.Log($"<color=cyan>[Effect START]</color> {gameObject.name}. Ожидание старта: {_startDelay}с.");
    }

    private float GetLifeTimeFromMaterial()
    {
        if (_particles == null || _particles.Length == 0) return _duration;
        foreach (Material m in _particles[0].sharedMaterials)
        {
            if (m.HasProperty("_LifetimeRange")) return m.GetVector("_LifetimeRange").y;
        }
        return 0.5f;
    }

    private float Now() => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

    public void SetRuntimeContinuousLoopOverride(bool hasOverride, bool value)
    {
        _hasRuntimeContinuousLoopOverride = hasOverride;
        _runtimeContinuousLoopOverrideValue = value;
    }

    public void FitToOwnerWidth(Transform target)
    {
        if (target == null) return;
        var controller = target.GetComponent<CharacterController>();
        if (controller == null) return;
        float targetWidth = controller.radius * 2f;
        transform.localScale = new Vector3(targetWidth * 4f, 1f, targetWidth * 4f);
    }

    public override void Setup(EffectSettings s, MagicCastData c)
    {
        _settings = s;
        _castData = c;
        float durationBefore = _duration;

        // For non-fixed groups keep emitter alive up to the largest runtime target:
        // - cast hit timing
        // - runtime settings lifetime (may include additional tail)
        if (!_hasFixedDuration)
        {
            float castHitDuration = (_castData != null && _castData.HitTime > 0f) ? _castData.HitTime : 0f;
            float settingsDuration = (_settings != null && _settings.defaultLifeTime > 0f) ? _settings.defaultLifeTime : 0f;
            float legacyHitDuration = 0f;
            if (castHitDuration <= 0f && settingsDuration <= 0f && EffectSkillsmanager.Instance != null)
            {
                float legacyHitTimeMs = EffectSkillsmanager.Instance.HitTime();
                if (legacyHitTimeMs > 0f)
                {
                    legacyHitDuration = legacyHitTimeMs / 1000f;
                }
            }

            _duration = Mathf.Max(_duration, castHitDuration, settingsDuration, legacyHitDuration);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[ParticleGroupSetup] group='{name}' fixedDuration={_hasFixedDuration} " +
            $"durationBefore={durationBefore:F3}s durationAfter={_duration:F3}s " +
            $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
            $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s castFlight={(_castData != null ? _castData.FlightTime : -1f):F3}s " +
            $"legacyHit={((EffectSkillsmanager.Instance != null) ? EffectSkillsmanager.Instance.HitTime() / 1000f : -1f):F3}s " +
            $"baseShaderLife={_baseShaderLifetime:F3}s runtimeLoop={_runtimeContinuousLoop} " +
            $"loopOverride={_hasRuntimeContinuousLoopOverride}:{_runtimeContinuousLoopOverrideValue}");
#endif
    }
    public override void StopPart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Now();
        float elapsed = _debugPlayStartedAt > 0f ? now - _debugPlayStartedAt : -1f;
        Debug.Log(
            $"[ParticleGroupStopPart] group='{name}' elapsed={elapsed:F3}s duration={_duration:F3}s " +
            $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
            $"effectRoot='{(_owner != null ? _owner.name : "null")}' settings='{(_settings != null ? _settings.name : "null")}'.");
#endif
        _stopped = true;
    }
}