using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V2 composite: thin SerializeReference parts + placement. Legacy ParticleGroup
/// children are hijacked onto ParticleStreamDriver. New ParticleGroupV2 plays directly.
/// </summary>
public class CompositeEffectV2 : CompositePrefabEffect
{
    // L2 emitter is X-forward (bDirectional). Unity LookRotation is Z-forward.
    // Keep this on the whole directional actor: Mesh PTRS_Actor and sprite
    // PTDU_Normal ProjectionNormal=(1,0,0) both read owner +X as hit.
    const float HitDirectionYawOffsetDegrees = -90f;

    [SerializeField]
    [SerializeReference]
    CompositePart[] _v2Parts;

    readonly IEffectAttachmentResolver _v2Resolver = new DefaultEffectAttachmentResolver();
    readonly List<PendingV2Part> _pendingDelayed = new List<PendingV2Part>();
    readonly List<CompositePart> _pendingHitCollider = new List<CompositePart>();
    readonly List<CompositePart> _pendingAnimationShoot = new List<CompositePart>();
    readonly Dictionary<CompositePart, BaseEffect> _spawned = new Dictionary<CompositePart, BaseEffect>();
    readonly HashSet<CompositePart> _launchedProjectiles = new HashSet<CompositePart>();
    readonly List<AnimationEventsBase> _v2ShootSources = new List<AnimationEventsBase>();

    AnimationEventsBase _v2AnimationEvents;
    Coroutine _v2PendingRoutine;
    Coroutine _v2FallbackShootRoutine;
    bool _v2SubscribedShoot;
    bool _v2SubscribedHit;

    protected override string DebugPrefix => "[CompositeEffectV2]";

    protected override bool UseLegacyLifetimeHacks => false;

    protected override bool ShouldUseLegacyPartPipeline => !HasSpawnableV2Parts;

    bool HasSpawnableV2Parts
    {
        get
        {
            if (_v2Parts == null)
            {
                return false;
            }

            for (int i = 0; i < _v2Parts.Length; i++)
            {
                if (_v2Parts[i] != null && _v2Parts[i].IsSpawnable)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        ClearV2Runtime();
        if (!ShouldUseLegacyPartPipeline)
        {
            SubscribeV2ShootIfNeeded();
            SubscribeV2HitIfNeeded();
        }
    }

    protected override void PlayV2()
    {
        if (!HasSpawnableV2Parts)
        {
            Debug.LogWarning("CompositeEffectV2: no V2 parts configured.");
            return;
        }

        _playStartedAt = Time.time;
        RefreshV2Context();
        LogHomeSpawnPlay();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Play started at={_playStartedAt:F3}s hit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"flight={(_castData != null ? _castData.FlightTime : -1f):F3}s serverShoot={(_castData != null ? _castData.serverTimeToShoot : -1f):F3}s.");
#endif

        QueueV2Parts();
        StartV2PendingRoutineIfNeeded();
        StartV2ShootFallbackIfNeeded();
        if (!SkipDestroyCompositeByLifetime)
        {
            EffectSettings lifeTimeSettings = SelectLifetimeSettings();
            if (lifeTimeSettings != null)
            {
                DestoryEffect(lifeTimeSettings, _castData);
            }
        }
    }

    protected override void PreparePartPlayback(CompositePrefabPart part, BaseEffect instance)
    {
        PrepareV2Playback(instance);
    }

    protected override void OnTimedCompositeDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogV2TaDestroyState("before flush");
#endif
        SpawnPendingAnimationShootParts();
        FlushPendingDelayedIndependentParts();
        FlushPendingHitAtCastEnd();
        UnsubscribeV2Shoot();
        UnsubscribeV2Hit();
        StopAndClearCoroutine(ref _v2PendingRoutine);
        StopAndClearCoroutine(ref _v2FallbackShootRoutine);
        DestroyOwnedV2Parts();
        ClearV2Runtime();
        base.OnTimedCompositeDestroy();
    }

    void PrepareV2Playback(BaseEffect instance)
    {
        if (HasParticleGroupV2(instance) && !HasLegacyParticleAuthoring(instance))
        {
            return;
        }

        ParticleStreamHijack.Convert(instance);
    }

