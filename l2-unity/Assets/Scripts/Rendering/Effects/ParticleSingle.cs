using UnityEngine;

public class ParticleSingle : EffectPart
{
    private const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    private const string DebugTraceHealEffectToken = "wh_heal";

    private static readonly int HasLifetimeShaderId = Shader.PropertyToID("_HasLifetime");
    private static readonly int HoldShaderId = Shader.PropertyToID("_Hold");
    private static readonly int FadeInShaderId = Shader.PropertyToID("_FadeIn");
    private static readonly int FadeoutShaderId = Shader.PropertyToID("_Fadeout");
    private static readonly int FadeoutStartTimeShaderId = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int LifetimeRangeShaderId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");

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
    private float _lastShaderFadeDiagLog;

    public void FixedUpdate()
    {
        if (_testSpawnOnlyNoTeardown)
        {
            return;
        }

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
                    $"[PARTICLE_SINGLE_SLOT_OFF] group='{name}' now={now:F3}s alive={(now - _spawnedAt):F3}s " +
                    $"duration={_duration:F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop} " +
                    $"forceSpawnLoop={_forceContinuousSpawning} runtimeLoop={_runtimeContinuousLoop} loopOv={_hasRuntimeContinuousLoopOverride}:{_runtimeContinuousLoopOverrideValue} " +
                    $"mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
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
                        $"[PARTICLE_SINGLE_STOP_MAX] group='{name}' now={now:F3}s spawned={_spawnedCount}/{_maxCount} " +
                        $"duration={_duration:F3}s shouldLoop={shouldLoopContinuously} " +
                        $"mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
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
            ApplyCompositeShaderHoldToAllRuntimeMaterials(now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal() && now - _lastShaderFadeDiagLog >= 0.25f)
            {
                _lastShaderFadeDiagLog = now;
                Debug.Log(
                    $"[PARTICLE_SINGLE_TICK] group='{name}' now={now:F3}s alive={(now - _spawnedAt):F3}s " +
                    $"duration={_duration:F3}s castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                    $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                    $"activeLoop={(_forceContinuousSpawning || _runtimeContinuousLoop)} mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
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

        if (_testSpawnOnlyNoTeardown)
        {
            if (_particles == null || _particles.Length == 0)
            {
                _particles = GetComponentsInChildren<Renderer>(true);
            }

            float t = Now();
            _lastEnable = t;
            _spawnedAt = t;
            _spawnedCount = 1;
            _active = true;
            _stopped = true;
            Spawn(t);
            return;
        }

        _lastEnable = Now();
        _lastLoop = 0f;
        _spawnedAt = 0f;
        _spawnedCount = 0;
        _active = false;
        _stopped = false;
        _lastShaderFadeDiagLog = 0f;

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
                $"[PARTICLE_SINGLE_PLAY] group='{name}' playAt={_lastEnable:F3}s " +
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
                $"[PARTICLE_SINGLE_SETUP] group='{name}' owner='{(_owner != null ? _owner.name : "null")}' " +
                $"durationBefore={durationBefore:F3}s durationAfter={_duration:F3}s fixedDuration={_hasFixedDuration} " +
                $"castHit={castHitDuration:F3}s settingsLife={settingsDuration:F3}s legacyHit={legacyHitDuration:F3}s " +
                $"settingsHide={(_settings != null ? _settings.hideTime : -1f):F3}s preserveShaderTime={_preserveShaderTimeInContinuousLoop}.");
        }
#endif
    }

    public override void StopPart()
    {
        if (_testSpawnOnlyNoTeardown)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ShouldTraceWhHeal())
        {
            float now = Now();
            Debug.Log(
                $"[PARTICLE_SINGLE_STOPPART] group='{name}' now={now:F3}s elapsed={(now - _lastEnable):F3}s " +
                $"duration={_duration:F3}s castHit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
                $"settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                $"mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
        }
#endif
       // _stopped = true;
       // SetActive(false);
       // _active = false;
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

            TryApplyCompositeShaderHoldToMaterial(runtimeMat, now);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldTraceWhHeal())
            {
                Debug.Log(
                    $"[PARTICLE_SINGLE_SPAWN] group='{name}' idx={i} now={now:F3}s shaderStartTime={shaderStartTime:F3}s relativeWarmup={_relativeWarmupTime:F3}s _Seed={seed:F3} " +
                    $"castHit={(_castData != null ? _castData.HitTime : -1f):F3}s settingsLife={(_settings != null ? _settings.defaultLifeTime : -1f):F3}s " +
                    $"diag={BuildShaderFadeDiagnosticLine(runtimeMat, now)}");
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
            if (mat != null && mat.HasProperty(LifetimeRangeShaderId))
            {
                return mat.GetVector(LifetimeRangeShaderId).y;
            }
        }

