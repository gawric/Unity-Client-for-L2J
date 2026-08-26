using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Thin Unity orchestrator. Inspector fields stay here for prefab serialization.
/// Playback, Jobs, and renderer work live in dedicated models.
/// </summary>
public class ParticleGroup : EffectPart
{
    [SerializeField] private L2Particle _owner;
    [SerializeField] private Renderer[] _particles;
    [Header("Spawning (Настройки появления)")]
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private int _countPerSecond = 15;
    [SerializeField] private int _maxCount = 2;
    [SerializeField] private bool _cloneParticlesToMaxCount;
    [SerializeField] private int _cloneParticleLimit = 64;
    [SerializeField] private bool _forceContinuousSpawning;
    [Tooltip("Batch identical mesh+material slots into one DrawCall. Shader must tag L2FxGpuInstancing=On.")]
    [SerializeField] private bool _useGpuInstancing = true;
    [Tooltip("Continuous loop keeps already-active particles alive without resetting shader _StartTime/_Seed. Use for effects whose shader animation must keep running, for example mesh spin.")]
    [SerializeField] private bool _preserveShaderTimeInContinuousLoop;

    [Space(10)]
    [SerializeField] private bool _isBurstSpawning;
    [SerializeField] private float _relativeWarmupTime;

    [Header("Loop & Timing")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private bool _hasFixedDuration = true;
    [SerializeField] private bool _instantKillAtCastEnd;
    [SerializeField] private bool _fitToBounds;

    [Header("Home Projectile Flight")]
    [SerializeField] private bool _homeFlightAnchor;
    [SerializeField] private float _homePathSideOffsetMultiplier = 1f;
    [SerializeField] private float _homePathSideOffsetScale = 1f;
    [SerializeField] private float _homePathHeightOffsetScale = 1f;
    [SerializeField] private float _homeFlightSpeedScale = 1f;

    readonly ParticleGroupPlaybackState _playback = new ParticleGroupPlaybackState();
    readonly ParticleGroupSimulation _simulation = new ParticleGroupSimulation();
    ParticleGroupRendererService _renderers;
    ParticleGroupHomeFlightProfile _runtimeHomeFlightProfile;
    bool _hasRuntimeHomeFlightProfile;
    float[] _particleSpawnTimes;
    bool[] _isParticleActive;

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

    public void StartGroupPlayback() => PlayPart();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public L2Particle OwnerParticle => _owner;
#endif

    public uint MeshEmitter3AppRandBaseState => _playback.MeshRandBase;

    ParticleGroupRendererService Renderers =>
        _renderers ??= new ParticleGroupRendererService(this);

    public void FixedUpdate()
    {
        if (_playback.Stopped || Renderers.Count == 0)
            return;

        float now = Now();
        if (!_playback.SpawnStopped && now - _playback.LastEnable < _startDelay)
            return;

        // _duration is the authored/server slot window. Using shader lifetime
        // here makes continuous caster effects recycle early and visibly replay.
        _simulation.ScheduleExpire(now, _duration);
        ParticleGroupJobPump.Enqueue(this, now);
    }

    internal JobHandle ConsumeExpireHandle() => _simulation.ConsumeExpireHandle();

    internal void ApplyExpireAndSpawn(float now)
    {
        if (_playback.Stopped)
            return;

        bool anyActive = Renderers.ApplyExpire(_simulation, _isParticleActive, _particleSpawnTimes);
        if (_playback.SpawnStopped)
        {
            if (!anyActive)
                _playback.Stopped = true;
            return;
        }

        bool loop = _playback.ShouldLoopContinuously(_forceContinuousSpawning);
        if (_playback.SpawnedCount < _maxCount || loop)
        {
            if (_isBurstSpawning && !loop && !_playback.BurstFinished)
            {
                ActivateBurst(now);
                _playback.BurstFinished = true;
            }
            else if (now - _playback.LastLoop >= 1f / _countPerSecond)
            {
                _playback.LastLoop = now;
                ActivateParticle(now);
                _playback.SpawnedCount++;
            }
        }
        else if (!anyActive)
        {
            _playback.Stopped = true;
        }
    }

    void LateUpdate()
    {
        if (_playback.Stopped || !_simulation.CanPackAndDraw)
            return;

        _simulation.CompleteExpire();
        _simulation.PackAndDraw(
            Renderers.ResolveGpuOwnerWorldPos(_simulation.GpuMaterials),
            Renderers.ResolveObjectToWorldMatrices());
    }

    public override void PlayPart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_playback.Stopped)
            EffectDoublePlayLog.Repeat(
                "ParticleGroup.PlayPart",
                this,
                _playback.LastEnable,
                _playback.FirstPlayStack);
#endif
        ParticleGroupJobPump.Remove(this);
        _simulation.CompleteExpire();

