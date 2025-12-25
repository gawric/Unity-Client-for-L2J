using UnityEngine;
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
    protected float HIT_OFFSET = 0.4f;

    protected float CalculateFlightTime(float distance)
    {
        float speed;
        if (distance <= DISTANCE_SPLIT_1)
        {
            HIT_OFFSET = 0.6f;
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
}
