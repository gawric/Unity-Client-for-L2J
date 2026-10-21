using UnityEngine;

public class MovementTarget
{
    private object _objectPoint;
    private float _distance;
    private bool _isRunning;
    public MovementTarget(object objectPoint , float distance , bool isRunning)
    {
        _objectPoint = objectPoint;
        _distance = distance;
        _isRunning = isRunning;
    }

    public void Set(object objectPoint, float distance, bool isRunning)
    {
        _objectPoint = objectPoint;
        _distance = distance;
        _isRunning = isRunning;
    }

    public MovementTarget(Entity entity, float distance)
    {
        _objectPoint = entity.transform;
        _distance = distance;
        _isRunning = true;
    }

    public bool IsRunningServer()
    {
        return _isRunning;
    }

    public float GetDistance()
    {
        return _distance;
    }

    public bool IsActorTarget()
    {
        return _objectPoint is Transform;
    }
    public Entity GetActorEntity()
    {
        Transform transform = _objectPoint as Transform;
        return transform != null ? transform.GetComponent<Entity>() : null;
    }

    public bool TryGetTarget(out Vector3 target)
    {
        target = Vector3.zero;
        switch (_objectPoint)
        {
            case Transform transform:
                if (transform == null)
                    return false;
                target = transform.position;
                return true;
            case Vector3 vector:
                target = vector;
                return true;
            default:
                return false;
        }
    }

    public Vector3 GetTarget()
    {
        return TryGetTarget(out Vector3 target) ? target : Vector3.zero;
    }
}