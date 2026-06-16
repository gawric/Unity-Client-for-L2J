using UnityEngine;

public class ParticleSingle : EffectPart
{
    private static readonly int AlphaShaderId = Shader.PropertyToID("_Alpha");
    private static readonly int SurfaceNormalsShaderId = Shader.PropertyToID("_SurfaceNormals");

    [SerializeField] private L2Particle _owner;
    [SerializeField] private Renderer[] _particles;

    [Header("Spawning (single slot)")]
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private int _countPerSecond = 15;
    [SerializeField] private int _maxCount = 1;
    [SerializeField] private bool _forceContinuousSpawning;
    [Tooltip("Do not reset _StartTime/_Seed while continuous loop is active.")]
    [SerializeField] private bool _preserveShaderTimeInContinuousLoop;

    [Space(10)]
    [SerializeField] private bool _isBurstSpawning;
    [SerializeField] private float _relativeWarmupTime;

    [Header("Loop & Timing")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private bool _hasFixedDuration = true;
    [SerializeField] private bool _instantKillAtCastEnd;
    [SerializeField] private bool _fitToBounds;

    [Tooltip("Если включено: один Spawn при PlayPart, без FixedUpdate-тайминга и без скрытия/остановки. Для проверки _Hold и прочего в материале вручную.")]
    [SerializeField] private bool _testSpawnOnlyNoTeardown = false;

    [Header("L2 shader (Hold / fade)")]
    [Tooltip("Значение _Hold в материале, если включён Continuous Loop части композита (overrideContinuousLoop + continuousLoop через SetRuntimeContinuousLoopOverride). Это не то же самое, что спавн-луп — см. код композита.")]
    [SerializeField] [Range(0f, 1f)] private float _shaderHold = 0.6f;
    [Tooltip("Иначе _Hold висит всё время каста и в L2SkillEffect может блокировать нормальный FadeOut.")]
    [SerializeField] private bool _releaseShaderHoldByCastProgress = true;
    [Tooltip("Доля времени текущего _duration от момента спавна части — 0 = начало эффекта, 1 = конец _duration (серверное/кастовое). Раньше этой точки _Hold держится на Shader Hold.")]
    [SerializeField] [Range(0f, 0.999f)] private float _shaderHoldReleaseStartNormalized = 0.85f;
    [Tooltip("Вкл.: от точки Release Start до конца каста _Hold линейно опускается к 0. Выкл.: при достижении Release Start _Hold сразу 0 (резкий срез).")]
    [SerializeField] private bool _smoothShaderHoldRelease = true;

    private readonly ParticleSingleLifetimeTracker _lifetime = new ParticleSingleLifetimeTracker();
    private readonly L2ShaderHoldController _holdController = new L2ShaderHoldController();

    private float _lastShaderFadeDiagLog;
    private bool _loggedHalfSecondCheckpoint;

    private void Update()
    {
        if (_testSpawnOnlyNoTeardown)
        {
            return;
        }

        if (_lifetime.Stopped && !_holdController.CastEndFadeRequested)
        {
            return;
        }

        if (!_holdController.CanApplyHoldUpdates(_lifetime.Stopped))
        {
            return;
        }

        ApplyCompositeShaderHoldToAllRuntimeMaterials(Now());
    }

    public void FixedUpdate()
    {
        if (_testSpawnOnlyNoTeardown)
        {
            return;
        }

        if (_lifetime.Stopped && !_holdController.CastEndFadeRequested)
        {
            return;
        }

        float now = Now();
        if (_lifetime.IsBeforeStartDelay(now, _startDelay))
        {
            return;
        }

        bool shouldLoopContinuously = _lifetime.ShouldLoopContinuously(_forceContinuousSpawning);
        if (_lifetime.IsSlotExpired(now, _preserveShaderTimeInContinuousLoop))
        {
            ParticleSingleLifetimeDebug.LogSlotOff(BuildDebugSnapshot(now), now);
            SetActive(false, "slot_expired");
            _lifetime.Active = false;
        }

        if (!_lifetime.Active)
        {
            if (_lifetime.SpawnedCount >= _maxCount && !shouldLoopContinuously)
            {
                ParticleSingleLifetimeDebug.LogStopMax(BuildDebugSnapshot(now), now);
                _lifetime.Stopped = true;
                return;
            }

            if (_isBurstSpawning && !shouldLoopContinuously)
            {
                Spawn(now);
                _lifetime.SpawnedCount = _maxCount;
                return;
            }

            float spawnInterval = 1f / Mathf.Max(1f, _countPerSecond);
            if (now - _lifetime.LastLoop >= spawnInterval)
            {
                _lifetime.LastLoop = now;
                Spawn(now);
                _lifetime.SpawnedCount += 1;
            }
        }
        else
        {
            UpdateOwnerWorldPos();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ParticleSingleLifetimeDebug.ShouldTrace(name, _owner, transform)
                && _lifetime.Active
                && !_loggedHalfSecondCheckpoint
                && now - _lifetime.SpawnedAt >= 0.45f)
            {
                _loggedHalfSecondCheckpoint = true;
                Renderer checkRenderer = ResolveRenderer();
                Material checkMat = checkRenderer != null && checkRenderer.materials != null && checkRenderer.materials.Length > 0
                    ? checkRenderer.materials[0]
                    : null;
                ParticleSingleLifetimeDebug.LogCheck500ms(BuildDebugSnapshot(now), now, checkRenderer, checkMat);
            }

            if (ParticleSingleLifetimeDebug.ShouldTrace(name, _owner, transform) && now - _lastShaderFadeDiagLog >= 0.25f)
            {
                _lastShaderFadeDiagLog = now;
                ParticleSingleLifetimeDebug.LogTick(BuildDebugSnapshot(now), now);
            }
#endif
        }
    }

