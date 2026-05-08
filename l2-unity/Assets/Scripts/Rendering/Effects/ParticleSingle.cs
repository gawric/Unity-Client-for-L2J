using UnityEngine;

public class ParticleSingle : EffectPart
{
    private const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    private const string DebugTraceHealEffectToken = "wh_heal";

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

    private bool _stopped = true;
    private bool _active;
    private float _lastEnable;
    private float _lastLoop;
    private float _spawnedAt;
    private int _spawnedCount;
    private float _baseShaderLifetime = -1f;
    private bool _runtimeContinuousLoop;
    private bool _hasRuntimeContinuousLoopOverride;
    private bool _runtimeContinuousLoopOverrideValue;
    private float _lastWhHealTickLog;

    public void FixedUpdate()
    {
        if (_stopped)
        {
            return;
        }

        float now = Now();
        if (now - _lastEnable < _startDelay)
        {
            return;
        }

        bool shouldLoopContinuously = _forceContinuousSpawning || _runtimeContinuousLoop;
        bool expired = _active && !_preserveShaderTimeInContinuousLoop && now - _spawnedAt >= _duration;
        if (expired)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal())
            {
                Debug.Log(
                    $"[WH_HEAL_SINGLE_SLOT_OFF] group='{name}' now={now:F3}s alive={(now - _spawnedAt):F3}s " +
                    $"duration={_duration:F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop} runtimeLoop={_runtimeContinuousLoop}.");
            }
#endif
            SetActive(false);
            _active = false;
        }

        if (!_active)
        {
            if (_spawnedCount >= _maxCount && !shouldLoopContinuously)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (ShouldTraceWhHeal())
                {
                    Debug.Log(
                        $"[WH_HEAL_SINGLE_STOP] group='{name}' now={now:F3}s spawned={_spawnedCount}/{_maxCount} " +
                        $"duration={_duration:F3}s shouldLoop={shouldLoopContinuously}.");
                }
#endif
                _stopped = true;
                return;
            }

            if (_isBurstSpawning && !shouldLoopContinuously)
            {
                Spawn(now);
                _spawnedCount = _maxCount;
                return;
            }

            float spawnInterval = 1f / Mathf.Max(1f, _countPerSecond);
            if (now - _lastLoop >= spawnInterval)
            {
                _lastLoop = now;
                Spawn(now);
                _spawnedCount += 1;
            }
        }
        else
        {
            UpdateOwnerWorldPos();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal() && now - _lastWhHealTickLog >= 0.25f)
            {
                _lastWhHealTickLog = now;
                Debug.Log(
                    $"[WH_HEAL_SINGLE_TICK] group='{name}' now={now:F3}s alive={(now - _spawnedAt):F3}s " +
                    $"duration={_duration:F3}s castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                    $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s mat={BuildRuntimeMaterialLifetimeSnapshot(now)}.");
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

        _lastEnable = Now();
        _lastLoop = 0f;
        _spawnedAt = 0f;
        _spawnedCount = 0;
        _active = false;
        _stopped = false;
        _lastWhHealTickLog = 0f;

        if (_duration < 0.01f)
        {
            _duration = GetLifeTimeFromMaterial();
        }

        if (_particles == null || _particles.Length == 0)
        {
            _particles = GetComponentsInChildren<Renderer>(true);
        }

        _baseShaderLifetime = Mathf.Max(0.01f, GetLifeTimeFromMaterial());
        _runtimeContinuousLoop = !_hasFixedDuration && _duration > _baseShaderLifetime + 0.05f;
        if (_hasRuntimeContinuousLoopOverride)
        {
            _runtimeContinuousLoop = _runtimeContinuousLoopOverrideValue;
        }

        SetActive(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceWhHeal())
        {
            Debug.Log(
                $"[WH_HEAL_SINGLE_PLAY] group='{name}' playAt={_lastEnable:F3}s " +
                $"duration={_duration:F3}s baseShaderLife={_baseShaderLifetime:F3}s fixedDuration={_hasFixedDuration} " +
                $"runtimeLoop={_runtimeContinuousLoop} loopOv={_hasRuntimeContinuousLoopOverride}:{_runtimeContinuousLoopOverrideValue} " +
                $"maxCount={_maxCount} cps={_countPerSecond} startDelay={_startDelay:F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop}.");
        }
#endif
    }

    public override void Setup(EffectSettings s, MagicCastData c)
    {
        _settings = s;
        _castData = c;
        float durationBefore = _duration;
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

        if (!_hasFixedDuration)
        {
            _duration = Mathf.Max(_duration, castHitDuration, settingsDuration, legacyHitDuration);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceWhHeal())
        {
            Debug.Log(
                $"[WH_HEAL_SINGLE_SETUP] group='{name}' owner='{(_owner != null ? _owner.name : "null")}' " +
                $"durationBefore={durationBefore:F3}s durationAfter={_duration:F3}s fixedDuration={_hasFixedDuration} " +
                $"castHit={castHitDuration:F3}s settingsLife={settingsDuration:F3}s legacyHit={legacyHitDuration:F3}s " +
                $"settingsHide={(_settings != null ? _settings.hideTime : -1f):F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop}.");
        }
#endif
    }

    public override void StopPart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceWhHeal())
        {
            float now = Now();
            Debug.Log(
                $"[WH_HEAL_SINGLE_STOPPART] group='{name}' now={now:F3}s elapsed={(now - _lastEnable):F3}s " +
                $"duration={_duration:F3}s castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s.");
        }
