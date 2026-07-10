using System.Collections.Generic;
using UnityEngine;

public class ParticleGroup : EffectPart
{
    private const string DebugTraceEffectName = "el_wind_strike_ta";
    private const string DebugTraceHealEffectToken = "wh_heal";
    private const string DebugTraceCurePoisonToken = "cure_poison";
    private const string DebugTraceMightCaToken = "might_ca";
    private const string DebugTraceMightTaToken = "wh_might_ta";
    private const string MeshEmitter3GroupName = "MeshEmitter3";
    private const string IceBoltTaEffectName = "el_ice_bolt_ta";
    private const string IcebergGroupName = "iceberg";
    private const string IcefragGroupName = "icefrag";
    private const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    private static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");
    private static readonly int SeedShaderId = Shader.PropertyToID("_Seed");
    private static readonly int DebugMeshPreviewShaderId = Shader.PropertyToID("_DebugMeshPreview");
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public L2Particle OwnerParticle => _owner;
#endif

    // Prevent auto-running in scene before explicit PlayPart/Setup.
    private bool _stopped = true;
    private bool _spawnStopped;
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
    private bool _burstSpawnFinished;
    private float _lastQuadSizeDiagLog;
    private float _lastCurePoisonShaderTimeLog;
    private float _lastMightCaShaderTimeLog;
    private float _lastMeshEmitter3ShaderTimeLog;
    private float _lastUplineGroupTickLog;
    private MaterialPropertyBlock _particleRuntimeProperties;

