using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// World nameplates — L2 canvas path: Project → screen-pixel glyphs →
/// <see cref="L2NameplateScreenBatch"/> (one draw). Title above name; bubbles deferred.
/// </summary>
public class NameplatesManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _nameplateViewDistance = 50f;
    [SerializeField] private LayerMask _entityMask;
    [SerializeField] private Color _defaultNameColor = Color.white;
    [SerializeField] private float _pixelScale = 1f;
    [Tooltip("Meters after L2 capsule top (Location + CollisionHeight). Negative lowers names.")]
    [SerializeField] private float _headHeightOffset = -0.12f;
    [Tooltip("L2 canvas: lock plate to whole screen pixels (hysteresis). Crisp glyphs; plate steps 1px.")]
    [SerializeField] private bool _snapAnchorToPixels = true;
    [Tooltip("Base hold (px) before snap moves. Close-up auto-widens (~2.8px at orbit 0.5m).")]
    [SerializeField] private float _snapHysteresisPx = 0.75f;
    [Tooltip("A/B: with snap, ignore fracX/Y (integer glyph quads). Best-so-far = ON.")]
    [SerializeField] private bool _snapDiscardSubpixelFrac = true;
    [Tooltip("Draw IdentityInterlude.Title above the name (same atlas).")]
    [SerializeField] private bool _drawTitles = true;
    [SerializeField] private float _titleGapPixels = 2f;
    [SerializeField] private string _atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas";
    [SerializeField] private string _csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii";

    /// <summary>Kept for inspector/debug; filled by SphereCast discovery.</summary>
    [SerializeField] public RaycastHit[] _entitiesInRange;

    private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>(64);
    private readonly List<PaintItem> _paintList = new List<PaintItem>(64);
    private readonly List<int> _removeIds = new List<int>(32);
    private readonly List<int> _entryKeys = new List<int>(64);
    private readonly Dictionary<int, Vector2> _snapPixels = new Dictionary<int, Vector2>(64);
    private readonly L2NameplateScreenBatch _batch = new L2NameplateScreenBatch();

    private Transform _playerTransform;
    private L2BitmapFont _font;
    private int _removeObjId;
    private bool _loggedReady;
    private bool _subscribed;

    private static NameplatesManager _instance;
    public static NameplatesManager Instance => _instance;

    public Camera Camera
    {
        get => _camera;
        set => _camera = value;
    }

    private struct Entry
    {
        public int Id;
        public Transform Target;
        public CharacterController CC;
        public CapsuleCollider Capsule;
        public Entity Entity;
        public string Name;
        public string Title;
        public Color NameColor;
        public Color TitleColor;
        public bool Visible;
    }

    private struct PaintItem
    {
        public int Id;
        public Vector3 World;
        public string Name;
        public string Title;
        public Color NameColor;
        public Color TitleColor;
        public bool IsLocalPlayer;
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
        _entries.Clear();
        _batch.Dispose();

        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void SetMask(LayerMask mask)
    {
        _entityMask = mask;
    }

    public void Remove(int id)
    {
        if (_entries.Remove(id))
        {
            _removeObjId = id;
            _snapPixels.Remove(id);
        }
    }

    private void UpsertEntry(Entity entity)
    {
        if (entity == null || entity.IdentityInterlude == null || entity.transform == null)
        {
            return;
        }

        NetworkIdentityInterlude idn = entity.IdentityInterlude;
        if (string.IsNullOrEmpty(idn.Name))
        {
            return;
        }

        int id = idn.Id;
        Color titleColor = _defaultNameColor;
        if (!string.IsNullOrEmpty(idn.TitleColor))
        {
            titleColor = StringUtils.HexToColor(idn.TitleColor);
        }

        CharacterController cc = null;
        CapsuleCollider capsule = null;
        if (_entries.TryGetValue(id, out Entry existing))
        {
            cc = existing.CC;
            capsule = existing.Capsule;
        }

        Transform target = entity.transform;
        if (cc == null)
        {
            cc = target.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = target.GetComponentInChildren<CharacterController>();
            }
        }

        if (capsule == null && cc == null)
        {
            capsule = target.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = target.GetComponentInChildren<CapsuleCollider>();
            }
        }

        Entry entry = new Entry
        {
            Id = id,
            Target = target,
            CC = cc,
            Capsule = capsule,
            Entity = entity,
            Name = idn.Name,
            Title = idn.Title ?? string.Empty,
            NameColor = _defaultNameColor,
            TitleColor = titleColor,
            Visible = true
        };

        _entries[id] = entry;
    }

    private void EnsureResources()
    {
        if (_font == null)
        {
            _font = L2BitmapFont.LoadFromResources(_atlasResourcePath, _csvResourcePath);
            if (_font != null && !_loggedReady)
            {
                _loggedReady = true;
                Debug.Log($"[Nameplates] Font ready atlas={_font.AtlasWidth}x{_font.AtlasHeight} (screen-space batch)");
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

        return UnityEngine.Camera.main;
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null)
        {
            if (PlayerEntity.Instance != null && PlayerEntity.Instance.transform != null)
            {
                _playerTransform = PlayerEntity.Instance.transform;
            }
            else
            {
                return;
            }
        }

        _entitiesInRange = Physics.SphereCastAll(
            _playerTransform.position,
            _nameplateViewDistance,
            transform.forward,
            0f,
            _entityMask);

        UpsertFromHits();
        EnsureHoverAndTarget();

        // Local player often missed by SphereCast from own origin — force upsert for paint.
        if (PlayerEntity.Instance != null)
        {
            UpsertEntry(PlayerEntity.Instance);
        }

        RefreshVisibilityFlags();
    }

    /// <summary>
    /// After camera LateUpdate: Project → screen-pixel glyphs (L2 UCanvas path).
    /// </summary>
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Camera targetCam = ResolveCamera();
        if (cam == null || targetCam == null || cam != targetCam)
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

        RebuildAndDraw(cam);
    }

    private void UpsertFromHits()
    {
        if (_entitiesInRange == null)
        {
            return;
        }

        for (int i = 0; i < _entitiesInRange.Length; i++)
        {
            Transform hitT = _entitiesInRange[i].transform;
            if (hitT == null)
            {
                continue;
            }

            Entity entity = hitT.GetComponent<Entity>();
            if (entity == null || entity.IdentityInterlude == null)
            {
                continue;
            }

            int id = entity.IdentityInterlude.Id;
            if (id == _removeObjId)
            {
                continue;
            }

            UpsertEntry(entity);
        }
    }

    private void EnsureHoverAndTarget()
    {
        if (ClickManager.Instance != null && ClickManager.Instance.HoverObjectData != null)
        {
            ObjectData hover = ClickManager.Instance.HoverObjectData;
            if (hover.ObjectTransform != null &&
                _entityMask == (_entityMask | (1 << hover.ObjectLayer)))
            {
                Entity e = hover.ObjectTransform.GetComponent<Entity>();
                if (e != null)
                {
                    UpsertEntry(e);
                }
            }
        }

        if (TargetManager.Instance != null && TargetManager.Instance.HasTarget())
        {
            Entity e = TargetManager.Instance.Target.Data.ObjectTransform.GetComponent<Entity>();
            if (e != null)
            {
                UpsertEntry(e);
            }
        }
    }

    private void RefreshVisibilityFlags()
    {
        _removeIds.Clear();
        _entryKeys.Clear();
        foreach (int id in _entries.Keys)
        {
            _entryKeys.Add(id);
        }

        for (int i = 0; i < _entryKeys.Count; i++)
        {
            int id = _entryKeys[i];
            if (!_entries.TryGetValue(id, out Entry e))
            {
                continue;
            }

            if (e.Target == null)
            {
                _removeIds.Add(id);
                continue;
            }

            bool visible = IsNameplateVisible(e.Target);
            e.Visible = visible;
            _entries[id] = e;

            bool isLocal = PlayerEntity.Instance != null && e.Entity == PlayerEntity.Instance;
            if (!visible && !isLocal && !IsHoverOrTarget(e.Target))
            {
                _removeIds.Add(id);
            }
        }

        for (int i = 0; i < _removeIds.Count; i++)
        {
            int rid = _removeIds[i];
            _entries.Remove(rid);
            _snapPixels.Remove(rid);
        }
    }

    private void BuildPaintList(Camera cam)
    {
        _paintList.Clear();

        foreach (KeyValuePair<int, Entry> kv in _entries)
        {
            Entry e = kv.Value;
            if (!e.Visible || e.Target == null || string.IsNullOrEmpty(e.Name))
            {
                continue;
            }

            if (e.Entity != null && e.Entity.IdentityInterlude != null)
            {
                NetworkIdentityInterlude idn = e.Entity.IdentityInterlude;
                e.Name = idn.Name;
                e.Title = idn.Title ?? string.Empty;
                if (!string.IsNullOrEmpty(idn.TitleColor))
                {
                    e.TitleColor = StringUtils.HexToColor(idn.TitleColor);
                }
            }

            bool isLocal = PlayerEntity.Instance != null && e.Entity == PlayerEntity.Instance;

            _paintList.Add(new PaintItem
            {
                Id = e.Id,
                World = GetHeadWorldPos(e),
                Name = e.Name,
                Title = _drawTitles ? e.Title : null,
                NameColor = e.NameColor,
                TitleColor = e.TitleColor,
                IsLocalPlayer = isLocal
            });
        }
    }

    private void RebuildAndDraw(Camera cam)
    {
        _batch.BeginFrame();

        float scale = _pixelScale > 0f ? _pixelScale : 1f;
        float screenH = cam.pixelHeight;
        float lineH = _font.MeasureHeight(scale);
        float titleGap = Mathf.Max(0f, _titleGapPixels);
        bool discardFrac = _snapAnchorToPixels && _snapDiscardSubpixelFrac;

        float orbitDist = 0f;
        if (CameraController.Instance != null)
        {
            orbitDist = CameraController.Instance.CurrentDistance;
        }

        for (int i = 0; i < _paintList.Count; i++)
        {
            PaintItem item = _paintList[i];
            Vector3 screen = cam.WorldToScreenPoint(item.World);
            if (screen.z <= 0f)
            {
                continue;
            }

            float rawX = screen.x;
            float rawY = screen.y;
            float depth = screen.z;

            float ax = rawX;
            float ay = rawY;
            if (_snapAnchorToPixels)
            {
                float snapDist = (item.IsLocalPlayer && orbitDist > 0.05f) ? orbitDist : depth;
                ax = SnapAxisWithHysteresis(item.Id, rawX, true, snapDist);
                ay = SnapAxisWithHysteresis(item.Id, rawY, false, snapDist);
            }

            float yNameTop = screenH - ay - lineH;

            bool hasTitle = !string.IsNullOrEmpty(item.Title);
            if (hasTitle)
            {
                float titleW = _font.MeasureWidth(item.Title, scale);
                float xTitle = ax - titleW * 0.5f;
                float yTitleTop = yNameTop - lineH - titleGap;
                _batch.AppendLine(
                    _font, item.Title, xTitle, yTitleTop, scale, item.TitleColor,
                    depth, screenH, discardFrac);
            }

            float nameW = _font.MeasureWidth(item.Name, scale);
            float xName = ax - nameW * 0.5f;
            _batch.AppendLine(
                _font, item.Name, xName, yNameTop, scale, item.NameColor,
                depth, screenH, discardFrac);
        }

        _batch.UploadAndDraw(cam);
    }

    private float SnapAxisWithHysteresis(int id, float raw, bool isX, float distanceAlongView)
    {
        float hold = Mathf.Max(0.51f, _snapHysteresisPx);
        // Close-up: WorldToScreen jumps ~±1px from sub-mm cam motion (logs at orbit=0.5).
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

    /// <summary>
    /// L2 DrawTargetName anchor — shared with lobby via <see cref="L2NameplateAnchor"/>.
    /// </summary>
    private Vector3 GetHeadWorldPos(Entry entry)
    {
        float ch = L2NameplateAnchor.DefaultCollisionHeightMeters;
        Entity entity = entry.Entity;
        if (entity != null && entity.Appearance != null)
        {
            ch = entity.Appearance.CollisionHeight;
        }

        return L2NameplateAnchor.GetHeadWorldPos(
            entry.Target, entry.CC, entry.Capsule, ch, _headHeightOffset);
    }

    private bool IsHoverOrTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (ClickManager.Instance != null &&
            ClickManager.Instance.HoverObjectData != null &&
            ClickManager.Instance.HoverObjectData.ObjectTransform == target)
        {
            return true;
        }

        if (TargetManager.Instance != null &&
            TargetManager.Instance.HasTarget() &&
            TargetManager.Instance.Target.Data.ObjectTransform == target)
        {
            return true;
        }

        return false;
    }

    private bool IsNameplateVisible(Transform target)
    {
        if (target == null || _playerTransform == null)
        {
            return false;
        }

        // Always keep own nameplate — SphereCast/occlusion from self often fails.
        if (PlayerEntity.Instance != null && target == PlayerEntity.Instance.transform)
        {
            return true;
        }

        if (IsHoverOrTarget(target))
        {
            return true;
        }

        if (Vector3.Distance(_playerTransform.position, target.position) > _nameplateViewDistance)
        {
            return false;
        }

        if (CameraController.Instance != null)
        {
            return CameraController.Instance.IsObjectVisible(target);
        }

        return true;
    }
}
