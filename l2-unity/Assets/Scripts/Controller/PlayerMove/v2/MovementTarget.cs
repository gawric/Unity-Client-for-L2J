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
    public Vector3 GetTarget()
    {
        switch (_objectPoint)
        {
            case Transform transform:
                return transform.position;
            case Vector3 vector:
                return vector;
            default:
                Debug.LogError("MovementTarget->GetTarget: Invalid object type");
                return Vector3.zero;
        }
    }
}