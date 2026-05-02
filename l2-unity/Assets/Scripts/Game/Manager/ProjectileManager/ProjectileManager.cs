
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;


public class ProjectileManager : AbstractProjectile, IProjectileManager
{
    private const string PROJECTILE_TIMER_LOG = "[PROJECTILE_TIMER]";
    [SerializeField] public ProjectileData defaultSettings;
    public event Action<GameObject, Transform, Vector3, Vector3> OnHitMonster;
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
        float speed = GetSpeed(distance);
        float flightTime = CalculateFlightTime(distance, speed);
        flightTime = ResolveVisibleFlightTimeForEffectOnly(settings, flightTime);
        float requiredSpeed = distance / flightTime;
        int projectileId = nextId++;

        Debug.Log($"CalculateAttackAndFlightTimes: LaunchProjectile dist={distance}, speed={speed}, fly={flightTime}");

        ProjectileData projectileData = CreateData(projectileId, distance, readyProjectile, startPos, target,
            adjustedTarget, requiredSpeed, settings, defaultSettings);
        projectileData.attackerEntityId = attackerEntityId;


        projectileData.flytime = flightTime;
        projectileData.lastPosition = Vector3.zero;
        AttachCastTimingSnapshot(projectileData);
        SetPosition(readyProjectile, startPos);
        var rotation = GetRotation(adjustedTarget, startPos);
        SetRotation(readyProjectile, rotation);
 
        activeProjectiles[projectileId] = projectileData;
        return projectileId;
    }

    private static float ResolveVisibleFlightTimeForEffectOnly(ProjectileData settings, float fallbackFlightTime)
    {
        if (settings == null || settings.impactType != ProjectileImpactType.EffectOnly)
        {
            return fallbackFlightTime;
        }

        float minVisibleFlightTime = 0.35f;
        MagicCastData castData = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetMagicCastData() : null;
        if (castData != null && castData.FlightTime > 0f)
        {
            return Mathf.Max(fallbackFlightTime, minVisibleFlightTime, castData.FlightTime);
        }

        return Mathf.Max(fallbackFlightTime, minVisibleFlightTime);
    }

    private void AttachCastTimingSnapshot(ProjectileData projectile)
    {
        MagicCastData castData = PlayerEntity.Instance != null ? PlayerEntity.Instance.GetMagicCastData() : null;
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
            $"[ProjectileLaunchTiming] id={projectile.id} impactType={projectile.impactType} " +
            $"launchGlobalFromCast={projectile.projectileLaunchGlobalFromCast:F3}s " +
            $"serverShoot={projectile.castServerShootSnapshot:F3}s serverHit={projectile.castServerHitSnapshot:F3}s " +
            $"deltaToServerShoot={projectile.projectileLaunchGlobalFromCast - projectile.castServerShootSnapshot:F3}s " +
            $"configuredFly={projectile.flytime:F3}s distance={projectile.distance:F3}m.");
#endif
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

        float baseJourneyProgress = (Time.time - projectile.startTime) / projectile.flytime;


        float speedMultiplier = 0.3f;
        speedMultiplier = GetCurveSpeed(projectile, baseJourneyProgress , speedMultiplier);



        float journeyProgress = baseJourneyProgress * speedMultiplier;


        journeyProgress = Mathf.Clamp01(journeyProgress);

        Vector3 currentPosition = GetCurrentPosition(projectile, journeyProgress);
  
        RefreshHitPosition(projectile);

        if (journeyProgress >= 1f)
        {
            if (projectile.impactType != ProjectileImpactType.EffectOnly)
            {
                CheckProjectileCollision(projectile, currentPosition, projectile.lastPosition);
            }
            SetPosition(projectile, projectile.targetPosition);
            projectile.lastPosition = currentPosition;
            if (projectile.impactType == ProjectileImpactType.EffectOnly)
            {
                RefreshHitPositionFromAnchor(projectile);
            }
            else
            {
                RefreshHitPosition(projectile);
            }
            LogProjectileImpactTiming(projectile);

            if (projectile.impactType == ProjectileImpactType.EffectOnly)
            {
                bool hasSubscribers = OnHitEffectProjectile != null;
                Debug.Log(
                    $"{PROJECTILE_TIMER_LOG} EffectOnlyTimeHit id={projectile.id} " +
                    $"elapsedSec={(Time.time - projectile.startTime):F3} flyTimeSec={projectile.flytime:F3} " +
                    $"eventSubscribers={hasSubscribers} hitPoint={projectile.hitPoint}");
                OnHitEffectProjectile?.Invoke(
                    projectile.prefab,
                    projectile.targetTransform,
                    projectile.hitPoint,
                    projectile.hitDirection,
                    projectile.attackerEntityId);
            }
            else
            {
                OnHitMonster?.Invoke(projectile.prefab, projectile.targetTransform, projectile.hitPoint, projectile.hitDirection);
            }
            return false;
        }

        if (projectile.impactType != ProjectileImpactType.EffectOnly)
        {
            CheckProjectileCollision(projectile, currentPosition, projectile.lastPosition);
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
        if (ownerEntity != null && ownerEntity.IdentityInterlude != null)
        {
            return ownerEntity.IdentityInterlude.Id;
        }

        if (PlayerEntity.Instance != null && PlayerEntity.Instance.IdentityInterlude != null)
        {
            return PlayerEntity.Instance.IdentityInterlude.Id;
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
                $"[ProjectileImpactTiming] id={projectile.id} impactType={projectile.impactType} " +
                $"globalSinceCastStart=-1 castDataSnapshot=null flytime={projectile.flytime:F3}s elapsed={projectileElapsed:F3}s.");
            return;
        }

        float globalSinceCastStart = Time.time - projectile.castStartTimeSnapshot;
        Debug.Log(
            $"[ProjectileImpactTiming] id={projectile.id} impactType={projectile.impactType} " +
            $"launchGlobalFromCast={projectile.projectileLaunchGlobalFromCast:F3}s " +
            $"globalSinceCastStart={globalSinceCastStart:F3}s serverShoot={projectile.castServerShootSnapshot:F3}s serverHit={projectile.castServerHitSnapshot:F3}s " +
            $"deltaLaunchToServerShoot={projectile.projectileLaunchGlobalFromCast - projectile.castServerShootSnapshot:F3}s " +
            $"deltaToServerHit={globalSinceCastStart - projectile.castServerHitSnapshot:F3}s " +
            $"projectileFlyConfigured={projectile.flytime:F3}s projectileElapsed={projectileElapsed:F3}s.");
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










