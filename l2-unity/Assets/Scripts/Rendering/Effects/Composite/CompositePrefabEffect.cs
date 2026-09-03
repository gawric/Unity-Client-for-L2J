using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CompositePrefabEffect : TimedCompositeEffectBase
{
    [SerializeField] private CompositePrefabPart[] _parts;
    [SerializeField] private float _serverHitLifetimeTailSeconds = 0f;

    [Header("Lifetime")]
    [Tooltip(
        "If enabled: root/part cast-timed lifetime uses MagicCastData.SkillAnimationDuration " +
        "(wall-clock SpAtk until idle) instead of HitTime. For melee skill FX that must end with the swing.")]
    [SerializeField] private bool _matchLifetimeToSkillAnimation;

    [Tooltip("Если включено: не вызывать DestoryEffect по lifetime корня — дочерние префабы (например wh_heal_ca) не уничтожаются вместе с композитом. Только для отладки шейдеров/Hold.")]
    [SerializeField] private bool _skipDestroyCompositeByLifetime;

    [Header("Effect Light")]
    [Tooltip(
        "Spawn FNManagerLight once per composite Play when a part SpawnPart succeeds. " +
        "Not wired to ParticleGroup/ParticleSingle — only CompositePrefabEffect.SpawnPart → StartSpawn.")]
    [SerializeField] private bool _useLight;
    [SerializeField] private LightEffectSetting _lightSettings;

    public event Action<CompositePrefabEffect, EffectResolveContext> StartSpawn;

    [Inject] IHomeProjectileService _homeProjectiles;
    readonly IEffectAttachmentResolver _resolver = new DefaultEffectAttachmentResolver();
    protected EffectResolveContext _context;
    readonly List<PendingCompositePart> _pendingParts = new List<PendingCompositePart>();
    readonly List<CompositePrefabPart> _pendingHitColliderParts = new List<CompositePrefabPart>();
    readonly List<CompositePrefabPart> _pendingAnimationShootParts = new List<CompositePrefabPart>();
    readonly Dictionary<CompositePrefabPart, BaseEffect> _spawnedPartInstances = new Dictionary<CompositePrefabPart, BaseEffect>();
    readonly HashSet<CompositePrefabPart> _launchedProjectileParts = new HashSet<CompositePrefabPart>();
    readonly HashSet<CompositePrefabPart> _launchedHomeProjectileParts = new HashSet<CompositePrefabPart>();
    readonly List<AnimationEventsBase> _shootEventSources = new List<AnimationEventsBase>();
    Coroutine _pendingSpawnRoutine;
    Coroutine _fallbackShootRoutine;
    Coroutine _visibilityProbeRoutine;
    AnimationEventsBase _animationEvents;
    protected float _playStartedAt;
    bool _isSubscribedToAnyShoot;
    bool _isSubscribedToProjectileEffectHit;
    bool _lightSpawned;
    string _lastShootChannel;
    float _lastShootAt = -1f;
    string _lastShootStack;

    protected IHomeProjectileService HomeProjectiles =>
        CompositeHomeProjectileAccess.Resolve(ref _homeProjectiles);

    protected override string DebugPrefix => "[CompositePrefabEffect]";
    protected override float RuntimeLifeTimeTailSeconds => _serverHitLifetimeTailSeconds;
    protected bool SkipDestroyCompositeByLifetime => _skipDestroyCompositeByLifetime;
    protected virtual bool ShouldUseLegacyPartPipeline => true;
    protected virtual bool UseLegacyLifetimeHacks => true;

    protected override float ResolveCastTimedLifetimeSeconds()
    {
        if (_matchLifetimeToSkillAnimation &&
            _castData != null &&
            _castData.SkillAnimationDuration > 0f)
        {
            return _castData.SkillAnimationDuration;
        }

        return base.ResolveCastTimedLifetimeSeconds();
    }

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
        _lightSpawned = false;
        if (ShouldUseLegacyPartPipeline)
        {
            SubscribeShootEventIfNeeded();
            SubscribeProjectileHitEventIfNeeded();
        }
    }

    public override void Play()
    {
        if (!ShouldUseLegacyPartPipeline)
        {
            PlayV2();
            return;
        }

        if (_parts == null || _parts.Length == 0)
        {
            Debug.LogWarning("CompositePrefabEffect: no parts configured.");
            return;
        }

        _lightSpawned = false;
        _lastShootChannel = null;
        _lastShootAt = -1f;
        _lastShootStack = null;
        _playStartedAt = Time.time;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Play started at={_playStartedAt:F3}s hit={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"flight={(_castData != null ? _castData.FlightTime : -1f):F3}s serverShoot={(_castData != null ? _castData.serverTimeToShoot : -1f):F3}s.");
#endif

        CompositePartScheduler.Queue(
            _parts,
            _castData,
            _pendingParts,
            _pendingHitColliderParts,
            _pendingAnimationShootParts,
            SpawnPart);
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

    protected virtual void PlayV2()
    {
    }

    public override void SetProgress(float normalizedTime)
    {
    }

    public void SetImpactHit(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_context == null)
        {
            _context = CompositeEffectUtilities.BuildContext(_owner, _castData);
        }

        CompositeEffectUtilities.ApplyImpactHit(_context, hitPoint, hitDirection);
    }

    protected virtual void PreparePartPlayback(CompositePrefabPart part, BaseEffect instance)
    {
    }

    protected void RaiseStartSpawnAndMaybeSpawnLight()
    {
        StartSpawn?.Invoke(this, _context);
        if (!_useLight || _lightSettings == null || _lightSpawned)
        {
            return;
        }

        if (!EffectLightPlacement.TryResolve(_context, _lightSettings, out Vector3 lightPoint, out Vector3 lightDir))
        {
            Debug.LogWarning($"{DebugPrefix} useLight=true but light placement failed (settings={_lightSettings.name}).");
            return;
        }

        _lightSpawned = true;
        FNManagerLight.Ensure().SpawnHitFlash(lightPoint, lightDir, _lightSettings);
    }

    void SpawnPart(CompositePrefabPart part)
    {
        if (part == null || part.prefab == null)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_spawnedPartInstances.ContainsKey(part))
            EffectDoublePlayLog.Note(
                "Composite.SpawnPart",
                this,
                $"part='{part.name}' already spawned instance='{_spawnedPartInstances[part]}'");
