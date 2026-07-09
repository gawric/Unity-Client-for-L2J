using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectileLaunchMode
{
    Disabled = 0,
    Immediate = 1,
    OnAnimationShoot = 2
}

[Serializable]
public class CompositeProjectileConfig
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.Disabled;
    // If false and launchMode=OnAnimationShoot, part stays hidden until shoot event.
    public bool showBeforeAnimationShoot = true;
    public ProjectileImpactType impactType = ProjectileImpactType.EffectOnly;
    public ProjectileData settingsOverride;
}

[Serializable]
public class CompositeHomeProjectileConfig
{
    public ProjectileLaunchMode launchMode = ProjectileLaunchMode.Disabled;
    // Original m_u003_b uses 450 Unreal units/sec. In Unity scale this is ~4.5 m/sec.
    public float speed = 4.5f;
    public float acceleration = 0f;
    // When distance to caster (home point or root) <= this, start fade-out.
    public float fadeStartDistance = 0.5f;
    public float fadeOutSeconds = 0.35f;
    // Optional hard finish if still within this range after fade started.
    public float arriveDistance = 0.2f;
    // 0 = fly until arrival at caster; >0 = safety cap only.
    public float maxLifetime = 0f;
    public EffectAttachmentPoint homeAttachmentPoint = EffectAttachmentPoint.CasterCenter;
    public Vector3 homeOffset = new Vector3(0f, 0.1f, 0f);
    public bool usePathArc = true;
    [Tooltip("Legacy apex shift. Used when pathApexAlongLine <= 0 (apex = 0.46 + factor*0.2).")]
    public float pathStartLineFactor = -0.15f;
    [Tooltip("Along-line peel from spawn (0=monster, 1=player).")]
    [Range(0f, 1f)]
    public float pathPeelAlongLine = 0.16f;
    [Tooltip("Along-line apex of side arc (0=monster, 1=player). 0 = use pathStartLineFactor legacy.")]
    [Range(0f, 1f)]
    public float pathApexAlongLine = 0f;
    [Tooltip("Along-line height reference for arc peak (0=monster, 1=player). 0 = midpoint (0.5).")]
    [Range(0f, 1f)]
    public float pathPeakHeightAlongLine = 0f;
    // Lateral bulge toward caster's left (original side arc).
    public float pathSideOffset = 1.25f;
    // Extra height above chord midpoint (climb then descend toward caster).
    public float pathHeightOffset = 0.44f;
    [Tooltip("Extra peak height per meter of horizontal travel monster→caster.")]
    public float pathDistanceHeightFactor = 0.112f;
    [Tooltip("Share of peak height applied at peel (early climb while spreading sideways).")]
    [Range(0f, 1f)]
    public float pathEarlyClimbFactor = 0.2f;
    [Tooltip("Speed multiplier before the orb reaches the arc apex.")]
    public float pathAscentSpeedScale = 1f;
    [Tooltip("Speed multiplier after the orb reaches the arc apex.")]
    public float pathDescentSpeedScale = 1f;
    public bool rotateToVelocity = true;
    public bool destroyOnArrive = true;
    [Tooltip("Spawn mirrored duplicate of each home flight anchor ParticleGroup (original m_u003_b x2).")]
    public bool mirrorDualFlight = false;
}