    public void FixedUpdate()
    {
        if (_stopped)
        {
            return;
        }

        float now = Now();
        float timeSinceEnable = now - _lastEnable;

        // 1. ПРОВЕРКА ЗАДЕРЖКИ СТАРТА ГРУППЫ
        if (!_spawnStopped && timeSinceEnable < _startDelay)
        {
            return;
        }

        // 2. КОНТРОЛЬ СМЕРТИ ЧАСТИЦ (Индивидуально)
        bool anyActive = false;
        if (_particles != null && _isParticleActive != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_isParticleActive[i])
                {
                    anyActive = true;
                    UpdateDynamicShaderWorldPositions(_particles[i]);
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

                        if (ShouldTraceMightTaMeshEmitter3())
                        {
                            Debug.Log(
                                $"[MESH_EMITTER3_SLOT_OFF] group='{name}' slot={i} now={now:F3}s " +
                                $"aliveFor={(now - _particleSpawnTimes[i]):F3}s groupDuration={_duration:F3}s " +
                                $"spawned={_spawnedCount}/{_maxCount} particleIndex={_particleIndex} " +
                                $"{BuildMeshEmitter3RendererSnapshot(_particles[i], now)} frame={Time.frameCount}.");
                        }

                        ParticleGroupLifetimeDebug.LogSlotOff(
                            name,
                            _owner,
                            transform,
                            i,
                            now,
                            _particleSpawnTimes[i],
                            _duration,
                            _particles[i],
                            "group_duration_expired");
                        DocExtractorParticleSnapshotLogger.OnSlotOff(this, i);
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

        if (ShouldTraceCurePoison() && anyActive && now - _lastCurePoisonShaderTimeLog >= 0.2f)
        {
            _lastCurePoisonShaderTimeLog = now;
            LogCurePoisonShaderTimeSample(now, "tick");
        }

        if (ShouldTraceMightCa() && anyActive && now - _lastMightCaShaderTimeLog >= 0.2f)
        {
            _lastMightCaShaderTimeLog = now;
            LogMightCaShaderTimeSample(now, "tick");
        }

        if (ShouldTraceMightTaMeshEmitter3() && anyActive && now - _lastMeshEmitter3ShaderTimeLog >= 0.1f)
        {
            _lastMeshEmitter3ShaderTimeLog = now;
            LogMeshEmitter3ShaderTimeSample(now, "tick");
        }

        if (ParticleGroupLifetimeDebug.ShouldTraceUpline(name, _owner, transform) &&
            anyActive &&
            now - _lastUplineGroupTickLog >= 0.25f)
        {
            _lastUplineGroupTickLog = now;
            LogUplineGroupTickSample(now);
        }

        if (L2FxQuadSizeDiagnostic.ShouldTrace(name, _owner, transform)
            && anyActive
            && now - _lastQuadSizeDiagLog >= L2FxQuadSizeDiagnostic.LogIntervalSec)
        {
            _lastQuadSizeDiagLog = now;
            Renderer quadRenderer = ResolveFirstActiveRenderer();
            if (quadRenderer != null)
            {
                Material runtimeMat = quadRenderer.materials != null && quadRenderer.materials.Length > 0
                    ? quadRenderer.materials[0]
                    : null;
                L2FxQuadSizeDiagnostic.Log(name, quadRenderer, now, runtimeMat);
            }
        }

        DocExtractorParticleSnapshotLogger.OnFixedUpdateTick(
            this,
            now,
            _particleSpawnTimes,
            _isParticleActive,
            _particles);
#endif

        // 3. ЛОГИКА СПАВНА
        if (_spawnStopped)
        {
            if (!anyActive)
            {
                _stopped = true;
            }

            return;
        }

        bool shouldLoopContinuously = _forceContinuousSpawning || _runtimeContinuousLoop;
        if (_spawnedCount < _maxCount || shouldLoopContinuously)
        {
            if (_isBurstSpawning && !shouldLoopContinuously && !_burstSpawnFinished)
            {
                // Burst only once per PlayPart; guard avoids re-arming slots if spawnedCount drifts.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (ShouldTraceCurePoison())
                {
                    Debug.Log(
                        $"[CURE_POISON_BURST] group='{name}' now={now:F3}s maxCount={_maxCount} " +
                        $"particleSlots={(_particles != null ? _particles.Length : 0)} frame={Time.frameCount}.");
                }
#endif
                for (int i = 0; i < _maxCount; i++)
                {
                    ActivateParticle(now);
                    _spawnedCount++;
                }

                _burstSpawnFinished = true;
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

    private void ApplySpawnTimingProperties(Renderer renderer, float shaderStartTime, float seed)
    {
        if (renderer == null)
        {
            return;
        }

        _particleRuntimeProperties ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_particleRuntimeProperties);
        _particleRuntimeProperties.SetFloat(StartTimeShaderId, shaderStartTime);
        _particleRuntimeProperties.SetFloat(SeedShaderId, seed);
        _particleRuntimeProperties.SetFloat(DebugMeshPreviewShaderId, 0f);
        renderer.SetPropertyBlock(_particleRuntimeProperties);
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceCurePoison() && _isParticleActive[_particleIndex] && pObj.activeSelf)
        {
            Material[] activeMats = _particles[_particleIndex].materials;
            Material activeMat = activeMats != null && activeMats.Length > 0 ? activeMats[0] : null;
            float prevStart = activeMat != null && activeMat.HasProperty("_StartTime") ? activeMat.GetFloat("_StartTime") : -1f;
            Debug.LogWarning(
                $"[CURE_POISON_RESPAWN] group='{name}' slot={_particleIndex} now={now:F3}s " +
                $"prevStartTime={prevStart:F3}s prevAlive={(now - _particleSpawnTimes[_particleIndex]):F3}s " +
                $"spawnedCount={_spawnedCount}/{_maxCount} burstDone={_burstSpawnFinished} frame={Time.frameCount}.");
        }

        if (ParticleGroupLifetimeDebug.ShouldTraceUpline(name, _owner, transform) &&
            _isParticleActive[_particleIndex] &&
            pObj.activeSelf)
        {
            Material[] activeMats = _particles[_particleIndex].materials;
            Material activeMat = activeMats != null && activeMats.Length > 0 ? activeMats[0] : null;
            float prevStart = activeMat != null && activeMat.HasProperty("_StartTime") ? activeMat.GetFloat("_StartTime") : -1f;
            ParticleGroupLifetimeDebug.LogRespawnWarning(
                name,
                _owner,
                transform,
                _particleIndex,
                now,
                _particleSpawnTimes[_particleIndex],
                prevStart);
        }
#endif

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
            UpdateDynamicShaderWorldPositions(_particles[_particleIndex]);
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
        ApplySpawnTimingProperties(_particles[_particleIndex], shaderStartTime, seed);
        Material[] runtimeMaterials = _particles[_particleIndex].materials;
        Material[] sharedMaterials = _particles[_particleIndex].sharedMaterials;
        for (int materialIndex = 0; materialIndex < runtimeMaterials.Length; materialIndex++)
        {
            Material m = runtimeMaterials[materialIndex];
            if (m == null)
            {
                continue;
            }

            Material shared = sharedMaterials != null && materialIndex < sharedMaterials.Length
                ? sharedMaterials[materialIndex]
                : null;
            if (shared != null)
            {
                L2MaterialPropertyCopier.CopyLifetimeFadeAndFxFromShared(m, shared);
            }

            // Keep alpha exactly as configured in shared material.
            if (m.HasProperty("_Alpha") && shared != null && shared.HasProperty("_Alpha"))
            {
                m.SetFloat("_Alpha", shared.GetFloat("_Alpha"));
            }

            // Debug preview is for Scene/material assets only; spawned slots must run from their own time.
            if (m.HasProperty(DebugMeshPreviewShaderId))
            {
                m.SetFloat(DebugMeshPreviewShaderId, 0f);
            }

            // Debug.Log("Set Start Time " + shaderStartTime + " Seed " + seed + "name " + m.name);
            m.SetFloat(StartTimeShaderId, shaderStartTime);
            m.SetFloat(SeedShaderId, seed);
            ApplySpawnSpin(_particles[_particleIndex], seed);
            SetDynamicShaderWorldPositions(m);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceCurePoison())
            {
                Debug.Log(
                    $"[CURE_POISON_SPAWN] group='{name}' slot={_particleIndex} mat='{m.name}' now={now:F3}s " +
                    $"shaderStart={shaderStartTime:F3}s seed={seed:F3} burstDone={_burstSpawnFinished} " +
                    $"spawnedCount={_spawnedCount + 1}/{_maxCount} frame={Time.frameCount}.");
            }

            if (ShouldTraceMightCa())
            {
                Vector4 delayRange = m.HasProperty("_InitialDelayRange") ? m.GetVector("_InitialDelayRange") : Vector4.zero;
                Debug.Log(
                    $"[MIGHT_CA_SPAWN] group='{name}' slot={_particleIndex} now={now:F3}s shaderStart={shaderStartTime:F3}s " +
                    $"warmup={_relativeWarmupTime:F3}s groupDuration={_duration:F3}s seed={seed:F3} " +
                    $"initDelayRange=({delayRange.x:F3},{delayRange.y:F3}) " +
                    $"note=INITIAL_DELAY at age=0 on activate is normal; watch MIGHT_CA_SHADER_TICK after delay " +
                    $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(m, now)} " +
                    $"{ShaderFadeDiagnostic.BuildLine(m, now)} frame={Time.frameCount}.");
            }

            if (ShouldTraceMightTaMeshEmitter3())
            {
                Debug.Log(
                    $"[MESH_EMITTER3_SPAWN] group='{name}' slot={_particleIndex} matIdx={materialIndex} " +
                    $"now={now:F3}s shaderStart={shaderStartTime:F3}s seed={seed:F3} " +
                    $"spawnedNext={_spawnedCount + 1}/{_maxCount} burst={_isBurstSpawning} " +
                    $"countPerSec={_countPerSecond} fixedDuration={_hasFixedDuration} runtimeLoop={_runtimeContinuousLoop} " +
                    $"{BuildMeshEmitter3RendererSnapshot(_particles[_particleIndex], now)} frame={Time.frameCount}.");
            }
#endif
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

            if (materialIndex == 0)
            {
                ParticleGroupLifetimeDebug.LogSpawn(
                    name,
                    _owner,
                    transform,
                    _particleIndex,
                    now,
                    shaderStartTime,
                    seed,
                    _duration,
                    _spawnedCount + 1,
                    _maxCount,
                    _particles[_particleIndex]);
                DocExtractorParticleSnapshotLogger.OnParticleActivated(
                    this,
                    _particleIndex,
                    _particles[_particleIndex],
                    now,
                    shaderStartTime,
                    seed);
            }
#endif
        }

        _particleIndex++;
    }