    void QueueV2Parts()
    {
        _pendingDelayed.Clear();
        _pendingHitCollider.Clear();
        _pendingAnimationShoot.Clear();

        for (int i = 0; i < _v2Parts.Length; i++)
        {
            CompositePart part = _v2Parts[i];
            if (part == null || !part.IsSpawnable)
            {
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                _pendingHitCollider.Add(part);
                continue;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
            {
                _pendingAnimationShoot.Add(part);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (V2TaSpawnLog.Matches(part))
                {
                    V2TaSpawnLog.Info(
                        "QUEUE shoot part='" + part.name +
                        "' hit=" + (_castData != null ? _castData.HitTime.ToString("0.###") : "-") +
                        "s shoot=" + (_castData != null ? _castData.serverTimeToShoot.ToString("0.###") : "-") +
                        "s wantsShoot=" + part.WantsAnimationShoot);
                }
#endif
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(part.spawnTiming, _castData);
            delay += Mathf.Max(0f, part.spawnDelaySeconds);
            if (delay <= 0f)
            {
                SpawnV2Part(part);
                continue;
            }

            float spawnAt = Time.time + delay;
            _pendingDelayed.Add(new PendingV2Part
            {
                Part = part,
                SpawnAtTime = spawnAt
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (V2TaSpawnLog.Matches(part))
            {
                float compositeLife = _rootRuntimeSettings != null
                    ? _rootRuntimeSettings.defaultLifeTime
                    : (_settings != null ? _settings.defaultLifeTime : -1f);
                float destroyAt = Time.time + Mathf.Max(0f, compositeLife);
                V2TaSpawnLog.Warn(
                    "QUEUE delayed part='" + part.name + "' type=" + part.GetType().Name +
                    " timing=" + part.spawnTiming +
                    " delay=" + delay.ToString("0.###") +
                    "s spawnAt=" + spawnAt.ToString("F3") +
                    " compositeLife=" + compositeLife.ToString("0.###") +
                    "s destroyAt=" + destroyAt.ToString("F3") +
                    " hit=" + (_castData != null ? _castData.HitTime.ToString("0.###") : "-") +
                    "s shoot=" + (_castData != null ? _castData.serverTimeToShoot.ToString("0.###") : "-") +
                    "s outlives=" + part.OutlivesComposite +
                    (destroyAt <= spawnAt + 0.02f
                        ? " RACE: composite dies at/before delayed spawn"
                        : string.Empty));
            }
#endif
        }
    }

    void SpawnV2Part(CompositePart part)
    {
        if (part == null || !part.IsSpawnable)
        {
            return;
        }

        if (_spawned.ContainsKey(part))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (V2TaSpawnLog.Matches(part))
            {
                V2TaSpawnLog.Warn(
                    "SPAWN SKIP already spawned part='" + part.name +
                    "' frame=" + Time.frameCount + " t=" + Time.time.ToString("F3"));
            }
#endif
            return;
        }

        RefreshV2Context();
        EnsureHitPointFromTarget();
        if (!part.TryResolveSpawn(_v2Resolver, _context, out Transform followTransform, out Vector3 worldPosition))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (V2TaSpawnLog.Matches(part))
            {
                V2TaSpawnLog.Warn(
                    "RESOLVE FAIL part='" + part.name +
                    "' attach=" + part.spawnAttachmentPoint +
                    " target=" + (_context != null && _context.TargetTransform != null
                        ? _context.TargetTransform.name
                        : "null"));
            }
#endif
            Debug.LogWarning(
                $"[HOME_SPAWN] resolve FAILED part='{part.name}' type={part.GetType().Name} " +
                $"placement={(part.placement != null ? part.placement.GetType().Name : "null")}");
            Debug.LogWarning($"{DebugPrefix} could not resolve placement for part {part.name}.");
            return;
        }

        Vector3 spawnPosition = CompositeEffectUtilities.ResolveSpawnPosition(
            followTransform,
            worldPosition,
            Vector3.zero);
        Quaternion rotation = ResolveV2SpawnRotation(part, followTransform);
        BaseEffect instance = Instantiate(part.prefab, spawnPosition, rotation);
        instance.gameObject.SetActive(true);
        Vector3 afterInstantiate = instance.transform.position;
        CompositePartSpawnHelper.AttachIfFollow(part.follow, followTransform, instance.transform, worldPosition);
        Vector3 afterAttach = instance.transform.position;
        CompositePartSpawnHelper.ApplyScale(part.scale, instance.transform);

        Transform setupOwner = followTransform != null
            ? followTransform
            : (_owner != null ? _owner : instance.transform);
        EffectSettings partSettings = CreateRuntimeSettings(
            _settings,
            applyTimedLifetime: part.UsesCastHitLifetime);
        if (partSettings == null)
        {
            Debug.LogWarning($"{DebugPrefix} settings are null for part {part.name}.");
            Destroy(instance.gameObject);
            return;
        }

        part.ConfigurePlayback(instance, partSettings, _castData, _context);
        instance.Setup(partSettings, _castData, setupOwner);
        PrepareV2Playback(instance);
        part.OnAfterSpawn(instance, _context);
        HomeFlightPart spawnedHome = part as HomeFlightPart;
        if (spawnedHome != null)
        {
            HomeProjectiles?.EnsureDualFlightRoots(instance, spawnedHome.mirrorDualFlight);
        }
        instance.Play();
        _spawned[part] = instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (V2TaSpawnLog.Matches(part))
        {
            V2TaSpawnLog.Info(
                "SPAWNED part='" + part.name + "' type=" + part.GetType().Name +
                " timing=" + part.spawnTiming +
                " attach=" + part.spawnAttachmentPoint +
                " follow=" + part.follow +
                " parent='" + (instance.transform.parent != null ? instance.transform.parent.name : "null") +
                "' goActive=" + instance.gameObject.activeInHierarchy +
                " authoredLife=" + instance.IsLifetimeOwnedByAuthoredStreams +
                " sincePlay=" + (Time.time - _playStartedAt).ToString("0.###") +
                "s frame=" + Time.frameCount);
        }
#endif
        TryLaunchImmediate(part, instance);
        RaiseStartSpawnAndMaybeSpawnLight();
        LogHomeSpawnPart(part, followTransform, worldPosition, spawnPosition, afterInstantiate, afterAttach, instance, setupOwner);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Spawned {part.Describe()} owner='{(setupOwner != null ? setupOwner.name : "null")}'.");
#endif
    }

    void LogHomeSpawnPlay()
    {
        Transform target = _context != null ? _context.TargetTransform : null;
        Transform caster = _context != null ? _context.CasterTransform : _owner;
        int partCount = _v2Parts != null ? _v2Parts.Length : 0;
        Debug.Log(
            $"[HOME_SPAWN] PlayV2 composite='{name}' v2Parts={partCount} " +
            $"legacy={ShouldUseLegacyPartPipeline} followSelfParent='{(transform.parent != null ? transform.parent.name : "null")}' " +
            $"compositePos={transform.position} caster='{(caster != null ? caster.name : "null")}' " +
            $"casterPos={(caster != null ? caster.position.ToString("F3") : "-")} " +
            $"target='{(target != null ? target.name : "null")}' " +
            $"targetPos={(target != null ? target.position.ToString("F3") : "-")}");
        if (_v2Parts == null)
        {
            return;
        }

        for (int i = 0; i < _v2Parts.Length; i++)
        {
            CompositePart p = _v2Parts[i];
            if (p == null)
            {
                Debug.Log($"[HOME_SPAWN]   part[{i}]=null");
                continue;
            }

            Debug.Log(
                $"[HOME_SPAWN]   part[{i}] name='{p.name}' type={p.GetType().Name} " +
                $"placement={(p.placement != null ? p.placement.GetType().Name : "null")} " +
                $"spawnPoint={p.spawnAttachmentPoint} " +
                $"timing={p.spawnTiming} follow={p.follow} prefab='{(p.prefab != null ? p.prefab.name : "null")}'");
        }
    }

    static void LogHomeSpawnPart(
        CompositePart part,
        Transform followTransform,
        Vector3 worldPosition,
        Vector3 spawnPosition,
        Vector3 afterInstantiate,
        Vector3 afterAttach,
        BaseEffect instance,
        Transform setupOwner)
    {
        Transform target = null;
        Vector3 targetPos = Vector3.zero;
        if (followTransform != null)
        {
            target = followTransform;
            targetPos = followTransform.position;
        }

        Vector3 rendererPos = instance != null ? instance.transform.position : Vector3.zero;
        Renderer rend = instance != null ? instance.GetComponentInChildren<Renderer>(true) : null;
        if (rend != null)
        {
            rendererPos = rend.bounds.center;
        }

        string spawnPoint = part.spawnAttachmentPoint.ToString();
        string placementName = part.placement != null ? part.placement.GetType().Name : "null";
        float dYResolved = worldPosition.y - targetPos.y;
        float dYGo = instance != null ? instance.transform.position.y - targetPos.y : 0f;

        Debug.LogWarning(
            $"[HOME_SPAWN] SPAWN part='{part.name}' spawnPoint={spawnPoint} type={part.GetType().Name} " +
            $"placement={placementName} follow={part.follow} " +
            $"followTf='{(followTransform != null ? followTransform.name : "null")}' " +
            $"setupOwner='{(setupOwner != null ? setupOwner.name : "null")}'\n" +
            $"  targetPos={targetPos:F3} resolvedWorld={worldPosition:F3} spawnPos={spawnPosition:F3}\n" +
            $"  afterInstantiate={afterInstantiate:F3} afterAttach={afterAttach:F3} " +
            $"goPos={(instance != null ? instance.transform.position.ToString("F3") : "-")} " +
            $"parent='{(instance != null && instance.transform.parent != null ? instance.transform.parent.name : "null")}'\n" +
            $"  rendererCenter={rendererPos:F3} " +
            $"dY_go-target={dYGo:F3} dY_resolved-target={dYResolved:F3}");

        Debug.DrawLine(targetPos, worldPosition, Color.magenta, 8f);
        Debug.DrawRay(worldPosition, Vector3.up * 0.4f, Color.cyan, 8f);
    }

    Quaternion ResolveV2SpawnRotation(CompositePart part, Transform resolvedTransform)
    {
        bool aimAlongHit = part != null && part.IsLaunchedProjectile;
        if (aimAlongHit &&
            _context != null &&
            _context.HasHitDirection &&
            _context.HitDirection.sqrMagnitude > 0.0001f)
        {
            return Quaternion.LookRotation(_context.HitDirection.normalized, Vector3.up) *
                   Quaternion.Euler(0f, HitDirectionYawOffsetDegrees, 0f);
        }

        if (resolvedTransform != null && (part == null || part.follow || part.inheritRotation))
        {
            Vector3 forward = resolvedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        return CompositeEffectUtilities.ResolveSpawnRotation(part != null && part.inheritRotation, resolvedTransform);
    }

    void RefreshV2Context()
    {
        _context = CompositeEffectUtilities.RebuildPreservingHit(_context, _owner, _castData);
    }

    void EnsureHitPointFromTarget()
    {
        if (_context == null || _context.HasHitPoint)
        {
            return;
        }

        Transform target = _context.TargetTransform != null
            ? _context.TargetTransform
            : _context.CasterTransform;
        Entity targetEntity = _context.TargetEntity != null
            ? _context.TargetEntity
            : _context.CasterEntity;
        if (targetEntity == null && target != null)
        {
            targetEntity = target.GetComponentInParent<Entity>();
        }

        Transform anchor = HitAnchorResolver.ResolveHitAnchor(targetEntity, target);
        if (anchor == null)
        {
            return;
        }

        _context.HasHitPoint = true;
        _context.HitPoint = anchor.position;
        if (!_context.HasHitDirection)
        {
            Vector3 fromCaster = _owner != null
                ? (anchor.position - _owner.position)
                : Vector3.zero;
            fromCaster.y = 0f;
            if (fromCaster.sqrMagnitude > 0.0001f)
            {
                _context.HasHitDirection = true;
                _context.HitDirection = fromCaster.normalized;
            }
        }
    }

    void TryLaunchImmediate(CompositePart part, BaseEffect instance)
    {
        if (part == null || instance == null || _launchedProjectiles.Contains(part))
        {
            return;
        }

        HomeFlightPart home = part as HomeFlightPart;
        if (home != null)
        {
            if (home.ShouldLaunchImmediately && home.TryLaunch(instance, _context, HomeProjectiles))
            {
                _launchedProjectiles.Add(part);
            }

            return;
        }

        ShotProjectilePart shot = part as ShotProjectilePart;
        if (shot == null || !shot.ShouldLaunchImmediately)
        {
            return;
        }

        if (shot.TryLaunch(instance, _context))
        {
            _launchedProjectiles.Add(part);
        }
    }

    void TryLaunchOnShoot(CompositePart part, BaseEffect instance)
    {
        if (part == null || instance == null || _launchedProjectiles.Contains(part))
        {
            return;
        }

        HomeFlightPart home = part as HomeFlightPart;
        if (home != null)
        {
            if (home.ShouldLaunchOnShoot && home.TryLaunch(instance, _context, HomeProjectiles))
            {
                _launchedProjectiles.Add(part);
            }

            return;
        }

        ShotProjectilePart shot = part as ShotProjectilePart;
        if (shot == null || !shot.ShouldLaunchOnShoot)
        {
            return;
        }

        if (shot.TryLaunch(instance, _context))
        {
            _launchedProjectiles.Add(part);
        }
    }

    void SubscribeV2ShootIfNeeded()
    {
        UnsubscribeV2Shoot();
        if (!RequiresV2AnimationShoot())
        {
            return;
        }

        if (_context?.CasterEntity?.Identity == null || IncomingPacketActions.Animations == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            V2TaSpawnLog.Warn(
                "SHOOT SUBSCRIBE FAIL composite='" + name +
                "' caster=" + (_context?.CasterEntity != null ? "ok" : "null") +
                " animations=" + (IncomingPacketActions.Animations != null ? "ok" : "null") +
                " — waiting for serverTimeToShoot fallback");
#endif
            return;
        }

        int casterId = _context.CasterEntity.Identity.Id;
        _v2AnimationEvents = IncomingPacketActions.Animations.GetAnimationEvents(casterId);
        if (_v2AnimationEvents == null || _v2ShootSources.Contains(_v2AnimationEvents))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_v2AnimationEvents == null)
            {
                V2TaSpawnLog.Warn(
                    "SHOOT SUBSCRIBE FAIL composite='" + name +
                    "' GetAnimationEvents(" + casterId + ")=null — fallback at serverTimeToShoot");
            }
#endif
            _v2SubscribedShoot = true;
            return;
        }

        _v2AnimationEvents.OnAnimationStartShoot += HandleV2AnimationShoot;
        _v2ShootSources.Add(_v2AnimationEvents);
        _v2SubscribedShoot = true;
    }

    void UnsubscribeV2Shoot()
    {
        UnsubscribeShootEventSources(
            _v2ShootSources,
            HandleV2AnimationShoot,
            ref _v2AnimationEvents,
            ref _v2SubscribedShoot);
    }

    bool RequiresV2AnimationShoot()
    {
        if (_v2Parts == null)
        {
            return false;
        }

        for (int i = 0; i < _v2Parts.Length; i++)
        {
            CompositePart part = _v2Parts[i];
            if (part != null && part.WantsAnimationShoot)
            {
                return true;
            }
        }

        return false;
    }

    bool RequiresV2HitCollider()
    {
        if (_v2Parts == null)
        {
            return false;
        }

        for (int i = 0; i < _v2Parts.Length; i++)
        {
            CompositePart part = _v2Parts[i];
            if (part != null && part.spawnTiming == CompositePartSpawnTiming.OnHitCollider)
            {
                return true;
            }
        }

        return false;
    }

    void SubscribeV2HitIfNeeded()
    {
        if (!RequiresV2HitCollider())
        {
            return;
        }

        if (ProjectileManager.Instance != null && !_v2SubscribedHit)
        {
            ProjectileManager.Instance.OnHitEffectProjectile += HandleV2ProjectileHit;
            _v2SubscribedHit = true;
        }
    }

    void UnsubscribeV2Hit()
    {
        if (ProjectileManager.Instance != null && _v2SubscribedHit)
        {
            ProjectileManager.Instance.OnHitEffectProjectile -= HandleV2ProjectileHit;
            _v2SubscribedHit = false;
        }
    }

    void HandleV2AnimationShoot(string _)
    {
        ProcessV2Shoot("direct");
    }

    void ProcessV2Shoot(string channel)
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
#endif
        foreach (KeyValuePair<CompositePart, BaseEffect> pair in _spawned)
        {
            CompositePart part = pair.Key;
            BaseEffect instance = pair.Value;
            if (part == null || instance == null)
            {
                continue;
            }

            part.OnAnimationShoot(instance, _context);
            TryLaunchOnShoot(part, instance);
        }
    }