[Serializable]
public class CompositePrefabPart
{
    public string name;
    public BaseEffect prefab;
    public EffectSettings settingsOverride;
    public EffectAttachmentPoint attachmentPoint = EffectAttachmentPoint.CasterRoot;
    public CompositePartSpawnTiming spawnTiming = CompositePartSpawnTiming.Immediate;
    // Spawns hit-timed part earlier than castData.HitTime (seconds).
    public float hitLeadSeconds = 0f;
    // Local offset from resolved attachment point (in attachment transform space if available).
    public Vector3 positionOffset = Vector3.zero;
    // Scales positionOffset by model height to keep visual placement consistent across races.
    public bool normalizeOffsetByOwnerHeight = false;
    public float referenceHeight = 1.8f;
    public float scale = 1f;
    public bool followResolvedTransform = true;
    public bool inheritRotation = true;
    public bool passCastDataToPart = true;
    [Header("Shader Target Position")]
    public bool passShaderTargetPosition = false;
    public EffectAttachmentPoint shaderTargetAttachmentPoint = EffectAttachmentPoint.CasterCenter;
    [Tooltip("Local offset from resolved shader target attachment point (in attachment transform space if available).")]
    public Vector3 shaderTargetPositionOffset = Vector3.zero;
    // If false, part keeps its own prefab/settings lifetime and is not stretched to cast HitTime.
    public bool useCastTimedLifetime = true;
    public bool overrideContinuousLoop = false;
    public bool continuousLoop = false;
    public bool disableShaderLifetime = false;
    public bool overrideHideTime = false;
    public float customHideTime = 1f;
    public bool enableFinalShaderLifetimeOnFade = false;
    public float finalShaderLifetimeMin = 0.15f;
    public float finalShaderLifetimeMax = 0.5f;
    public CompositeProjectileConfig projectile = new CompositeProjectileConfig();
    public CompositeHomeProjectileConfig homeProjectile = new CompositeHomeProjectileConfig();
}

public class CompositePrefabEffect : TimedCompositeEffectBase
{
    private const float HIT_DIRECTION_YAW_OFFSET_DEGREES = 90f;
    [SerializeField] private CompositePrefabPart[] _parts;
    [SerializeField] private float _serverHitLifetimeTailSeconds = 0f;

    [Tooltip("Если включено: не вызывать DestoryEffect по lifetime корня — дочерние префабы (например wh_heal_ca) не уничтожаются вместе с композитом. Только для отладки шейдеров/Hold.")]
    [SerializeField] private bool _skipDestroyCompositeByLifetime;

    private readonly IEffectAttachmentResolver _resolver = new DefaultEffectAttachmentResolver();
    private EffectResolveContext _context;
    private readonly List<PendingCompositePart> _pendingParts = new List<PendingCompositePart>();
    private readonly List<CompositePrefabPart> _pendingHitColliderParts = new List<CompositePrefabPart>();
    private readonly List<CompositePrefabPart> _pendingAnimationShootParts = new List<CompositePrefabPart>();
    private readonly Dictionary<CompositePrefabPart, BaseEffect> _spawnedPartInstances = new Dictionary<CompositePrefabPart, BaseEffect>();
    private readonly HashSet<CompositePrefabPart> _launchedProjectileParts = new HashSet<CompositePrefabPart>();
    private readonly HashSet<CompositePrefabPart> _launchedHomeProjectileParts = new HashSet<CompositePrefabPart>();
    private readonly List<AnimationEventsBase> _shootEventSources = new List<AnimationEventsBase>();
    private Coroutine _pendingSpawnRoutine;
    private Coroutine _fallbackShootRoutine;
    private Coroutine _visibilityProbeRoutine;
    private AnimationEventsBase _animationEvents;
    private float _playStartedAt;
    private bool _isSubscribedToAnyShoot;
    private bool _isSubscribedToProjectileEffectHit;
    protected override string DebugPrefix => "[CompositePrefabEffect]";
    protected override float RuntimeLifeTimeTailSeconds => _serverHitLifetimeTailSeconds;

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        InitializeTimedComposite(settings, castData);
        _context = CompositeEffectUtilities.BuildContext(owner, castData);
        _spawnedPartInstances.Clear();
        _launchedProjectileParts.Clear();
        _launchedHomeProjectileParts.Clear();
        _pendingHitColliderParts.Clear();
        _pendingAnimationShootParts.Clear();
        SubscribeShootEventIfNeeded();
        SubscribeProjectileHitEventIfNeeded();
    }

    public override void Play()
    {

        if (_parts == null || _parts.Length == 0)
        {
            Debug.LogWarning("CompositePrefabEffect: no parts configured.");
            return;
        }
    
        _playStartedAt = Time.time;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Play started at={_playStartedAt:F3}s hit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"flight={(_castData != null ? _castData.FlightTime : -1f):F3}s serverShoot={(_castData != null ? _castData.serverTimeToShoot : -1f):F3}s.");