#endif

        _context = CompositeEffectUtilities.RebuildPreservingHit(_context, _owner, _castData);
        if (!CompositePartSpawnHelper.TryResolveAttachment(
                part, _context, _resolver, out Transform resolvedTransform, out Vector3 worldPosition))
        {
            return;
        }

        BaseEffect instance = CompositePartSpawnHelper.SpawnInstance(
            part, resolvedTransform, worldPosition, _owner, _context, UseLegacyLifetimeHacks);
        if (instance == null)
        {
            return;
        }

        if (!TryResolvePartSettings(part, out EffectSettings partSettings))
        {
            return;
        }

        if (UseLegacyLifetimeHacks)
        {
            CompositePartSpawnHelper.ApplyHideTimeOverride(part, partSettings);
        }

        Transform setupOwner = CompositePartSpawnHelper.ResolveSetupOwner(
            resolvedTransform, _owner, instance.transform);
        MagicCastData partCastData = part.passCastDataToPart ? _castData : null;
        instance.Setup(partSettings, partCastData, setupOwner);
        if (UseLegacyLifetimeHacks)
        {
            CompositePartSpawnHelper.ApplyLoopOverrides(part, instance.transform);
        }

        CompositePartSpawnHelper.ApplyHomeFlightOverrides(part, instance.transform);
        if (part.homeProjectile != null && part.homeProjectile.IsEnabled)
        {
            instance.PrepareDestroyOnHomeArrival();
        }

        PreparePartPlayback(part, instance);
        CompositePartSpawnHelper.ApplyShaderTargetPosition(part, instance.transform, _context, _resolver);
        instance.Play();
        CompositeProjectileLaunchHelper.ApplyPreShootVisibility(part, instance, DebugPrefix);
        _spawnedPartInstances[part] = instance;
        if (UseLegacyLifetimeHacks)
        {
            StartFinalShaderLifetimeRoutineIfNeeded(part, instance.transform, partSettings);
        }

        TryLaunchPartAsProjectileImmediately(part, instance);
        TryLaunchPartAsHomeProjectileImmediately(part, instance);
        LogSpawnedPart(part, partSettings, setupOwner);
        RaiseStartSpawnAndMaybeSpawnLight();
    }

    bool TryResolvePartSettings(CompositePrefabPart part, out EffectSettings partSettings)
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

    void SubscribeShootEventIfNeeded()
    {
        CompositePlaybackSubscriptions.SubscribeShootIfNeeded(
            _parts,
            _context,
            _shootEventSources,
            HandleAnimationShoot,
            out _animationEvents,
            ref _isSubscribedToAnyShoot);
    }

    void SubscribeProjectileHitEventIfNeeded()
    {
        CompositePlaybackSubscriptions.SubscribeHitIfNeeded(
            _parts,
            HandleProjectileEffectHit,
            ref _isSubscribedToProjectileEffectHit);
    }

    void HandleAnimationShoot(string _)
    {
        ProcessShootEvent("direct");
    }

    void ProcessShootEvent(string channel)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_lastShootAt > 0f && _pendingAnimationShootParts.Count > 0)
            EffectDoublePlayLog.Repeat(
                $"Composite.ShootEvent first={_lastShootChannel} this={channel}",
                this,
                _lastShootAt,
                _lastShootStack);
        _lastShootChannel = channel;
        _lastShootAt = Time.time;
        _lastShootStack = EffectDoublePlayLog.CaptureStack();
