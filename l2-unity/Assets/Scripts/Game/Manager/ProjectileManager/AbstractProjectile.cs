using System;
using UnityEngine;
using UnityEngine.ProBuilder;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class AbstractProjectile : MonoBehaviour
{
    protected LayerMask _entityMask;
    // Constants for speed calculation
    protected const float SPEED_RANGE_1 = 8f;    // Speed up to 4 meters
    protected const float SPEED_RANGE_2_MAX = 11f; // Max speed in second range
    protected const float SPEED_RANGE_3_MAX = 12f; // Max speed in third range

    protected const float DISTANCE_SPLIT_1 = 4f;  // First distance split point
    protected const float DISTANCE_SPLIT_2 = 8f;  // Second distance split point
    protected const float DISTANCE_SPLIT_3 = 12f; // Third distance split point
    protected float HIT_OFFSET = 0.3f;


    public event Action<Transform, Transform, Vector3, Vector3> OnHitCollider;
    protected float GetSpeed(float distance)
    {
        float speed;
        if (distance <= DISTANCE_SPLIT_1)
        {
            //HIT_OFFSET = 0.6f;
            speed = SPEED_RANGE_1;
        }
        else if (distance <= DISTANCE_SPLIT_2)
        {
            speed = SPEED_RANGE_1 + (distance - DISTANCE_SPLIT_1) *
                   ((SPEED_RANGE_2_MAX - SPEED_RANGE_1) / (DISTANCE_SPLIT_2 - DISTANCE_SPLIT_1));
        }
        else if (distance <= DISTANCE_SPLIT_3)
        {
            speed = SPEED_RANGE_2_MAX + (distance - DISTANCE_SPLIT_2) *
                   ((SPEED_RANGE_3_MAX - SPEED_RANGE_2_MAX) / (DISTANCE_SPLIT_3 - DISTANCE_SPLIT_2));
        }
        else
        {
            speed = SPEED_RANGE_3_MAX;
        }
        return speed;
    }
    protected float CalculateFlightTime(float distance , float speed)
    {
        float flightTime = (distance / speed);
        return distance >= 4 ? Mathf.Max(flightTime - HIT_OFFSET, 0.1f) : Mathf.Max(flightTime, 0.1f);
    }

    public ProjectileData CreateData(int projectileId , float distance, GameObject readyProjectile , Vector3 startPos , Transform target , Vector3 adjustedTarget , float requiredSpeed , ProjectileData settings, ProjectileData defaultSettings)
    {
        return new ProjectileData
        {
            id = projectileId,
            prefab = readyProjectile,
            transform = readyProjectile.transform,
            startPosition = startPos,
            targetTransform = target,
            targetPosition = adjustedTarget,
            speed = requiredSpeed,
            damage = settings?.damage ?? defaultSettings.damage,
            lifetime = settings?.lifetime ?? defaultSettings.lifetime,
            speedCurve = settings?.speedCurve ?? defaultSettings.speedCurve,
            startTime = Time.time,
            distance = distance,
            useGravity = settings?.useGravity ?? defaultSettings.useGravity,
            gravityScale = settings?.gravityScale ?? defaultSettings.gravityScale,
            hitPointCollider = Vector3.zero,
            hitNormalCollider = Vector3.zero,
            isActive = true
        };
    }

    protected void ClearParentObject(GameObject readyProjectile)
    {
        if (readyProjectile.transform.parent != null)
            readyProjectile.transform.SetParent(null);
    }

    protected void CheckProjectileCollision(ProjectileData projectile, Vector3 currentPosition, Vector3 lastPosition)
    {
        if (projectile.targetTransform == null || projectile.hitPointCollider != Vector3.zero)
            return;

        if (lastPosition != Vector3.zero)
        {

            float extraMargin = 0.2f;
            Vector3 movementVector = currentPosition - lastPosition;
            float distance = movementVector.magnitude + extraMargin;
            float realDistance = movementVector.magnitude;
            Vector3 direction = movementVector.normalized;


            if (distance > 0)
            {
                float distanceNpc = Vector3.Distance(projectile.startPosition, projectile.targetTransform.position);
                if (distanceNpc > 2)
                {
                    SphereCast(lastPosition, direction, distance, projectile);
                }
                else if (distanceNpc <= 2)
                {
                    RayCast(lastPosition, direction, movementVector, projectile);
                }
            }
        }
    }

    private void SphereCast(Vector3 lastPosition , Vector3 direction , float distance , ProjectileData projectile)
    {
        float sphereRadius = 0.3f;
        RaycastHit hitSphereCast;
        if (Physics.SphereCast(lastPosition, sphereRadius, direction, out hitSphereCast, distance, _entityMask))
        {
            var hitPos = hitSphereCast.point;
            OnHit(hitSphereCast, projectile);
        }
    }

    private void RayCast(Vector3 lastPosition , Vector3 direction , Vector3 movementVector , ProjectileData projectile)
    {
        RaycastHit hitRaycast;
        float distanceRaycast = movementVector.magnitude + 0.01f;
        if (Physics.Raycast(lastPosition, direction, out hitRaycast, distanceRaycast, _entityMask))
        {
            var hitPos = hitRaycast.point;
            OnHit(hitRaycast, projectile);
        }
    }

    private void OnHit(RaycastHit hit, ProjectileData projectile)
    {
        GameObject gameObject = hit.transform.parent.gameObject;
        Entity entity = gameObject.GetComponent<Entity>();
        GameObject targetGameObject = projectile.targetTransform.gameObject;
        Entity targetEntity = targetGameObject.GetComponent<Entity>();

        if (targetEntity == null || entity == null) return;

        if (entity.IdentityInterlude.Id == targetEntity.IdentityInterlude.Id)
        {

            projectile.hitPointCollider = hit.point;
            projectile.hitNormalCollider = hit.normal;
            projectile.hitDirection = VectorUtils.CalcHitDirection(hit.point, projectile.startPosition);



            OnHitCollider?.Invoke(projectile.prefab.transform, projectile.targetTransform, projectile.hitPointCollider, projectile.hitDirection);
        }
    }

    protected Quaternion GetRotation(Vector3 adjustedTarget, Vector3 startPos)
    {
        Vector3 direction = adjustedTarget - startPos;
        Quaternion rotation = Quaternion.LookRotation(direction);
        return rotation *= Quaternion.Euler(0, 90, 0);
    }
    protected void SetPosition(GameObject readyProjectile, Vector3 startPos)
    {
        readyProjectile.transform.position = startPos;
    }
    protected void SetRotation(GameObject readyProjectile, Quaternion rotation)
    {
        readyProjectile.transform.rotation = rotation;
    }
    protected void SetRotation(ProjectileData projectile, Quaternion rotation)
    {
        projectile.transform.rotation = rotation;
    }

    protected void SetPosition(ProjectileData projectile, Vector3 targetPosition)
    {
        projectile.transform.position = targetPosition;
    }

    protected Vector3 GetCurrentPosition(ProjectileData projectile, float journeyProgress)
    {
        return Vector3.Lerp(
             projectile.startPosition,
             projectile.targetPosition,
             journeyProgress
         );
    }

    protected void RefreshHitPosition(ProjectileData projectile)
    {
        projectile.hitPoint = projectile.targetPosition;
        projectile.hitNormal = Vector3.up;
        projectile.hitDirection = VectorUtils.CalcHitDirection(projectile.targetPosition, projectile.startPosition);
    }

    protected float GetCurveSpeed(ProjectileData projectile, float baseJourneyProgress, float speedMultiplier)
    {
        return projectile.speedCurve?.Evaluate(baseJourneyProgress) ?? speedMultiplier;
    }

    protected void CalcNewTargetPosition(ProjectileData projectile)
    {
        if (projectile.targetTransform != null)
        {

            Vector3 previousTargetPosition = projectile.targetPosition;
            Vector3 newTargetPosition = VectorUtils.GetCollision(projectile.startPosition, projectile.targetTransform);
            float moveThreshold = 0.1f;

            if (Vector3.Distance(previousTargetPosition, newTargetPosition) > moveThreshold)
            {
                projectile.targetPosition = newTargetPosition;
            }
        }
    }
}
