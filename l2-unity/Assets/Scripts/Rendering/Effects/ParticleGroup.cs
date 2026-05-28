using System.Collections.Generic;
using UnityEngine;

public class ParticleGroup : EffectPart
{
    private const string DebugTraceEffectName = "el_wind_strike_ta";
    private const string DebugTraceHealEffectToken = "wh_heal";
    private const string IceBoltTaEffectName = "el_ice_bolt_ta";
    private const string IcebergGroupName = "iceberg";
    private const string IcefragGroupName = "icefrag";
    private const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    [SerializeField] private L2Particle _owner;
    [SerializeField] private Renderer[] _particles;
    [Header("Spawning (Настройки появления)")]
    [SerializeField] private float _startDelay = 0f;    // ЗАДЕРЖКА ПЕРЕД СТАРТОМ (в сек)
    [SerializeField] private int _countPerSecond = 15;
    [SerializeField] private int _maxCount = 2;
    [SerializeField] private bool _cloneParticlesToMaxCount;
    [SerializeField] private int _cloneParticleLimit = 64;
    [SerializeField] private bool _forceContinuousSpawning;
    [Tooltip("Continuous loop keeps already-active particles alive without resetting shader _StartTime/_Seed. Use for effects whose shader animation must keep running, for example mesh spin.")]
    [SerializeField] private bool _preserveShaderTimeInContinuousLoop;

    [Space(10)]
    [SerializeField] private bool _isBurstSpawning;    // Мгновенный выстрел (после задержки)
    [SerializeField] private float _relativeWarmupTime; // Прогрев (для колец)

    [Header("Loop & Timing")]
    [SerializeField] private float _duration = 0.2f;    // Индивидуальная жизнь частицы
    [SerializeField] private bool _hasFixedDuration = true;
    [SerializeField] private bool _instantKillAtCastEnd;
    [SerializeField] private bool _fitToBounds;

    [Header("Home Projectile Flight")]
    [SerializeField] private bool _homeFlightAnchor;
    [SerializeField] private float _homePathSideOffsetMultiplier = 1f;
    [SerializeField] private float _homePathSideOffsetScale = 1f;
    [SerializeField] private float _homePathHeightOffsetScale = 1f;
    [SerializeField] private float _homeFlightSpeedScale = 1f;

    private bool _hasRuntimeHomeFlightProfile;
    private ParticleGroupHomeFlightProfile _runtimeHomeFlightProfile;

    public bool IsHomeFlightAnchor =>
        _homeFlightAnchor || (_hasRuntimeHomeFlightProfile && _runtimeHomeFlightProfile.isFlightAnchor);

    public bool TryGetHomeFlightProfile(out ParticleGroupHomeFlightProfile profile)
    {
        if (_hasRuntimeHomeFlightProfile)
        {
            profile = _runtimeHomeFlightProfile;
            return profile.isFlightAnchor;
        }

        if (!_homeFlightAnchor)
        {
            profile = default;
            return false;
        }

        profile = new ParticleGroupHomeFlightProfile
        {
            isFlightAnchor = true,
            pathSideOffsetMultiplier = _homePathSideOffsetMultiplier,
            pathSideOffsetScale = _homePathSideOffsetScale,
            pathHeightOffsetScale = _homePathHeightOffsetScale,
            speedScale = _homeFlightSpeedScale
        };
        return true;
    }

    public void ApplyRuntimeHomeFlightProfile(ParticleGroupHomeFlightProfile profile)
    {
        _hasRuntimeHomeFlightProfile = true;
        _runtimeHomeFlightProfile = profile;
    }

    public void ClearRuntimeHomeFlightProfile()
    {
        _hasRuntimeHomeFlightProfile = false;
        _runtimeHomeFlightProfile = default;
    }

    /// <summary>Starts this group's particle system (used for runtime mirror duplicate).</summary>
    public void StartGroupPlayback()
    {
        PlayPart();
    }

    public Renderer[] Particles => _particles;

