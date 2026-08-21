
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using VContainer;

public class ProjectileManager : AbstractProjectile, IProjectileManager
{
    [Inject] HitManager _hits;
    private const string PROJECTILE_TIMER_LOG = "[PROJECTILE_TIMER]";
    private const string MagicProjectileSyncTag = "[MAGIC_PROJECTILE_SYNC]";

    [SerializeField] public ProjectileData defaultSettings;
    public event Action<GameObject, Transform, Vector3, Vector3, int> OnHitMonster;
    public event Action<GameObject, Transform, Vector3, Vector3, int> OnHitEffectProjectile;

    private Dictionary<int, ProjectileData> activeProjectiles = new Dictionary<int, ProjectileData>();
    private int nextId = 0;

    #region Singleton
    public static IProjectileManager Instance { get; private set; }



    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _entityMask = LayerMask.GetMask("EntityClick");
        Instance = this;
    }

    private void Start()
    {
        HitManager hits = _hits != null ? _hits : HitManager.Instance;
        if (hits != null)
            hits.BindProjectileHits();
    }
    #endregion

    public int LaunchProjectile(GameObject readyProjectile, Vector3 startPos, Transform target, ProjectileData settings = null, float offset = 0.3f)
    {
        if (readyProjectile == null)
        {
            Debug.LogError("Ready projectile cannot be null!");
            return -1;
        }
        HIT_OFFSET = offset;
        int attackerEntityId = ResolveProjectileAttackerEntityId(readyProjectile);
        ClearParentObject(readyProjectile);

        Vector3 adjustedTarget = VectorUtils.GetCollision(startPos, target);

        float distance = Vector3.Distance(startPos, adjustedTarget);
        bool isArrowStick = settings != null && settings.impactType == ProjectileImpactType.ArrowStick;

        float speed;
        float flightTime;
        // NArrow + skill bolt share ANProjectile accel (dirMul=3000).
        speed = ProjectileFlightTimeCalculator.L2ProjectileAccelUnityPerSec2;
        flightTime = ProjectileFlightTimeCalculator.CalculateL2AccelFlightTimeSeconds(distance);
        if (isArrowStick)
        {
            flightTime = ResolveArrowStickFlightTime(settings, flightTime);
        }

        float requiredSpeed = distance / Mathf.Max(flightTime, 0.01f);
        int projectileId = nextId++;

        if (isArrowStick)
        {
            float settingsFly = settings != null ? settings.flytime : -1f;
            float flyIfDist1500 =
                ProjectileFlightTimeCalculator.CalculateL2ArrowFlightTimeIfConstantSpeed(distance);
            Debug.Log(
                $"[BOW_ARROW] LAUNCH id={projectileId} dist3d={distance:F3} " +
                $"accel={speed:F3} avgSpeed={requiredSpeed:F3} flySec={flightTime:F3} " +
                $"settingsFly={settingsFly:F3} flyIfDist1500={flyIfDist1500:F3} " +
                $"start={startPos} aim={adjustedTarget} uuAccel=3000 path=(t/T)^2");
        }
        else
        {
            Debug.Log(
                $"[MAGIC_PROJ] LAUNCH id={projectileId} dist3d={distance:F3} " +
                $"accel={speed:F3} avgSpeed={requiredSpeed:F3} flySec={flightTime:F3} " +
                $"impact={(settings != null ? settings.impactType.ToString() : "null")} " +
                $"uuAccel=3000 path=(t/T)^2 uu→unity=/52.5");
        }

        ProjectileData projectileData = CreateData(projectileId, distance, readyProjectile, startPos, target,
            adjustedTarget, requiredSpeed, settings, defaultSettings);
        projectileData.attackerEntityId = attackerEntityId;


        projectileData.flytime = flightTime;
        projectileData.lifetime = flightTime;
        projectileData.lastPosition = Vector3.zero;
        AttachCastTimingSnapshot(projectileData);
        SetPosition(readyProjectile, startPos);
        var rotation = GetRotation(adjustedTarget, startPos);
        SetRotation(readyProjectile, rotation);
 
        activeProjectiles[projectileId] = projectileData;
        return projectileId;
    }

    /// <summary>
    /// Prefer shoot-time accel flytime; reject ProjectileData default 5s.
    /// </summary>
    private static float ResolveArrowStickFlightTime(ProjectileData settings, float fallbackFlightTime)
    {
        if (settings == null)
        {
            return fallbackFlightTime;
        }

        float configured = settings.flytime;
        if (configured > 0.05f && configured < 4.5f)
        {
            return configured;
        }

        return fallbackFlightTime;
    }

    private void AttachCastTimingSnapshot(ProjectileData projectile)
    {
        MagicCastData castData = ResolveLaunchCastData(projectile);
        if (castData == null)
        {
            projectile.castStartTimeSnapshot = -1f;
            projectile.castServerShootSnapshot = -1f;
            projectile.castServerHitSnapshot = -1f;
            projectile.projectileLaunchGlobalFromCast = -1f;
            return;
        }

        projectile.castStartTimeSnapshot = castData.StartTime;
        projectile.castServerShootSnapshot = castData.serverTimeToShoot;
        projectile.castServerHitSnapshot = castData.HitTime;
        projectile.projectileLaunchGlobalFromCast = Time.time - castData.StartTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"{MagicProjectileSyncTag} LAUNCH id={projectile.id} impactType={projectile.impactType} " +
            $"launchGlobalFromCast={projectile.projectileLaunchGlobalFromCast:F3}s " +
            $"serverShoot={projectile.castServerShootSnapshot:F3}s serverHit={projectile.castServerHitSnapshot:F3}s " +
            $"deltaLaunchToShoot={projectile.projectileLaunchGlobalFromCast - projectile.castServerShootSnapshot:F3}s " +
            $"configuredFly={projectile.flytime:F3}s distance={projectile.distance:F3}m.");
