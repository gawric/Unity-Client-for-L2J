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
                continue;
            }

            float delay = CompositeEffectUtilities.ResolveSpawnDelay(part.spawnTiming, _castData);
            delay += Mathf.Max(0f, part.spawnDelaySeconds);
            if (delay <= 0f)
            {
                SpawnV2Part(part);
                continue;
            }

            _pendingDelayed.Add(new PendingV2Part
            {
                Part = part,
                SpawnAtTime = Time.time + delay
            });
        }
    }

    void SpawnV2Part(CompositePart part)
    {
        if (part == null || !part.IsSpawnable || _spawned.ContainsKey(part))
        {
            return;
        }

        RefreshV2Context();
        EnsureHitPointFromTarget();
        if (part.placement == null ||
            !part.placement.TryResolve(_v2Resolver, _context, out Transform followTransform, out Vector3 worldPosition))
        {
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
        AttachToResolvedTransformIfNeeded(part.follow, followTransform, instance.transform, worldPosition);
        ApplyPartScale(part.scale, instance.transform);

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
        instance.Play();
        part.OnAfterSpawn(instance, _context);
        _spawned[part] = instance;
        TryLaunchImmediate(part, instance);
        RaiseStartSpawnAndMaybeSpawnLight();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Spawned {part.Describe()} owner='{(setupOwner != null ? setupOwner.name : "null")}'.");
#endif
    }

    Quaternion ResolveV2SpawnRotation(CompositePart part, Transform resolvedTransform)
    {
        if (_context != null &&
            _context.HasHitDirection &&
            _context.HitDirection.sqrMagnitude > 0.0001f)
        {
            return Quaternion.LookRotation(_context.HitDirection.normalized, Vector3.up) *
                   Quaternion.Euler(0f, HitDirectionYawOffsetDegrees, 0f);
        }

        return CompositeEffectUtilities.ResolveSpawnRotation(part != null && part.inheritRotation, resolvedTransform);
    }

    void RefreshV2Context()
    {
        bool hadHitPoint = _context != null && _context.HasHitPoint;
        Vector3 hitPoint = hadHitPoint ? _context.HitPoint : Vector3.zero;
        bool hadHitDirection = _context != null && _context.HasHitDirection;
        Vector3 hitDirection = hadHitDirection ? _context.HitDirection : Vector3.forward;

        _context = CompositeEffectUtilities.BuildContext(_owner, _castData);

        if (hadHitPoint)
        {
            _context.HasHitPoint = true;
            _context.HitPoint = hitPoint;
        }

        if (hadHitDirection)
        {
            _context.HasHitDirection = true;
            _context.HitDirection = hitDirection;
        }
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
        ShotProjectilePart shot = part as ShotProjectilePart;
        if (shot == null || !shot.ShouldLaunchImmediately || _launchedProjectiles.Contains(part))
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
        ShotProjectilePart shot = part as ShotProjectilePart;
        if (shot == null || !shot.ShouldLaunchOnShoot || _launchedProjectiles.Contains(part))
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
            return;
        }

        int casterId = _context.CasterEntity.Identity.Id;
        _v2AnimationEvents = IncomingPacketActions.Animations.GetAnimationEvents(casterId);
        if (_v2AnimationEvents == null || _v2ShootSources.Contains(_v2AnimationEvents))
        {
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
