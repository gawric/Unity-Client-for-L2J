using System;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager Instance { get; private set; }
    private const string ETC_NAME = "etc_";
    private const string IMPACT_DEBUG_TAG = "[HIT_DEBUG]";
    private const int SoulshotImpactEffectId = 99998;
    private const int NormalImpactEffectId = 99997;

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
        MonsterStateMachine targetStateMachine,
        Vector3 hitCollider,
        Vector3 hitColliderDirection)
    {
        string attackerName = attacker != null ? attacker.name : "null";
        Debug.Log(
            $"[HIT_FX] 6.HitManager.HandleHitCollider ENTER frame={Time.frameCount} t={Time.time:F3} " +
            $"attacker={attackerName} smNull={targetStateMachine == null} " +
            $"soulshot={(attaker != null && attaker.IsSoulshotCharged)} point={hitCollider}");

        if (targetStateMachine == null)
        {
            Debug.LogWarning("[HIT_FX] 6.HitManager SKIP targetStateMachine=null");
            return;
        }

        if (targetStateMachine.State == MonsterState.IDLE)
        {
            targetStateMachine.NotifyEvent(Event.HIT_REACTION);
        }

        Entity targetEntity = ResolveTargetEntity(targetStateMachine);
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

    private static Entity ResolveTargetEntity(MonsterStateMachine targetStateMachine)
    {
        if (targetStateMachine == null)
        {
            return null;
        }

        if (targetStateMachine.Entity != null)
        {
            return targetStateMachine.Entity;
        }

        return targetStateMachine.GetComponentInParent<Entity>();
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
        int attackerId = attacker != null && attacker.IdentityInterlude != null ? attacker.IdentityInterlude.Id : 0;
        int targetId = target != null && target.IdentityInterlude != null ? target.IdentityInterlude.Id : 0;

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
        if (entityId <= 0 || World.Instance == null)
        {
            return null;
        }

        return World.Instance.GetEntityNoLockSync(entityId);
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
