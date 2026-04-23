
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

    private bool _stopped;
    private float _lastEnable;
    private float _lastLoop;
    private int _particleIndex = 0;
    private int _spawnedCount = 0;

    private float[] _particleSpawnTimes;
    private bool[] _isParticleActive;

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
        if (_spawnedCount < _maxCount || _forceContinuousSpawning)
        {
            if (_isBurstSpawning)
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
            //Debug.Log($"<color=red>[Effect STOP]</color> {gameObject.name} полностью завершен.");
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
        foreach (Material m in _particles[_particleIndex].materials)
        {
           // Debug.Log("Set Start Time " + shaderStartTime + " Seed " + seed + "name " + m.name);
            m.SetFloat("_StartTime", shaderStartTime);
            m.SetFloat("_Seed", seed);
            if (SurfaceNormal != Vector3.zero)
                m.SetVector("_SurfaceNormals", SurfaceNormal);
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

        if (_duration < 0.01f) _duration = GetLifeTimeFromMaterial();

        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<Renderer>(true);

        _particleSpawnTimes = new float[_particles.Length];
        _isParticleActive = new bool[_particles.Length];

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

    public void FitToOwnerWidth(Transform target)
    {
        if (target == null) return;
        var controller = target.GetComponent<CharacterController>();
        if (controller == null) return;
        float targetWidth = controller.radius * 2f;
        transform.localScale = new Vector3(targetWidth * 4f, 1f, targetWidth * 4f);
    }

    public override void Setup(EffectSettings s, MagicCastData c) { }
    public override void StopPart() { _stopped = true; }
}