#endif
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
        CompositeProjectileLaunchHelper.RevealOnShootParts(_parts, _spawnedPartInstances);
        CompositeProjectileLaunchHelper.ProcessShootLaunches(
            _parts,
            _spawnedPartInstances,
            _launchedProjectileParts,
            _context.TargetTransform,
            _playStartedAt,
            DebugPrefix);

        IHomeProjectileService homeProjectiles = HomeProjectiles;
        if (homeProjectiles != null)
        {
            homeProjectiles.ProcessShootLaunches(
                _parts,
                _spawnedPartInstances,
                _launchedHomeProjectileParts,
                _context);
        }
    }

    void SpawnPendingAnimationShootParts()
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

            float extraDelay = Mathf.Max(0f, part.spawnDelaySeconds);
            if (extraDelay > 0f)
            {
                _pendingParts.Add(new PendingCompositePart
                {
                    Part = part,
                    SpawnAtTime = Time.time + extraDelay
                });
                continue;
            }

            SpawnPart(part);
        }

        _pendingAnimationShootParts.Clear();
        StartPendingPartsRoutineIfNeeded();
    }

    void HandleProjectileEffectHit(
        GameObject projectilePrefab,
        Transform target,
        Vector3 hitPoint,
        Vector3 hitDirection,
        int attackerEntityId)
    {
        if (_pendingHitColliderParts.Count == 0 || projectilePrefab == null || HitManager.Instance == null)
        {
            return;
        }

        if (!HitManager.Instance.TryPrepareProjectileEffectHit(
                projectilePrefab,
                hitPoint,
                hitDirection,
                attackerEntityId,
                attacker => CompositePlaybackSubscriptions.IsFromLaunchedCompositeProjectile(
                    attacker, _spawnedPartInstances),
                out Vector3 resolvedHitPoint,
                out Vector3 resolvedHitDirection))
        {
            return;
        }

        EffectResolveContext previous = _context;
        bool hadHitPoint = previous != null && previous.HasHitPoint;
        Vector3 previousHitPoint = hadHitPoint ? previous.HitPoint : Vector3.zero;
        bool hadHitDirection = previous != null && previous.HasHitDirection;
        Vector3 previousHitDirection = hadHitDirection ? previous.HitDirection : Vector3.forward;

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

    void TryLaunchPartAsProjectileImmediately(CompositePrefabPart part, BaseEffect spawned)
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

    void TryLaunchPartAsHomeProjectileImmediately(CompositePrefabPart part, BaseEffect spawned)
    {
        if (part == null || spawned == null ||
            part.homeProjectile == null || !part.homeProjectile.ShouldLaunchImmediately)
        {
            return;
        }

        IHomeProjectileService homeProjectiles = HomeProjectiles;
        if (homeProjectiles == null || _context?.CasterTransform == null || _launchedHomeProjectileParts.Contains(part))
        {
            return;
        }

        if (homeProjectiles.TryLaunchPart(part, spawned, _context))
        {
            _launchedHomeProjectileParts.Add(part);
        }
    }

    void StartShootFallbackRoutineIfNeeded()
    {
        if (_castData == null || !CompositeProjectileLaunchHelper.RequiresAnimationShootEvent(_parts))
        {
            return;
        }

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

    IEnumerator ShootFallbackRoutine()
    {
        float shootAt = _castData.StartTime + _castData.serverTimeToShoot;
        float remaining = shootAt - Time.time;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else
        {
            Debug.Log($"{DebugPrefix} Shoot fallback catch-up: event time already passed by {-remaining:F3}s.");
        }
#endif

        ProcessShootEvent("fallback");
        _fallbackShootRoutine = null;
    }

    void StartFinalShaderLifetimeRoutineIfNeeded(
        CompositePrefabPart part,
        Transform instanceTransform,
        EffectSettings partSettings)
    {
        if (part == null || instanceTransform == null || !part.disableShaderLifetime ||
            !part.enableFinalShaderLifetimeOnFade || partSettings == null)
        {
            return;
        }

        float finalWindow = Mathf.Max(part.finalShaderLifetimeMax, part.finalShaderLifetimeMin, 0f);
        float delay = Mathf.Max(0f, partSettings.defaultLifeTime - partSettings.hideTime - finalWindow);
        StartCoroutine(EnableFinalShaderLifetimeAfterDelay(
            instanceTransform, delay, part.finalShaderLifetimeMin, part.finalShaderLifetimeMax));
    }

    IEnumerator EnableFinalShaderLifetimeAfterDelay(Transform instanceTransform, float delay, float rangeMin, float rangeMax)
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
    }

    void LogSpawnedPart(CompositePrefabPart part, EffectSettings partSettings, Transform setupOwner)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{DebugPrefix} Spawned part='{part.name}' point={part.attachmentPoint} " +
            $"follow={part.followResolvedTransform} hitTime={(_castData != null ? _castData.HitTime : -1f):F3}s " +
            $"lifeTime={partSettings.defaultLifeTime:F3}s scale={part.scale:F2} offset={part.positionOffset} owner='{(setupOwner != null ? setupOwner.name : "null")}'.");
