using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Lobby names — same L2 canvas path as <see cref="NameplatesManager"/>:
/// Project → screen-pixel glyphs → <see cref="L2NameplateScreenBatch"/> (one draw).
/// </summary>
public class LobbyNameplatesManager : MonoBehaviour
{
    private const int MaxSlots = 8;

    [SerializeField] private Camera _camera;
    [SerializeField] private float _nameplateViewDistance = 80f;
    [SerializeField] private Color _defaultNameColor = Color.white;
    [Tooltip("Glyph pixel scale. Tune later to match L2; 1 = native atlas pixels.")]
    [SerializeField] private float _pixelScale = 1f;
    [Tooltip("Meters after L2 capsule top (Location + CollisionHeight). Negative lowers names. Shared with world.")]
    [SerializeField] private float _headHeightOffset = -0.12f;
    [Tooltip("L2 canvas: lock plate to whole screen pixels (hysteresis).")]
    [SerializeField] private bool _snapAnchorToPixels = true;
    [SerializeField] private float _snapHysteresisPx = 0.75f;
    [SerializeField] private bool _snapDiscardSubpixelFrac = true;
    [SerializeField] private string _atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas";
    [SerializeField] private string _csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii";

    private readonly List<PaintItem> _paintList = new List<PaintItem>(MaxSlots);
    private readonly Dictionary<int, Vector2> _snapPixels = new Dictionary<int, Vector2>(MaxSlots);
    private readonly L2NameplateScreenBatch _batch = new L2NameplateScreenBatch();

    private L2BitmapFont _font;
    private bool _loggedReady;
    private bool _subscribed;

    private static LobbyNameplatesManager _instance;
    public static LobbyNameplatesManager Instance => _instance;

    public Camera Camera
    {
        get => _camera;
        set => _camera = value;
    }

    private struct PaintItem
    {
        public int Slot;
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

    private void OnEnable()
    {
        if (!_subscribed)
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _subscribed = false;
        }
    }

    private void OnDestroy()
    {
        OnDisable();
        _batch.Dispose();

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
                Debug.Log($"[LobbyNameplates] Font ready atlas={_font.AtlasWidth}x{_font.AtlasHeight} (screen-space batch)");
            }
        }

        if (_font != null)
        {
            _batch.EnsureMaterial(_font.Atlas);
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

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Camera targetCam = ResolveCamera();
        if (cam == null || targetCam == null || cam != targetCam || !cam.isActiveAndEnabled)
        {
            return;
        }

        EnsureResources();
        if (_font == null || !_batch.IsReady)
        {
            return;
        }

        BuildPaintList(cam);
        if (_paintList.Count == 0)
        {
            return;
        }

        if (!RebuildAndDraw(cam))
        {
            return;
        }
    }

    private bool RebuildAndDraw(Camera cam)
    {
        _batch.BeginFrame();

        float scale = _pixelScale > 0f ? _pixelScale : 1f;
        float screenH = cam.pixelHeight;
        float lineH = _font.MeasureHeight(scale);
        bool discardFrac = _snapAnchorToPixels && _snapDiscardSubpixelFrac;

        for (int i = 0; i < _paintList.Count; i++)
        {
            PaintItem item = _paintList[i];
            Vector3 screen = cam.WorldToScreenPoint(item.World);
            if (screen.z <= 0f)
            {
                continue;
            }

            float ax = screen.x;
            float ay = screen.y;
            if (_snapAnchorToPixels)
            {
                ax = SnapAxisWithHysteresis(item.Slot, screen.x, true, screen.z);
                ay = SnapAxisWithHysteresis(item.Slot, screen.y, false, screen.z);
            }

            float textW = _font.MeasureWidth(item.Name, scale);
            float x = ax - textW * 0.5f;
            // GUI/L2 Y-down top of string; projected point = bottom of text.
            float yTop = screenH - ay - lineH;

            _batch.AppendLine(
                _font, item.Name, x, yTop, scale, item.Color, screen.z, screenH, discardFrac);
        }

        return _batch.UploadAndDraw(cam);
    }

    private float SnapAxisWithHysteresis(int id, float raw, bool isX, float distanceAlongView)
    {
        float hold = Mathf.Max(0.51f, _snapHysteresisPx);
        if (distanceAlongView > 0.01f && distanceAlongView < 2.5f)
        {
            hold = Mathf.Max(hold, 1.4f / Mathf.Max(0.45f, distanceAlongView));
        }

        float candidate = Mathf.Round(raw);

        if (!_snapPixels.TryGetValue(id, out Vector2 last))
        {
            last = new Vector2(candidate, candidate);
            _snapPixels[id] = last;
            return candidate;
        }

        float prev = isX ? last.x : last.y;
        float snapped = Mathf.Abs(raw - prev) < hold ? prev : candidate;
        if (isX)
        {
            last.x = snapped;
        }
        else
        {
            last.y = snapped;
        }

        _snapPixels[id] = last;
        return snapped;
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
                Slot = i,
                World = GetHeadWorldPos(t, info),
                Name = info.Name,
                Color = ResolveNameColor(info.Karma)
            });
        }
    }

    private Vector3 GetHeadWorldPos(Transform target, CharSelectInfoPackage info)
    {
        // Char-select has no CollisionHeight on Appearance yet — same default as world.
        _ = info;
        return L2NameplateAnchor.GetHeadWorldPos(
            target, L2NameplateAnchor.DefaultCollisionHeightMeters, _headHeightOffset);
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