        if (_fitToBounds && OwnerTarget != null)
            FitToOwnerWidth(OwnerTarget);

        Renderers.SetParticles(_particles);
        Renderers.CollectIfEmpty();
        float shaderSlotDuration = Renderers.ReadShaderSlotDuration(_duration);
        if (_duration < 0.01f || _duration < shaderSlotDuration)
            _duration = shaderSlotDuration;

        Renderers.EnsureClones(_cloneParticlesToMaxCount, _maxCount, _cloneParticleLimit, ref _playback.RuntimeClonesCreated);
        _particles = Renderers.Particles;
        Renderers.ExpandShaderDrivenBounds();

        EnsureManagedSlots(Renderers.Count);
        _simulation.EnsureLifetime(Renderers.Count, reset: true);
        bool gpuBound = _simulation.TryBindGpu(_particles, _useGpuInstancing);
        if (gpuBound)
            Renderers.DisableForGpuDraw();
        else
            Renderers.EnableForGameObjectDraw();

        _playback.BaseShaderLifetime = Mathf.Max(0.01f, Renderers.ReadLifetimeMax(_duration));
        _playback.Begin(Now(), _countPerSecond, _hasFixedDuration, _duration);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _playback.FirstPlayStack = EffectDoublePlayLog.CaptureStack();
        DocExtractorDropAdenaCoinLogger.OnPlayPart(this);
#endif
        Renderers.HideAll();
    }

    public override void Setup(EffectSettings s, MagicCastData c)
    {
        _settings = s;
        _castData = c;
        if (!_hasFixedDuration)
        {
            _duration = EffectCastDurationResolver.Resolve(
                _duration,
                _hasFixedDuration,
                _settings,
                _castData,
                out _,
                out _);
        }
    }

    public override void StopPart()
    {
        _playback.StopSpawning();
        if (_instantKillAtCastEnd)
        {
            DeactivateAllParticles();
            _playback.Stopped = true;
        }
    }

    public void SetRuntimeContinuousLoopOverride(bool hasOverride, bool value) =>
        _playback.SetLoopOverride(hasOverride, value);

    public void FitToOwnerWidth(Transform target)
    {
        if (target == null)
            return;
        var controller = target.GetComponent<CharacterController>();
        if (controller == null)
            return;
        float targetWidth = controller.radius * 2f;
        transform.localScale = new Vector3(targetWidth * 4f, 1f, targetWidth * 4f);
    }

    void ActivateBurst(float now)
    {
        bool skipRestart = _playback.ShouldLoopContinuously(_forceContinuousSpawning) &&
                           _preserveShaderTimeInContinuousLoop;
        if (_simulation.TryActivateGpuBurst(
            now,
            _relativeWarmupTime,
            _maxCount,
            skipRestart,
            _playback.MeshRandBase,
            _playback.SpriteRandBase,
            ref _playback.ParticleIndex,
            _isParticleActive,
            _particleSpawnTimes))
        {
            _playback.SpawnedCount += _maxCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DocExtractorDropAdenaCoinLogger.OnBurst(
                this,
                _playback.MeshRandBase,
                _maxCount,
                now,
                now - _relativeWarmupTime);
#endif
            return;
        }

        for (int i = 0; i < _maxCount; i++)
        {
            ActivateParticle(now);
            _playback.SpawnedCount++;
        }
    }

    void ActivateParticle(float now)
    {
        if (Renderers.Count == 0)
            return;

        _playback.WrapIndex(Renderers.Count);
        EnsureManagedSlots(Renderers.Count);
        _simulation.EnsureLifetime(Renderers.Count);

        if (_simulation.GpuEnabled)
        {
            ActivateGpuParticle(now);
            return;
        }

        Renderer renderer = Renderers.Particles[_playback.ParticleIndex];
        GameObject particleObject = renderer.gameObject;
        bool loop = _playback.ShouldLoopContinuously(_forceContinuousSpawning);
        if (loop &&
            _isParticleActive[_playback.ParticleIndex] &&
            particleObject.activeSelf &&
            _preserveShaderTimeInContinuousLoop)
        {
            Renderers.UpdateWorldPositions(renderer);
            _playback.AdvanceIndex(Renderers.Count);
            return;
        }

        particleObject.SetActive(true);
        int slot = _playback.ParticleIndex;
        float shaderStartTime = now - _relativeWarmupTime;
        _simulation.MarkSlotActive(
            slot,
            now,
            _isParticleActive,
            _particleSpawnTimes);
        Renderers.ActivateGoSlot(
            slot,
            shaderStartTime,
            Random.Range(-100f, 100f),
            _playback.MeshRandBase,
            _playback.SpriteRandBase);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DocExtractorDropAdenaCoinLogger.OnSlot(
            this,
            slot,
            _playback.MeshRandBase,
            now,
            shaderStartTime);
#endif
        _playback.AdvanceIndex(Renderers.Count);
    }

    void ActivateGpuParticle(float now)
    {
        bool loop = _playback.ShouldLoopContinuously(_forceContinuousSpawning);
        if (loop &&
            _isParticleActive[_playback.ParticleIndex] &&
            _preserveShaderTimeInContinuousLoop)
        {
            _playback.AdvanceIndex(Renderers.Count);
            return;
        }

        int slot = _playback.ParticleIndex;
        float shaderStartTime = now - _relativeWarmupTime;
        _simulation.ActivateGpuSlot(
            slot,
            now,
            shaderStartTime,
            Random.Range(-100f, 100f),
            _playback.MeshRandBase,
            _playback.SpriteRandBase,
            _isParticleActive,
            _particleSpawnTimes);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DocExtractorDropAdenaCoinLogger.OnSlot(
            this,
            slot,
            _playback.MeshRandBase,
            now,
            shaderStartTime);
#endif
        _playback.AdvanceIndex(Renderers.Count);
    }

    void DeactivateAllParticles()
    {
        ParticleGroupJobPump.Remove(this);
        _simulation.CompleteExpire();
        if (Renderers.Count == 0)
            return;

        EnsureManagedSlots(Renderers.Count);
        _simulation.EnsureLifetime(Renderers.Count);
        _simulation.ClearAllActive(_isParticleActive);
        Renderers.HideAll();
    }

    void EnsureManagedSlots(int count)
    {
        if (count <= 0)
            return;
        if (_particleSpawnTimes != null && _particleSpawnTimes.Length == count)
            return;

        _particleSpawnTimes = new float[count];
        _isParticleActive = new bool[count];
    }

    void OnDisable()
    {
        ParticleGroupJobPump.Remove(this);
        _simulation.CompleteExpire();
        if (_isParticleActive != null)
            Renderers.ApplyExpire(_simulation, _isParticleActive, _particleSpawnTimes);
    }

    void OnDestroy()
    {
        ParticleGroupJobPump.Remove(this);
        _simulation.Dispose();
    }
}