    public void SetSyncedParticleDuration(float durationSec)
    {
        _duration = Mathf.Max(0.01f, durationSec);
    }

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
    private bool _debugFirstSpawnLogged;
    private int _icebergPlayCount;
    private int _icebergSlotOnCount;
    private int _icebergSlotAutoOffCount;
    private float _lastWhHealShaderTimeLog;
    private float _lastWhHealPreserveLog;
    private bool _runtimeParticleClonesCreated;

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
                    UpdateOwnerWorldPos(_particles[i]);
                    UpdateMeshLifetimeScale(_particles[i], now);
                    if (now - _particleSpawnTimes[i] >= _duration)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (ShouldTraceIcefrag())
                        {
                            Debug.Log(
                                $"[ICEFRAG_SLOT_AUTO_OFF] group='{name}' slot={i} now={now:F3}s " +
                                $"aliveFor={(now - _particleSpawnTimes[i]):F3}s duration={_duration:F3}s.");
                        }

                        if (ShouldTraceIcebergDuration())
                        {
                            _icebergSlotAutoOffCount += 1;
                            Debug.Log(
                                $"[ICEBERG_SLOT_AUTO_OFF] group='{name}' slot={i} now={now:F3}s " +
                                $"aliveFor={(now - _particleSpawnTimes[i]):F3}s duration={_duration:F3}s " +
                                $"slotAutoOffCount={_icebergSlotAutoOffCount} frame={Time.frameCount}.");
                        }

                        if (ShouldTraceWhHeal())
                        {
                            Debug.Log(
                                $"[WH_HEAL_GROUP_SLOT_OFF] group='{name}' slot={i} now={now:F3}s " +
                                $"aliveFor={(now - _particleSpawnTimes[i]):F3}s groupDuration={_duration:F3}s " +
                                $"preserveLoopTime={_preserveShaderTimeInContinuousLoop} runtimeLoop={_runtimeContinuousLoop} " +
                                $"shaderMat={BuildRuntimeMaterialLifetimeSnapshot(_particles[i], now)} frame={Time.frameCount}.");
                        }
#endif
                        //Debug.Log($"<color=orange>[Particle DIE]</color> {gameObject.name} слот [{i}] выключен.");
                        _particles[i].gameObject.SetActive(false);
                        _isParticleActive[i] = false;
                    }
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceWhHeal() && anyActive && now - _lastWhHealShaderTimeLog >= 0.25f)
        {
            _lastWhHealShaderTimeLog = now;
            LogWhHealShaderTimeSample(now, "tick");
        }
