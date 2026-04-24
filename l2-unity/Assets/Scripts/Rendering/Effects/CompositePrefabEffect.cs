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
    public ProjectileImpactType impactType = ProjectileImpactType.EffectOnly;
    public ProjectileData settingsOverride;
}

[Serializable]
public class CompositePrefabPart
{
    public string name;
    public BaseEffect prefab;
    public EffectSettings settingsOverride;
    public EffectAttachmentPoint attachmentPoint = EffectAttachmentPoint.CasterRoot;
    public CompositePartSpawnTiming spawnTiming = CompositePartSpawnTiming.Immediate;
    // Local offset from resolved attachment point (in attachment transform space if available).
    public Vector3 positionOffset = Vector3.zero;
    // Scales positionOffset by model height to keep visual placement consistent across races.
    public bool normalizeOffsetByOwnerHeight = false;
    public float referenceHeight = 1.8f;
    public float scale = 1f;
    public bool followResolvedTransform = true;
    public bool inheritRotation = true;
    public bool passCastDataToPart = true;
    public bool overrideContinuousLoop = false;
    public bool continuousLoop = false;
    public bool disableShaderLifetime = false;
    public bool overrideHideTime = false;
    public float customHideTime = 1f;
    public bool enableFinalShaderLifetimeOnFade = false;
    public float finalShaderLifetimeMin = 0.15f;
    public float finalShaderLifetimeMax = 0.5f;
    public CompositeProjectileConfig projectile = new CompositeProjectileConfig();
}

public class CompositePrefabEffect : TimedCompositeEffectBase
{
    [SerializeField] private CompositePrefabPart[] _parts;
    [SerializeField] private float _serverHitLifetimeTailSeconds = 0f;

    private readonly IEffectAttachmentResolver _resolver = new DefaultEffectAttachmentResolver();
    private EffectResolveContext _context;
    private readonly List<PendingCompositePart> _pendingParts = new List<PendingCompositePart>();
    private readonly Dictionary<CompositePrefabPart, BaseEffect> _spawnedPartInstances = new Dictionary<CompositePrefabPart, BaseEffect>();
    private readonly HashSet<CompositePrefabPart> _launchedProjectileParts = new HashSet<CompositePrefabPart>();
    private readonly List<AnimationEventsBase> _shootEventSources = new List<AnimationEventsBase>();
    private Coroutine _pendingSpawnRoutine;
    private Coroutine _fallbackShootRoutine;
    private Coroutine _visibilityProbeRoutine;
    private AnimationEventsBase _animationEvents;
    private float _playStartedAt;
    private bool _isSubscribedToAnyShoot;
    protected override string DebugPrefix => "[CompositePrefabEffect]";
    protected override float RuntimeLifeTimeTailSeconds => _serverHitLifetimeTailSeconds;

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        InitializeTimedComposite(settings, castData);
        _context = CompositeEffectUtilities.BuildContext(owner, castData);
        _spawnedPartInstances.Clear();
        _launchedProjectileParts.Clear();
        SubscribeShootEventIfNeeded();
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
        DestroyCompositeByLifetime();
    }

    public override void SetProgress(float normalizedTime)
    {
        // Composite root delegates playback to spawned child effects.
    }

    private void SpawnPart(CompositePrefabPart part)
    {
        if (!IsPartSpawnable(part))
        {
            return;
        }

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
            partSettings.hideTime = Mathf.Max(0f, Mathf.Min(part.customHideTime, partSettings.defaultLifeTime));
        }

        Transform setupOwner = ResolveSetupOwner(resolvedTransform, instance.transform);
        MagicCastData partCastData = part.passCastDataToPart ? _castData : null;
        instance.Setup(partSettings, partCastData, setupOwner);
        ApplyPartLoopOverrides(part, instance.transform);
        instance.Play();
        _spawnedPartInstances[part] = instance;
        StartFinalShaderLifetimeRoutineIfNeeded(part, instance.transform, partSettings);
        TryLaunchPartAsProjectileImmediately(part, instance);

        LogSpawnedPart(part, partSettings, setupOwner);
    }

    private void SubscribeShootEventIfNeeded()
    {
        UnsubscribeShootEvent();
        if (!RequiresAnimationShootLaunch() || _context?.CasterEntity?.IdentityInterlude == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                $"{DebugPrefix} Shoot subscription skipped. requiresLaunch={RequiresAnimationShootLaunch()} " +
                $"casterIdMissing={_context?.CasterEntity?.IdentityInterlude == null}.");
#endif
            return;
        }

        int casterId = _context.CasterEntity.IdentityInterlude.Id;
        _animationEvents = AnimationManager.Instance.GetAnimationEvents(casterId);
        TrySubscribeShootSource(_animationEvents, "AnimationManager");

        AnimationEventsBase hierarchySource = _context.CasterEntity.GetComponentInChildren<AnimationEventsBase>(true);
        TrySubscribeShootSource(hierarchySource, "CasterHierarchy");

        if (!_isSubscribedToAnyShoot)
        {
            AnimationEventsBase.OnAnyAnimationShoot += HandleAnyAnimationShoot;
            _isSubscribedToAnyShoot = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{DebugPrefix} Subscribed to global OnAnyAnimationShoot fallback.");
#endif
        }
    }

    private void TrySubscribeShootSource(AnimationEventsBase source, string sourceName)
    {
        if (source == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"{DebugPrefix} Shoot source '{sourceName}' is null.");
#endif
            return;
        }

        if (_shootEventSources.Contains(source))
        {
            return;
        }

        source.OnAnimationStartShoot += HandleAnimationShoot;
        _shootEventSources.Add(source);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{DebugPrefix} Subscribed to '{sourceName}' shoot source name='{source.name}' instanceId={source.GetInstanceID()} casterId={_context?.CasterEntity?.IdentityInterlude?.Id ?? 0}.");
