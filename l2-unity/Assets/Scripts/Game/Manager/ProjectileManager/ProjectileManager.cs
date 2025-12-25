using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class ProjectileManager : AbstractProjectile , IProjectileManager
{
    [SerializeField] public ProjectileData defaultSettings;
    public event Action<GameObject , Transform, Vector3, Vector3> OnHitMonster;
    public event Action<Transform, Vector3, Vector3> OnHitCollider;

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

    public int LaunchProjectile(GameObject readyProjectile, Vector3 startPos, Transform target, ProjectileData settings = null)
    {
        if (readyProjectile == null)
        {
            Debug.LogError("Ready projectile cannot be null!");
            return -1;
        }

        ClearParentObject(readyProjectile);

        Vector3 adjustedTarget = VectorUtils.GetCollision(startPos, target);
        float distance = Vector3.Distance(startPos, adjustedTarget);
        float flightTime = CalculateFlightTime(distance);
        float requiredSpeed = distance / flightTime;

        int projectileId = nextId++;
        ProjectileData projectileData = CreateData(projectileId, distance, readyProjectile, startPos, target,
            adjustedTarget, requiredSpeed, settings, defaultSettings);

        if (distance <= 4f)
        {
            projectileData.speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        }

        readyProjectile.transform.position = startPos;
        Vector3 direction = adjustedTarget - startPos;
        Quaternion rotation = Quaternion.LookRotation(direction);
        rotation *= Quaternion.Euler(0, 90, 0);
        readyProjectile.transform.rotation = rotation;

        activeProjectiles[projectileId] = projectileData;
        return projectileId;
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

        if (projectile.targetTransform != null)
        {
            projectile.targetPosition = VectorUtils.GetCollision(projectile.transform.position, projectile.targetTransform);
        }

        float flightTime = CalculateFlightTime(projectile.distance);
        float fractionOfJourney = (Time.time - projectile.startTime) / flightTime;
        Vector3 currentPosition = Vector3.Lerp(
            projectile.startPosition,
            projectile.targetPosition,
            fractionOfJourney
        );

        // Check for collision
        CheckProjectileCollision(projectile, currentPosition);

        if (fractionOfJourney >= 1f)
        {

            projectile.hitPoint = projectile.targetPosition;
            projectile.hitNormal = Vector3.up;
            projectile.hitDirection = VectorUtils.CalcHitDirection(currentPosition, projectile.startPosition);


            if (projectile.prefab == null & projectile.targetTransform == null)
            {
                Debug.LogError("Prefab is null before OnHit invoke");
                return false;
            }

            OnHitMonster?.Invoke(projectile.prefab, projectile.targetTransform, projectile.hitPointCollider, projectile.hitDirection);
            return false;
        }

        projectile.transform.position = currentPosition;

        if (projectile.targetTransform != null)
        {
            Vector3 direction = projectile.targetPosition - projectile.transform.position;
            Quaternion rotation = Quaternion.LookRotation(direction);
            rotation *= Quaternion.Euler(0, 90, 0);
            projectile.transform.rotation = rotation;
        }

        return true;
    }

    private void CheckProjectileCollision(ProjectileData projectile, Vector3 currentPosition)
    {
        if (projectile.targetTransform == null || projectile.hitPointCollider != Vector3.zero)
            return;

        Vector3 rayDirection = (currentPosition - projectile.startPosition).normalized;
        float distanceToTarget = Vector3.Distance(currentPosition, projectile.targetPosition);


            RaycastHit hit;
     
            if (Physics.Raycast(
                currentPosition,
                rayDirection,
                out hit,
                distanceToTarget,
                _entityMask))
            {
                GameObject gameObject = hit.transform.parent.gameObject;
                Entity entity = gameObject.GetComponent<Entity>();

                GameObject targetGameObject = projectile.targetTransform.gameObject;
                Entity targetEntity = targetGameObject.GetComponent<Entity>();

                if (targetEntity == null | entity == null) return;

                if (entity.IdentityInterlude.Id == targetEntity.IdentityInterlude.Id)
                {
                    projectile.hitPointCollider = hit.point;
                    projectile.hitNormalCollider = hit.normal;
                    projectile.hitDirection = VectorUtils.CalcHitDirection(hit.point, projectile.startPosition);
                    OnHitCollider?.Invoke(projectile.targetTransform , projectile.hitPointCollider , projectile.hitDirection);
                }
            
        }
    }

   

}