    public override void PlayPart()
    {
        if (_fitToBounds && OwnerTarget != null)
        {
            FitToOwnerWidth(OwnerTarget);
        }

        if (_particles == null || _particles.Length == 0)
        {
            _particles = GetComponentsInChildren<Renderer>(true);
        }

        if (_testSpawnOnlyNoTeardown)
        {
            float t = Now();
            _lifetime.LastEnable = t;
            _lifetime.SpawnedAt = t;
            _lifetime.SpawnedCount = 1;
            _lifetime.Active = true;
            _lifetime.Stopped = true;
            Spawn(t);
            return;
        }

        float now = Now();
        _lifetime.ResetForPlayPart(now);
        _lastShaderFadeDiagLog = 0f;
        _loggedHalfSecondCheckpoint = false;
        _holdController.ResetReleaseLogs();
        _holdController.ResetCastEndFade();

        float materialLifetime = _lifetime.ReadLifetimeFromSharedMaterials(
            ResolveRenderer(),
            name,
            _owner,
            transform,
            _duration);
        _lifetime.PrepareForPlay(
            materialLifetime,
            _hasFixedDuration,
            _holdController.HasRuntimeLoopOverride,
            _holdController.RuntimeLoopOverrideValue);
        _duration = _lifetime.Duration;

        SetActive(false, "playpart_reset");
        ParticleSingleLifetimeDebug.LogPlay(BuildDebugSnapshot(now));
    }

    public override void Setup(EffectSettings s, MagicCastData c)
    {
        _settings = s;
        _castData = c;
        float durationBefore = _duration;
        _lifetime.ResolveDurationFromSetup(
            ref _duration,
            _hasFixedDuration,
            _settings,
            _castData,
            out float legacyHitDuration);

        ParticleSingleDebugSnapshot snap = BuildDebugSnapshot(Now());
        snap.LegacyHitDuration = legacyHitDuration;
        ParticleSingleLifetimeDebug.LogSetup(snap, durationBefore);
        _lifetime.Duration = _duration;
    }

