using UnityEngine;

public class ProjectileData
{
    public ProjectileData()
    {

        id = 0;
        prefab = null;
        transform = null;
        startPosition = Vector3.zero;
        targetTransform = null;
        speed = 8f;
        lifetime = 5f;
        flytime = 5f;
        //speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        speedCurve = GetDefaultCurve();
        startTime = 0f;
        distance = 0f;
        useGravity = false;
        gravityScale = 1f;
        isActive = true;
    }

    public ProjectileData(GameObject prefabGo , Transform transform1 , Vector3 startPos , Transform endPos)
    {
        id = 0;
        prefab = prefabGo;
        transform = transform1; 
        startPosition = startPos;
        targetTransform = endPos;
        speed = 8f;
        lifetime = 5f;
        flytime = 5f;
        //speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        speedCurve = GetDefaultCurve();
        startTime = 0f;
        distance = 0f;
        useGravity = false;
        gravityScale = 1f;
        isActive = true;
    }

    public ProjectileData(ProjectileData other)
    {
        if (other != null)
        {
            id = other.id;
            prefab = other.prefab;
            transform = other.transform;
            startPosition = other.startPosition;
            targetTransform = other.targetTransform;
            speed = other.speed;
            damage = other.damage;
            lifetime = other.lifetime;
            speedCurve = other.speedCurve;
            startTime = other.startTime;
            distance = other.distance;
            useGravity = other.useGravity;
            gravityScale = other.gravityScale;
            flytime = other.flytime;
            isActive = other.isActive;
        }

    }

    private AnimationCurve GetDefaultCurve()
    {
        return new AnimationCurve(
            new Keyframe(0, 0.3f, 0, 0.5f),     // Start: 30% speed, smooth start
            new Keyframe(0.25f, 0.5f, 0, 1f),    // 25% journey: 50% speed
            new Keyframe(0.5f, 1f, 0, 0),        // Middle: 100% speed (no acceleration)
            new Keyframe(0.75f, 1f, 0, 0),       // 75% journey: 100% speed (constant)
            new Keyframe(1, 1f, 0, 0)           // End: 100% speed
        );
    }

    public int id;
    public GameObject prefab;
    public Transform transform;
    public Vector3 startPosition;
    public Transform targetTransform;
    public Vector3 targetPosition;
    public float speed;
    public float damage;
    public float lifetime;
    public AnimationCurve speedCurve;
    public float startTime;
    public float distance;
    public bool useGravity;
    public float gravityScale;
    public bool isActive;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public float flytime;
    public Vector3 hitPointCollider;
    public Vector3 hitNormalCollider;
    public Vector3 hitDirection;
    public BoxCollider arrowCollider;
}
