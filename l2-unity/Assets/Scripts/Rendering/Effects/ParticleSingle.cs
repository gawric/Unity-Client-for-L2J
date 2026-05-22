using UnityEngine;

public class ParticleSingle : EffectPart
{
    private const string OwnerWorldPosShaderProperty = "_OwnerWorldPos";
    private const string DebugTraceHealEffectToken = "wh_heal";

    private static readonly int HoldShaderId = Shader.PropertyToID("_Hold");
    private static readonly int LifetimeRangeShaderId = Shader.PropertyToID("_LifetimeRange");
    private static readonly int StartTimeShaderId = Shader.PropertyToID("_StartTime");
    private static readonly int HasLifetimeShaderId = Shader.PropertyToID("_HasLifetime");
    private static readonly int InitialDelayRangeShaderId = Shader.PropertyToID("_InitialDelayRange");
    private static readonly int FadeInShaderId = Shader.PropertyToID("_FadeIn");
    private static readonly int FadeInEndTimeShaderId = Shader.PropertyToID("_FadeInEndTime");
    private static readonly int FadeoutShaderId = Shader.PropertyToID("_Fadeout");
    private static readonly int FadeoutStartTimeShaderId = Shader.PropertyToID("_FadeoutStartTime");
    private static readonly int AtlasUvRemapShaderId = Shader.PropertyToID("_AtlasUvRemap");
    private static readonly int AtlasUvMinMaxShaderId = Shader.PropertyToID("_AtlasUvMinMax");
    private static readonly int IgnoreMainTexAlphaShaderId = Shader.PropertyToID("_IgnoreMainTexAlpha");
    private static readonly int EmitterAlphaShaderId = Shader.PropertyToID("_EmitterAlpha");
    private static readonly int AlphaEdgeFeatherShaderId = Shader.PropertyToID("_AlphaEdgeFeather");
    private static readonly int SplitRibbonByLumShaderId = Shader.PropertyToID("_SplitRibbonByLum");
    private static readonly int SoftLumMinShaderId = Shader.PropertyToID("_SoftLumMin");
    private static readonly int SoftLumMaxShaderId = Shader.PropertyToID("_SoftLumMax");
    private static readonly int LineLumMinShaderId = Shader.PropertyToID("_LineLumMin");
    private static readonly int LineLumMaxShaderId = Shader.PropertyToID("_LineLumMax");
    private static readonly int SoftOpacityMulShaderId = Shader.PropertyToID("_SoftOpacityMul");
    private static readonly int LineOpacityMulShaderId = Shader.PropertyToID("_LineOpacityMul");
    private static readonly int SoftRgbBoostShaderId = Shader.PropertyToID("_SoftRgbBoost");
    private static readonly int LineRgbBoostShaderId = Shader.PropertyToID("_LineRgbBoost");
    private static readonly int SplitByUvLayerShaderId = Shader.PropertyToID("_SplitByUvLayer");
    private static readonly int UvLayerCenterShaderId = Shader.PropertyToID("_UvLayerCenter");
    private static readonly int UvLayerDistMinShaderId = Shader.PropertyToID("_UvLayerDistMin");
    private static readonly int UvLayerDistMaxShaderId = Shader.PropertyToID("_UvLayerDistMax");
    private static readonly int OuterSoftOpacityMulShaderId = Shader.PropertyToID("_OuterSoftOpacityMul");
    private static readonly int OuterLineOpacityMulShaderId = Shader.PropertyToID("_OuterLineOpacityMul");
    private static readonly int OuterSoftRgbBoostShaderId = Shader.PropertyToID("_OuterSoftRgbBoost");
    private static readonly int OuterLineRgbBoostShaderId = Shader.PropertyToID("_OuterLineRgbBoost");
    private static readonly int ColorScaleRepeatsShaderId = Shader.PropertyToID("_ColorScaleRepeats");
    private static readonly int ColorScaleCountShaderId = Shader.PropertyToID("_ColorScaleCount");
    private static readonly int ColorScaleTime1ShaderId = Shader.PropertyToID("_ColorScaleTime1");
    private static readonly int ColorScaleTime2ShaderId = Shader.PropertyToID("_ColorScaleTime2");
    private static readonly int ColorScale0ShaderId = Shader.PropertyToID("_ColorScale0");
    private static readonly int ColorScale1ShaderId = Shader.PropertyToID("_ColorScale1");
    private static readonly int ColorScale2ShaderId = Shader.PropertyToID("_ColorScale2");
    private static readonly int BAlphaBlendShaderId = Shader.PropertyToID("_bAlphaBlend");
    private static readonly int BillboardToCameraShaderId = Shader.PropertyToID("_BillboardToCamera");
    private static readonly int BillboardWorldUpShaderId = Shader.PropertyToID("_BillboardWorldUp");
    private static readonly int BillboardEulerOffsetShaderId = Shader.PropertyToID("_BillboardEulerOffset");

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
                    $"activeLoop={(_forceContinuousSpawning || _runtimeContinuousLoop)} " +
                    $"[FADE_PHASE]={BuildFirstMaterialFadePhase(now)} mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
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
                $"[FADE_PHASE]={BuildFirstMaterialFadePhase(now)} mats=[{BuildAllRuntimeMaterialsFadeDiag(now)}].");
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

            Material sharedMat = sharedMaterials != null && i < sharedMaterials.Length ? sharedMaterials[i] : null;

            if (runtimeMat.HasProperty("_Alpha") && sharedMat != null && sharedMat.HasProperty("_Alpha"))
            {
                runtimeMat.SetFloat("_Alpha", sharedMat.GetFloat("_Alpha"));
            }

            TryCopyShaderLifetimeFadeFromShared(runtimeMat, sharedMat);

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
                    $"[FADE_PHASE]={ShaderFadeDiagnostic.FadePhaseLabel(runtimeMat, now)} " +
                    $"diag={ShaderFadeDiagnostic.BuildLine(runtimeMat, now)}");
            }
