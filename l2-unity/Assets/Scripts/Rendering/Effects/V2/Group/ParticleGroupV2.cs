using UnityEngine;

/// <summary>
/// Thin V2 emitter: authored fields + stream + slot set + draw batch.
/// ParticleGroup vs ParticleSingle is flags (maxCount, respawn, stretch), not a second class.
/// No loop/hold, JobPump, or home-flight. Legacy ParticleGroup stays on old prefabs.
/// </summary>
public sealed class ParticleGroupV2 : EffectPart, IParticleEmitterV2
{
    [SerializeField] L2Particle _owner;
    [SerializeField] Renderer[] _particles;
    [Header("Spawning")]
    [SerializeField] float _startDelay;
    [SerializeField] int _countPerSecond = 15;
    [SerializeField] int _maxCount = 1;
    [SerializeField] bool _cloneParticlesToMaxCount;
    [SerializeField] int _cloneParticleLimit = 64;
    [SerializeField] bool _useGpuInstancing = true;
    [SerializeField] bool _isBurstSpawning;
    [SerializeField] float _relativeWarmupTime;
    [SerializeField] float _warmupTicksPerSecond;
    [Header("Timing")]
    [SerializeField] float _duration = 0.2f;
    [SerializeField] bool _hasFixedDuration = true;
    [SerializeField] bool _instantKillAtCastEnd;
    [Tooltip("UC RespawnDeadParticles. Independent of HasFixedDuration.")]
    [SerializeField] bool _respawnDeadParticles = true;
    [Tooltip("UC MaxParticles=1 + no respawn: AdjustparticleLife stretches shader life to HitTime.")]
    [SerializeField] bool _stretchParticleLifeToWindow;

    ParticleSlotSet _slots;
    ParticleDrawBatch _batch;
    ParticleStreamRuntime _runtime;
    ParticleLifetimePolicy _lifetimePolicy = ParticleLifetimePolicy.Authored;
    float _emissionWindow = 1f;
    bool _hasExternalEmissionWindow;
    bool _hostOwnedEmission;
    bool _streamVisible = true;
    bool _destroying;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool _loggedHiddenTick;
#endif

    public bool IsGpuDraw => _runtime != null && _runtime.IsGpuDraw;
    public Material[] GpuMaterials => _runtime != null ? _runtime.GpuMaterials : null;
    public bool IsComplete => _runtime != null && _runtime.IsComplete;
    public ParticleLifetimePolicy LifetimePolicy => _lifetimePolicy;
    public bool HasFixedDuration => _hasFixedDuration;
    public float AuthoredDuration => Mathf.Max(0.01f, _duration);
    public ParticleDrawBatch DrawBatch
    {
        get
        {
            EnsureRuntime();
            return _batch;
        }
    }

    public ParticleGroupAuthoring CaptureAuthoring()
    {
        return new ParticleGroupAuthoring
        {
            particles = _particles,
            startDelay = _startDelay,
            countPerSecond = _countPerSecond,
            maxCount = _maxCount,
            cloneToMaxCount = _cloneParticlesToMaxCount,
            cloneLimit = _cloneParticleLimit,
            useGpuInstancing = _useGpuInstancing,
            isBurstSpawning = _isBurstSpawning,
            relativeWarmupTime = _relativeWarmupTime,
            warmupTicksPerSecond = _warmupTicksPerSecond,
            duration = _duration,
            hasFixedDuration = _hasFixedDuration,
            instantKillAtCastEnd = _instantKillAtCastEnd,
            // Cast-window: UC RespawnDeadParticles (serialized). Fixed-duration
            // parts keep the old no-respawn rule so existing projectile prefabs
            // without this field do not start looping.
            respawnDeadParticles = _hostOwnedEmission || (!_hasFixedDuration && _respawnDeadParticles),
            hostOwnedEmission = _hostOwnedEmission,
            stretchParticleLifeToWindow = _stretchParticleLifeToWindow
        };
    }

    /// <summary>
    /// NPC deco: keep emitting until StopPart. Call before PlayPart.
    /// </summary>
    public void BindHostOwnedEmission()
    {
        _hostOwnedEmission = true;
    }

