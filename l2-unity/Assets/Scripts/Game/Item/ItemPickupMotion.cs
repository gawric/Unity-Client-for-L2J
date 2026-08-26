using UnityEngine;

/// <summary>
/// Simplified AL2Pickup::Tick spin: approach TargetRotation with RotationRate, then settle.
/// No AL2NMover / mesh detach.
/// </summary>
public sealed class ItemPickupMotion : MonoBehaviour
{
    [SerializeField] private Vector3 _rotationRateEuler = new Vector3(0f, 220f, 0f);
    [SerializeField] private float _spinSeconds = 1.6f;

    Vector3 _targetEuler;
    float _elapsed;
    bool _settled = true;

    void Awake()
    {
        enabled = false;
    }

    public void BeginSpin(int objectId)
    {
        float sign = (objectId & 1) == 0 ? 1f : -1f;
        _rotationRateEuler = new Vector3(0f, 220f * sign, 40f * sign);
        _targetEuler = transform.eulerAngles + new Vector3(0f, 360f * sign, 0f);
        _elapsed = 0f;
        _settled = false;
        enabled = true;
    }

    void Update()
    {
        if (_settled)
        {
            enabled = false;
            return;
        }

        float dt = Time.deltaTime;
        _elapsed += dt;
        Vector3 current = transform.eulerAngles;

        current.x = ApproachAxis(current.x, _targetEuler.x, _rotationRateEuler.x, dt);
        current.y = ApproachAxis(current.y, _targetEuler.y, _rotationRateEuler.y, dt);
        current.z = ApproachAxis(current.z, _targetEuler.z, _rotationRateEuler.z, dt);
        transform.eulerAngles = current;

        if (_elapsed >= _spinSeconds || NearTarget(current, _targetEuler))
        {
            transform.eulerAngles = _targetEuler;
            _settled = true;
            enabled = false;
        }
    }

    static bool NearTarget(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a.x, b.x)) < 2f
            && Mathf.Abs(Mathf.DeltaAngle(a.y, b.y)) < 2f
            && Mathf.Abs(Mathf.DeltaAngle(a.z, b.z)) < 2f;
    }

    /// <summary>UE2-style approach with wrap (degrees).</summary>
    static float ApproachAxis(float current, float target, float rate, float dt)
    {
        float delta = Mathf.DeltaAngle(current, target);
        float step = rate * dt;
        if (Mathf.Abs(step) < 0.0001f)
            return current;

        if (rate >= 0f)
        {
            if (delta >= 0f && delta < step)
                return target;
            if (delta < 0f && delta > -step)
                return target;
        }
        else
        {
            if (delta <= 0f && delta > step)
                return target;
            if (delta > 0f && delta < -step)
                return target;
        }

        return current + step;
    }
}
