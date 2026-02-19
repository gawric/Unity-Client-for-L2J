
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;


public class ProjectileManager : AbstractProjectile, IProjectileManager
{
    [SerializeField] public ProjectileData defaultSettings;
    public event Action<GameObject, Transform, Vector3, Vector3> OnHitMonster;
    private Vector3 _lastPosition = Vector3.zero;

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
        ClearParentObject(readyProjectile);

        Vector3 adjustedTarget = VectorUtils.GetCollision(startPos, target);

        float distance = Vector3.Distance(startPos, adjustedTarget);
        float speed = GetSpeed(distance);
        float flightTime = CalculateFlightTime(distance, speed);
        float requiredSpeed = distance / flightTime;
        _lastPosition = Vector3.zero;
        int projectileId = nextId++;

        Debug.Log($"CalculateAttackAndFlightTimes: LaunchProjectile dist={distance}, speed={speed}, fly={flightTime}");

        ProjectileData projectileData = CreateData(projectileId, distance, readyProjectile, startPos, target,
            adjustedTarget, requiredSpeed, settings, defaultSettings);


        projectileData.flytime = flightTime;
        SetPosition(readyProjectile, startPos);
        var rotation = GetRotation(adjustedTarget, startPos);
        SetRotation(readyProjectile, rotation);
 
        activeProjectiles[projectileId] = projectileData;
        return projectileId;
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
            CheckProjectileCollision(projectile, currentPosition, _lastPosition);
            SetPosition(projectile, projectile.targetPosition);
            _lastPosition = currentPosition;
            RefreshHitPosition(projectile);


            OnHitMonster?.Invoke(projectile.prefab, projectile.targetTransform, projectile.hitPoint, projectile.hitDirection);
            return false;
        }

        CheckProjectileCollision(projectile, currentPosition, _lastPosition);
        SetPosition(projectile, currentPosition);

        if (projectile.targetTransform != null)
        {
            Vector3 dir = projectile.targetPosition - projectile.transform.position;
            Quaternion rotation = Quaternion.LookRotation(dir);
            rotation *= Quaternion.Euler(0, 90, 0);
            SetRotation(projectile , rotation);
        }



        _lastPosition = currentPosition;
        return true;
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