#endif

        QueueImmediateAndDelayedParts();
        StartPendingPartsRoutineIfNeeded();
        StartShootFallbackRoutineIfNeeded();
        if (!_skipDestroyCompositeByLifetime)
        {
            DestroyCompositeByLifetime();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else
        {
            Debug.Log($"{DebugPrefix} SkipDestroyCompositeByLifetime=true — корень композита не будет уничтожен по lifetime (дочерние объекты остаются в сцене).");
        }
#endif
    }

    public override void SetProgress(float normalizedTime)
    {
        // Composite root delegates playback to spawned child effects.
    }

    private void RefreshResolveContext()
    {
        _context = CompositeEffectUtilities.BuildContext(_owner, _castData);
    }

    private void SpawnPart(CompositePrefabPart part)
    {
        if (!IsPartSpawnable(part))
        {
            return;
        }

        RefreshResolveContext();

        if (!TryResolveAttachment(part, out Transform resolvedTransform, out Vector3 worldPosition))
        {
            return;
        }

        BaseEffect instance = SpawnPartInstance(part, resolvedTransform, worldPosition);
        if (instance == null)
        {
            return;
        }

        if (!TryResolvePartSettings(part, out EffectSettings partSettings))
        {
            return;
        }

        if (part.overrideHideTime && partSettings != null)
        {
            float hide = part.customHideTime > 1e-4f
                ? part.customHideTime
                : (part.disableShaderLifetime ? 0.5f : 0f);
            partSettings.hideTime = Mathf.Max(0f, Mathf.Min(hide, partSettings.defaultLifeTime));
        }

        Transform setupOwner = ResolveSetupOwner(resolvedTransform, instance.transform);
        MagicCastData partCastData = part.passCastDataToPart ? _castData : null;
        instance.Setup(partSettings, partCastData, setupOwner);
        ApplyPartShaderTargetPosition(part, instance.transform);
        ApplyPartLoopOverrides(part, instance.transform);
        ApplyPartHomeFlightOverrides(part, instance.transform);
        if (CompositeHomeProjectileLaunchHelper.IsEnabled(part))
        {
            instance.PrepareDestroyOnHomeArrival();
        }

        instance.Play();
        CompositeProjectileLaunchHelper.ApplyPreShootVisibility(part, instance, DebugPrefix);
        _spawnedPartInstances[part] = instance;
        StartFinalShaderLifetimeRoutineIfNeeded(part, instance.transform, partSettings);
        TryLaunchPartAsProjectileImmediately(part, instance);
        TryLaunchPartAsHomeProjectileImmediately(part, instance);

        LogSpawnedPart(part, partSettings, setupOwner);
    }

    private void SubscribeShootEventIfNeeded()
    {
        UnsubscribeShootEvent();
        if (!CompositeProjectileLaunchHelper.RequiresAnimationShootEvent(_parts))
        {
            return;
        }

        if (_context?.CasterEntity?.IdentityInterlude == null || AnimationManager.Instance == null)
        {
            return;
        }

        int casterId = _context.CasterEntity.IdentityInterlude.Id;
        _animationEvents = AnimationManager.Instance.GetAnimationEvents(casterId);
        TrySubscribeShootSource(_animationEvents, "AnimationManager");

        if (!_isSubscribedToAnyShoot)
        {
            _isSubscribedToAnyShoot = true;
        }
    }

    private void TrySubscribeShootSource(AnimationEventsBase source, string sourceName)
    {

        if (_shootEventSources.Contains(source))
        {
            return;
        }

        if (source == null)
        {
            return;
        }

        source.OnAnimationStartShoot += HandleAnimationShoot;
        _shootEventSources.Add(source);
    }

    private void HandleAnimationShoot(string _)
    {
        ProcessShootEvent("direct");
    }


    private void ProcessShootEvent(string channel)
    {
        SpawnPendingAnimationShootParts();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_castData != null)
        {
            float globalSinceCast = Time.time - _castData.StartTime;
            Debug.Log(
                "[MAGIC_PROJECTILE_SYNC] ShootEvent " +
                $"channel={channel} globalSinceCast={globalSinceCast:F3}s hit={_castData.HitTime:F3}s " +
                $"flight={_castData.FlightTime:F3}s serverShoot={_castData.serverTimeToShoot:F3}s " +
                $"deltaGlobalToShoot={globalSinceCast - _castData.serverTimeToShoot:F3}s " +
                $"deltaGlobalToHit={globalSinceCast - _castData.HitTime:F3}s sincePlayStarted={Time.time - _playStartedAt:F3}s");
        }
        else
        {
            Debug.Log($"[MAGIC_PROJECTILE_SYNC] ShootEvent channel={channel} castData=null sincePlayStarted={Time.time - _playStartedAt:F3}s");
        }