    public void SetStreamVisible(bool visible)
    {
        _streamVisible = visible;
        _runtime?.SetVisible(visible);
    }

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        ResolveEmissionWindow(settings, castData);
        EnsureRuntime().Bind(CaptureAuthoring());
    }

    /// <summary>
    /// Composite policy (ShotProjectile / Stationary) pushes the window.
    /// Group does not interpret HitTime vs FlightTime.
    /// </summary>
    public void SetEmissionWindow(float windowSeconds, EmitterStopMode stopMode)
    {
        _hasExternalEmissionWindow = true;
        _emissionWindow = Mathf.Max(0.01f, windowSeconds);
        _instantKillAtCastEnd = stopMode == EmitterStopMode.Kill;
        _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
    }

    public override void PlayPart()
    {
        if (!isActiveAndEnabled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[ParticleGroupV2] Play SKIP inactive '" + name +
                "' path=" + GetHierarchyPath() +
                " goActive=" + gameObject.activeInHierarchy +
                " enabled=" + enabled);
#endif
            return;
        }

        ParticleStreamRuntime runtime = EnsureRuntime();
        ParticleGroupAuthoring authoring = CaptureAuthoring();
        float authoredLife = L2FxAdjustParticleLife.ReadAuthoredMaxLife(
            ResolveSlotRenderer(),
            _duration);
        float particleLife = authoredLife;
        if (authoring.stretchParticleLifeToWindow &&
            _emissionWindow > authoredLife + 1e-4f)
        {
            particleLife = _emissionWindow;
            _lifetimePolicy = ParticleLifetimePolicy.StretchParticleLifetimeToCast;
        }

        authoring.authoredParticleLife = authoredLife;
        authoring.targetParticleLife = particleLife;
        runtime.Bind(authoring);
        runtime.DebugName = name;
        runtime.SetVisible(_streamVisible);
        float now = Now();
        float emitWindow = _hostOwnedEmission
            ? float.PositiveInfinity
            : Mathf.Max(_emissionWindow, particleLife);
        runtime.Start(now, emitWindow, particleLife);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _loggedHiddenTick = false;
        int slotCount = runtime.SlotCount;
        float warmupFrac = Mathf.Max(0f, _relativeWarmupTime);
        Debug.Log(
            "[ParticleGroupV2] Play START '" + name +
            "' path=" + GetHierarchyPath() +
            " visible=" + _streamVisible +
            " gpu=" + runtime.IsGpuDraw +
            " slots=" + slotCount +
            " cps=" + _countPerSecond +
            " max=" + _maxCount +
            " burst=" + _isBurstSpawning +
            " hasFixedDuration=" + _hasFixedDuration +
            " externalWindow=" + _hasExternalEmissionWindow +
            " instantKill=" + _instantKillAtCastEnd +
            " window=" + _emissionWindow.ToString("0.###") +
            " duration=" + _duration.ToString("0.###") +
            " shaderLife=" + particleLife.ToString("0.###") +
            " authoredLife=" + authoredLife.ToString("0.###") +
            " stretch=" + authoring.stretchParticleLifeToWindow +
            " respawn=" + authoring.respawnDeadParticles +
            " warmupFrac=" + warmupFrac.ToString("0.###") +
            " warmupTicks=" + _warmupTicksPerSecond.ToString("0.###") +
            " now=" + now.ToString("0.###"));
        if (NpcDeco2911Trace.Matches(name))
        {
            NpcDeco2911Trace.Log(
                "PlayPart hostOwned=" + _hostOwnedEmission +
                " streamVisible=" + _streamVisible +
                " gpuAfterStart=" + runtime.IsGpuDraw +
                " spawnedTotal=" + runtime.SpawnedTotal);
            NpcDeco2911Trace.DumpGroup(this);
        }
#endif
    }

    public override void StopPart()
    {
        if (_runtime == null)
        {
            return;
        }

        _runtime.Stop(_runtime.InstantKillAtCastEnd ? EmitterStopMode.Kill : EmitterStopMode.Drain);
    }

    void FixedUpdate()
    {
        if (!_streamVisible)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_loggedHiddenTick && _runtime != null)
            {
                _loggedHiddenTick = true;
                Debug.LogWarning(
                    "[ParticleGroupV2] Tick SKIP hidden '" + name +
                    "' — streamVisible=false, particles will not spawn/draw.");
            }
#endif
            return;
        }

        _runtime?.Tick(Now());
    }

    void LateUpdate()
    {
        if (!_streamVisible)
        {
            return;
        }

        _runtime?.LateDraw();
    }

    void OnDisable()
    {
        if (_runtime == null)
        {
            return;
        }

        _runtime.Stop(_destroying ? EmitterStopMode.Kill : EmitterStopMode.Drain);
    }

    void OnDestroy()
    {
        _destroying = true;
        _runtime?.Stop(EmitterStopMode.Kill);
        _runtime?.Dispose();
        _runtime = null;
        _batch?.Release();
        _batch = null;
        _slots = null;
    }

    ParticleStreamRuntime EnsureRuntime()
    {
        if (_runtime != null)
        {
            return _runtime;
        }

        _slots ??= new ParticleSlotSet(this);
        _batch ??= new ParticleDrawBatch();
        _runtime = new ParticleStreamRuntime(this, _slots, _batch);
        return _runtime;
    }

    Renderer ResolveSlotRenderer()
    {
        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] != null)
                {
                    return _particles[i];
                }
            }
        }

        return GetComponentInChildren<Renderer>(true);
    }

    void ResolveEmissionWindow(EffectSettings settings, MagicCastData castData)
    {
        if (_hostOwnedEmission)
        {
            _lifetimePolicy = ParticleLifetimePolicy.Authored;
            _emissionWindow = Mathf.Max(0.01f, _duration);
            return;
        }

        if (_hasExternalEmissionWindow)
        {
            _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
            _emissionWindow = Mathf.Max(0.01f, _emissionWindow);
            return;
        }

        if (_hasFixedDuration)
        {
            _lifetimePolicy = ParticleLifetimePolicy.Authored;
            _emissionWindow = Mathf.Max(0.01f, _duration);
            return;
        }

        _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
        float fallback = Mathf.Max(0.01f, _duration);
        _emissionWindow = EffectCastDurationResolver.Resolve(
            fallback,
            false,
            settings,
            castData,
            out _,
            out _);
        if (_emissionWindow < 0.01f)
        {
            _emissionWindow = fallback;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    string GetHierarchyPath()
    {
        Transform current = transform;
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
#endif
}