#endif
        }
    }

    /// <summary>
    /// Копирует lifetime/fade, атлас-UV, альфу, feather, EmitterAlpha, split luma и split UV-слоя (MagicCircleBrighten) из shared в runtime после <c>renderer.materials</c>.
    /// </summary>
    private static void TryCopyShaderLifetimeFadeFromShared(Material runtimeMat, Material sharedMat)
    {
        if (runtimeMat == null || sharedMat == null)
        {
            return;
        }

        if (runtimeMat.HasProperty(HasLifetimeShaderId) && sharedMat.HasProperty(HasLifetimeShaderId))
        {
            runtimeMat.SetFloat(HasLifetimeShaderId, sharedMat.GetFloat(HasLifetimeShaderId));
        }

        if (runtimeMat.HasProperty(LifetimeRangeShaderId) && sharedMat.HasProperty(LifetimeRangeShaderId))
        {
            runtimeMat.SetVector(LifetimeRangeShaderId, sharedMat.GetVector(LifetimeRangeShaderId));
        }

        if (runtimeMat.HasProperty(InitialDelayRangeShaderId) && sharedMat.HasProperty(InitialDelayRangeShaderId))
        {
            runtimeMat.SetVector(InitialDelayRangeShaderId, sharedMat.GetVector(InitialDelayRangeShaderId));
        }

        if (runtimeMat.HasProperty(FadeInShaderId) && sharedMat.HasProperty(FadeInShaderId))
        {
            runtimeMat.SetFloat(FadeInShaderId, sharedMat.GetFloat(FadeInShaderId));
        }

        if (runtimeMat.HasProperty(FadeInEndTimeShaderId) && sharedMat.HasProperty(FadeInEndTimeShaderId))
        {
            runtimeMat.SetFloat(FadeInEndTimeShaderId, sharedMat.GetFloat(FadeInEndTimeShaderId));
        }

        if (runtimeMat.HasProperty(FadeoutShaderId) && sharedMat.HasProperty(FadeoutShaderId))
        {
            runtimeMat.SetFloat(FadeoutShaderId, sharedMat.GetFloat(FadeoutShaderId));
        }

        if (runtimeMat.HasProperty(FadeoutStartTimeShaderId) && sharedMat.HasProperty(FadeoutStartTimeShaderId))
        {
            runtimeMat.SetFloat(FadeoutStartTimeShaderId, sharedMat.GetFloat(FadeoutStartTimeShaderId));
        }

        if (runtimeMat.HasProperty(AtlasUvRemapShaderId) && sharedMat.HasProperty(AtlasUvRemapShaderId))
        {
            runtimeMat.SetFloat(AtlasUvRemapShaderId, sharedMat.GetFloat(AtlasUvRemapShaderId));
        }

        if (runtimeMat.HasProperty(AtlasUvMinMaxShaderId) && sharedMat.HasProperty(AtlasUvMinMaxShaderId))
        {
            runtimeMat.SetVector(AtlasUvMinMaxShaderId, sharedMat.GetVector(AtlasUvMinMaxShaderId));
        }

        if (runtimeMat.HasProperty(IgnoreMainTexAlphaShaderId) && sharedMat.HasProperty(IgnoreMainTexAlphaShaderId))
        {
            runtimeMat.SetFloat(IgnoreMainTexAlphaShaderId, sharedMat.GetFloat(IgnoreMainTexAlphaShaderId));
        }

        if (runtimeMat.HasProperty(EmitterAlphaShaderId) && sharedMat.HasProperty(EmitterAlphaShaderId))
        {
            runtimeMat.SetFloat(EmitterAlphaShaderId, sharedMat.GetFloat(EmitterAlphaShaderId));
        }

        if (runtimeMat.HasProperty(AlphaEdgeFeatherShaderId) && sharedMat.HasProperty(AlphaEdgeFeatherShaderId))
        {
            runtimeMat.SetFloat(AlphaEdgeFeatherShaderId, sharedMat.GetFloat(AlphaEdgeFeatherShaderId));
        }

        if (runtimeMat.HasProperty(SplitRibbonByLumShaderId) && sharedMat.HasProperty(SplitRibbonByLumShaderId))
        {
            runtimeMat.SetFloat(SplitRibbonByLumShaderId, sharedMat.GetFloat(SplitRibbonByLumShaderId));
        }

        if (runtimeMat.HasProperty(SoftLumMinShaderId) && sharedMat.HasProperty(SoftLumMinShaderId))
        {
            runtimeMat.SetFloat(SoftLumMinShaderId, sharedMat.GetFloat(SoftLumMinShaderId));
        }

        if (runtimeMat.HasProperty(SoftLumMaxShaderId) && sharedMat.HasProperty(SoftLumMaxShaderId))
        {
            runtimeMat.SetFloat(SoftLumMaxShaderId, sharedMat.GetFloat(SoftLumMaxShaderId));
        }

        if (runtimeMat.HasProperty(LineLumMinShaderId) && sharedMat.HasProperty(LineLumMinShaderId))
        {
            runtimeMat.SetFloat(LineLumMinShaderId, sharedMat.GetFloat(LineLumMinShaderId));
        }

        if (runtimeMat.HasProperty(LineLumMaxShaderId) && sharedMat.HasProperty(LineLumMaxShaderId))
        {
            runtimeMat.SetFloat(LineLumMaxShaderId, sharedMat.GetFloat(LineLumMaxShaderId));
        }

        if (runtimeMat.HasProperty(SoftOpacityMulShaderId) && sharedMat.HasProperty(SoftOpacityMulShaderId))
        {
            runtimeMat.SetFloat(SoftOpacityMulShaderId, sharedMat.GetFloat(SoftOpacityMulShaderId));
        }

        if (runtimeMat.HasProperty(LineOpacityMulShaderId) && sharedMat.HasProperty(LineOpacityMulShaderId))
        {
            runtimeMat.SetFloat(LineOpacityMulShaderId, sharedMat.GetFloat(LineOpacityMulShaderId));
        }

        if (runtimeMat.HasProperty(SoftRgbBoostShaderId) && sharedMat.HasProperty(SoftRgbBoostShaderId))
        {
            runtimeMat.SetFloat(SoftRgbBoostShaderId, sharedMat.GetFloat(SoftRgbBoostShaderId));
        }

        if (runtimeMat.HasProperty(LineRgbBoostShaderId) && sharedMat.HasProperty(LineRgbBoostShaderId))
        {
            runtimeMat.SetFloat(LineRgbBoostShaderId, sharedMat.GetFloat(LineRgbBoostShaderId));
        }

        if (runtimeMat.HasProperty(SplitByUvLayerShaderId) && sharedMat.HasProperty(SplitByUvLayerShaderId))
        {
            runtimeMat.SetFloat(SplitByUvLayerShaderId, sharedMat.GetFloat(SplitByUvLayerShaderId));
        }

        if (runtimeMat.HasProperty(UvLayerCenterShaderId) && sharedMat.HasProperty(UvLayerCenterShaderId))
        {
            runtimeMat.SetVector(UvLayerCenterShaderId, sharedMat.GetVector(UvLayerCenterShaderId));
        }

        if (runtimeMat.HasProperty(UvLayerDistMinShaderId) && sharedMat.HasProperty(UvLayerDistMinShaderId))
        {
            runtimeMat.SetFloat(UvLayerDistMinShaderId, sharedMat.GetFloat(UvLayerDistMinShaderId));
        }

        if (runtimeMat.HasProperty(UvLayerDistMaxShaderId) && sharedMat.HasProperty(UvLayerDistMaxShaderId))
        {
            runtimeMat.SetFloat(UvLayerDistMaxShaderId, sharedMat.GetFloat(UvLayerDistMaxShaderId));
        }

        if (runtimeMat.HasProperty(OuterSoftOpacityMulShaderId) && sharedMat.HasProperty(OuterSoftOpacityMulShaderId))
        {
            runtimeMat.SetFloat(OuterSoftOpacityMulShaderId, sharedMat.GetFloat(OuterSoftOpacityMulShaderId));
        }

        if (runtimeMat.HasProperty(OuterLineOpacityMulShaderId) && sharedMat.HasProperty(OuterLineOpacityMulShaderId))
        {
            runtimeMat.SetFloat(OuterLineOpacityMulShaderId, sharedMat.GetFloat(OuterLineOpacityMulShaderId));
        }

        if (runtimeMat.HasProperty(OuterSoftRgbBoostShaderId) && sharedMat.HasProperty(OuterSoftRgbBoostShaderId))
        {
            runtimeMat.SetFloat(OuterSoftRgbBoostShaderId, sharedMat.GetFloat(OuterSoftRgbBoostShaderId));
        }

        if (runtimeMat.HasProperty(OuterLineRgbBoostShaderId) && sharedMat.HasProperty(OuterLineRgbBoostShaderId))
        {
            runtimeMat.SetFloat(OuterLineRgbBoostShaderId, sharedMat.GetFloat(OuterLineRgbBoostShaderId));
        }

        if (runtimeMat.HasProperty(ColorScaleRepeatsShaderId) && sharedMat.HasProperty(ColorScaleRepeatsShaderId))
        {
            runtimeMat.SetFloat(ColorScaleRepeatsShaderId, sharedMat.GetFloat(ColorScaleRepeatsShaderId));
        }

        if (runtimeMat.HasProperty(ColorScaleCountShaderId) && sharedMat.HasProperty(ColorScaleCountShaderId))
        {
            runtimeMat.SetFloat(ColorScaleCountShaderId, sharedMat.GetFloat(ColorScaleCountShaderId));
        }

        if (runtimeMat.HasProperty(ColorScaleTime1ShaderId) && sharedMat.HasProperty(ColorScaleTime1ShaderId))
        {
            runtimeMat.SetFloat(ColorScaleTime1ShaderId, sharedMat.GetFloat(ColorScaleTime1ShaderId));
        }

        if (runtimeMat.HasProperty(ColorScaleTime2ShaderId) && sharedMat.HasProperty(ColorScaleTime2ShaderId))
        {
            runtimeMat.SetFloat(ColorScaleTime2ShaderId, sharedMat.GetFloat(ColorScaleTime2ShaderId));
        }

        if (runtimeMat.HasProperty(ColorScale0ShaderId) && sharedMat.HasProperty(ColorScale0ShaderId))
        {
            runtimeMat.SetColor(ColorScale0ShaderId, sharedMat.GetColor(ColorScale0ShaderId));
        }

        if (runtimeMat.HasProperty(ColorScale1ShaderId) && sharedMat.HasProperty(ColorScale1ShaderId))
        {
            runtimeMat.SetColor(ColorScale1ShaderId, sharedMat.GetColor(ColorScale1ShaderId));
        }

        if (runtimeMat.HasProperty(ColorScale2ShaderId) && sharedMat.HasProperty(ColorScale2ShaderId))
        {
            runtimeMat.SetColor(ColorScale2ShaderId, sharedMat.GetColor(ColorScale2ShaderId));
        }

        if (runtimeMat.HasProperty(BAlphaBlendShaderId) && sharedMat.HasProperty(BAlphaBlendShaderId))
        {
            runtimeMat.SetFloat(BAlphaBlendShaderId, sharedMat.GetFloat(BAlphaBlendShaderId));
        }

        if (runtimeMat.HasProperty(BillboardToCameraShaderId) && sharedMat.HasProperty(BillboardToCameraShaderId))
        {
            runtimeMat.SetFloat(BillboardToCameraShaderId, sharedMat.GetFloat(BillboardToCameraShaderId));
        }

        if (runtimeMat.HasProperty(BillboardWorldUpShaderId) && sharedMat.HasProperty(BillboardWorldUpShaderId))
        {
            runtimeMat.SetVector(BillboardWorldUpShaderId, sharedMat.GetVector(BillboardWorldUpShaderId));
        }

        if (runtimeMat.HasProperty(BillboardEulerOffsetShaderId) && sharedMat.HasProperty(BillboardEulerOffsetShaderId))
        {
            runtimeMat.SetVector(BillboardEulerOffsetShaderId, sharedMat.GetVector(BillboardEulerOffsetShaderId));
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
        return ShaderFadeDiagnostic.BuildLine(mat, now);
    }

    private string BuildFirstMaterialFadePhase(float now)
    {
        Renderer renderer = ResolveRenderer();
        if (renderer == null)
        {
            return "no_renderer";
        }

        Material[] mats = renderer.materials;
        if (mats == null || mats.Length == 0 || mats[0] == null)
        {
            return "no_mat";
        }

        return ShaderFadeDiagnostic.FadePhaseLabel(mats[0], now);
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
