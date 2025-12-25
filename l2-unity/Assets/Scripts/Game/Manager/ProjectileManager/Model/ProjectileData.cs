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
            isActive = other.isActive;
        }

    }

    private AnimationCurve GetDefaultCurve()
    {
        return new AnimationCurve(
            new Keyframe(0, 0.3f, 0, 0.5f),     // Начало: 30% скорости, плавный старт
            new Keyframe(0.3f, 0.5f, 0, 1f),     // 30% пути: 50% скорости
            new Keyframe(0.6f, 0.6f, 0, 1.5f),   // 60% пути: 70% скорости
            new Keyframe(0.8f, 0.9f, 0, 2f),     // 80% пути: 90% скорости
            new Keyframe(0.9f, 0.95f, 0, 1f),   // 90% пути: 95% скорости
            new Keyframe(1, 1f, 0, 0)           // Конец: 100% скорости
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

    public Vector3 hitPointCollider;
    public Vector3 hitNormalCollider;
    public Vector3 hitDirection;
}
