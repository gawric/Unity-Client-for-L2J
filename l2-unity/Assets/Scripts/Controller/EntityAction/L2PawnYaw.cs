using UnityEngine;

// APawn::physicsRotation steps actor yaw toward DesiredRotation.Yaw with
// fixedTurn. Live ReplaySession dump: RotationRate.Yaw = 0xFDE8.
// MagicProcess replaces that rate with 0x50000 until the cast ends.
// 65536 heading units = 360 degrees.
// The three rates are const on this service. The turn itself is the VContainer singleton.
public sealed class L2PawnYaw
{
    public const int FullCircle = 65536;
    public const int RotationRateYaw = 0xFDE8;
    public const int MagicRotationRateYaw = 0x50000;

    public float DegreesPerSecond(int headingUnitsPerSecond)
    {
        return headingUnitsPerSecond * (360f / FullCircle);
    }

    public float StepDegrees(float currentYaw, float desiredYaw, float maxStepDegrees)
    {
        float delta = Mathf.DeltaAngle(currentYaw, desiredYaw);
        float abs = Mathf.Abs(delta);
        if (abs <= maxStepDegrees || maxStepDegrees < 0f)
            return desiredYaw;
        return currentYaw + Mathf.Sign(delta) * maxStepDegrees;
    }

    public void RequestLook(Transform actor, Vector3 worldPoint, int headingUnitsPerSecond)
    {
        if (actor == null)
            return;
        Vector3 dir = worldPoint - actor.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        RequestYaw(actor, Quaternion.LookRotation(dir).eulerAngles.y, headingUnitsPerSecond);
    }

    public void RequestYaw(Transform actor, float desiredYaw, int headingUnitsPerSecond)
    {
        if (actor == null)
            return;
        L2PawnYawDrive drive = actor.GetComponent<L2PawnYawDrive>();
        if (drive == null)
            drive = actor.gameObject.AddComponent<L2PawnYawDrive>();
        drive.Bind(this);
        drive.SetDesired(desiredYaw, headingUnitsPerSecond);
    }
}

public sealed class L2PawnYawDrive : MonoBehaviour
{
    L2PawnYaw _yaw;
    float _desiredYaw;
    int _rate = L2PawnYaw.RotationRateYaw;
    bool _active;

    public bool Active => _active;

    public void Bind(L2PawnYaw yaw)
    {
        _yaw = yaw;
    }

    public void Stop()
    {
        _active = false;
    }

    public void SetDesired(float desiredYaw, int headingUnitsPerSecond)
    {
        _desiredYaw = desiredYaw;
        _rate = headingUnitsPerSecond > 0 ? headingUnitsPerSecond : L2PawnYaw.RotationRateYaw;
        _active = true;
    }

    void LateUpdate()
    {
        if (!_active || _yaw == null)
            return;

        float current = transform.eulerAngles.y;
        float maxStep = _yaw.DegreesPerSecond(_rate) * Time.deltaTime;
        float next = _yaw.StepDegrees(current, _desiredYaw, maxStep);
        transform.rotation = Quaternion.Euler(0f, next, 0f);
        if (Mathf.Abs(Mathf.DeltaAngle(next, _desiredYaw)) <= 0.05f)
            _active = false;
    }
}