#endif
    }

    private bool RequiresAnimationShootLaunch()
    {
        return CompositeProjectileLaunchHelper.RequiresAnimationShootLaunch(_parts);
    }

    private void HandleAnimationShoot(string _)
    {
        ProcessShootEvent("direct");
    }

    private void HandleAnyAnimationShoot(int objectId, AnimationEventsBase source, string animationName)
    {
        if (source == null || _context?.CasterEntity?.IdentityInterlude == null)
        {
            return;
        }
        int casterId = _context.CasterEntity.IdentityInterlude.Id;
        if (objectId != casterId)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{DebugPrefix} Global shoot fallback matched objectId={objectId} animation='{animationName}' instanceId={source.GetInstanceID()}.");
#endif
        ProcessShootEvent("global");
    }

    private void ProcessShootEvent(string channel)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Time.time;
        float elapsed = _playStartedAt > 0f ? now - _playStartedAt : -1f;
        Debug.Log(
            $"{DebugPrefix} OnAnimationShoot[{channel}] at={now:F3}s elapsed={elapsed:F3}s " +
            $"hit={(_castData != null ? _castData.HitTime : -1f):F3}s flight={(_castData != null ? _castData.FlightTime : -1f):F3}s " +
            $"serverShoot={(_castData != null ? _castData.serverTimeToShoot : -1f):F3}s target='{(_context?.TargetTransform != null ? _context.TargetTransform.name : "null")}'.");
#endif

        if (_parts == null || _context?.TargetTransform == null || ProjectileManager.Instance == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                $"{DebugPrefix} OnAnimationShoot skipped. partsNull={_parts == null} " +
                $"targetNull={_context?.TargetTransform == null} projectileManagerNull={ProjectileManager.Instance == null}.");
#endif
            return;
        }

        CompositeProjectileLaunchHelper.ProcessShootLaunches(
            _parts,
            _spawnedPartInstances,
            _launchedProjectileParts,
            _context.TargetTransform,
            _playStartedAt,
            DebugPrefix);
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

    private void StartShootFallbackRoutineIfNeeded()
    {
        if (_castData == null || _castData.serverTimeToShoot <= 0f || !RequiresAnimationShootLaunch())
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

        if (_launchedProjectileParts.Count == 0)
        {
            ProcessShootEvent("fallback");
        }

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
        Quaternion rotation = CompositeEffectUtilities.ResolveSpawnRotation(part.inheritRotation, resolvedTransform);

        // Spawn without parent first, then attach with worldPositionStays=true.
        // This prevents inheriting oversized bone scale when following transforms.
        BaseEffect instance = Instantiate(part.prefab, spawnPosition, rotation);
        instance.gameObject.SetActive(true);

        AttachToResolvedTransformIfNeeded(part, resolvedTransform, instance.transform, adjustedOffset);
        ApplyPartScale(part, instance.transform);
        ApplyShaderLifetimeOverride(part, instance.transform);

        return instance;
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

        float delay = Mathf.Max(0f, partSettings.defaultLifeTime - partSettings.hideTime);
        StartCoroutine(EnableFinalShaderLifetimeAfterDelay(instanceTransform, delay, part.finalShaderLifetimeMin, part.finalShaderLifetimeMax));
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

    private bool TryResolvePartSettings(CompositePrefabPart part, out EffectSettings partSettings)
    {
        EffectSettings sourceSettings = part.settingsOverride != null ? part.settingsOverride : _settings;
        partSettings = CreateRuntimeSettings(sourceSettings);
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
        QueueImmediateAndDelayedParts(_parts, _pendingParts, _castData, SpawnPart);
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
        StopAndClearCoroutine(ref _pendingSpawnRoutine);
        StopAndClearCoroutine(ref _fallbackShootRoutine);
        StopAndClearCoroutine(ref _visibilityProbeRoutine);

        _pendingParts.Clear();
        _spawnedPartInstances.Clear();
        _launchedProjectileParts.Clear();
    }

    private void UnsubscribeShootEvent()
    {
        UnsubscribeShootEventSources(
            _shootEventSources,
            HandleAnimationShoot,
            ref _animationEvents,
            ref _isSubscribedToAnyShoot,
            HandleAnyAnimationShoot);
    }
}
