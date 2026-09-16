using UnityEngine;

/// <summary>
/// L2 SkyMeshActor16: L2sky_Cylinder + WhiteRing (RenderDoc EID 1661).
/// One instance. Opacity is only this slider. Tint follows GetHazeColor LUT day and night.
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class L2HazeRing : MonoBehaviour
{
    const string TexPath = "Data/Clock/WhiteRing";
    const string ShaderName = "L2/Sky/HazeRing";
    const string MeshName = "L2sky_Cylinder2";
    const string MeshObjectName = "L2HazeRingMesh";

    /// Native cylinder: horizon ring Y=0, soft top Y=10, xz radius ~26.05 (RenderDoc EID 1661).
    public const float MeshHorizonY = 0f;
    public const float MeshTopY = 10f;
    public const float MeshRadius = 26.0515f;

    /// <summary>World Y above camera of the WhiteRing horizon after sky projection.</summary>
    public static float HorizonHeight(float skyZ)
    {
        return 0f;
    }

    /// <summary>World Y above camera of the WhiteRing soft top (~+21°).</summary>
    public static float SoftTopHeight(float skyZ)
    {
        float z = skyZ > 1f ? skyZ : 88f;
        float len = Mathf.Sqrt(MeshRadius * MeshRadius + MeshTopY * MeshTopY);
        return z * (MeshTopY / len);
    }

    public static L2HazeRing Instance { get; private set; }

    static readonly int HazeColorId = Shader.PropertyToID("_HazeColor");

    static bool s_SessionOpacitySet;
    static float s_SessionOpacity;

    [SerializeField] Texture2D _whiteRing;
    [SerializeField] bool _followCamera = true;
    [Header("Haze")]
    [Tooltip("1 = L2. 0 = off. Does not change the clock.")]
    [SerializeField] [Range(0f, 1f)] float _opacity = 0.6f;

    Transform _tf;
    MeshFilter _mf;
    MeshRenderer _mr;
    Material _mat;
    Mesh _mesh;
    Camera _cam;

    public float Opacity
    {
        get { return _opacity; }
        set { _opacity = Mathf.Clamp01(value); }
    }

    public static L2HazeRing EnsureOn(GameObject host)
    {
        if (Instance != null)
        {
            return Instance;
        }

        L2HazeRing ring = host != null ? host.GetComponent<L2HazeRing>() : null;
        if (ring == null && host != null)
        {
            ring = host.AddComponent<L2HazeRing>();
        }

        return ring;
    }

    void ApplyHazeColor(Color tint)
    {
        if (_mat == null)
        {
            return;
        }

        // LUT is already display bytes / 255. SetColor would sRGB→linear and muddy the morning haze.
        _mat.SetVector(HazeColorId, new Vector4(tint.r, tint.g, tint.b, tint.a));
    }

    void ApplySessionOpacity()
    {
        if (s_SessionOpacitySet)
        {
            _opacity = Mathf.Clamp01(s_SessionOpacity);
            return;
        }

        s_SessionOpacity = Mathf.Clamp01(_opacity);
        s_SessionOpacitySet = true;
    }

    void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        ApplySessionOpacity();
        L2HazeLut.EnsureLoaded();
        EnsureRing();
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyRing();
    }

    void LateUpdate()
    {
        if (Instance != this)
        {
            return;
        }

        L2HazeLut.EnsureLoaded();
        EnsureRing();
        if (_tf == null || _mat == null)
        {
            return;
        }

        ApplyHazeLayer(_tf.gameObject);

        WorldClock clock = WorldClock.Instance;
        if (clock == null)
        {
            clock = WorldClock.EnsurePersistent();
        }

        float hours = clock != null ? clock.WorldHours : 12f;
        Color tint = L2HazeLut.SampleAt(hours);
        float opacity = Mathf.Clamp01(_opacity);
        s_SessionOpacity = opacity;
        s_SessionOpacitySet = true;
        tint.a = opacity;
        ApplyHazeColor(tint);

        bool draw = opacity > 0.001f;
        if (_mr != null)
        {
            _mr.enabled = draw;
        }

        _tf.gameObject.SetActive(draw);

        Camera cam = ResolveCamera();
        _tf.position = _followCamera && cam != null ? cam.transform.position : Vector3.zero;
        _tf.rotation = Quaternion.identity;
        _tf.localScale = Vector3.one;
        _mat.SetFloat("_SkyDistance", ResolveSkyDistance(cam));
    }

    void EnsureRing()
    {
        if (_whiteRing == null)
        {
            _whiteRing = Resources.Load<Texture2D>(TexPath);
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null || _whiteRing == null)
        {
            return;
        }

        _whiteRing.wrapModeU = TextureWrapMode.Repeat;
        _whiteRing.wrapModeV = TextureWrapMode.Clamp;

        if (_mesh == null || _mesh.name != MeshName)
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }

            _mesh = BuildCylinderMesh();
            _mesh.hideFlags = HideFlags.DontSave;
            if (_mf != null)
            {
                _mf.sharedMesh = _mesh;
            }
        }

        if (_tf != null)
        {
            return;
        }

        var go = new GameObject(MeshObjectName);
        go.hideFlags = HideFlags.HideAndDontSave;
        _tf = go.transform;
        _tf.SetParent(transform, false);
        ApplyHazeLayer(go);

        _mf = go.AddComponent<MeshFilter>();
        _mf.sharedMesh = _mesh;

        _mr = go.AddComponent<MeshRenderer>();
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows = false;
        _mr.allowOcclusionWhenDynamic = false;
        _mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        _mat = new Material(shader);
        _mat.hideFlags = HideFlags.DontSave;
        _mat.SetTexture("_MainTex", _whiteRing);
        Color tint = L2HazeLut.ColorModifierDefault;
        tint.a = Mathf.Clamp01(_opacity);
        ApplyHazeColor(tint);
        _mat.renderQueue = 2980;
        _mr.sharedMaterial = _mat;
    }

    static Mesh BuildCylinderMesh()
    {
        float[] u =
        {
            -0.0058f, 0.1203f, 0.2460f, 0.3705f, 0.4938f, 0.6163f, 0.7388f, 0.8620f,
            0.9866f, 1.1122f, 1.2383f, 1.3640f, 1.4885f, 1.6118f, 1.7343f, 1.8568f,
            1.9801f, 2.1046f, 2.2303f
        };
        float[] unrealX =
        {
            -4.5663f, 4.4634f, 12.9485f, 19.8657f, 24.3805f, 25.9485f, 24.3806f, 19.8657f,
            12.9486f, 4.4634f, -4.5663f, -13.0514f, -19.9686f, -24.4835f, -26.0515f, -24.4835f,
            -19.9686f, -13.0515f, -4.5663f
        };
        float[] unrealY =
        {
            25.6050f, 25.6050f, 22.5167f, 16.7125f, 8.8925f, 0f, -8.8925f, -16.7125f,
            -22.5167f, -25.6050f, -25.6050f, -22.5167f, -16.7125f, -8.8925f, 0f, 8.8925f,
            16.7125f, 22.5167f, 25.6050f
        };
        float[] ringY = { -100f, 0f, 0f, 10f };
        float[] ringV = { 0.9900f, 0.9878f, 0.9900f, 0.0105f };

        const int cols = 19;
        const int rings = 4;
        var positions = new Vector3[cols * rings];
        var uvs = new Vector2[cols * rings];
        for (int r = 0; r < rings; r++)
        {
            for (int i = 0; i < cols; i++)
            {
                int idx = r * cols + i;
                positions[idx] = new Vector3(unrealX[i], ringY[r], unrealY[i]);
                uvs[idx] = new Vector2(u[i], ringV[r]);
            }
        }

        var indices = new int[18 * 2 * 6];
        int t = 0;
        for (int band = 0; band < 2; band++)
        {
            int botRing = band * 2;
            int topRing = botRing + 1;
            for (int i = 0; i < cols - 1; i++)
            {
                int b0 = botRing * cols + i;
                int b1 = botRing * cols + i + 1;
                int t0 = topRing * cols + i;
                int t1 = topRing * cols + i + 1;
                indices[t++] = b0;
                indices[t++] = t0;
                indices[t++] = b1;
                indices[t++] = b1;
                indices[t++] = t0;
                indices[t++] = t1;
            }
        }

        var mesh = new Mesh { name = MeshName };
        mesh.vertices = positions;
        mesh.uv = uvs;
        mesh.triangles = indices;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 400f);
        return mesh;
    }

    float ResolveSkyDistance(Camera cam)
    {
        float far = cam != null ? cam.farClipPlane : 500f;
        return Mathf.Clamp(far * 0.88f, 80f, 4000f);
    }

    void ApplyHazeLayer(GameObject go)
    {
        if (L2FxCompositorRuntime.PreferGpuQueue)
            L2FxCompositorLayers.ApplyL2HazeLayer(go);
        else
            go.layer = 0;
    }

    Camera ResolveCamera()
    {
        if (CameraController.Instance != null)
        {
            Camera fromController = CameraController.Instance.GetComponent<Camera>();
            if (fromController == null)
            {
                fromController = CameraController.Instance.GetComponentInChildren<Camera>();
            }

            if (fromController != null && fromController.enabled && fromController.gameObject.activeInHierarchy)
            {
                _cam = fromController;
                return _cam;
            }
        }

        Camera main = Camera.main;
        if (main != null && main.enabled && main.gameObject.activeInHierarchy)
        {
            _cam = main;
            return _cam;
        }

        Camera[] cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
        {
            Camera c = cams[i];
            if (c != null && c.enabled && c.gameObject.activeInHierarchy && !c.orthographic)
            {
                _cam = c;
                return _cam;
            }
        }

        return _cam;
    }

    void DestroyRing()
    {
        if (_mat != null)
        {
            if (Application.isPlaying)
                Destroy(_mat);
            else
                DestroyImmediate(_mat);
            _mat = null;
        }

        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
            _mesh = null;
        }

        if (_tf != null)
        {
            if (Application.isPlaying)
                Destroy(_tf.gameObject);
            else
                DestroyImmediate(_tf.gameObject);
            _tf = null;
        }

        _mf = null;
        _mr = null;
    }
}
