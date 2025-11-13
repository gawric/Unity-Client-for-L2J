using UnityEngine;
using UnityEngine.UIElements;

public class MapMarkerFollowCamera : MonoBehaviour
{
    public Camera targetCamera;
    private VisualElement _marker;

     // Optional: Add an offset value to fine-tune the rotation
    public float rotationOffset = 90f;


    private float _lastAngle = float.NaN;
    private float _desiredAngle = float.NaN;

    public void SetElement(VisualElement marker)
    {
        _marker = marker;
    }

    public void TurnUpdate(Camera targetCamera , bool isWindowHide)
    {
        float cameraYaw = targetCamera.transform.eulerAngles.y;
        _desiredAngle = cameraYaw + rotationOffset;
        _desiredAngle = NormalizeAngle(_desiredAngle);

        if (_marker == null || targetCamera == null || isWindowHide) return;

  
        //Debug.Log($"Camera Yaw: {cameraYaw}, Desired Angle: {desiredAngle}");

        if (!Mathf.Approximately(_desiredAngle, _lastAngle))
        {
            _lastAngle = _desiredAngle;
            _marker.style.rotate = new Rotate(new Angle(_desiredAngle, AngleUnit.Degree));
        }
    }

    public void ManualTurnUpdate()
    {
        if (!Mathf.Approximately(_desiredAngle, _lastAngle))
        {
            _lastAngle = _desiredAngle;
            _marker.style.rotate = new Rotate(new Angle(_desiredAngle, AngleUnit.Degree));
        }
    }

    static float NormalizeAngle(float a)
    {
        // Normalize to 0..360 range first
        a = a % 360f;
        if (a < 0) a += 360f;

        // Then convert to -180..180 range
        if (a > 180f)
            a -= 360f;

        return a;
    }
}