#endif

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

    private void ApplySpawnSpin(Renderer renderer, float seed)
    {
        if (renderer == null)
        {
            return;
        }

        ParticleGroupSpawnSpin spawnSpin = GetComponent<ParticleGroupSpawnSpin>();
        if (spawnSpin != null)
        {
            spawnSpin.Apply(renderer, seed);
        }
    }

    private void UpdateMeshLifetimeScale(Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return;
        }

        ParticleGroupMeshLifetimeScale meshScale = GetComponent<ParticleGroupMeshLifetimeScale>();
        if (meshScale != null)
        {
            meshScale.Apply(renderer, now);
        }
    }

    private void ApplySyncedShaderLifetime(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        ParticleGroupMeshLifetimeScale meshScale = GetComponent<ParticleGroupMeshLifetimeScale>();
        if (meshScale == null)
        {
            return;
        }

        Material curveMat = renderer.sharedMaterial;
        float particleLifetime = meshScale.ResolveParticleLifetimeSecForSpawn(curveMat);
        Vector4 lifetimeRange = new Vector4(particleLifetime, particleLifetime, 0f, 0f);
        Material[] runtimeMaterials = renderer.materials;
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material mat = runtimeMaterials[i];
            if (mat != null && mat.HasProperty("_LifetimeRange"))
            {
                mat.SetVector("_LifetimeRange", lifetimeRange);
            }
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
        bool shouldLoopContinuously = _forceContinuousSpawning || _runtimeContinuousLoop;
        if (shouldLoopContinuously &&
            _isParticleActive[_particleIndex] &&
            pObj.activeSelf &&
            _preserveShaderTimeInContinuousLoop)
        {
            // Some long-lived effects use shader time for continuous motion.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal() && now - _lastWhHealPreserveLog >= 0.5f)
            {
                _lastWhHealPreserveLog = now;
                float alive = now - _particleSpawnTimes[_particleIndex];
                Debug.Log(
                    $"[WH_HEAL_PRESERVE_SHADER_TIME] group='{name}' slot={_particleIndex} now={now:F3}s " +
                    $"alive={alive:F3}s groupDuration={_duration:F3}s skipRestart=true " +
                    $"shaderMat={BuildRuntimeMaterialLifetimeSnapshot(_particles[_particleIndex], now)} frame={Time.frameCount}.");
            }
#endif
            UpdateOwnerWorldPos(_particles[_particleIndex]);
            _particleIndex++;
            return;
        }

        pObj.SetActive(true);

        _particleSpawnTimes[_particleIndex] = now;
        _isParticleActive[_particleIndex] = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceIcefrag())
        {
            Debug.Log(
                $"[ICEFRAG_SLOT_ON] group='{name}' slot={_particleIndex} now={now:F3}s " +
                $"activeSelf={pObj.activeSelf} duration={_duration:F3}s spawnedCount={_spawnedCount + 1}/{_maxCount} " +
                $"countPerSec={_countPerSecond} startDelay={_startDelay:F3}s mat={BuildMaterialLifetimeSnapshot(_particles[_particleIndex])}.");
        }

        if (ShouldTraceIcebergDuration())
        {
            _icebergSlotOnCount += 1;
            Debug.Log(
                $"[ICEBERG_SLOT_ON] group='{name}' slot={_particleIndex} now={now:F3}s " +
                $"duration={_duration:F3}s spawnedCount={_spawnedCount + 1}/{_maxCount} " +
                $"countPerSec={_countPerSecond} slotOnCount={_icebergSlotOnCount} frame={Time.frameCount}.");
        }

        if (ShouldTraceDebug() && !_debugFirstSpawnLogged)
        {
            _debugFirstSpawnLogged = true;
            float sincePlay = _debugPlayStartedAt > 0f ? now - _debugPlayStartedAt : -1f;
            float sinceEnable = now - _lastEnable;
            Debug.Log(
                $"[TA_PARTICLE_FIRST_SPAWN] group='{name}' now={now:F3}s sincePlay={sincePlay:F3}s sinceEnable={sinceEnable:F3}s " +
                $"startDelay={_startDelay:F3}s burst={_isBurstSpawning} countPerSec={_countPerSecond} maxCount={_maxCount} " +
                $"duration={_duration:F3}s fixedDuration={_hasFixedDuration} runtimeLoop={_runtimeContinuousLoop}.");
        }
#endif

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
            ApplySpawnSpin(_particles[_particleIndex], seed);
            ApplySyncedShaderLifetime(_particles[_particleIndex]);
            UpdateMeshLifetimeScale(_particles[_particleIndex], now);
            SetOwnerWorldPos(m);
            if (SurfaceNormal != Vector3.zero)
            {
                m.SetVector("_SurfaceNormals", SurfaceNormal);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal())
            {
                float hasLt = m.HasProperty("_HasLifetime") ? m.GetFloat("_HasLifetime") : -1f;
                Vector4 life = m.HasProperty("_LifetimeRange") ? m.GetVector("_LifetimeRange") : Vector4.zero;
                Vector4 delay = m.HasProperty("_InitialDelayRange") ? m.GetVector("_InitialDelayRange") : Vector4.zero;
                float st = m.GetFloat("_StartTime");
                float sd = m.HasProperty("_Seed") ? m.GetFloat("_Seed") : 0f;
                Debug.Log(
                    $"[WH_HEAL_SHADER_SPAWN] group='{name}' slot={_particleIndex} matIdx={materialIndex} mat='{m.name}' " +
                    $"now={now:F3}s _StartTime={st:F3} shaderAgeApprox={(now - st):F3}s _Seed={sd:F3} " +
                    $"_HasLifetime={hasLt:F3} _LifetimeRange=({life.x:F3},{life.y:F3}) _InitialDelay=({delay.x:F3},{delay.y:F3}) " +
                    $"warmup={_relativeWarmupTime:F3}s [FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(m, now)} " +
                    $"{ShaderFadeDiagnostic.BuildLine(m, now)} frame={Time.frameCount}.");
            }