#endif
        CompositeProjectileLaunchHelper.RevealOnShootParts(
            _parts,
            _spawnedPartInstances);

        CompositeProjectileLaunchHelper.ProcessShootLaunches(
            _parts,
            _spawnedPartInstances,
            _launchedProjectileParts,
            _context.TargetTransform,
            _playStartedAt,
            DebugPrefix);

        CompositeHomeProjectileLaunchHelper.ProcessShootLaunches(
            _parts,
            _spawnedPartInstances,
            _launchedHomeProjectileParts,
            _context,
            _playStartedAt,
            DebugPrefix);
    }

    private void SpawnPendingAnimationShootParts()
    {
        if (_pendingAnimationShootParts.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _pendingAnimationShootParts.Count; i++)
        {
            CompositePrefabPart part = _pendingAnimationShootParts[i];
            if (part == null || _spawnedPartInstances.ContainsKey(part))
            {
                continue;
            }

            SpawnPart(part);
        }

        _pendingAnimationShootParts.Clear();
    }

    private void HandleProjectileEffectHit(GameObject projectilePrefab, Transform target, Vector3 hitPoint, Vector3 hitDirection, int attackerEntityId)
    {
        if (_pendingHitColliderParts.Count == 0 || projectilePrefab == null)
        {
            return;
        }

        if (HitManager.Instance == null)
        {
            return;
        }

        if (!HitManager.Instance.TryPrepareProjectileEffectHit(
                projectilePrefab,
                hitPoint,
                hitDirection,
                attackerEntityId,
                IsFromLaunchedCompositeProjectile,
                out Vector3 resolvedHitPoint,
                out Vector3 resolvedHitDirection))
        {
            return;
        }

        bool hadHitPoint = _context != null && _context.HasHitPoint;
        Vector3 previousHitPoint = hadHitPoint ? _context.HitPoint : Vector3.zero;
        bool hadHitDirection = _context != null && _context.HasHitDirection;
        Vector3 previousHitDirection = hadHitDirection ? _context.HitDirection : Vector3.forward;

        if (_context != null)
        {
            _context.HasHitPoint = true;
            _context.HitPoint = resolvedHitPoint;
            _context.HasHitDirection = true;
            _context.HitDirection = resolvedHitDirection;
        }

        for (int i = 0; i < _pendingHitColliderParts.Count; i++)
        {
            SpawnPart(_pendingHitColliderParts[i]);
        }

        if (_context != null)
        {
            _context.HasHitPoint = hadHitPoint;
            _context.HitPoint = previousHitPoint;
            _context.HasHitDirection = hadHitDirection;
            _context.HitDirection = previousHitDirection;
        }

        _pendingHitColliderParts.Clear();
    }

    private void TryLaunchPartAsProjectileImmediately(CompositePrefabPart part, BaseEffect spawned)
    {
        if (part == null || spawned == null || !CompositeProjectileLaunchHelper.ShouldLaunchImmediately(part))
        {
            return;
        }

        if (_context?.TargetTransform == null || ProjectileManager.Instance == null || _launchedProjectileParts.Contains(part))
        {
            return;
        }

        if (CompositeProjectileLaunchHelper.TryLaunch(part, spawned, _context.TargetTransform, _playStartedAt, DebugPrefix))
        {
            _launchedProjectileParts.Add(part);
        }
    }

    private void TryLaunchPartAsHomeProjectileImmediately(CompositePrefabPart part, BaseEffect spawned)
    {
        if (part == null || spawned == null || !CompositeHomeProjectileLaunchHelper.ShouldLaunchImmediately(part))
        {
            return;
        }

        if (_context?.CasterTransform == null || _launchedHomeProjectileParts.Contains(part))
        {
            return;
        }

        if (CompositeHomeProjectileLaunchHelper.TryLaunch(part, spawned, _context, _playStartedAt, DebugPrefix))
        {
            _launchedHomeProjectileParts.Add(part);
        }
    }

    private void StartShootFallbackRoutineIfNeeded()
    {
        if (_castData == null || !CompositeProjectileLaunchHelper.RequiresAnimationShootEvent(_parts))
        {
            return;
        }

        // Same gate as before: at serverTimeToShoot==0 rely on the animation shoot event only (avoid one-frame fallback racing the animator).
        if (_castData.serverTimeToShoot <= 0f)
        {
            return;
        }

        if (_fallbackShootRoutine != null)
        {
            StopCoroutine(_fallbackShootRoutine);
        }

        _fallbackShootRoutine = StartCoroutine(ShootFallbackRoutine());
    }

    private IEnumerator ShootFallbackRoutine()
    {
        float shootAt = _castData.StartTime + _castData.serverTimeToShoot;
        float remaining = shootAt - Time.time;

        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{DebugPrefix} Shoot fallback catch-up: event time already passed by {-remaining:F3}s.");
#endif
        }

        ProcessShootEvent("fallback");

        _fallbackShootRoutine = null;
    }

    private bool IsPartSpawnable(CompositePrefabPart part)
    {
        return part != null && part.prefab != null;
    }

    private bool TryResolveAttachment(CompositePrefabPart part, out Transform resolvedTransform, out Vector3 worldPosition)
    {
        if (_resolver.Resolve(part.attachmentPoint, _context, out resolvedTransform, out worldPosition))
        {
            return true;
        }

        Debug.LogWarning($"CompositePrefabEffect: could not resolve point {part.attachmentPoint} for part {part.name}.");
        return false;
    }

    private BaseEffect SpawnPartInstance(CompositePrefabPart part, Transform resolvedTransform, Vector3 worldPosition)
    {
        Vector3 adjustedOffset = GetAdjustedOffset(part, resolvedTransform);
        Vector3 spawnPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            resolvedTransform,
            worldPosition,
            adjustedOffset);
        Quaternion rotation = ResolvePartSpawnRotation(part, resolvedTransform);

        // Spawn without parent first, then attach with worldPositionStays=true.
        // This prevents inheriting oversized bone scale when following transforms.
        BaseEffect instance = Instantiate(part.prefab, spawnPosition, rotation);
        instance.gameObject.SetActive(true);

        AttachToResolvedTransformIfNeeded(part, resolvedTransform, instance.transform, adjustedOffset, worldPosition);
        ApplyPartScale(part, instance.transform);
        ApplyShaderLifetimeOverride(part, instance.transform);

        return instance;
    }

    private Quaternion ResolvePartSpawnRotation(CompositePrefabPart part, Transform resolvedTransform)
    {
        if (part != null &&
            part.attachmentPoint == EffectAttachmentPoint.WorldHitPoint &&
            _context != null &&
            _context.HasHitDirection &&
            _context.HitDirection.sqrMagnitude > 0.0001f)
        {
            // Impact VFX meshes are authored with local forward offset from Unity Z forward.
            // Apply yaw compensation so hit flashes face the incoming hit direction visually.
            return Quaternion.LookRotation(_context.HitDirection.normalized) *
                   Quaternion.Euler(0f, HIT_DIRECTION_YAW_OFFSET_DEGREES, 0f);
        }

        return CompositeEffectUtilities.ResolveSpawnRotation(part != null && part.inheritRotation, resolvedTransform);
    }

    private void StartFinalShaderLifetimeRoutineIfNeeded(CompositePrefabPart part, Transform instanceTransform, EffectSettings partSettings)
    {
        if (part == null || instanceTransform == null || !part.disableShaderLifetime || !part.enableFinalShaderLifetimeOnFade)
        {
            return;
        }

        if (partSettings == null)
        {
            return;
        }

        // Start final shader lifetime BEFORE BeginFadeOut/StopPart moment, otherwise
        // fade window is never visible because parts are disabled in the same frame.
        float finalWindow = Mathf.Max(part.finalShaderLifetimeMax, part.finalShaderLifetimeMin, 0f);
        float delay = Mathf.Max(0f, partSettings.defaultLifeTime - partSettings.hideTime - finalWindow);
        StartCoroutine(EnableFinalShaderLifetimeAfterDelay(instanceTransform, delay, part.finalShaderLifetimeMin, part.finalShaderLifetimeMax));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!string.IsNullOrEmpty(instanceTransform.name) &&
            instanceTransform.name.IndexOf("wh_heal", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.Log(
                $"{DebugPrefix} Final shader lifetime scheduled for '{instanceTransform.name}' " +
                $"startIn={delay:F3}s window={finalWindow:F3}s life={partSettings.defaultLifeTime:F3}s hide={partSettings.hideTime:F3}s " +
                $"range=({part.finalShaderLifetimeMin:F3},{part.finalShaderLifetimeMax:F3}).");
        }
#endif
    }

    private IEnumerator EnableFinalShaderLifetimeAfterDelay(Transform instanceTransform, float delay, float rangeMin, float rangeMax)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (instanceTransform == null)
        {
            yield break;
        }

        EffectShaderLifetimeHelper.Apply(instanceTransform, true, rangeMin, rangeMax);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Final shader lifetime enabled for '{instanceTransform.name}' " +
            $"afterDelay={delay:F3}s range=({rangeMin:F3},{rangeMax:F3}).");
