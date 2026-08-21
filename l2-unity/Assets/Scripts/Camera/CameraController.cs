using UnityEngine;

public class CameraController : MonoBehaviour
{
    // L2 rotator: 65536 = 360°. CameraPitch hardcoded -2700 (= -14.83° in UE).
    // Unity Euler X is inverted vs UE pitch: negative UE pitch (look down) → positive Unity X.
    public const float L2PitchDegrees = 2700f * 360f / 65536f;
    public const float L2FovHorizontal = 90f;
    public const float L2DistanceMeters = 2.3f;
    public const float L2HeightOffsetMeters = 0.2f;
    public const float L2CollisionRadiusMeters = 0.1f;
    public const float L2TraceExtraMeters = 0.3f;
    public const float L2MinDistanceMeters = 1f;
    public const float L2MaxDistanceMeters = 6f;
    public const float L2ReferencePlayerHeight = 1.6f;

    private Vector3 _lerpLookAt;
    [SerializeField] private float _x, _y = L2PitchDegrees;
    private Vector3 _lookAt;
    private LayerMask _collisionMask;

    [SerializeField] private Transform _target;

    [Header("L2 scale (Unity player is shorter than L2 1.6m)")]
    [SerializeField] private float _unityPlayerHeight = 0.85f;
    [SerializeField] private float _l2PlayerHeight = L2ReferencePlayerHeight;
    [SerializeField] private bool _applyL2Fov = true;

    [Header("Camera controls")]
    [SerializeField] private bool _smoothCamera = true;
    [SerializeField] private Vector3 _camOffset = Vector3.zero;
    [SerializeField] private float _lookHeightAdjust = -0.3f;
    [SerializeField] private float _smoothness = 8f;
    [SerializeField] private float _camSpeed = 3f;
    [SerializeField] private float _pitchAngle = L2PitchDegrees;
    [Tooltip("A/B: skip collision pull-in; camera stays at zoom distance (test nameplate wave).")]
    [SerializeField] private bool _disableCollisionZoom = false;

    [Header("Zoom controls")]
    [SerializeField] private float _minDistance = L2MinDistanceMeters * (0.85f / L2ReferencePlayerHeight);
    [SerializeField] private float _maxDistance = 8f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _camDistance = L2DistanceMeters * (0.85f / L2ReferencePlayerHeight) + 0.5f;
    [SerializeField] private float _currentDistance = 0;

    [Header("Bone stickiness")]
    [SerializeField] private bool _stickToBone = false;
    [SerializeField] private Transform _rootBone;
    [SerializeField] private float _rootBoneHeight = 0;

    [SerializeField] private CameraCollisionDetection _collisionDetector;
    private Camera _camera;
    private float _resolvedPlayerHeight;

    public Transform Target { get { return _target; } set { _target = value; } }

    public bool StickToBone { get { return _stickToBone; } set { _stickToBone = value; } }
    public float CurrentDistance { get { return _currentDistance; } }
    public float MaxDistance { get { return _maxDistance; } }

    public float WorldScale
    {
        get
        {
            float l2Height = _l2PlayerHeight > 0.01f ? _l2PlayerHeight : L2ReferencePlayerHeight;
            return _unityPlayerHeight / l2Height;
        }
    }