#endif
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
        _debugFirstSpawnLogged = false;
        _lastWhHealShaderTimeLog = 0f;
        _lastWhHealPreserveLog = 0f;

        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<Renderer>(true);

        ParticleGroupMeshLifetimeScale meshLifetimeScale = GetComponent<ParticleGroupMeshLifetimeScale>();
        if (meshLifetimeScale != null)
        {
            meshLifetimeScale.SyncBeforePlay(this);
        }
        else if (_duration < 0.01f)
        {
            _duration = GetLifeTimeFromMaterial();
        }

        EnsureRuntimeParticleCapacity();

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

        ParticleGroupMeshLifetimeScale meshScale = GetComponent<ParticleGroupMeshLifetimeScale>();
        if (meshScale != null)
        {
            meshScale.ResetAllCachedScales();
        }

        //Debug.Log($"<color=cyan>[Effect START]</color> {gameObject.name}. Ожидание старта: {_startDelay}с.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceDebug())
        {
            Debug.Log(
                $"[TA_PARTICLE_PLAYPART] group='{name}' playAt={_debugPlayStartedAt:F3}s startDelay={_startDelay:F3}s " +
                $"burst={_isBurstSpawning} countPerSec={_countPerSecond} maxCount={_maxCount} " +
                $"duration={_duration:F3}s fixedDuration={_hasFixedDuration} forceContinuous={_forceContinuousSpawning}.");
        }

        if (ShouldTraceIcebergDuration())
        {
            _icebergPlayCount += 1;
            _icebergSlotOnCount = 0;
            _icebergSlotAutoOffCount = 0;
            Debug.Log(
                $"[ICEBERG_DURATION_START] group='{name}' playAt={_debugPlayStartedAt:F3}s " +
                $"configuredDuration={_duration:F3}s startDelay={_startDelay:F3}s " +
                $"fixedDuration={_hasFixedDuration} countPerSec={_countPerSecond} maxCount={_maxCount} " +
                $"playCount={_icebergPlayCount} frame={Time.frameCount} instanceId={GetInstanceID()} owner='{(_owner != null ? _owner.name : "null")}'.");
        }

        if (ShouldTraceIcefrag())
        {
            Debug.Log(
                $"[ICEFRAG_DURATION_START] group='{name}' playAt={_debugPlayStartedAt:F3}s " +
                $"duration={_duration:F3}s startDelay={_startDelay:F3}s countPerSec={_countPerSecond} maxCount={_maxCount} " +
                $"fixedDuration={_hasFixedDuration} owner='{(_owner != null ? _owner.name : "null")}'.");
        }

        if (ShouldTraceWhHeal())
        {
            Debug.Log(
                $"[WH_HEAL_GROUP_PLAY] group='{name}' playAt={_debugPlayStartedAt:F3}s owner='{(_owner != null ? _owner.name : "null")}' " +
                $"duration={_duration:F3}s fixedDuration={_hasFixedDuration} baseShaderLife={_baseShaderLifetime:F3}s " +
                $"runtimeLoop={_runtimeContinuousLoop} loopOv={_hasRuntimeContinuousLoopOverride}:{_runtimeContinuousLoopOverrideValue} " +
                $"forceContinuous={_forceContinuousSpawning} preserveShaderTime={_preserveShaderTimeInContinuousLoop} " +
                $"burst={_isBurstSpawning} countPerSec={_countPerSecond} maxCount={_maxCount} startDelay={_startDelay:F3}s " +
                $"warmup={_relativeWarmupTime:F3}s frame={Time.frameCount}.");
        }
