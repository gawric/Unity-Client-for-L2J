using UnityEngine;

[System.Serializable]
public class CameraCollisionDetection {
    [SerializeField] private LayerMask _collisionLayer;
    [SerializeField] private float _adjustedDistance;
    [SerializeField] private Transform _collisionObject;
    [SerializeField] private Vector2 _clipPlaneOffset = new Vector2(2f, 1f);
    [SerializeField] private bool _debug = false;
    private Camera _camera;
    private Transform _target;
    private Vector3 _offset;

    public float AdjustedDistance { get { return _adjustedDistance; } }

    public CameraCollisionDetection(Camera camera, Transform target, Vector3 cameraOffset, LayerMask collisionmask) {
        _camera = camera;
        _target = target;
        _offset = cameraOffset;
        _collisionLayer = collisionmask;
        _adjustedDistance = 0f;
    }

    public void SetMask(LayerMask collisionMask)
    {
        _collisionLayer = collisionMask;
    }

    public void DetectSphereCollision(
        Vector3 lookAt,
        Quaternion rotation,
        float desiredDistance,
        float radius,
        float extraLength)
    {
        _adjustedDistance = desiredDistance;
        if (desiredDistance <= 0.001f)
        {
            return;
        }

        Vector3 desiredPos = lookAt + rotation * (Vector3.forward * -desiredDistance);
        Vector3 delta = desiredPos - lookAt;
        float dist = delta.magnitude;
        if (dist <= 0.001f)
        {
            return;
        }

        Vector3 dir = delta / dist;
        RaycastHit hit;
        if (Physics.SphereCast(
            lookAt,
            Mathf.Max(0.01f, radius),
            dir,
            out hit,
            dist + extraLength,
            _collisionLayer,
            QueryTriggerInteraction.Ignore))
        {
            _adjustedDistance = Mathf.Clamp(hit.distance, 0.05f, desiredDistance);
            _collisionObject = hit.transform;
        }
        else
        {
            _collisionObject = null;
        }
    }

    public Vector3[] GetCameraClipPoints(float distance) {
        Vector3[] cameraClipPoints = new Vector3[5];
        Quaternion camRot = _camera.transform.rotation;
        Vector3 desiredPos = camRot * (Vector3.forward * -distance) + _target.position + _offset;

        float z = _camera.nearClipPlane;
        float x = Mathf.Tan(_camera.fieldOfView / _clipPlaneOffset.x) * z;
        float y = x / _camera.aspect / _clipPlaneOffset.y;

        cameraClipPoints[0] = (camRot * new Vector3(-x, y, z)) + desiredPos;
        cameraClipPoints[1] = (camRot * new Vector3(x, y, z)) + desiredPos;
        cameraClipPoints[2] = (camRot * new Vector3(-x, -y, z)) + desiredPos;
        cameraClipPoints[3] = (camRot * new Vector3(x, -y, z)) + desiredPos;
        cameraClipPoints[4] = desiredPos - (_camera.transform.forward * 0.25f);

        return cameraClipPoints;
    }

    public Vector3[] GetCameraViewPortPoints() {
        if (_camera == null)
        {
            return new Vector3[0];
        }

        Vector3[] cameraClipPoints = new Vector3[5];
        Quaternion camRot = _camera.transform.rotation;
        Vector3 camPos = _camera.transform.position;

        float z = _camera.nearClipPlane;
        float x = Mathf.Tan(_camera.fieldOfView) * z;
        float y = x / _camera.aspect / _clipPlaneOffset.y;

        cameraClipPoints[0] = (camRot * new Vector3(-x, y, z)) + camPos;
        cameraClipPoints[1] = (camRot * new Vector3(x, y, z)) + camPos;
        cameraClipPoints[2] = (camRot * new Vector3(-x, -y, z)) + camPos;
        cameraClipPoints[3] = (camRot * new Vector3(x, -y, z)) + camPos;
        cameraClipPoints[4] = camPos - (_camera.transform.forward * 0.25f);

        return cameraClipPoints;
    }
}