        return 0.5f;
    }

    /// <summary>
    /// См. <see cref="TimedCompositeEffectBase.ApplyPartLoopOverrides"/>: continuous loop → пишем _Hold в L2SkillEffect.
    /// Ближе к концу <see cref="_duration"/> снимаем Hold, иначе шейдер «залипает» и FadeOut не проявляется.
    /// </summary>
    private void TryApplyCompositeShaderHoldToMaterial(Material runtimeMat, float now)
    {
        if (runtimeMat == null || !runtimeMat.HasProperty(HoldShaderId))
        {
            return;
        }

        if (!_hasRuntimeContinuousLoopOverride || !_runtimeContinuousLoopOverrideValue)
        {
            return;
        }

        runtimeMat.SetFloat(HoldShaderId, EvaluateShaderHoldForCastNow(now));
    }

    private void ApplyCompositeShaderHoldToAllRuntimeMaterials(float now)
    {
        if (!_hasRuntimeContinuousLoopOverride || !_runtimeContinuousLoopOverrideValue || !_releaseShaderHoldByCastProgress)
        {
            return;
        }

        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return;
        }

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            TryApplyCompositeShaderHoldToMaterial(materials[i], now);
        }
    }

    private float EvaluateShaderHoldForCastNow(float now)
    {
        if (!_releaseShaderHoldByCastProgress)
        {
            return _shaderHold;
        }

        float dur = Mathf.Max(1e-4f, _duration);
        float u = Mathf.Clamp01((now - _spawnedAt) / dur);
        return EvaluateShaderHoldForNormalizedCast(u);
    }

    private float EvaluateShaderHoldForNormalizedCast(float u)
    {
        if (!_releaseShaderHoldByCastProgress)
        {
            return _shaderHold;
        }

        u = Mathf.Clamp01(u);
        float start = Mathf.Clamp(_shaderHoldReleaseStartNormalized, 0f, 0.999f);
        if (u < start)
        {
            return _shaderHold;
        }

        if (_smoothShaderHoldRelease)
        {
            if (start >= 1f - 1e-4f)
            {
                return 0f;
            }

            float t = Mathf.InverseLerp(start, 1f, u);
            return Mathf.Lerp(_shaderHold, 0f, t);
        }

        return 0f;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static string BuildShaderFadeDiagnosticLine(Material mat, float now)
    {
        if (mat == null)
        {
            return "null_mat";
        }

        string sn = mat.shader != null ? mat.shader.name : "no_shader";
        float hasLt = mat.HasProperty(HasLifetimeShaderId) ? mat.GetFloat(HasLifetimeShaderId) : -1f;
        float fadeout = mat.HasProperty(FadeoutShaderId) ? mat.GetFloat(FadeoutShaderId) : -1f;
        float fadeIn = mat.HasProperty(FadeInShaderId) ? mat.GetFloat(FadeInShaderId) : -1f;
        float fadeStart = mat.HasProperty(FadeoutStartTimeShaderId) ? mat.GetFloat(FadeoutStartTimeShaderId) : -1f;
        Vector4 life = mat.HasProperty(LifetimeRangeShaderId) ? mat.GetVector(LifetimeRangeShaderId) : Vector4.zero;
        float lifeMax = Mathf.Max(life.x, life.y, 1e-6f);
        float hold = mat.HasProperty(HoldShaderId) ? mat.GetFloat(HoldShaderId) : -1f;
        float st = mat.HasProperty(StartTimeShaderId) ? mat.GetFloat(StartTimeShaderId) : -1f;
        float age = st > -0.5f ? now - st : -1f;
        float tail = (fadeStart >= 0f && lifeMax > 1e-4f) ? lifeMax - fadeStart : -1f;
        float fadeFrac =
            tail > 1e-4f && age >= fadeStart
                ? Mathf.Clamp01((age - fadeStart) / tail)
                : -1f;
        return
            $"{mat.name} shader={sn} HasLt={hasLt} Fadeout={fadeout} FadeIn={fadeIn} " +
            $"fadeStart={fadeStart:F4} lifeMax={lifeMax:F4} tail={tail:F4} Hold={hold} " +
            $"StartT={st:F4} age={age:F4}s fadeFrac={fadeFrac:F3}";
    }

    private string BuildAllRuntimeMaterialsFadeDiag(float now)
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return "no_renderer";
        }

        Material[] mats = renderer.materials;
        if (mats == null || mats.Length == 0)
        {
            return "no_runtime_material";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < mats.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(" | ");
            }

            sb.Append('[').Append(i).Append("] ").Append(BuildShaderFadeDiagnosticLine(mats[i], now));
        }

        return sb.ToString();
    }
#endif

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


}
