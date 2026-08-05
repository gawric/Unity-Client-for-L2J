using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lobby names — L2 canvas path: Project → glyph quads → one mesh / one material draw
/// (atlas * vertexColor). Tune via _pixelScale / _headHeightOffset.
/// </summary>
public class LobbyNameplatesManager : MonoBehaviour
{
    private const int MaxSlots = 8;
    private const string ShaderResourcePath = "Data/Shaders/UI/L2BitmapFont";
    private const string ShaderName = "L2/UI/BitmapFont";

    [SerializeField] private Camera _camera;
    [SerializeField] private float _nameplateViewDistance = 80f;
    [SerializeField] private Color _defaultNameColor = Color.white;
    [Tooltip("Reserved for UU→meters when porting world offsets. Unused while head uses CharacterController.")]
    [SerializeField] private float _worldCalibK = 1f;
    [Tooltip("Glyph pixel scale. Tune later to match L2; 1 = native atlas pixels.")]
    [SerializeField] private float _pixelScale = 1f;
    [Tooltip("Meters added after CharacterController capsule top. Negative lowers names (CC is often taller than mesh).")]
    [SerializeField] private float _headHeightOffset = -0.12f;
    [SerializeField] private string _atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas";
    [SerializeField] private string _csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii";

    private readonly List<PaintItem> _paintList = new List<PaintItem>(MaxSlots);
    private readonly List<Vector3> _pixelVerts = new List<Vector3>(256);
    private readonly List<Vector3> _worldVerts = new List<Vector3>(256);
    private readonly List<Vector2> _uvs = new List<Vector2>(256);
    private readonly List<Color32> _colors = new List<Color32>(256);
    private readonly List<int> _indices = new List<int>(384);

    private L2BitmapFont _font;
    private Material _material;
    private Mesh _mesh;
    private bool _loggedReady;

    private static LobbyNameplatesManager _instance;
    public static LobbyNameplatesManager Instance => _instance;

    public Camera Camera
    {
        get => _camera;
        set => _camera = value;
    }

    private struct PaintItem
    {
        public Vector3 World;
        public string Name;
        public Color Color;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        EnsureResources();
    }