    private Renderer ResolveFirstActiveRenderer()
    {
        if (_particles == null || _isParticleActive == null)
        {
            return null;
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_isParticleActive[i] && _particles[i] != null)
            {
                return _particles[i];
            }
        }

        return null;
    }

    public override void PlayPart()
    {
        if (_fitToBounds && OwnerTarget != null) FitToOwnerWidth(OwnerTarget);

        _lastEnable = Now();
        float spawnInterval = _countPerSecond > 0 ? 1f / _countPerSecond : 0.05f;
        _lastLoop = _lastEnable - spawnInterval;
        _particleIndex = 0;
        _spawnedCount = 0;
        _stopped = false;
        _spawnStopped = false;
        _burstSpawnFinished = false;
        _debugPlayStartedAt = _lastEnable;
        _debugFirstSpawnLogged = false;
        _lastQuadSizeDiagLog = 0f;
        _lastWhHealShaderTimeLog = 0f;
        _lastWhHealPreserveLog = 0f;
        _lastCurePoisonShaderTimeLog = 0f;
        _lastMightCaShaderTimeLog = 0f;
        _lastMeshEmitter3ShaderTimeLog = 0f;
        _lastUplineGroupTickLog = 0f;

        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<Renderer>(true);

        float shaderSlotDurationForLog = GetShaderSlotDurationFromMaterial();
        if (_duration < 0.01f)
        {
            _duration = shaderSlotDurationForLog;
        }
        else
        {
            if (_duration < shaderSlotDurationForLog)
            {
                _duration = shaderSlotDurationForLog;
            }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ParticleGroupLifetimeDebug.LogPlayPart(
            name,
            _owner,
            transform,
            _debugPlayStartedAt,
            _startDelay,
            _duration,
            shaderSlotDurationForLog,
            _countPerSecond,
            _maxCount,
            _preserveShaderTimeInContinuousLoop,
            _runtimeContinuousLoop);
        DocExtractorParticleSnapshotLogger.OnPlayPart(this);
#endif

        for (int i = 0; i < _particles.Length; i++)
            if (_particles[i] != null) _particles[i].gameObject.SetActive(false);

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

        if (ShouldTraceCurePoison())
        {
            Debug.Log(
                $"[CURE_POISON_PLAY] group='{name}' playAt={_debugPlayStartedAt:F3}s slots={(_particles != null ? _particles.Length : 0)} " +
                $"duration={_duration:F3}s shaderLife={_baseShaderLifetime:F3}s burst={_isBurstSpawning} maxCount={_maxCount} " +
                $"mat0={BuildMaterialLifetimeSnapshot(_particles != null && _particles.Length > 0 ? _particles[0] : null)} frame={Time.frameCount}.");
        }

        if (ShouldTraceMightTaMeshEmitter3())
        {
            Debug.Log(
                $"[MESH_EMITTER3_PLAYPART] group='{name}' playAt={_debugPlayStartedAt:F3}s " +
                $"startDelay={_startDelay:F3}s countPerSec={_countPerSecond} maxCount={_maxCount} " +
                $"duration={_duration:F3}s fixedDuration={_hasFixedDuration} baseShaderLifetime={_baseShaderLifetime:F3}s " +
                $"runtimeLoop={_runtimeContinuousLoop} burst={_isBurstSpawning} particleSlots={(_particles != null ? _particles.Length : 0)} " +
                $"owner='{(_owner != null ? _owner.name : "null")}' frame={Time.frameCount}.");

            if (_particles != null)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    Debug.Log(
                        $"[MESH_EMITTER3_PLAYPART_SLOT] group='{name}' slot={i} " +
                        $"{BuildMeshEmitter3RendererSnapshot(_particles[i], _debugPlayStartedAt)} frame={Time.frameCount}.");
                }
            }
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

    private float GetShaderSlotDurationFromMaterial()
    {
        float lifetime = GetLifeTimeFromMaterial();
        float maxDelay = 0f;
        if (_particles != null && _particles.Length > 0)
        {
            foreach (Material m in _particles[0].sharedMaterials)
            {
                if (m != null && m.HasProperty("_InitialDelayRange"))
                {
                    maxDelay = m.GetVector("_InitialDelayRange").y;
                }
            }
        }

        return lifetime + maxDelay + 0.03f;
    }

    private float Now() => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

    private void UpdateDynamicShaderWorldPositions(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            SetDynamicShaderWorldPositions(materials[i]);
        }
    }

    private void SetDynamicShaderWorldPositions(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(OwnerWorldPosShaderProperty))
        {
            material.SetVector(OwnerWorldPosShaderProperty, ResolveOwnerWorldPosForShader(material));
        }

        if (material.HasProperty(L2MaterialPropertyCopier.L2FxTargetWorldPosId))
        {
            bool hasTarget = TryResolveShaderTargetWorldPos(out Vector3 targetWorldPos);
            material.SetVector(L2MaterialPropertyCopier.L2FxTargetWorldPosId, targetWorldPos);
            if (material.HasProperty(L2MaterialPropertyCopier.UseExternalTargetPositionId))
            {
                material.SetFloat(L2MaterialPropertyCopier.UseExternalTargetPositionId, hasTarget ? 1f : 0f);
            }
        }
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
            _duration = EffectCastDurationResolver.Resolve(
                _duration,
                _hasFixedDuration,
                _settings,
                _castData,
                out float legacyHitDuration,
                out bool serverHitOverridesSettings);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EffectCastDurationResolver.LogMismatchIfNeeded(
                "ParticleGroup.Setup",
                name,
                (_castData != null ? _castData.HitTime : 0f),
                (_settings != null ? _settings.defaultLifeTime : 0f),
                _duration,
                serverHitOverridesSettings);
