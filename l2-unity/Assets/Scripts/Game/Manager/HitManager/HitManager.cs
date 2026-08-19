using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class HitManager : MonoBehaviour
{
    [Inject] World _world;
    public static HitManager Instance { get; private set; }
    private const string ETC_NAME = "etc_";
    private const string IMPACT_DEBUG_TAG = "[HIT_DEBUG]";
    private const int SoulshotImpactEffectId = 99998;
    private const int NormalImpactEffectId = 99997;
    private bool _projectileHitSubscribed;
    private readonly Dictionary<int, RemoteMeleeHit> _remoteHits = new Dictionary<int, RemoteMeleeHit>();
    private readonly List<int> _remoteHitScratch = new List<int>();

    sealed class RemoteMeleeHit
    {
        public int AttackerId;
        public Entity Attacker;
        public Entity Target;
        public bool Soulshot;
        public bool Fired;
        public float FireAt;
        public AnimationEventsBase Events;
        public Action<string> OnShot;
        public Action<string> OnHit;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        TrySubscribeProjectileHits();
    }

    private void Start()
    {
        TrySubscribeProjectileHits();
    }

    private void OnDisable()
    {
        UnsubscribeProjectileHits();
        ClearRemoteMeleeHits();
    }

    private void Update()
    {
        TickRemoteMeleeHits();
    }

    /// <summary>
    /// Called from ProjectileManager.Awake when Instance becomes available (order-safe).
    /// Subscribe once for HitManager lifetime; unsubscribe only on HitManager disable/destroy.
    /// </summary>
    public void BindProjectileHits()
    {
        TrySubscribeProjectileHits();
    }

    private void TrySubscribeProjectileHits()
    {
        if (_projectileHitSubscribed || ProjectileManager.Instance == null)
        {
            return;
        }

        ProjectileManager.Instance.OnHitMonster += OnProjectileHitMonster;
        _projectileHitSubscribed = true;
    }

    private void UnsubscribeProjectileHits()
    {
        if (!_projectileHitSubscribed)
        {
            return;
        }

        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.OnHitMonster -= OnProjectileHitMonster;
        }

        _projectileHitSubscribed = false;
    }

    /// <summary>
    /// Bow ArrowStick Hit Time — lives on HitManager so AttackDto→Idle cannot drop the subscription.
    /// </summary>
    private void OnProjectileHitMonster(
        GameObject prefab,
        Transform target,
        Vector3 hitPointCollider,
        Vector3 hitDirection,
        int attackerEntityId)
    {
        if (prefab != null && target != null)
        {
            HandleHitBody(prefab, target, hitPointCollider, hitDirection);
        }

        Entity attacker = ResolveEntityFromWorld(attackerEntityId);
        if (attacker == null)
            attacker = PlayerEntity.Instance;
        Entity targetEntity = target != null
            ? target.GetComponentInParent<Entity>()
            : null;
        if (targetEntity == null && attacker != null)
            targetEntity = attacker.GetTargetEntity();

        MonsterEntity monster = targetEntity as MonsterEntity;
        if (monster == null)
        {
            Debug.Log(
                $"[HIT_FX] OnProjectileHitMonster SKIP not MonsterEntity " +
                $"type={(targetEntity != null ? targetEntity.GetType().Name : "null")}");
            return;
        }

        if (attacker != null && attacker.HitIsMissed())
        {
            Debug.Log(
                $"[HIT_FX] OnProjectileHitMonster SKIP HitIsMissed=true monster={monster.name}");
            return;
        }

        Transform attackerTf = prefab != null ? prefab.transform : null;
        Debug.Log(
            $"[HIT_FX] OnProjectileHitMonster → HandleHitCollider monster={monster.name} " +
            $"soulshot={(attacker != null && attacker.IsSoulshotCharged)}");
        HandleHitCollider(
            attacker,
            attackerTf,
            monster,
            hitPointCollider,
            hitDirection);

        if (monster.IsDead() || monster.CalculateRemainingHp() <= 0)
        {
            monster.SetDead(true);
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.Die(monster);
        }
    }

    public void HandleHitBody(GameObject source, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        string sourceNameLower = source.name.ToLower();
        string etcNameLower = ETC_NAME.ToLower();

        if (sourceNameLower.IndexOf(etcNameLower) > -1)
        {
            HandleArrowHit(source, target, hitPointCollider, hitDirection);
        }
        else
        {
            Debug.LogWarning("HitManager> HandleHit Errors no detected hit type");
        }
    }

    public void HandleHitCollider(
        Entity attaker,
        Transform attacker,
        Entity targetEntity,
        Vector3 hitCollider,
        Vector3 hitColliderDirection)
    {
        string attackerName = attacker != null ? attacker.name : "null";
        Debug.Log(
            $"[HIT_FX] 6.HitManager.HandleHitCollider ENTER frame={Time.frameCount} t={Time.time:F3} " +
            $"attacker={attackerName} target={(targetEntity != null ? targetEntity.name : "null")} " +
            $"soulshot={(attaker != null && attaker.IsSoulshotCharged)} point={hitCollider}");

        if (targetEntity == null)
        {
            Debug.LogWarning("[HIT_FX] 6.HitManager SKIP targetEntity=null");
            return;
        }
        Vector3 impactDir = ResolveSoulshotImpactDirection(attaker, targetEntity, hitCollider);
        Vector3 spawnPoint = ResolveSoulshotSpawnPoint(targetEntity, impactDir, hitCollider);

        if (EffectManager.Instance == null)
        {
            Debug.LogWarning("[HIT_FX] 6.HitManager SKIP EffectManager.Instance=null");
            return;
        }

        if (attaker != null && attaker.IsSoulshotCharged)
        {
            LogSoulshotImpactOrientation(attaker, targetEntity, hitCollider, spawnPoint, impactDir);
            Debug.Log(
                $"[HIT_FX] 6.HitManager → EffectManager.PlayerImpactEffect " +
                $"id={SoulshotImpactEffectId} (soulshot) spawn={spawnPoint} dir={impactDir}");
            EffectManager.Instance.PlayerImpactEffect(SoulshotImpactEffectId, spawnPoint, impactDir);
            attaker.IsSoulshotCharged = false;
        }
        else
        {
            Debug.Log(
                $"[HIT_FX] 6.HitManager → EffectManager.PlayerImpactEffect " +
                $"id={NormalImpactEffectId} (normal) spawn={spawnPoint} dir={impactDir}");
            EffectManager.Instance.PlayerImpactEffect(NormalImpactEffectId, spawnPoint, impactDir);
        }
    }

    /// <summary>
    /// Remote melee (Monster / CharInfo). Local PlayerEntity stays on AttackShot + sword collision.
    /// Both wait for clip AttackShot / OnAnimationHit; timer is only a late fallback.
    /// Bow: ignore AttackShot / HitShoot — the arrow has not left yet. Impact is ArrowStick Hit Time.
    /// </summary>
    public void ArmRemoteMeleeHit(Entity attacker, Entity target, AttackDto dto)
    {
        if (attacker == null || target == null || attacker is PlayerEntity)
            return;
        if (!(attacker is MonsterEntity) && !(attacker is UserEntity))
            return;
        if (attacker.Identity == null)
            return;

        Hit hit = dto != null ? dto.FirstHit : null;
        if (hit != null && hit.isMiss())
        {
            Debug.Log(
                $"[HIT_FX] ArmRemoteMelee SKIP miss attacker={attacker.name} target={target.name}");
            return;
        }

        if (IsBowWeapon(attacker))
        {
            DropRemoteMeleeHit(attacker.Identity.Id);
            if (hit != null && hit.hasSoulshot())
                attacker.IsSoulshotCharged = true;
            Debug.Log(
                $"[HIT_FX] ArmRemoteMelee SKIP bow attacker={attacker.name} " +
                "impact waits ArrowStick Hit Time");
            return;
        }

        FlushRemoteMeleeHit(attacker);

        float delay = ResolveRemoteHitDelay(attacker);
        int attackerId = attacker.Identity.Id;
        RemoteMeleeHit pending = new RemoteMeleeHit
        {
            AttackerId = attackerId,
            Attacker = attacker,
            Target = target,
            Soulshot = hit != null && hit.hasSoulshot(),
            FireAt = Time.time + delay
        };

        AnimationEventsBase events = attacker.GetAnimatorController();
        if (events == null)
            events = attacker.GetComponentInChildren<AnimationEventsBase>(true);
        if (events != null)
        {
            pending.Events = events;
            pending.OnShot = _ => TryFireRemoteMeleeHit(attackerId);
            pending.OnHit = _ => TryFireRemoteMeleeHit(attackerId);
            events.OnAnimationAttackShot += pending.OnShot;
            events.OnAnimationStartHit += pending.OnHit;
        }

        _remoteHits[attackerId] = pending;
        Debug.Log(
            $"[HIT_FX] ArmRemoteMelee attacker={attacker.name} id={attackerId} " +
            $"target={target.name} ss={pending.Soulshot} delay={delay:F3} " +
            $"hasEvents={(events != null)}");
    }

    public void FlushRemoteMeleeHit(Entity attacker)
    {
        if (attacker == null || attacker.Identity == null)
            return;
        TryFireRemoteMeleeHit(attacker.Identity.Id);
    }

    void TickRemoteMeleeHits()
    {
        if (_remoteHits.Count == 0)
            return;

        _remoteHitScratch.Clear();
        foreach (KeyValuePair<int, RemoteMeleeHit> kv in _remoteHits)
        {
            RemoteMeleeHit pending = kv.Value;
            if (pending == null || pending.Fired)
            {
                _remoteHitScratch.Add(kv.Key);
                continue;
            }

            if (pending.Attacker == null || pending.Attacker.IsDead())
            {
                _remoteHitScratch.Add(kv.Key);
                continue;
            }

            if (Time.time >= pending.FireAt)
                _remoteHitScratch.Add(kv.Key);
        }

        for (int i = 0; i < _remoteHitScratch.Count; i++)
        {
            int id = _remoteHitScratch[i];
            RemoteMeleeHit pending;
            if (!_remoteHits.TryGetValue(id, out pending) || pending == null)
                continue;
            if (pending.Fired || pending.Attacker == null || pending.Attacker.IsDead())
            {
                DropRemoteMeleeHit(id);
                continue;
            }

            TryFireRemoteMeleeHit(id);
        }
    }

    void TryFireRemoteMeleeHit(int attackerId)
    {
        RemoteMeleeHit pending;
        if (!_remoteHits.TryGetValue(attackerId, out pending) || pending == null || pending.Fired)
            return;

        pending.Fired = true;
        UnbindRemoteMeleeHit(pending);
        _remoteHits.Remove(attackerId);

        Entity attacker = pending.Attacker;
        Entity target = pending.Target;
        if (attacker == null || target == null)
            return;

        attacker.IsSoulshotCharged = pending.Soulshot;
        Vector3 hitPoint = ResolveTargetImpactCenter(target);
        Debug.Log(
            $"[HIT_FX] FireRemoteMelee attacker={attacker.name} id={attackerId} " +
            $"target={target.name} ss={pending.Soulshot} point={hitPoint}");
        HandleHitCollider(attacker, attacker.transform, target, hitPoint, Vector3.zero);
    }

    void DropRemoteMeleeHit(int attackerId)
    {
        RemoteMeleeHit pending;
        if (!_remoteHits.TryGetValue(attackerId, out pending))
            return;
        UnbindRemoteMeleeHit(pending);
        _remoteHits.Remove(attackerId);
    }

    void ClearRemoteMeleeHits()
    {
        _remoteHitScratch.Clear();
        foreach (KeyValuePair<int, RemoteMeleeHit> kv in _remoteHits)
            _remoteHitScratch.Add(kv.Key);
        for (int i = 0; i < _remoteHitScratch.Count; i++)
            DropRemoteMeleeHit(_remoteHitScratch[i]);
        _remoteHitScratch.Clear();
    }

    static void UnbindRemoteMeleeHit(RemoteMeleeHit pending)
    {
        if (pending == null || pending.Events == null)
            return;
        if (pending.OnShot != null)
            pending.Events.OnAnimationAttackShot -= pending.OnShot;
        if (pending.OnHit != null)
            pending.Events.OnAnimationStartHit -= pending.OnHit;
        pending.Events = null;
        pending.OnShot = null;
        pending.OnHit = null;
    }

    static bool IsBowWeapon(Entity attacker)
    {
        if (attacker == null)
            return false;

        UserEntity user = attacker as UserEntity;
        if (user != null)
            return string.Equals(user.WeaponAnim, "bow", StringComparison.OrdinalIgnoreCase);

        return attacker.Gear != null &&
               !string.IsNullOrEmpty(attacker.Gear.WeaponAnim) &&
               string.Equals(attacker.Gear.WeaponAnim, "bow", StringComparison.OrdinalIgnoreCase);
    }

    static float ResolveRemoteHitDelay(Entity attacker)
    {
        float pAtk = 333f;
        if (attacker != null && attacker.Stats != null)
        {
            if (attacker.Stats.PAtkRealSpeed > 1f)
                pAtk = attacker.Stats.PAtkRealSpeed;
            else if (attacker.Stats.PAtkSpd > 1)
                pAtk = attacker.Stats.PAtkSpd;
            else if (attacker.Stats.BasePAtkSpeed > 1f)
                pAtk = attacker.Stats.BasePAtkSpeed;
        }

        float cycleSec = TimeUtils.ConvertMsToSec(CalcBaseParam.CalculateTimeL2j(pAtk));
        return Mathf.Clamp(cycleSec * 0.75f, 0.12f, 1.5f);
    }

    public bool TryPrepareProjectileEffectHit(
        GameObject projectilePrefab,
        Vector3 hitPoint,
        Vector3 hitDirection,
        int attackerEntityId,
        Func<Transform, bool> isFromTrackedProjectile,
        out Vector3 resolvedHitPoint,
        out Vector3 resolvedHitDirection)
    {
        resolvedHitPoint = hitPoint;
        resolvedHitDirection = Vector3.forward;

        if (projectilePrefab == null || isFromTrackedProjectile == null)
        {
            return false;
        }

        Transform attacker = projectilePrefab.transform;
        if (attacker == null || !isFromTrackedProjectile(attacker))
        {
            return false;
        }

        Entity attackerEntity = ResolveEntityFromWorld(attackerEntityId);
        Entity targetEntity = null;
        if (attackerEntity != null && attackerEntity.TargetId > 0)
        {
            targetEntity = ResolveEntityFromWorld(attackerEntity.TargetId);
        }

        resolvedHitDirection = ResolveSoulshotImpactDirection(attackerEntity, targetEntity, resolvedHitPoint);
        if (resolvedHitDirection.sqrMagnitude < 0.0001f && hitDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 flat = hitDirection;
            flat.y = 0f;
            resolvedHitDirection = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        }

        Vector3 spawnPoint = ResolveSoulshotSpawnPoint(targetEntity, resolvedHitDirection, resolvedHitPoint);
        if (attackerEntity != null && attackerEntity.IsSoulshotCharged)
        {
            EffectManager.Instance.PlayerImpactEffect(SoulshotImpactEffectId, spawnPoint, resolvedHitDirection);
            attackerEntity.IsSoulshotCharged = false;
        }
        else
        {
            EffectManager.Instance.PlayerImpactEffect(NormalImpactEffectId, spawnPoint, resolvedHitDirection);
        }

        return true;
    }

    /// <summary>
    /// L2 Action_Attack: Rotation(targetLoc - attackerLoc) on XZ.
    /// Uses World entities — never sword / collider sweep direction.
    /// </summary>
    private static Vector3 ResolveSoulshotImpactDirection(Entity attacker, Entity target, Vector3 hitPointFallback)
    {
        Transform attackerTf = ResolveAttackerTransform(attacker);
        Transform targetTf = target != null ? target.transform : null;

        if (attackerTf != null && targetTf != null)
        {
            Vector3 toTarget = Flat(targetTf.position - attackerTf.position);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                return toTarget;
            }
        }

        if (attackerTf != null)
        {
            Vector3 toHit = Flat(hitPointFallback - attackerTf.position);
            if (toHit.sqrMagnitude > 0.0001f)
            {
                return toHit;
            }
        }

        return Vector3.forward;
    }

    /// <summary>
    /// L2 AssociateAttackedNotify: spawn at victim Location, then pull back along
    /// attacker→victim by ~CollisionRadius (not sword hit point, not bone attach).
    /// Prefer this over composite positionOffset — offset must scale per target.
    /// </summary>
    // Slightly less than full CollisionRadius — full radius sat a bit too close to the attacker.
    private const float SoulshotSpawnRadiusScale = 0.3f;

    private static Vector3 ResolveSoulshotSpawnPoint(Entity target, Vector3 impactDir, Vector3 hitPointFallback)
    {
        if (target == null)
        {
            return hitPointFallback;
        }

        Vector3 center = ResolveTargetImpactCenter(target);
        float radius = ResolveTargetCollisionRadius(target) * SoulshotSpawnRadiusScale;
        Vector3 dir = Flat(impactDir);
        if (dir.sqrMagnitude < 0.0001f || radius <= 0.0001f)
        {
            return center;
        }

        // L2: Location - Transform((CollisionRadius*k, 0, 0), Rotation) → toward attacker.
        return center - dir * radius;
    }

    private static float ResolveTargetCollisionRadius(Entity target)
    {
        if (target == null)
        {
            return 0.35f;
        }

        // Prefer CharacterController — already Unity meters (dog vs humanoid).
        CharacterController controller = ResolveCharacterController(target);
        if (controller != null)
        {
            float scaleX = Mathf.Abs(controller.transform.lossyScale.x);
            float fromCapsule = controller.radius * scaleX;
            if (fromCapsule > 0.01f)
            {
                return fromCapsule;
            }
        }

        // Appearance.CollisionRadius is inconsistent across spawn paths:
        // NpcgrpTable already divides by 52.5 (Unity meters); some packets store L2 UU.
        if (target.Appearance != null && target.Appearance.CollisionRadius > 0.0001f)
        {
            float raw = target.Appearance.CollisionRadius;
            // L2 UU radii are typically >= ~5; Unity meters after /52.5 are usually < ~2.
            float fromAppearance = raw > 3f
                ? VectorUtils.ConvertL2jDistance(raw)
                : raw;
            if (fromAppearance > 0.01f)
            {
                return fromAppearance;
            }
        }

        if (TryHorizontalBoundsExtent(target, out float extent) && extent > 0.01f)
        {
            return extent;
        }

        return 0.35f;
    }

    private static CharacterController ResolveCharacterController(Entity target)
    {
        if (target == null)
        {
            return null;
        }

        if (target is MonsterEntity monster)
        {
            CharacterController fromMonster = monster.GetCharacterController();
            if (fromMonster != null)
            {
                return fromMonster;
            }
        }
        else if (target is NpcEntity npc)
        {
            CharacterController fromNpc = npc.GetCharacterController();
            if (fromNpc != null)
            {
                return fromNpc;
            }
        }

        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = target.GetComponentInChildren<CharacterController>(true);
        }

        return controller;
    }

    private static bool TryHorizontalBoundsExtent(Entity target, out float extent)
    {
        extent = 0f;
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponentInParent<BaseEffect>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
        return extent > 0.01f;
    }

    private static Vector3 ResolveTargetImpactCenter(Entity target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        CharacterController controller = ResolveCharacterController(target);
        if (controller != null)
        {
            return controller.transform.TransformPoint(controller.center);
        }

        return target.transform.position;
    }

    private static Transform ResolveAttackerTransform(Entity attacker)
    {
        if (attacker != null && attacker.transform != null)
        {
            return attacker.transform;
        }

        return PlayerEntity.Instance != null ? PlayerEntity.Instance.transform : null;
    }

    private static Entity ResolveTargetEntity(Entity targetEntity)
    {
        return targetEntity;
    }

    private static void LogSoulshotImpactOrientation(
        Entity attacker,
        Entity target,
        Vector3 hitPoint,
        Vector3 spawnPoint,
        Vector3 resolvedImpactDir)
    {
        Transform attackerTf = ResolveAttackerTransform(attacker);
        Transform targetTf = target != null ? target.transform : null;

        Vector3 attackerPos = attackerTf != null ? attackerTf.position : Vector3.zero;
        Vector3 attackerFwd = Flat(attackerTf != null ? attackerTf.forward : Vector3.forward);
        Vector3 targetPos = targetTf != null ? targetTf.position : hitPoint;
        Vector3 targetFwd = Flat(targetTf != null ? targetTf.forward : Vector3.forward);
        float radius = ResolveTargetCollisionRadius(target);

        Vector3 playerToHit = Flat(hitPoint - attackerPos);
        Vector3 playerToTarget = Flat(targetPos - attackerPos);
        Vector3 resolved = Flat(resolvedImpactDir);

        float angResolvedVsP2T = AngleDeg(resolved, playerToTarget);
        float angResolvedVsP2H = AngleDeg(resolved, playerToHit);
        float angResolvedVsAttackerFwd = AngleDeg(resolved, attackerFwd);
        float angResolvedVsTargetFwd = AngleDeg(resolved, targetFwd);

        string source = attackerTf != null && targetTf != null ? "PLAYER_TO_TARGET_ENTITY" : "PLAYER_TO_HIT_FALLBACK";
        int attackerId = attacker != null && attacker.Identity != null ? attacker.Identity.Id : 0;
        int targetId = target != null && target.Identity != null ? target.Identity.Id : 0;

        Debug.Log(
            $"{IMPACT_DEBUG_TAG} soulshotOrient source={source} " +
            $"attackerId={attackerId} targetId={targetId} " +
            $"attackerPos={attackerPos} attackerFwd={attackerFwd} " +
            $"targetPos={targetPos} targetFwd={targetFwd} hitPoint={hitPoint} " +
            $"spawnPoint={spawnPoint} collisionRadius={radius:F3} " +
            $"playerToTarget={playerToTarget} playerToHit={playerToHit} resolved={resolved} " +
            $"angResolvedVsP2T={angResolvedVsP2T:F1} angResolvedVsP2H={angResolvedVsP2H:F1} " +
            $"angResolvedVsAttackerFwd={angResolvedVsAttackerFwd:F1} angResolvedVsTargetFwd={angResolvedVsTargetFwd:F1}");
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    private static float AngleDeg(Vector3 a, Vector3 b)
    {
        if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f)
        {
            return -1f;
        }

        return Vector3.Angle(a, b);
    }

    private Entity ResolveEntityFromWorld(int entityId)
    {
        World world = _world != null ? _world : World.Instance;
        if (entityId <= 0 || world == null)
            return null;

        return world.GetEntityNoLockSync(entityId);
    }

    private void HandleArrowHit(GameObject arrow, Transform target, Vector3 hitPointCollider, Vector3 hitDirection)
    {
        if (!target.CompareTag("Npc"))
        {
            return;
        }

        MonsterEntity entity = target.GetComponent<MonsterEntity>();
        if (entity == null)
        {
            return;
        }

        entity.AttachArrowToNearestBone(arrow, hitPointCollider, target, hitDirection);

        Collider arrowCollider = arrow.GetComponent<Collider>();
        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }
    }
}

public enum HitType
{
    Projectile,
    Melee
}
