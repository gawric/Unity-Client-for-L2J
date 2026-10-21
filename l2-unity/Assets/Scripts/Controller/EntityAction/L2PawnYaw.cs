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
    public const float ArriveDeg = 0.05f;

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
        L2PawnYawDrive drive = EnsureDrive(actor);
        RequestLook(drive, actor, worldPoint, headingUnitsPerSecond);
    }

    public L2PawnYawDrive EnsureDrive(Transform actor)
    {
        if (actor == null)
            return null;
        L2PawnYawDrive drive = actor.GetComponent<L2PawnYawDrive>();
        if (drive == null)
        {
            drive = actor.gameObject.AddComponent<L2PawnYawDrive>();
            drive.enabled = false;
        }
        drive.Bind(this);
        return drive;
    }

    public void RequestLook(L2PawnYawDrive drive, Transform actor, Vector3 worldPoint, int headingUnitsPerSecond)
    {
        if (drive == null || actor == null)
            return;
        Vector3 dir = worldPoint - actor.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        drive.Bind(this);
        drive.SetDesired(Quaternion.LookRotation(dir).eulerAngles.y, headingUnitsPerSecond);
    }

    public void RequestYaw(Transform actor, float desiredYaw, int headingUnitsPerSecond)
    {
        L2PawnYawDrive drive = EnsureDrive(actor);
        if (drive == null)
            return;
        drive.SetDesired(desiredYaw, headingUnitsPerSecond);
    }
}

public sealed class L2PawnYawDrive : MonoBehaviour
{
    L2PawnYaw _yaw;
    float _desiredYaw;
    int _rate = L2PawnYaw.RotationRateYaw;
    bool _active;
    bool _appliedInUpdate;

    public bool Active => _active;

    public void Bind(L2PawnYaw yaw)
    {
        _yaw = yaw;
    }

    public void Stop()
    {
        Deactivate();
    }

    public void SetDesired(float desiredYaw, int headingUnitsPerSecond)
    {
        _desiredYaw = desiredYaw;
        _rate = headingUnitsPerSecond > 0 ? headingUnitsPerSecond : L2PawnYaw.RotationRateYaw;
        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, desiredYaw)) <= L2PawnYaw.ArriveDeg)
        {
            Deactivate();
            return;
        }

        _active = true;
        if (!enabled)
            enabled = true;
    }

    /// <summary>
    /// Step yaw on the Update path (with position). Skips LateUpdate this frame.
    /// </summary>
    public bool TryConsumeStep(float deltaTime, out Quaternion rotation)
    {
        rotation = transform.rotation;
        if (!_active || _yaw == null)
            return false;

        float current = rotation.eulerAngles.y;
        if (Mathf.Abs(Mathf.DeltaAngle(current, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
        {
            Deactivate();
            return false;
        }

        float maxStep = _yaw.DegreesPerSecond(_rate) * deltaTime;
        float next = _yaw.StepDegrees(current, _desiredYaw, maxStep);
        if (Mathf.Abs(Mathf.DeltaAngle(current, next)) <= 0.01f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(next, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
                Deactivate();
            return false;
        }

        rotation = Quaternion.Euler(0f, next, 0f);
        if (Mathf.Abs(Mathf.DeltaAngle(next, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
            Deactivate();
        else
            _appliedInUpdate = true;
        return true;
    }

    void Deactivate()
    {
        _active = false;
        _appliedInUpdate = false;
        if (enabled)
            enabled = false;
    }

    void LateUpdate()
    {
        if (_appliedInUpdate)
        {
            _appliedInUpdate = false;
            return;
        }

        if (!_active || _yaw == null)
        {
            if (enabled)
                enabled = false;
            return;
        }

        float current = transform.eulerAngles.y;
        if (Mathf.Abs(Mathf.DeltaAngle(current, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
        {
            Deactivate();
            return;
        }

        float maxStep = _yaw.DegreesPerSecond(_rate) * Time.deltaTime;
        float next = _yaw.StepDegrees(current, _desiredYaw, maxStep);
        if (Mathf.Abs(Mathf.DeltaAngle(current, next)) <= 0.01f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(next, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
                Deactivate();
            return;
        }

        transform.rotation = Quaternion.Euler(0f, next, 0f);
        if (Mathf.Abs(Mathf.DeltaAngle(next, _desiredYaw)) <= L2PawnYaw.ArriveDeg)
            Deactivate();
    }
}