#endif
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
        _spawnStopped = true;

        if (_instantKillAtCastEnd)
        {
            DeactivateAllParticles();
            _stopped = true;
        }
    }

    private void DeactivateAllParticles()
    {
        if (_particles == null)
        {
            return;
        }

        if (_particleSpawnTimes == null || _particleSpawnTimes.Length != _particles.Length)
        {
            _particleSpawnTimes = new float[_particles.Length];
            _isParticleActive = new bool[_particles.Length];
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] != null)
            {
                _particles[i].gameObject.SetActive(false);
            }

            _isParticleActive[i] = false;
        }
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
        return ShouldTraceEffectToken(DebugTraceHealEffectToken);
    }

    private bool ShouldTraceCurePoison()
    {
        if (!string.IsNullOrEmpty(name) &&
            name.IndexOf("BlueDust", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return ShouldTraceEffectToken(DebugTraceCurePoisonToken);
    }

    private bool ShouldTraceMightCa()
    {
        if (!string.IsNullOrEmpty(name) &&
            name.IndexOf("SpriteEmitter7", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return ShouldTraceEffectToken(DebugTraceMightCaToken);
    }

    private bool ShouldTraceMightTaMeshEmitter3()
    {
        if (string.IsNullOrEmpty(name) ||
            name.IndexOf(MeshEmitter3GroupName, System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return ShouldTraceEffectToken(DebugTraceMightTaToken);
    }

    private bool ShouldTraceEffectToken(string token)
    {
        if (!string.IsNullOrEmpty(name) &&
            name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (_owner != null && !string.IsNullOrEmpty(_owner.name) &&
            _owner.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        Transform t = transform;
        for (int depth = 0; t != null && depth < 16; depth++, t = t.parent)
        {
            if (!string.IsNullOrEmpty(t.name) &&
                t.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogWhHealShaderTimeSample(float now, string reason)
    {
        LogShaderTimeSample(now, reason, "WH_HEAL_SHADER_TICK");
    }

    private void LogCurePoisonShaderTimeSample(float now, string reason)
    {
        LogShaderTimeSample(now, reason, "CURE_POISON_SHADER_TICK");
    }

    private void LogMightCaShaderTimeSample(float now, string reason)
    {
        LogShaderTimeSample(now, reason, "MIGHT_CA_SHADER_TICK");
    }

    private void LogMeshEmitter3ShaderTimeSample(float now, string reason)
    {
        if (_particles == null)
        {
            return;
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            bool trackedActive = _isParticleActive != null && i < _isParticleActive.Length && _isParticleActive[i];
            float alive = trackedActive && _particleSpawnTimes != null && i < _particleSpawnTimes.Length
                ? now - _particleSpawnTimes[i]
                : -1f;
            Debug.Log(
                $"[MESH_EMITTER3_TICK] reason={reason} group='{name}' slot={i} now={now:F3}s " +
                $"trackedActive={trackedActive} alive={alive:F3}s groupDuration={_duration:F3}s " +
                $"spawned={_spawnedCount}/{_maxCount} particleIndex={_particleIndex} " +
                $"{BuildMeshEmitter3RendererSnapshot(_particles[i], now)} frame={Time.frameCount}.");
        }
    }

    private void LogUplineGroupTickSample(float now)
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

            ParticleGroupLifetimeDebug.LogTick(
                name,
                _owner,
                transform,
                i,
                now,
                _particleSpawnTimes[i],
                _duration,
                _spawnedCount,
                _maxCount,
                _particles[i]);
            return;
        }
    }

    private void LogShaderTimeSample(float now, string reason, string logTag)
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
            float seed = mat0 != null && mat0.HasProperty("_Seed") ? mat0.GetFloat("_Seed") : 0f;
            Debug.Log(
                $"[{logTag}] reason={reason} group='{name}' slot={i} now={now:F3}s seed={seed:F3} " +
                $"alive={(now - _particleSpawnTimes[i]):F3}s groupDuration={_duration:F3}s " +
                $"{BuildRuntimeMaterialLifetimeSnapshot(_particles[i], now)} " +
                $"[FADE_PHASE]={fadePhase} {fadeLine} frame={Time.frameCount}.");
            return;
        }
    }

    private string BuildMeshEmitter3RendererSnapshot(Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return "renderer=null";
        }

        Material shared = renderer.sharedMaterial;
        Material runtime = null;
        Material[] mats = renderer.materials;
        if (mats != null && mats.Length > 0)
        {
            runtime = mats[0];
        }

        _particleRuntimeProperties ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_particleRuntimeProperties);

        float matStart = runtime != null && runtime.HasProperty(StartTimeShaderId) ? runtime.GetFloat(StartTimeShaderId) : -1f;
        float matSeed = runtime != null && runtime.HasProperty(SeedShaderId) ? runtime.GetFloat(SeedShaderId) : 0f;
        float matPreview = runtime != null && runtime.HasProperty(DebugMeshPreviewShaderId) ? runtime.GetFloat(DebugMeshPreviewShaderId) : -1f;
        float matHold = runtime != null && runtime.HasProperty("_Hold") ? runtime.GetFloat("_Hold") : -1f;
        Vector4 matLifetime = runtime != null && runtime.HasProperty("_LifetimeRange") ? runtime.GetVector("_LifetimeRange") : Vector4.zero;
        Vector4 matDelay = runtime != null && runtime.HasProperty("_InitialDelayRange") ? runtime.GetVector("_InitialDelayRange") : Vector4.zero;
        float matAge = matStart > -0.5f ? now - matStart : -1f;
        float pbStart = _particleRuntimeProperties.GetFloat(StartTimeShaderId);
        float pbSeed = _particleRuntimeProperties.GetFloat(SeedShaderId);
        float pbPreview = _particleRuntimeProperties.GetFloat(DebugMeshPreviewShaderId);
        float pbAge = pbStart > 0f ? now - pbStart : -1f;

        return
            $"renderer='{renderer.name}' goActive={renderer.gameObject.activeSelf} enabled={renderer.enabled} " +
            $"shared='{(shared != null ? shared.name : "null")}' runtime='{(runtime != null ? runtime.name : "null")}' " +
            $"matStart={matStart:F3} matAge={matAge:F3}s matSeed={matSeed:F3} matPreview={matPreview:F3} matHold={matHold:F3} " +
            $"matLife=({matLifetime.x:F3},{matLifetime.y:F3}) matDelay=({matDelay.x:F3},{matDelay.y:F3}) " +
            $"pbStart={pbStart:F3} pbAge={pbAge:F3}s pbSeed={pbSeed:F3} pbPreview={pbPreview:F3}";
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