#endif
    }

    static MagicCastData ResolveLaunchCastData(ProjectileData projectile)
    {
        if (projectile != null && projectile.attackerEntityId != 0)
        {
            Entity caster = EntityActionSkill.ResolveEntity(projectile.attackerEntityId);
            if (caster != null)
            {
                MagicCastData fromCaster = caster.GetMagicCastData();
                if (fromCaster != null)
                    return fromCaster;
            }
        }

        return PlayerEntity.Instance != null ? PlayerEntity.Instance.GetMagicCastData() : null;
    }


    private void Update()
    {

        List<int> projectilesToRemove = new List<int>();

        foreach (var pair in activeProjectiles)
        {
            int projectileId = pair.Key;
            ProjectileData projectile = pair.Value;
            try
            {
                if (!projectile.isActive || !UpdateProjectile(projectile))
                {
                    projectilesToRemove.Add(projectileId);
                }
            }
            catch (InvalidCastException ex)
            {
                Debug.LogError($"Error updating projectile {projectileId}: {ex.Message}");
                projectilesToRemove.Add(projectileId);
            }
            catch (MissingReferenceException ex)
            {
                Debug.LogError(
                    $"[ProjectileUpdateException] MissingReference id={projectileId} " +
                    $"prefabNull={(projectile?.prefab == null)} transformNull={(projectile?.transform == null)} " +
                    $"targetNull={(projectile?.targetTransform == null)} msg={ex.Message}");
                projectilesToRemove.Add(projectileId);
            }


        }

        foreach (int projectileId in projectilesToRemove)
        {
            if (activeProjectiles.TryGetValue(projectileId, out ProjectileData projectile))
            {
                if (projectile.prefab != null)
                {
                    //Destroy(projectile.prefab);
                }
                activeProjectiles.Remove(projectileId);
            }
        }
    }

    private bool UpdateProjectile(ProjectileData projectile)
    {
        if (!projectile.isActive) return false;
        if (projectile.transform == null)
        {
            Debug.LogWarning(
                $"[ProjectileUpdate] destroyed transform id={projectile.id} prefabNull={(projectile.prefab == null)} " +
                $"targetNull={(projectile.targetTransform == null)} impactType={projectile.impactType} now={Time.time:F3}");
            return false;
        }

       
        CalcNewTargetPosition(projectile);

        float elapsed = Time.time - projectile.startTime;
        float timeProgress = elapsed / Mathf.Max(projectile.flytime, 0.01f);

        // NArrow + skill: Accel=dir*3000 from rest → path (t/T)² (not linear).
        float journeyProgress = ProjectileFlightTimeCalculator.CalculateL2AccelJourneyProgress(
            elapsed, projectile.flytime);

        Vector3 currentPosition = GetCurrentPosition(projectile, journeyProgress);
  
        RefreshHitPosition(projectile);

        // Hit Time = end of scheduled flytime (wall clock), not collider.
        if (timeProgress >= 1f)
        {
            // ArrowStick / EffectOnly: Hit Time = end of flytime. No NPC collider dependency.
            SetPosition(projectile, projectile.targetPosition);
            projectile.lastPosition = currentPosition;
            RefreshHitPositionFromAnchor(projectile);
            projectile.hitPointCollider = projectile.hitPoint;
            LogProjectileImpactTiming(projectile);

            if (projectile.impactType == ProjectileImpactType.EffectOnly)
            {
                bool hasSubscribers = OnHitEffectProjectile != null;
                float gs = projectile.castStartTimeSnapshot > 0f ? Time.time - projectile.castStartTimeSnapshot : -1f;
                Debug.Log(
                    $"{PROJECTILE_TIMER_LOG} EffectOnlyTimeHit id={projectile.id} " +
                    $"elapsedSec={(Time.time - projectile.startTime):F3} flyTimeSec={projectile.flytime:F3} " +
                    $"eventSubscribers={hasSubscribers} hitPoint={projectile.hitPoint}");
                Debug.Log(
                    $"{MagicProjectileSyncTag} EFFECT_ONLY_HIT_FIRE id={projectile.id} " +
                    $"globalSinceCast={gs:F3}s serverHit={projectile.castServerHitSnapshot:F3}s " +
                    $"deltaToServerHit={(gs >= 0f ? gs - projectile.castServerHitSnapshot : -1f):F3}s " +
                    $"elapsedSinceLaunch={(Time.time - projectile.startTime):F3}s");
                OnHitEffectProjectile?.Invoke(
                    projectile.prefab,
                    projectile.targetTransform,
                    projectile.hitPoint,
                    projectile.hitDirection,
                    projectile.attackerEntityId);
            }
            else
            {
                // Bow: Soulshot / shoot impact on Hit Time, then stick arrow.
                Debug.Log(
                    $"{PROJECTILE_TIMER_LOG} ArrowStickTimeHit id={projectile.id} " +
                    $"elapsedSec={elapsed:F3} flyTimeSec={projectile.flytime:F3} " +
                    $"hitPoint={projectile.hitPoint}");

                OnHitMonster?.Invoke(
                    projectile.prefab,
                    projectile.targetTransform,
                    projectile.hitPoint,
                    projectile.hitDirection,
                    projectile.attackerEntityId);
            }
            return false;
        }

        SetPosition(projectile, currentPosition);

        if (projectile.targetTransform != null)
        {
            Vector3 dir = projectile.targetPosition - projectile.transform.position;
            Quaternion rotation = Quaternion.LookRotation(dir);
            rotation *= Quaternion.Euler(0, 90, 0);
            SetRotation(projectile , rotation);
        }



        projectile.lastPosition = currentPosition;
        return true;
    }

    private int ResolveProjectileAttackerEntityId(GameObject projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            return 0;
        }

        Entity ownerEntity = projectilePrefab.GetComponentInParent<Entity>();
        if (ownerEntity != null && ownerEntity.Identity != null)
        {
            return ownerEntity.Identity.Id;
        }

        if (PlayerEntity.Instance != null && PlayerEntity.Instance.Identity != null)
        {
            return PlayerEntity.Instance.Identity.Id;
        }

        return 0;
    }

    private void RefreshHitPositionFromAnchor(ProjectileData projectile)
    {
        if (projectile == null)
        {
            return;
        }

        Transform fallbackTarget = projectile.targetTransform;
        Entity targetEntity = fallbackTarget != null ? fallbackTarget.GetComponentInParent<Entity>() : null;
        Transform anchor = HitAnchorResolver.ResolveHitAnchor(targetEntity, fallbackTarget);
        Vector3 hitPoint = anchor != null ? anchor.position : projectile.targetPosition;

        projectile.hitPoint = hitPoint;
        projectile.hitNormal = Vector3.up;
        projectile.hitDirection = VectorUtils.CalcHitDirection(hitPoint, projectile.startPosition);
    }

    private void LogProjectileImpactTiming(ProjectileData projectile)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float projectileElapsed = Time.time - projectile.startTime;
        if (projectile.castStartTimeSnapshot <= 0f)
        {
        Debug.Log(
            $"{MagicProjectileSyncTag} IMPACT id={projectile.id} impactType={projectile.impactType} " +
            $"globalSinceCastStart=-1 castSnapshot=null flytime={projectile.flytime:F3}s elapsed={projectileElapsed:F3}s.");
            return;
        }

        float globalSinceCastStart = Time.time - projectile.castStartTimeSnapshot;
        Debug.Log(
            $"{MagicProjectileSyncTag} IMPACT id={projectile.id} impactType={projectile.impactType} " +
            $"launchGlobalFromCast={projectile.projectileLaunchGlobalFromCast:F3}s " +
            $"globalSinceCastStart={globalSinceCastStart:F3}s serverShoot={projectile.castServerShootSnapshot:F3}s serverHit={projectile.castServerHitSnapshot:F3}s " +
            $"deltaLaunchToShoot={projectile.projectileLaunchGlobalFromCast - projectile.castServerShootSnapshot:F3}s " +
            $"deltaToServerHit={globalSinceCastStart - projectile.castServerHitSnapshot:F3}s " +
            $"configuredFly={projectile.flytime:F3}s elapsedSinceLaunch={projectileElapsed:F3}s.");
#endif
    }

    public void StopProjectile(int projectileId)
    {
        if (activeProjectiles.TryGetValue(projectileId, out ProjectileData projectile))
        {
            projectile.isActive = false;
            if (projectile.prefab != null)
            {
                //Destroy(projectile.prefab);
            }
            activeProjectiles.Remove(projectileId);
        }
    }



}