    void SpawnPendingAnimationShootParts()
    {
        if (_pendingAnimationShoot.Count == 0)
        {
            return;
        }

        Debug.Log($"[HOME_SPAWN] AnimationShoot spawning {_pendingAnimationShoot.Count} part(s)");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        for (int logI = 0; logI < _pendingAnimationShoot.Count; logI++)
        {
            CompositePart shootPart = _pendingAnimationShoot[logI];
            if (V2TaSpawnLog.Matches(shootPart))
            {
                V2TaSpawnLog.Info(
                    "SHOOT QUEUE part='" + shootPart.name +
                    "' sincePlay=" + (Time.time - _playStartedAt).ToString("0.###") +
                    "s channel pending count=" + _pendingAnimationShoot.Count);
            }
        }
#endif

        for (int i = 0; i < _pendingAnimationShoot.Count; i++)
        {
            CompositePart part = _pendingAnimationShoot[i];
            if (part == null || _spawned.ContainsKey(part))
            {
                continue;
            }

            float extraDelay = Mathf.Max(0f, part.spawnDelaySeconds);
            if (extraDelay > 0f)
            {
                _pendingDelayed.Add(new PendingV2Part
                {
                    Part = part,
                    SpawnAtTime = Time.time + extraDelay
                });
                continue;
            }

            SpawnV2Part(part);
        }

        _pendingAnimationShoot.Clear();
        StartV2PendingRoutineIfNeeded();
    }