#endif
    }

    private void ApplyShaderLifetimeOverride(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || !part.disableShaderLifetime || instanceTransform == null)
        {
            return;
        }

        EffectShaderLifetimeHelper.Apply(instanceTransform, false, 0.5f, 0.5f);
    }

    private Transform ResolveSetupOwner(Transform resolvedTransform, Transform instanceTransform)
    {
        return resolvedTransform != null ? resolvedTransform : (_owner != null ? _owner : instanceTransform);
    }

    private void ApplyPartShaderTargetPosition(CompositePrefabPart part, Transform instanceTransform)
    {
        if (part == null || instanceTransform == null || !part.passShaderTargetPosition)
        {
            return;
        }

        if (!_resolver.Resolve(part.shaderTargetAttachmentPoint, _context, out Transform targetTransform, out Vector3 targetWorldPosition))
        {
            return;
        }

        Vector3 adjustedTargetWorldPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            targetTransform,
            targetWorldPosition,
            part.shaderTargetPositionOffset);

        EffectPart[] effectParts = instanceTransform.GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < effectParts.Length; i++)
        {
            if (effectParts[i] != null)
            {
                effectParts[i].SetShaderTargetWorldPosOverride(true, adjustedTargetWorldPosition, targetTransform);
            }
        }
    }

    private bool TryResolvePartSettings(CompositePrefabPart part, out EffectSettings partSettings)
    {
        EffectSettings sourceSettings = part.settingsOverride != null ? part.settingsOverride : _settings;
        partSettings = CreateRuntimeSettings(sourceSettings, part.useCastTimedLifetime);
        if (partSettings != null)
        {
            return true;
        }

        Debug.LogWarning($"CompositePrefabEffect: settings are null for part {part.name}.");
        return false;
    }

    private void LogSpawnedPart(CompositePrefabPart part, EffectSettings partSettings, Transform setupOwner)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Spawned part='{part.name}' point={part.attachmentPoint} " +
            $"follow={part.followResolvedTransform} hitTime={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"lifeTime={partSettings.defaultLifeTime:F3}s scale={part.scale:F2} offset={part.positionOffset} owner='{(setupOwner != null ? setupOwner.name : "null")}'.");