#endif
    }

    void StartPendingPartsRoutineIfNeeded()
    {
        if (_pendingParts.Count > 0 && _pendingSpawnRoutine == null)
        {
            _pendingSpawnRoutine = StartCoroutine(SpawnPendingPartsRoutine());
        }
    }

    void DestroyCompositeByLifetime()
    {
        EffectSettings lifeTimeSettings = SelectLifetimeSettings();
        if (lifeTimeSettings != null)
        {
            DestoryEffect(lifeTimeSettings, _castData);
        }
    }

    IEnumerator SpawnPendingPartsRoutine()
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
        CompositePlaybackSubscriptions.UnsubscribeShoot(
            _shootEventSources, HandleAnimationShoot, ref _isSubscribedToAnyShoot);
        _animationEvents = null;
        CompositePlaybackSubscriptions.UnsubscribeHit(
            HandleProjectileEffectHit, ref _isSubscribedToProjectileEffectHit);
        StopAndClearCoroutine(ref _pendingSpawnRoutine);
        StopAndClearCoroutine(ref _fallbackShootRoutine);
        StopAndClearCoroutine(ref _visibilityProbeRoutine);

        _pendingParts.Clear();
        _pendingHitColliderParts.Clear();
        _pendingAnimationShootParts.Clear();
        DestroyOwnedSpawnedParts();
        _spawnedPartInstances.Clear();
        _launchedProjectileParts.Clear();
        _launchedHomeProjectileParts.Clear();
    }

    void DestroyOwnedSpawnedParts()
    {
        foreach (KeyValuePair<CompositePrefabPart, BaseEffect> pair in _spawnedPartInstances)
        {
            if (pair.Value == null)
            {
                continue;
            }

            if (_launchedProjectileParts.Contains(pair.Key) ||
                _launchedHomeProjectileParts.Contains(pair.Key))
            {
                continue;
            }

            Destroy(pair.Value.gameObject);
        }
    }
}