    public override void StopPart()
    {
        if (_testSpawnOnlyNoTeardown)
        {
            return;
        }

        float now = Now();
        Renderer stopRenderer = ResolveRenderer();
        Material stopMat = stopRenderer != null && stopRenderer.materials != null && stopRenderer.materials.Length > 0
            ? stopRenderer.materials[0]
            : null;
        ParticleSingleLifetimeDebug.LogStopPart(BuildDebugSnapshot(now), now, stopRenderer, stopMat);

        _lifetime.RuntimeContinuousLoop = false;
        _lifetime.SpawnedCount = _maxCount;

        if (_holdController.TryBeginCastEndFadeDefer(BuildHoldSettings()))
        {
            ParticleSingleLifetimeDebug.LogHoldReleaseDefer(name, BuildCastTimeline(now), BuildHoldSettings());
            return;
        }

        CompleteImmediateStop(now);
    }

    public void SetRuntimeContinuousLoopOverride(bool hasOverride, bool value)
    {
        _holdController.SetRuntimeLoopOverride(hasOverride, value);

        if ((_lifetime.Active && !_lifetime.Stopped) || _holdController.CastEndFadeRequested)
        {
            ApplyCompositeShaderHoldToAllRuntimeMaterials(Now());
        }
    }

    public void FitToOwnerWidth(Transform target)
    {
        if (target == null)
        {
            return;
        }

        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller == null)
        {
            return;
        }