#endif
        _stopped = true;
        SetActive(false);
        _active = false;
    }

    public void SetRuntimeContinuousLoopOverride(bool hasOverride, bool value)
    {
        _hasRuntimeContinuousLoopOverride = hasOverride;
        _runtimeContinuousLoopOverrideValue = value;
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

        SetActive(true);
        _active = true;
        _spawnedAt = now;

        float shaderStartTime = now - _relativeWarmupTime;
        float seed = Random.Range(-100f, 100f);
        Material[] runtimeMaterials = renderer.materials;
        Material[] sharedMaterials = renderer.sharedMaterials;
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material runtimeMat = runtimeMaterials[i];
            if (runtimeMat == null)
            {
                continue;
            }

            if (runtimeMat.HasProperty("_Alpha") && sharedMaterials != null && i < sharedMaterials.Length)
            {
                Material sharedMat = sharedMaterials[i];
                if (sharedMat != null && sharedMat.HasProperty("_Alpha"))
                {
                    runtimeMat.SetFloat("_Alpha", sharedMat.GetFloat("_Alpha"));
                }
            }

            runtimeMat.SetFloat("_StartTime", shaderStartTime);
            runtimeMat.SetFloat("_Seed", seed);
            SetOwnerWorldPos(runtimeMat);
            if (SurfaceNormal != Vector3.zero)
            {
                runtimeMat.SetVector("_SurfaceNormals", SurfaceNormal);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal())
            {
                float hasLifetime = runtimeMat.HasProperty("_HasLifetime") ? runtimeMat.GetFloat("_HasLifetime") : -1f;
                Vector4 life = runtimeMat.HasProperty("_LifetimeRange") ? runtimeMat.GetVector("_LifetimeRange") : Vector4.zero;
                Vector4 initDelay = runtimeMat.HasProperty("_InitialDelayRange") ? runtimeMat.GetVector("_InitialDelayRange") : Vector4.zero;
                Debug.Log(
                    $"[WH_HEAL_SINGLE_SPAWN] group='{name}' mat='{runtimeMat.name}' now={now:F3}s " +
                    $"_StartTime={shaderStartTime:F3}s shaderAgeApprox={(now - shaderStartTime):F3}s _Seed={seed:F3} " +
                    $"_HasLifetime={hasLifetime:F3} _LifetimeRange=({life.x:F3},{life.y:F3}) _InitialDelay=({initDelay.x:F3},{initDelay.y:F3}) " +
                    $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s.");
            }
#endif
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

    private void SetActive(bool value)
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return;
        }

        renderer.gameObject.SetActive(value);
    }

    private float GetLifeTimeFromMaterial()
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null || renderer.sharedMaterials == null)
        {
            return _duration;
        }

        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            Material mat = renderer.sharedMaterials[i];
            if (mat != null && mat.HasProperty("_LifetimeRange"))
            {
                return mat.GetVector("_LifetimeRange").y;
            }
        }

        return 0.5f;
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
        if (material == null || !material.HasProperty(OwnerWorldPosShaderProperty))
        {
            return;
        }

        material.SetVector(OwnerWorldPosShaderProperty, ResolveOwnerWorldPos());
    }

    private Vector3 ResolveOwnerWorldPos()
    {
        if (PlayerEntity.Instance != null)
        {
            return PlayerEntity.Instance.transform.position;
        }

        if (OwnerTarget != null)
        {
            return OwnerTarget.position;
        }

        if (FollowTarget != null)
        {
            return FollowTarget.position;
        }

        return transform.position + transform.forward;
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

    private string BuildRuntimeMaterialLifetimeSnapshot(float now)
    {
        Renderer renderer = ResolveRenderer();
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
        float hasLifetime = mat.HasProperty("_HasLifetime") ? mat.GetFloat("_HasLifetime") : -1f;
        Vector4 lifetime = mat.HasProperty("_LifetimeRange") ? mat.GetVector("_LifetimeRange") : Vector4.zero;
        float startTime = mat.HasProperty("_StartTime") ? mat.GetFloat("_StartTime") : -1f;
        float age = startTime > -0.5f ? now - startTime : -1f;
        return $"{mat.name}:_HasLifetime={hasLifetime:F3} life=({lifetime.x:F3},{lifetime.y:F3}) _StartTime={startTime:F3} age~={age:F3}s";
    }
}