    void HandleV2ProjectileHit(
        GameObject projectilePrefab,
        Transform target,
        Vector3 hitPoint,
        Vector3 hitDirection,
        int attackerEntityId)
    {
        if (_pendingHitCollider.Count == 0 || projectilePrefab == null)
        {
            return;
        }

        Vector3 resolvedHitPoint = hitPoint;
        Vector3 resolvedHitDirection = hitDirection;
        if (HitManager.Instance != null &&
            !HitManager.Instance.TryPrepareProjectileEffectHit(
                projectilePrefab,
                hitPoint,
                hitDirection,
                attackerEntityId,
                IsFromLaunchedV2Projectile,
                out resolvedHitPoint,
                out resolvedHitDirection))
        {
            if (projectilePrefab.transform == null ||
                !IsFromLaunchedV2Projectile(projectilePrefab.transform))
            {
                return;
            }

            resolvedHitPoint = hitPoint;
            resolvedHitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection : Vector3.forward;
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

        for (int i = 0; i < _pendingHitCollider.Count; i++)
        {
            SpawnV2Part(_pendingHitCollider[i]);
        }

        if (_context != null)
        {
            _context.HasHitPoint = hadHitPoint;
            _context.HitPoint = previousHitPoint;
            _context.HasHitDirection = hadHitDirection;
            _context.HitDirection = previousHitDirection;
        }

        _pendingHitCollider.Clear();
    }

    void FlushPendingHitAtCastEnd()
    {
        if (_pendingHitCollider.Count == 0)
        {
            return;
        }

        RefreshV2Context();
        EnsureHitPointFromTarget();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} HitTime fallback spawn {_pendingHitCollider.Count} impact part(s) " +
            $"(projectile event did not fire before composite HitTime).");
#endif