        float targetWidth = controller.radius * 2f;
        transform.localScale = new Vector3(targetWidth * 4f, 1f, targetWidth * 4f);
    }

    private void Spawn(float now)
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return;
        }

        SetActive(true, "spawn");
        _lifetime.MarkSpawned(now);
        _holdController.ResetReleaseLogs();
        _holdController.ResetCastEndFade();

        float shaderStartTime = now - _relativeWarmupTime;
        float seed = Random.Range(-100f, 100f);
        L2ShaderHoldController.Settings holdSettings = BuildHoldSettings();
        float holdValue = _holdController.EvaluateHold(holdSettings, BuildCastTimeline(now));

        Material[] runtimeMaterials = renderer.materials;
        Material[] sharedMaterials = renderer.sharedMaterials;
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material runtimeMat = runtimeMaterials[i];
            if (runtimeMat == null)
            {
                continue;
            }

            Material sharedMat = sharedMaterials != null && i < sharedMaterials.Length ? sharedMaterials[i] : null;

            if (runtimeMat.HasProperty(AlphaShaderId) && sharedMat != null && sharedMat.HasProperty(AlphaShaderId))
            {
                runtimeMat.SetFloat(AlphaShaderId, sharedMat.GetFloat(AlphaShaderId));
            }

            L2MaterialPropertyCopier.CopyLifetimeFadeAndFxFromShared(runtimeMat, sharedMat);
            runtimeMat.SetFloat(L2MaterialPropertyCopier.StartTimeId, shaderStartTime);
            runtimeMat.SetFloat(L2MaterialPropertyCopier.SeedId, seed);
            SetOwnerWorldPos(runtimeMat);

            if (SurfaceNormal != Vector3.zero)
            {
                runtimeMat.SetVector(SurfaceNormalsShaderId, SurfaceNormal);
            }

            _holdController.ApplySpawnHoldSpecs(runtimeMat, in holdSettings, holdValue);

            if (runtimeMat.HasProperty(L2MaterialPropertyCopier.EmitterAlphaId))
            {
                _holdController.CaptureBaseEmitterAlpha(new[] { runtimeMat });
            }

            ParticleSingleLifetimeDebug.LogSpawn(
                BuildDebugSnapshot(now),
                i,
                now,
                shaderStartTime,
                _relativeWarmupTime,
                seed,
                holdValue,
                _releaseShaderHoldByCastProgress,
                _shaderHoldReleaseStartNormalized,
                _smoothShaderHoldRelease,
                renderer,
                runtimeMat);
        }
    }

    private Renderer ResolveRenderer()
    {
        if (_particles == null || _particles.Length == 0)
        {
            return null;
        }

        return _particles[0];
    }

    private void SetActive(bool value, string reason = null)
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            ParticleSingleLifetimeDebug.LogSetActive(BuildDebugSnapshot(Now()), value, reason, null, false);
            return;
        }

        bool wasActive = renderer.gameObject.activeSelf;
        renderer.gameObject.SetActive(value);
        ParticleSingleLifetimeDebug.LogSetActive(BuildDebugSnapshot(Now()), value, reason, renderer, wasActive);
    }

    private void ApplyCompositeShaderHoldToAllRuntimeMaterials(float now)
    {
        if (!_holdController.CanApplyHoldUpdates(_lifetime.Stopped))
        {
            return;
        }

        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return;
        }

        if (_holdController.ApplyToMaterials(
                renderer.materials,
                BuildHoldSettings(),
                BuildCastTimeline(now),
                name,
                _lifetime.SpawnedAt,
                _lifetime.BaseShaderLifetime))
        {
            _lifetime.Stopped = true;
        }
    }

    private void CompleteImmediateStop(float now)
    {
        _holdController.CompleteImmediateStop();
        ApplyCompositeShaderHoldToAllRuntimeMaterials(now);
        _lifetime.Stopped = true;
    }

    private L2ShaderHoldController.Settings BuildHoldSettings()
    {
        return new L2ShaderHoldController.Settings
        {
            ShaderHold = _shaderHold,
            ReleaseByCastProgress = _releaseShaderHoldByCastProgress,
            ReleaseStartNormalized = _shaderHoldReleaseStartNormalized,
            SmoothRelease = _smoothShaderHoldRelease
        };
    }

    private L2ShaderHoldController.CastTimeline BuildCastTimeline(float now)
    {
        return new L2ShaderHoldController.CastTimeline
        {
            Now = now,
            CastStartTime = _lifetime.LastEnable,
            SlotDuration = _duration,
            Settings = _settings
        };
    }

    private ParticleSingleDebugSnapshot BuildDebugSnapshot(float now)
    {
        return new ParticleSingleDebugSnapshot
        {
            GroupName = name,
            Transform = transform,
            Owner = _owner,
            Renderer = ResolveRenderer(),
            Now = now,
            LastEnable = _lifetime.LastEnable,
            SpawnedAt = _lifetime.SpawnedAt,
            Duration = _duration,
            BaseShaderLifetime = _lifetime.BaseShaderLifetime,
            HasFixedDuration = _hasFixedDuration,
            PreserveShaderTime = _preserveShaderTimeInContinuousLoop,
            ForceContinuousSpawning = _forceContinuousSpawning,
            RuntimeContinuousLoop = _lifetime.RuntimeContinuousLoop,
            HasLoopOverride = _holdController.HasRuntimeLoopOverride,
            LoopOverrideValue = _holdController.RuntimeLoopOverrideValue,
            Active = _lifetime.Active,
            Stopped = _lifetime.Stopped,
            SpawnedCount = _lifetime.SpawnedCount,
            MaxCount = _maxCount,
            CountPerSecond = _countPerSecond,
            StartDelay = _startDelay,
            ShouldLoopContinuously = _lifetime.ShouldLoopContinuously(_forceContinuousSpawning),
            Settings = _settings,
            CastData = _castData
        };
    }

    private float Now() => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

    private void UpdateOwnerWorldPos()
    {
        Renderer renderer = ResolveRenderer();
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
        if (material == null || !material.HasProperty(L2MaterialPropertyCopier.OwnerWorldPosProperty))
        {
            return;
        }

        material.SetVector(L2MaterialPropertyCopier.OwnerWorldPosProperty, ResolveOwnerWorldPos());
    }
}