    private static CameraController _instance;
    public static CameraController Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _camera = GetComponent<Camera>();
            _resolvedPlayerHeight = _unityPlayerHeight;
        }
        else
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    private void Start()
    {
        _lerpLookAt = Vector3.zero;
        _y = _pitchAngle;
        _currentDistance = _camDistance;
        ApplyL2Fov();
    }

    public void SetMask(LayerMask collisionMask)
    {
        _collisionMask = collisionMask;
        if (_collisionDetector != null)
        {
            _collisionDetector.SetMask(collisionMask);
        }
    }

    private void Update()
    {
        if (_target != null)
        {
            UpdateZoom();
        }
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        UpdateInputs();
        UpdatePosition();
    }

    public void SetTarget(GameObject go)
    {
        _target = go.transform;
        _resolvedPlayerHeight = ResolvePlayerHeight();
        _lookAt = LookAtPoint();
        _lerpLookAt = _lookAt;
        _y = _pitchAngle;
        _currentDistance = _camDistance;

        _rootBone = _target.FindRecursive(child => child.tag == "Root");
        if (_rootBone != null)
        {
            _rootBoneHeight = _rootBone.position.y - _target.position.y;
        }

        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        _collisionDetector = new CameraCollisionDetection(_camera, _target, _camOffset, _collisionMask);
        ApplyL2Fov();
    }

    public bool IsObjectVisible(Transform target)
    {
        if (_collisionDetector == null || target == null)
        {
            return false;
        }

        RaycastHit hit;
        Vector3[] cameraClips = _collisionDetector.GetCameraViewPortPoints();
        if (cameraClips.Length == 0)
        {
            return false;
        }

        bool visible = false;
        Vector3 head = target.position + Vector3.up * _resolvedPlayerHeight;
        for (int i = 0; i < cameraClips.Length; i++)
        {
            if (!Physics.Linecast(cameraClips[i], head, out hit, _collisionMask))
            {
                visible = true;
                break;
            }
        }

        if (!visible)
        {
            return false;
        }

        Camera cam = _camera != null ? _camera : Camera.main;
        if (cam == null)
        {
            return false;
        }

        Vector3 viewPort = cam.WorldToViewportPoint(target.position);
        return viewPort.x <= 1 && viewPort.x >= 0 && viewPort.y <= 1 && viewPort.y >= 0 && viewPort.z >= -0.2f;
    }

    private void UpdateInputs()
    {
        if (InputManager.Instance == null || !InputManager.Instance.TurnCamera)
        {
            return;
        }

        _x += Input.GetAxis("Mouse X") * _camSpeed;
        _y -= Input.GetAxis("Mouse Y") * _camSpeed;
        _y = ClampAngle(_y, -80f, 80f);
    }

    private void UpdateZoom()
    {
        if (InputManager.Instance == null)
        {
            return;
        }

        float scrollAxis = InputManager.Instance.ZoomAxis;
        _camDistance = Mathf.Clamp(_camDistance - scrollAxis * _zoomSpeed * 0.1f, _minDistance, _maxDistance);
    }

    private void UpdatePosition()
    {
        Quaternion rotation = Quaternion.Euler(_y, _x, 0f);
        _lookAt = LookAtPoint();

        if (_smoothCamera)
        {
            if (_lerpLookAt == Vector3.zero)
            {
                _lerpLookAt = _lookAt;
            }

            _lerpLookAt = Vector3.Lerp(_lerpLookAt, _lookAt, _smoothness * Time.deltaTime);
        }
        else
        {
            _lerpLookAt = _lookAt;
        }

        float desiredDistance = _camDistance;
        if (!_disableCollisionZoom)
        {
            if (_collisionDetector == null && _camera != null)
            {
                _collisionDetector = new CameraCollisionDetection(_camera, _target, _camOffset, _collisionMask);
            }

            if (_collisionDetector != null)
            {
                _collisionDetector.DetectSphereCollision(
                    _lerpLookAt,
                    rotation,
                    desiredDistance,
                    L2CollisionRadiusMeters * WorldScale,
                    L2TraceExtraMeters * WorldScale);
                desiredDistance = _collisionDetector.AdjustedDistance;
            }
        }

        _currentDistance = desiredDistance;
        Vector3 adjustedPosition = _lerpLookAt + rotation * (Vector3.forward * -_currentDistance);
        if (float.IsNaN(adjustedPosition.x))
        {
            return;
        }

        transform.SetPositionAndRotation(adjustedPosition, rotation);
    }

    public void SetHeading(float heading)
    {
        _x = heading;
    }

    Vector3 LookAtPoint()
    {
        float boneOffset = 0f;
        if (_stickToBone && _rootBone != null)
        {
            boneOffset = _rootBone.position.y - _target.position.y - _rootBoneHeight;
        }

        float aboveHead = L2HeightOffsetMeters * WorldScale;
        CharacterController cc = FindCharacterController();
        if (cc != null && cc.height > 0.1f)
        {
            Vector3 localTop = cc.center + Vector3.up * (cc.height * 0.5f);
            return cc.transform.TransformPoint(localTop) + _camOffset + Vector3.up * (aboveHead + _lookHeightAdjust + boneOffset);
        }

        float lookHeight = _resolvedPlayerHeight + aboveHead + _lookHeightAdjust;
        return _target.position + _camOffset + Vector3.up * (lookHeight + boneOffset);
    }

    float ResolvePlayerHeight()
    {
        CharacterController cc = FindCharacterController();
        if (cc != null && cc.height > 0.1f)
        {
            return cc.height;
        }

        return _unityPlayerHeight;
    }

    CharacterController FindCharacterController()
    {
        if (_target == null)
        {
            return null;
        }

        CharacterController cc = _target.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = _target.GetComponentInChildren<CharacterController>();
        }

        return cc;
    }

    void ApplyL2Fov()
    {
        if (!_applyL2Fov)
        {
            return;
        }

        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        if (_camera == null)
        {
            return;
        }

        _camera.fieldOfView = VerticalFovFromHorizontal(L2FovHorizontal, _camera.aspect);
    }

    static float VerticalFovFromHorizontal(float horizontalFov, float aspect)
    {
        float h = horizontalFov * Mathf.Deg2Rad;
        float v = 2f * Mathf.Atan(Mathf.Tan(h * 0.5f) / Mathf.Max(0.01f, aspect));
        return v * Mathf.Rad2Deg;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
        {
            angle += 360f;
        }

        if (angle > 360f)
        {
            angle -= 360f;
        }

        return Mathf.Clamp(angle, min, max);
    }
}