        for (int i = 0; i < _pendingHitCollider.Count; i++)
        {
            SpawnV2Part(_pendingHitCollider[i]);
        }

        _pendingHitCollider.Clear();
    }

    void FlushPendingDelayedIndependentParts()
    {
        if (_pendingDelayed.Count == 0)
        {
            return;
        }

        for (int i = _pendingDelayed.Count - 1; i >= 0; i--)
        {
            PendingV2Part pending = _pendingDelayed[i];
            if (pending == null || pending.Part == null)
            {
                _pendingDelayed.RemoveAt(i);
                continue;
            }

            if (!pending.Part.OutlivesComposite)
            {
                continue;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (V2TaSpawnLog.Matches(pending.Part))
            {
                V2TaSpawnLog.Warn(
                    "FLUSH ON DESTROY part='" + pending.Part.name +
                    "' was scheduled spawnAt=" + pending.SpawnAtTime.ToString("F3") +
                    " now=" + Time.time.ToString("F3") +
                    " remaining=" + (pending.SpawnAtTime - Time.time).ToString("0.###") +
                    "s — composite died before delayed spawn");
            }
#endif
            SpawnV2Part(pending.Part);
            _pendingDelayed.RemoveAt(i);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LogV2TaDestroyState(string stage)
    {
        int delayedTa = 0;
        for (int i = 0; i < _pendingDelayed.Count; i++)
        {
            if (_pendingDelayed[i] != null && V2TaSpawnLog.Matches(_pendingDelayed[i].Part))
            {
                delayedTa++;
            }
        }

        int shootTa = 0;
        for (int i = 0; i < _pendingAnimationShoot.Count; i++)
        {
            if (V2TaSpawnLog.Matches(_pendingAnimationShoot[i]))
            {
                shootTa++;
            }
        }

        if (delayedTa == 0 && shootTa == 0)
        {
            return;
        }

        V2TaSpawnLog.Warn(
            "DESTROY " + stage +
            " composite='" + name +
            "' delayedTa=" + delayedTa +
            " shootPendingTa=" + shootTa +
            " spawned=" + _spawned.Count +
            " sincePlay=" + (Time.time - _playStartedAt).ToString("0.###") +
            "s frame=" + Time.frameCount);
    }
#endif

    bool IsFromLaunchedV2Projectile(Transform attacker)
    {
        foreach (KeyValuePair<CompositePart, BaseEffect> pair in _spawned)
        {
            if (pair.Key == null || !pair.Key.IsLaunchedProjectile || pair.Value == null)
            {
                continue;
            }

            Transform spawnedTransform = pair.Value.transform;
            if (attacker == spawnedTransform || attacker.IsChildOf(spawnedTransform))
            {
                return true;
            }
        }

        return false;
    }

    void StartV2PendingRoutineIfNeeded()
    {
        if (_pendingDelayed.Count > 0 && _v2PendingRoutine == null)
        {
            _v2PendingRoutine = StartCoroutine(SpawnPendingV2Routine());
        }
    }

    IEnumerator SpawnPendingV2Routine()
    {
        while (_pendingDelayed.Count > 0)
        {
            float now = Time.time;
            for (int i = _pendingDelayed.Count - 1; i >= 0; i--)
            {
                PendingV2Part pending = _pendingDelayed[i];
                if (pending == null || pending.Part == null)
                {
                    _pendingDelayed.RemoveAt(i);
                    continue;
                }

                if (now >= pending.SpawnAtTime)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (V2TaSpawnLog.Matches(pending.Part))
                    {
                        V2TaSpawnLog.Info(
                            "DELAY DUE part='" + pending.Part.name +
                            "' spawnAt=" + pending.SpawnAtTime.ToString("F3") +
                            " now=" + now.ToString("F3") +
                            " late=" + (now - pending.SpawnAtTime).ToString("0.###") +
                            "s frame=" + Time.frameCount);
                    }
#endif
                    SpawnV2Part(pending.Part);
                    _pendingDelayed.RemoveAt(i);
                }
            }

            yield return null;
        }

        _v2PendingRoutine = null;
    }

    void StartV2ShootFallbackIfNeeded()
    {
        if (_castData == null || !RequiresV2AnimationShoot() || _castData.serverTimeToShoot <= 0f)
        {
            return;
        }

        if (_v2FallbackShootRoutine != null)
        {
            StopCoroutine(_v2FallbackShootRoutine);
        }

        _v2FallbackShootRoutine = StartCoroutine(V2ShootFallbackRoutine());
    }

    IEnumerator V2ShootFallbackRoutine()
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

        ProcessV2Shoot("fallback");
        _v2FallbackShootRoutine = null;
    }

    void DestroyOwnedV2Parts()
    {
        foreach (KeyValuePair<CompositePart, BaseEffect> pair in _spawned)
        {
            if (pair.Value == null ||
                _launchedProjectiles.Contains(pair.Key) ||
                (pair.Key != null && pair.Key.OutlivesComposite))
            {
                continue;
            }

            Destroy(pair.Value.gameObject);
        }
    }

    void ClearV2Runtime()
    {
        _pendingDelayed.Clear();
        _pendingHitCollider.Clear();
        _pendingAnimationShoot.Clear();
        _spawned.Clear();
        _launchedProjectiles.Clear();
    }

    static bool HasParticleGroupV2(BaseEffect instance)
    {
        return instance != null &&
               instance.GetComponentInChildren<ParticleGroupV2>(true) != null;
    }

    static bool HasLegacyParticleAuthoring(BaseEffect instance)
    {
        if (instance == null)
        {
            return false;
        }

        ParticleGroup[] groups = instance.GetComponentsInChildren<ParticleGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null &&
                groups[i].GetComponent<ParticleGroupV2>() == null)
            {
                return true;
            }
        }

        ParticleSingle[] singles = instance.GetComponentsInChildren<ParticleSingle>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            if (singles[i] != null &&
                singles[i].GetComponent<ParticleGroupV2>() == null)
            {
                return true;
            }
        }

        return false;
    }

    sealed class PendingV2Part
    {
        public CompositePart Part;
        public float SpawnAtTime;
    }
}