#endif
    }

    private Vector3 GetAdjustedOffset(CompositePrefabPart part, Transform resolvedTransform)
    {
        if (part == null || !part.normalizeOffsetByOwnerHeight)
        {
            return part != null ? part.positionOffset : Vector3.zero;
        }

        float height = ResolveAttachmentHeight(resolvedTransform);
        float reference = Mathf.Max(0.01f, part.referenceHeight);
        float multiplier = Mathf.Max(0.01f, height / reference);
        return part.positionOffset * multiplier;
    }

    private void QueueImmediateAndDelayedParts()
    {
        _pendingParts.Clear();
        _pendingHitColliderParts.Clear();
        _pendingAnimationShootParts.Clear();

        if (_parts == null)
        {
            return;
        }

        for (int i = 0; i < _parts.Length; i++)
        {
            CompositePrefabPart part = _parts[i];
            if (part == null || part.prefab == null)
            {
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                _pendingHitColliderParts.Add(part);
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
            {
                _pendingAnimationShootParts.Add(part);
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(part.spawnTiming, _castData, part.hitLeadSeconds);
            if (delay <= 0f)
            {
                SpawnPart(part);
                continue;
            }

            _pendingParts.Add(new PendingCompositePart
            {
                Part = part,
                SpawnAtTime = Time.time + delay
            });
        }
    }

    private void StartPendingPartsRoutineIfNeeded()
    {
        if (_pendingParts.Count > 0)
        {
            _pendingSpawnRoutine = StartCoroutine(SpawnPendingPartsRoutine());
        }
    }

    private void DestroyCompositeByLifetime()
    {
        EffectSettings lifeTimeSettings = SelectLifetimeSettings();
        if (lifeTimeSettings != null)
        {
            DestoryEffect(lifeTimeSettings, _castData);
        }
    }


   

    private IEnumerator SpawnPendingPartsRoutine()
    {
        while (_pendingParts.Count > 0)
        {
            float now = Time.time;
            for (int i = _pendingParts.Count - 1; i >= 0; i--)
            {
                PendingCompositePart pending = _pendingParts[i];
                if (pending == null || pending.Part == null)
                {
                    _pendingParts.RemoveAt(i);
                    continue;
                }

                if (now >= pending.SpawnAtTime)
                {
                    SpawnPart(pending.Part);
                    _pendingParts.RemoveAt(i);
                }
            }

            yield return null;
        }

        _pendingSpawnRoutine = null;
    }

    protected override void OnTimedCompositeDestroy()
    {
        UnsubscribeShootEvent();
        UnsubscribeProjectileHitEvent();
        StopAndClearCoroutine(ref _pendingSpawnRoutine);
        StopAndClearCoroutine(ref _fallbackShootRoutine);
        StopAndClearCoroutine(ref _visibilityProbeRoutine);

        _pendingParts.Clear();
        _pendingHitColliderParts.Clear();
        _pendingAnimationShootParts.Clear();
        _spawnedPartInstances.Clear();
        _launchedProjectileParts.Clear();
        _launchedHomeProjectileParts.Clear();
    }

    private void UnsubscribeShootEvent()
    {
        UnsubscribeShootEventSources(
            _shootEventSources,
            HandleAnimationShoot,
            ref _animationEvents,
            ref _isSubscribedToAnyShoot);
    }

    private void SubscribeProjectileHitEventIfNeeded()
    {
        if (!RequiresHitColliderSpawn())
        {
            return;
        }

        if (ProjectileManager.Instance != null && !_isSubscribedToProjectileEffectHit)
        {
            ProjectileManager.Instance.OnHitEffectProjectile += HandleProjectileEffectHit;
            _isSubscribedToProjectileEffectHit = true;
        }
    }

    private void UnsubscribeProjectileHitEvent()
    {
        if (ProjectileManager.Instance != null && _isSubscribedToProjectileEffectHit)
        {
            ProjectileManager.Instance.OnHitEffectProjectile -= HandleProjectileEffectHit;
            _isSubscribedToProjectileEffectHit = false;
        }
    }

    private bool RequiresHitColliderSpawn()
    {
        if (_parts == null)
        {
            return false;
        }

        for (int i = 0; i < _parts.Length; i++)
        {
            if (_parts[i] != null && _parts[i].spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsFromLaunchedCompositeProjectile(Transform attacker)
    {
        foreach (KeyValuePair<CompositePrefabPart, BaseEffect> pair in _spawnedPartInstances)
        {
            if (!CompositeProjectileLaunchHelper.IsProjectilePart(pair.Key))
            {
                continue;
            }

            BaseEffect spawned = pair.Value;
            if (spawned == null)
            {
                continue;
            }

            Transform spawnedTransform = spawned.transform;
            if (attacker == spawnedTransform || attacker.IsChildOf(spawnedTransform))
            {
                return true;
            }
        }

        return false;
    }
}