    private void OnDestroy()
    {
        if (_mesh != null)
        {
            Destroy(_mesh);
            _mesh = null;
        }

        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void EnsureResources()
    {
        if (_font == null)
        {
            _font = L2BitmapFont.LoadFromResources(_atlasResourcePath, _csvResourcePath);
            if (_font != null && !_loggedReady)
            {
                _loggedReady = true;
                Debug.Log($"[LobbyNameplates] Font ready atlas={_font.AtlasWidth}x{_font.AtlasHeight} (batched DrawMesh)");
            }
        }

        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                shader = Shader.Find(ShaderName);
            }

            if (shader == null)
            {
                Debug.LogError($"[LobbyNameplates] Shader '{ShaderName}' not found.");
                return;
            }

            _material = new Material(shader)
            {
                name = "L2LobbyBitmapFont (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 5000
            };
            if (_font != null && _font.Atlas != null)
            {
                _material.mainTexture = _font.Atlas;
                _material.SetTexture("_MainTex", _font.Atlas);
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "L2LobbyNameplates", hideFlags = HideFlags.HideAndDontSave };
            _mesh.MarkDynamic();
        }
    }

    private Camera ResolveCamera()
    {
        if (_camera != null)
        {
            return _camera;
        }

        if (CharacterSelector.Instance != null && CharacterSelector.Instance.Camera != null)
        {
            return CharacterSelector.Instance.Camera;
        }

        return null;
    }

    private void LateUpdate()
    {
        EnsureResources();
        Camera cam = ResolveCamera();
        if (_font == null || _material == null || _mesh == null || cam == null || !cam.isActiveAndEnabled)
        {
            return;
        }

        BuildPaintList(cam);
        if (_paintList.Count == 0)
        {
            return;
        }

        if (!RebuildMesh(cam))
        {
            return;
        }

        // One submit for all lobby names (L2 FCanvasUtil flush equivalent).
        Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, 0, cam);
    }

    private bool RebuildMesh(Camera cam)
    {
        _pixelVerts.Clear();
        _worldVerts.Clear();
        _uvs.Clear();
        _colors.Clear();
        _indices.Clear();

        float scale = _pixelScale > 0f ? _pixelScale : 1f;
        float screenH = Screen.height;
        float lineH = _font.MeasureHeight(scale);

        for (int i = 0; i < _paintList.Count; i++)
        {
            PaintItem item = _paintList[i];
            Vector3 screen = cam.WorldToScreenPoint(item.World);
            if (screen.z <= 0f)
            {
                continue;
            }

            float textW = _font.MeasureWidth(item.Name, scale);
            float x = screen.x - textW * 0.5f;
            // GUI/L2 Y-down top of string; projected point = bottom of text.
            float yTop = screenH - screen.y - lineH;

            int vertStart = _pixelVerts.Count;
            _font.AppendString(
                item.Name,
                x,
                yTop,
                scale,
                item.Color,
                _pixelVerts,
                _uvs,
                _colors,
                _indices);

            float z = screen.z;
            for (int v = vertStart; v < _pixelVerts.Count; v++)
            {
                Vector3 p = _pixelVerts[v];
                // pixel Y is top-down → Unity screen Y bottom-up
                float sy = screenH - p.y;
                _worldVerts.Add(cam.ScreenToWorldPoint(new Vector3(p.x, sy, z)));
            }
        }

        _mesh.Clear(false);
        if (_worldVerts.Count == 0)
        {
            return false;
        }

        _mesh.SetVertices(_worldVerts);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_indices, 0, false);
        _mesh.RecalculateBounds();
        return true;
    }

    private void BuildPaintList(Camera cam)
    {
        _paintList.Clear();

        if (CharacterSelector.Instance == null)
        {
            return;
        }

        IReadOnlyList<GameObject> pawns = CharacterSelector.Instance.CharacterPawns;
        if (pawns == null)
        {
            return;
        }

        int count = Mathf.Min(pawns.Count, MaxSlots);
        for (int i = 0; i < count; i++)
        {
            GameObject pawn = pawns[i];
            if (pawn == null)
            {
                continue;
            }

            SelectableCharacterEntity entity = pawn.GetComponent<SelectableCharacterEntity>();
            if (entity == null || entity.CharacterInfoInterlude == null)
            {
                continue;
            }

            Transform t = entity.transform;
            if (!IsNameplateVisible(cam, t))
            {
                continue;
            }

            CharSelectInfoPackage info = entity.CharacterInfoInterlude;
            if (string.IsNullOrEmpty(info.Name))
            {
                continue;
            }

            _paintList.Add(new PaintItem
            {
                World = GetHeadWorldPos(t, info),
                Name = info.Name,
                Color = ResolveNameColor(info.Karma)
            });
        }
    }

    private Vector3 GetHeadWorldPos(Transform target, CharSelectInfoPackage info)
    {
        _ = info;

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = target.GetComponentInChildren<CharacterController>();
        }

        if (cc != null)
        {
            // Capsule top = center + up * (height * 0.5); offset tunes CC vs real head.
            Vector3 localTop = cc.center + Vector3.up * (cc.height * 0.5f);
            return target.TransformPoint(localTop) + Vector3.up * _headHeightOffset;
        }

        return target.position + Vector3.up * (0.92f + _headHeightOffset);
    }

    private Color ResolveNameColor(int karma)
    {
        if (karma <= 0)
        {
            return _defaultNameColor;
        }

        float t = Mathf.Clamp01(karma / 1000f);
        return Color.Lerp(Color.white, new Color(1f, 0.25f, 0.25f, 1f), t);
    }

    private bool IsNameplateVisible(Camera cam, Transform target)
    {
        if (target == null || cam == null)
        {
            return false;
        }

        return Vector3.Distance(cam.transform.position, target.position) <= _nameplateViewDistance;
    }
}