#endif
    }

    private void EnsureRuntimeParticleCapacity()
    {
        if (!Application.isPlaying || !_cloneParticlesToMaxCount || _runtimeParticleClonesCreated || _particles == null || _particles.Length == 0)
        {
            return;
        }

        int desiredCount = Mathf.Clamp(_maxCount, _particles.Length, Mathf.Max(_particles.Length, _cloneParticleLimit));
        if (desiredCount <= _particles.Length)
        {
            return;
        }

        List<Renderer> particles = new List<Renderer>(_particles);
        int sourceCount = particles.Count;
        for (int i = particles.Count; i < desiredCount; i++)
        {
            Renderer source = particles[i % sourceCount];
            if (source == null)
            {
                continue;
            }

            GameObject clone = Instantiate(source.gameObject, source.transform.parent);
            clone.name = $"{source.gameObject.name}_RuntimeClone";
            clone.SetActive(false);

            Renderer cloneRenderer = clone.GetComponent<Renderer>();
            if (cloneRenderer != null)
            {
                particles.Add(cloneRenderer);
            }
        }

        _particles = particles.ToArray();
        _runtimeParticleClonesCreated = true;
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

    private void UpdateOwnerWorldPos(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            SetOwnerWorldPos(materials[i]);
        }
    }

    private void SetOwnerWorldPos(Material material)
    {
        if (material == null || !material.HasProperty(OwnerWorldPosShaderProperty))
        {
            return;
        }

        material.SetVector(OwnerWorldPosShaderProperty, ResolveOwnerWorldPos());
    }


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

        if (ShouldTraceWhHeal())
        {
            Debug.Log(
                $"[WH_HEAL_GROUP_SETUP] group='{name}' owner='{(_owner != null ? _owner.name : "null")}' settings='{(_settings != null ? _settings.name : "null")}' " +
                $"hideTime={(_settings != null ? _settings.hideTime : -1f):F3}s defaultLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"sameAsLineAboveDurationAfter={_duration:F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop} " +
                $"sharedMat0={BuildMaterialLifetimeSnapshot(_particles != null && _particles.Length > 0 ? _particles[0] : null)}.");
        }
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

        if (ShouldTraceIcebergDuration())
        {
            Debug.Log(
                $"[ICEBERG_DURATION_STOP] group='{name}' elapsed={elapsed:F3}s " +
                $"duration={_duration:F3}s startDelay={_startDelay:F3}s " +
                $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"playCount={_icebergPlayCount} slotOnCount={_icebergSlotOnCount} " +
                $"slotAutoOffCount={_icebergSlotAutoOffCount} frame={Time.frameCount}.");
        }

        if (ShouldTraceIcefrag())
        {
            Debug.Log(
                $"[ICEFRAG_DURATION_STOP] group='{name}' elapsed={elapsed:F3}s duration={_duration:F3}s " +
                $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s.");
        }

        if (ShouldTraceWhHeal())
        {
            Debug.Log(
                $"[WH_HEAL_GROUP_STOP] group='{name}' elapsed={elapsed:F3}s duration={_duration:F3}s " +
                $"preserveShaderTime={_preserveShaderTimeInContinuousLoop} runtimeLoop={_runtimeContinuousLoop} frame={Time.frameCount}.");

            float nowStop = Now();
            if (_particles != null)
            {
                for (int si = 0; si < _particles.Length; si++)
                {
                    if (_particles[si] == null || !_particles[si].gameObject.activeSelf)
                    {
                        continue;
                    }

                    Material[] smats = _particles[si].materials;
                    Material m0 = smats != null && smats.Length > 0 ? smats[0] : null;
                    Debug.Log(
                        $"[WH_HEAL_GROUP_STOP_FADE] group='{name}' slot={si} now={nowStop:F3}s " +
                        $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(m0, nowStop)} " +
                        $"{ShaderFadeDiagnostic.BuildLine(m0, nowStop)} frame={Time.frameCount}.");
                    break;
                }
            }
        }
