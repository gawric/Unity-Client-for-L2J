using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Simple L2 clouds from package StaticMeshes (L2Sky_S):
/// Final01+02 on L2sky_sp02 (lid), Final03+silhouette on L2sky_sp01 (belt).
/// FBX keeps UE vertex units (same as RenderDoc CSV). Shader fits the cap radius (~1050) to far clip.
/// Layer L2Clouds: UNORM blit after encode, before haze (D3D9 SrcA/InvSrcA, no fxGain).
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class L2CloudSimple : MonoBehaviour
{
    const string ShaderName = "L2/Sky/Cloud";
    const string TexDomeA0 = "Data/Clock/Cloud_Final01";
    const string TexDomeA1 = "Data/Clock/Cloud_Final02";
    const string TexDomeB0 = "Data/Clock/Cloud_Final03";
    const string TexDomeB1 = "Data/Clock/Cloud_Silhouette";

    const string MeshCapPath = "Data/StaticMeshes/L2Sky_S/L2sky_sp02";
    const string MeshBeltPath = "Data/StaticMeshes/L2Sky_S/L2sky_sp01";

    static readonly int TextureFactorId = Shader.PropertyToID("_TextureFactor");
    static readonly int SkyParamsId = Shader.PropertyToID("_SkyParams");
    static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    // Cap xz ~1050, belt xz ~258 (package / CSV). Same skyZ, each uses its own radius
    // so the belt sits on the horizon instead of cutting through the village.
    const float SkyCapRadius = 1050f;
    const float SkyBeltRadius = 258f;
    const float BeltNativeBottomY = 0.085f;
    const float BeltNestIntoHaze = 0.75f;

    static readonly Color DaySheetTint = L2CloudLut.SheetFallback;
    static readonly Color DaySilhouetteTint = L2CloudLut.SilhouetteFallback;

    public static L2CloudSimple Instance { get; private set; }

    [SerializeField] bool _followCamera = true;
    [SerializeField] [Range(0f, 1f)] float _opacity = 1f;

    Texture2D _domeA0, _domeA1, _domeB0, _domeB1;
    Mesh _capMesh, _beltMesh;
    CloudChunk _domeA, _domeB;
    Camera _cam;

    struct CloudChunk
    {
        public Transform tf;
        public MeshFilter mf;
        public MeshRenderer mr;
        public Mesh mesh;
        public Material[] mats;
    }

    public static L2CloudSimple EnsureOn(GameObject host)
    {
        if (Instance != null)
            return Instance;
        if (host == null)
            return null;
        L2CloudSimple c = host.GetComponent<L2CloudSimple>();
        return c != null ? c : host.AddComponent<L2CloudSimple>();
    }

    void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        StripStaleChildren();
        EnsureChunks();
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
        DestroyChunks();
    }

    void LateUpdate()
    {
        if (Instance != this)
            return;

        EnsureChunks();
        if (_domeA.tf == null)
            return;

        WorldClock clock = WorldClock.Instance ?? WorldClock.EnsurePersistent();
        float hours = clock != null ? clock.WorldHours : 12f;
        L2CloudLut.Sample cloudColors = L2CloudLut.SampleAt(hours);
        float opacity = Mathf.Clamp01(_opacity);
        bool draw = opacity > 0.01f;
        SetActive(ref _domeA, draw);
        SetActive(ref _domeB, draw);
        if (!draw)
            return;

        Camera cam = ResolveCamera();
        Vector3 pos = _followCamera && cam != null ? cam.transform.position : Vector3.zero;
        float skyZ = ResolveSkyDistance(cam);
        Place(ref _domeA, pos, skyZ, SkyCapRadius, 15f);
        float beltBottom = BeltNativeBottomY * (skyZ / SkyBeltRadius);
        float beltLift = L2HazeRing.HorizonHeight(skyZ) - beltBottom - BeltNestIntoHaze;
        Place(ref _domeB, pos, skyZ, SkyBeltRadius, beltLift);
        ApplyTint(_domeA.mats, 0, cloudColors.sheet, opacity);
        ApplyTint(_domeA.mats, 1, cloudColors.sheet, opacity);
        ApplyTint(_domeB.mats, 0, cloudColors.sheet, opacity);
        ApplyTint(_domeB.mats, 1, cloudColors.silhouette, opacity);
    }

    void StripStaleChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
                continue;
            string n = child.name;
            if (n == "L2CloudSimple_Patch" || n == "L2CloudSimple_DomeA" || n == "L2CloudSimple_DomeB" ||
                n == "L2CloudSimple_Cap" || n == "L2CloudSimple_Ring")
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    static void ApplyTint(Material[] mats, int i, Color tint, float fade)
    {
        if (mats == null || i < 0 || i >= mats.Length || mats[i] == null)
            return;
        mats[i].SetVector(TextureFactorId, new Vector4(tint.r * fade, tint.g * fade, tint.b * fade, tint.a));
    }

    void Place(ref CloudChunk chunk, Vector3 pos, float skyZ, float meshRadius, float liftY)
    {
        if (chunk.tf == null)
            return;
        ApplyCloudLayer(chunk.tf.gameObject);
        chunk.tf.position = pos;
        chunk.tf.rotation = Quaternion.identity;
        chunk.tf.localScale = Vector3.one;
        if (chunk.mats == null)
            return;
        for (int i = 0; i < chunk.mats.Length; i++)
        {
            if (chunk.mats[i] != null)
                chunk.mats[i].SetVector(SkyParamsId, new Vector4(skyZ, meshRadius, liftY, 0f));
        }
    }

    static void SetActive(ref CloudChunk chunk, bool draw)
    {
        if (chunk.mr != null)
            chunk.mr.enabled = draw;
        if (chunk.tf != null)
            chunk.tf.gameObject.SetActive(draw);
    }

    void EnsureChunks()
    {
        Load(ref _domeA0, TexDomeA0);
        Load(ref _domeA1, TexDomeA1);
        Load(ref _domeB0, TexDomeB0);
        Load(ref _domeB1, TexDomeB1);
        if (_capMesh == null)
            _capMesh = LoadPackageMesh(MeshCapPath);
        if (_beltMesh == null)
            _beltMesh = LoadPackageMesh(MeshBeltPath);
        Shader shader = Shader.Find(ShaderName);
        if (shader == null || _domeA0 == null || _domeA1 == null || _domeB0 == null || _domeB1 == null ||
            _capMesh == null || _beltMesh == null)
            return;

        Wrap(_domeA0, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
        Wrap(_domeA1, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
        Wrap(_domeB0, TextureWrapMode.Repeat, TextureWrapMode.Clamp);
        Wrap(_domeB1, TextureWrapMode.Repeat, TextureWrapMode.Clamp);

        EnsureChunk(ref _domeA, "L2CloudSimple_Cap", _capMesh, 2);
        EnsureChunk(ref _domeB, "L2CloudSimple_Ring", _beltMesh, 2);

        if (_domeA.mats == null)
        {
            _domeA.mats = new[]
            {
                MakeMat(shader, _domeA0, DaySheetTint, 2970, true, SkyCapRadius),
                MakeMat(shader, _domeA1, DaySheetTint, 2971, true, SkyCapRadius)
            };
            _domeA.mr.sharedMaterials = _domeA.mats;
        }

        if (_domeB.mats == null)
        {
            _domeB.mats = new[]
            {
                MakeMat(shader, _domeB0, DaySheetTint, 2973, true, SkyBeltRadius),
                MakeMat(shader, _domeB1, DaySilhouetteTint, 2974, false, SkyBeltRadius)
            };
            _domeB.mr.sharedMaterials = _domeB.mats;
        }
    }

    static void Load(ref Texture2D slot, string path)
    {
        if (slot == null)
            slot = Resources.Load<Texture2D>(path);
    }

    /// <summary>
    /// FBX under StaticMeshes. Sky export keeps UE units (no *0.01). Scale Factor = 1.
    /// </summary>
    static Mesh LoadPackageMesh(string resourcesPath)
    {
        Mesh direct = Resources.Load<Mesh>(resourcesPath);
        if (direct != null)
            return direct;

        Mesh[] all = Resources.LoadAll<Mesh>(resourcesPath);
        if (all == null || all.Length == 0)
            return null;

        Mesh best = null;
        int bestVerts = -1;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].vertexCount <= bestVerts)
                continue;
            best = all[i];
            bestVerts = all[i].vertexCount;
        }

        return best;
    }

    static void Wrap(Texture2D tex, TextureWrapMode u, TextureWrapMode v)
    {
        if (tex == null)
            return;
        tex.wrapModeU = u;
        tex.wrapModeV = v;
    }

    void EnsureChunk(ref CloudChunk chunk, string goName, Mesh source, int materialPasses)
    {
        if (source == null)
            return;

        bool needRebuild = chunk.mesh == null || chunk.mesh.name != source.name + "_cloud";
        if (needRebuild)
        {
            DestroyObj(chunk.mesh);
            chunk.mesh = BuildMultiPassMesh(source, materialPasses);
            if (chunk.mf != null)
                chunk.mf.sharedMesh = chunk.mesh;
        }

        if (chunk.tf != null)
            return;

        var go = new GameObject(goName);
        go.hideFlags = HideFlags.HideAndDontSave;
        ApplyCloudLayer(go);
        chunk.tf = go.transform;
        chunk.tf.SetParent(transform, false);

        chunk.mf = go.AddComponent<MeshFilter>();
        chunk.mf.sharedMesh = chunk.mesh;
        chunk.mr = go.AddComponent<MeshRenderer>();
        chunk.mr.shadowCastingMode = ShadowCastingMode.Off;
        chunk.mr.receiveShadows = false;
        chunk.mr.allowOcclusionWhenDynamic = false;
        chunk.mr.lightProbeUsage = LightProbeUsage.Off;
        chunk.mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    /// <summary>
    /// Package mesh has one active material; dual cloud sheets need N identical passes.
    /// </summary>
    static Mesh BuildMultiPassMesh(Mesh source, int passes)
    {
        var mesh = new Mesh { name = source.name + "_cloud" };
        mesh.hideFlags = HideFlags.DontSave;
        mesh.vertices = source.vertices;
        mesh.uv = source.uv;
        mesh.normals = source.normals;
        int n = source.vertexCount;
        var colors = new Color[n];
        // RenderDoc mesh_out_1.csv: FF in_Color0 is exactly white for every
        // cloud vertex. FBX may contain a stale/black color channel; ignore it.
        for (int i = 0; i < n; i++)
            colors[i] = Color.white;
        mesh.colors = colors;
        int[] tris = source.triangles;
        if (tris == null || tris.Length < 3)
        {
            // Prefer first non-empty submesh.
            for (int i = 0; i < source.subMeshCount; i++)
            {
                int[] sub = source.GetTriangles(i);
                if (sub != null && sub.Length >= 3)
                {
                    tris = sub;
                    break;
                }
            }
        }

        if (tris == null)
            tris = System.Array.Empty<int>();

        passes = Mathf.Max(1, passes);
        mesh.subMeshCount = passes;
        for (int i = 0; i < passes; i++)
            mesh.SetTriangles(tris, i, true);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Material MakeMat(Shader shader, Texture2D tex, Color tint, int queue, bool alphaBlend, float meshRadius)
    {
        var mat = new Material(shader);
        mat.hideFlags = HideFlags.DontSave;
        mat.SetTexture("_MainTex", tex);
        mat.SetVector(TextureFactorId, new Vector4(tint.r, tint.g, tint.b, tint.a));
        mat.SetVector(SkyParamsId, new Vector4(88f, meshRadius, 0f, 0f));
        mat.SetFloat(SrcBlendId, alphaBlend ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
        mat.SetFloat(DstBlendId, alphaBlend ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.One);
        mat.renderQueue = queue;
        return mat;
    }

    static void ApplyCloudLayer(GameObject go)
    {
        if (go == null)
            return;
        if (L2FxCompositorRuntime.PreferGpuQueue)
            L2FxCompositorLayers.ApplyL2CloudsLayer(go);
        else
            go.layer = 0;
    }

    float ResolveSkyDistance(Camera cam)
    {
        float far = cam != null ? cam.farClipPlane : 500f;
        return Mathf.Clamp(far * 0.88f, 80f, 4000f);
    }

    Camera ResolveCamera()
    {
        if (CameraController.Instance != null)
        {
            Camera fromController = CameraController.Instance.GetComponent<Camera>();
            if (fromController == null)
                fromController = CameraController.Instance.GetComponentInChildren<Camera>();
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

    void DestroyChunks()
    {
        DestroyChunk(ref _domeA);
        DestroyChunk(ref _domeB);
    }

    static void DestroyChunk(ref CloudChunk chunk)
    {
        if (chunk.mats != null)
        {
            for (int i = 0; i < chunk.mats.Length; i++)
                DestroyObj(chunk.mats[i]);
            chunk.mats = null;
        }

        DestroyObj(chunk.mesh);
        chunk.mesh = null;
        if (chunk.tf != null)
            DestroyObj(chunk.tf.gameObject);
        chunk.tf = null;
        chunk.mf = null;
        chunk.mr = null;
    }

    static void DestroyObj(Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