#endif
        _runtimeContinuousLoop = false;
        _hasRuntimeContinuousLoopOverride = true;
        _runtimeContinuousLoopOverrideValue = false;
        _stopped = true;
    }

    private bool ShouldTraceDebug()
    {
        return _owner != null &&
               !string.IsNullOrEmpty(_owner.name) &&
               _owner.name.IndexOf(DebugTraceEffectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool ShouldTraceIcebergDuration()
    {
        if (_owner == null || string.IsNullOrEmpty(_owner.name) || string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _owner.name.IndexOf(IceBoltTaEffectName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               name.IndexOf(IcebergGroupName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool ShouldTraceIcefrag()
    {
        if (_owner == null || string.IsNullOrEmpty(_owner.name) || string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _owner.name.IndexOf(IceBoltTaEffectName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               name.IndexOf(IcefragGroupName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool ShouldTraceWhHeal()
    {
        if (!string.IsNullOrEmpty(name) &&
            name.IndexOf(DebugTraceHealEffectToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (_owner != null && !string.IsNullOrEmpty(_owner.name) &&
            _owner.name.IndexOf(DebugTraceHealEffectToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        Transform t = transform;
        for (int depth = 0; t != null && depth < 16; depth++, t = t.parent)
        {
            if (!string.IsNullOrEmpty(t.name) &&
                t.name.IndexOf(DebugTraceHealEffectToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogWhHealShaderTimeSample(float now, string reason)
    {
        if (_particles == null || _isParticleActive == null)
        {
            return;
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            if (!_isParticleActive[i] || _particles[i] == null || !_particles[i].gameObject.activeSelf)
            {
                continue;
            }

            Material[] mats = _particles[i].materials;
            Material mat0 = mats != null && mats.Length > 0 ? mats[0] : null;
            string fadePhase = ShaderFadeDiagnostic.FadePhaseLabel(mat0, now);
            string fadeLine = ShaderFadeDiagnostic.BuildLine(mat0, now);
            Debug.Log(
                $"[WH_HEAL_SHADER_TICK] reason={reason} group='{name}' slot={i} now={now:F3}s " +
                $"alive={(now - _particleSpawnTimes[i]):F3}s groupDuration={_duration:F3}s " +
                $"{BuildRuntimeMaterialLifetimeSnapshot(_particles[i], now)} " +
                $"[FADE_PHASE]={fadePhase} {fadeLine} frame={Time.frameCount}.");
            return;
        }
    }

    private string BuildRuntimeMaterialLifetimeSnapshot(Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return "no_renderer";
        }

        Material[] mats = renderer.materials;
        if (mats == null || mats.Length == 0 || mats[0] == null)
        {
            return "no_runtime_material";
        }

        Material mat = mats[0];
        Vector4 initialDelay = mat.HasProperty("_InitialDelayRange") ? mat.GetVector("_InitialDelayRange") : Vector4.zero;
        Vector4 lifetime = mat.HasProperty("_LifetimeRange") ? mat.GetVector("_LifetimeRange") : Vector4.zero;
        float hasLifetime = mat.HasProperty("_HasLifetime") ? mat.GetFloat("_HasLifetime") : -1f;
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : -1f;
        float age = startTime > -0.5f ? now - startTime : -1f;
        return
            $"{mat.name}:_HasLifetime={hasLifetime:F3} life=({lifetime.x:F3},{lifetime.y:F3}) " +
            $"initDelay=({initialDelay.x:F3},{initialDelay.y:F3}) _StartTime={startTime:F3} shaderAge~={age:F3}s";
    }
#endif

    private string BuildMaterialLifetimeSnapshot(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
        {
            return "no_material";
        }

        Material mat = renderer.sharedMaterials[0];
        if (mat == null)
        {
            return "null_material";
        }

        Vector4 initialDelay = mat.HasProperty("_InitialDelayRange") ? mat.GetVector("_InitialDelayRange") : Vector4.zero;
        Vector4 lifetime = mat.HasProperty("_LifetimeRange") ? mat.GetVector("_LifetimeRange") : Vector4.zero;
        float hasLifetime = mat.HasProperty("_HasLifetime") ? mat.GetFloat("_HasLifetime") : -1f;
        return $"{mat.name}:initDelay=({initialDelay.x:F3},{initialDelay.y:F3}) life=({lifetime.x:F3},{lifetime.y:F3}) hasLifetime={hasLifetime:F3}";
    }
}