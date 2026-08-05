using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// World nameplates — same L2 path as <see cref="LobbyNameplatesManager"/>:
/// discover entities → Project → atlas glyph quads → one DrawMesh.
/// Title is a second line above the name (same font/atlas); hover/target bubbles deferred.
/// </summary>
public class NameplatesManager : MonoBehaviour
{
    private const string ShaderResourcePath = "Data/Shaders/UI/L2BitmapFont";
    private const string ShaderName = "L2/UI/BitmapFont";

    [SerializeField] private Camera _camera;
    [SerializeField] private float _nameplateViewDistance = 50f;
    [SerializeField] private LayerMask _entityMask;
    [SerializeField] private Color _defaultNameColor = Color.white;
    [SerializeField] private float _pixelScale = 1f;
    [Tooltip("Meters after L2 capsule top (Location + CollisionHeight). Negative lowers names.")]
    [SerializeField] private float _headHeightOffset = -0.12f;
    [Tooltip("Local player only: nameplate plane depth = camera orbit (not head z). Stops scale breath while orbiting.")]
    [SerializeField] private bool _stabilizeLocalPlayerPwFromOrbit = true;
    [Tooltip("L2 canvas: lock plate to whole screen pixels (hysteresis). Crisp glyphs; plate steps 1px. Off = smooth but point-filter shimmers at ~.5.")]
    [SerializeField] private bool _snapAnchorToPixels = true;
    [Tooltip("Base hold (px) before snap moves. Close-up auto-widens (~2.8px at orbit 0.5m).")]
    [SerializeField] private float _snapHysteresisPx = 0.75f;
    [Tooltip("If plate depth < near+margin, push plane out. Does not rewrite halfH/pw.")]
    [SerializeField] private bool _clampPlaneToNearClip = true;
    [Tooltip("A/B close-up: with snap, ignore fracX/Y (integer glyph quads). Best-so-far = ON.")]
    [SerializeField] private bool _snapDiscardSubpixelFrac = true;
    [Tooltip("Local: reproject snapped ax/ay from head z onto planeDepth (orbit).")]
    [SerializeField] private bool _reprojectLocalAnchorToPlane = true;
    [Tooltip("Draw IdentityInterlude.Title above the name (same atlas).")]
    [SerializeField] private bool _drawTitles = true;
    [SerializeField] private float _titleGapPixels = 2f;
    [SerializeField] private string _atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas";
    [SerializeField] private string _csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii";
    [Header("Diag — local player nameplate")]
    [SerializeField] private bool _diagLocalPlayer = false;
    [Tooltip("Seconds between diag lines while camera moves / values change.")]
    [SerializeField] private float _diagIntervalSec = 0.05f;
    [SerializeField] private bool _diagLogToFile = true;

    /// <summary>Kept for inspector/debug; filled by SphereCast discovery.</summary>
    [SerializeField] public RaycastHit[] _entitiesInRange;

    private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>(64);
    private readonly List<PaintItem> _paintList = new List<PaintItem>(64);
    private readonly List<Vector3> _pixelVerts = new List<Vector3>(1024);
    private readonly List<Vector3> _worldVerts = new List<Vector3>(1024);
    private readonly List<Vector2> _uvs = new List<Vector2>(1024);
    private readonly List<Color32> _colors = new List<Color32>(1024);
    private readonly List<int> _indices = new List<int>(1536);
    private readonly List<int> _removeIds = new List<int>(32);
    private readonly List<int> _entryKeys = new List<int>(64);
    private readonly Dictionary<int, Vector2> _snapPixels = new Dictionary<int, Vector2>(64);

    private Transform _playerTransform;
    private L2BitmapFont _font;
    private Material _material;
    private Mesh _mesh;
    private Vector3 _meshOrigin;
    private int _removeObjId;
    private bool _loggedReady;
    private bool _subscribed;

    // Local-player nameplate diagnostics (wave while orbiting).
    private float _diagNextTime;
    private Vector3 _diagPrevHead;
    private Vector3 _diagPrevCamPos;
    private float _diagPrevScreenZ;
    private float _diagPrevPw;
    private float _diagPrevAx;
    private float _diagPrevAy;
    private bool _diagHasPrev;
    private string _diagLogPath;

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
                Debug.Log($"[Nameplates] Font ready atlas={_font.AtlasWidth}x{_font.AtlasHeight} (batched DrawMesh)");
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
                Debug.LogError($"[Nameplates] Shader '{ShaderName}' not found.");
                return;
            }

            _material = new Material(shader)
            {
                name = "L2WorldBitmapFont (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 5000
            };
            if (_font != null && _font.Atlas != null)
            {
                _material.mainTexture = _font.Atlas;
                _material.SetTexture("_MainTex", _font.Atlas);
            }
        }

        // World path uses camera-local world verts (lobby-compatible shader, _ClipSpace=0).
        _material.SetFloat("_ClipSpace", 0f);

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "L2WorldNameplates", hideFlags = HideFlags.HideAndDontSave };
            _mesh.MarkDynamic();
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

        // Local player often missed by SphereCast from own origin — force upsert for paint/diag.
        if (PlayerEntity.Instance != null)
        {
            UpsertEntry(PlayerEntity.Instance);
        }

        RefreshVisibilityFlags();
    }

    /// <summary>
    /// After camera LateUpdate: project with final view, rigid camera-plane glyph quads.
    /// </summary>
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Camera targetCam = ResolveCamera();
        if (cam == null || targetCam == null || cam != targetCam)
        {
            return;
        }

        EnsureResources();
        if (_font == null || _material == null || _mesh == null)
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

        // Camera-relative verts → translate by cam position (float precision at world ~4700).
        Graphics.DrawMesh(_mesh, Matrix4x4.Translate(_meshOrigin), _material, 0, cam);
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

    private bool RebuildMesh(Camera cam)
    {
        _pixelVerts.Clear();
        _worldVerts.Clear();
        _uvs.Clear();
        _colors.Clear();
        _indices.Clear();

        // Origin for camera-relative verts (precision). DrawMesh translates back.
        _meshOrigin = cam.transform.position;

        float scale = _pixelScale > 0f ? _pixelScale : 1f;
        float screenH = cam.pixelHeight;
        float lineH = _font.MeasureHeight(scale);
        float titleGap = Mathf.Max(0f, _titleGapPixels);

        float orbitDist = 0f;
        if (_stabilizeLocalPlayerPwFromOrbit && CameraController.Instance != null)
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
                // Pass view distance so close-up can widen hysteresis (scrRaw jumps ~1px at 0.5m).
                float snapDist = (item.IsLocalPlayer && orbitDist > 0.05f) ? orbitDist : depth;
                ax = SnapAxisWithHysteresis(item.Id, rawX, true, snapDist);
                ay = SnapAxisWithHysteresis(item.Id, rawY, false, snapDist);
            }

            // Local player: pw/halfH from orbit (stable while yawing). Monsters: head depth.
            float orbitForItem = item.IsLocalPlayer ? orbitDist : 0f;
            float pw = ResolveWorldUnitsPerPixel(cam, depth, orbitForItem);
            float halfH = pw * screenH * 0.5f;

            float tanHalfFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float planeDepth = cam.orthographic
                ? 1f
                : halfH / Mathf.Max(0.001f, tanHalfFov);
            float near = cam.nearClipPlane;
            bool clamped = false;
            // Push plane past near clip, but NEVER rewrite halfH/pw (that shrinks glyphs
            // when clamp flickers). Main Camera near=0.1 → usually no-op at orbit 0.5.
            if (_clampPlaneToNearClip && !cam.orthographic)
            {
                float minDepth = near + 0.1f;
                if (planeDepth < minDepth)
                {
                    planeDepth = minDepth;
                    clamped = true;
                }
            }

            // Head is at screen.z; quads sit at planeDepth (orbit). Reproject snap
            // anchor onto the glyph plane so edges don't shear when cam rotates.
            if (_reprojectLocalAnchorToPlane
                && item.IsLocalPlayer
                && orbitDist > 0.05f
                && planeDepth > 0.05f)
            {
                float depthScale = depth / planeDepth;
                float cx = cam.pixelWidth * 0.5f;
                float cy = screenH * 0.5f;
                ax = (ax - cx) * depthScale + cx;
                ay = (ay - cy) * depthScale + cy;
            }

            // Diag off for play — enable _diagLocalPlayer + uncomment when debugging.
            // if (item.IsLocalPlayer && _diagLocalPlayer)
            // {
            //     TryLogLocalPlayerDiag(cam, item, screen, ax, ay, pw, halfH, planeDepth, near, clamped);
            // }

            float yNameTop = screenH - ay - lineH;

            bool hasTitle = !string.IsNullOrEmpty(item.Title);
            if (hasTitle)
            {
                float titleW = _font.MeasureWidth(item.Title, scale);
                // Keep fractional line origin; AppendLineCameraLocal does Floor+frac
                // so the whole line stays centered on snapped ax (no double-Floor).
                float xTitle = ax - titleW * 0.5f;
                float yTitleTop = yNameTop - lineH - titleGap;

                AppendLineCameraLocal(
                    cam, item.Title, xTitle, yTitleTop, scale, item.TitleColor,
                    halfH, planeDepth, screenH);
            }

            float nameW = _font.MeasureWidth(item.Name, scale);
            float xName = ax - nameW * 0.5f;

            AppendLineCameraLocal(
                cam, item.Name, xName, yNameTop, scale, item.NameColor,
                halfH, planeDepth, screenH);
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

    /// <summary>
    /// Camera-local planar quads. halfH/planeDepth from stable orbit (local) or head depth.
    /// Verts relative to cam.position for float32 precision at world ~4700.
    /// Always Floor pixel origins + frac shift so AppendString builds integer quads and the
    /// whole line moves rigidly (avoids half-pixel glyph tear at tiny pw / close cam).
    /// </summary>
    private void AppendLineCameraLocal(
        Camera cam,
        string text,
        float x,
        float yTop,
        float scale,
        Color color,
        float halfH,
        float planeDepth,
        float screenH)
    {
        float ox = Mathf.Floor(x);
        float oy = Mathf.Floor(yTop);
        float fracX = x - ox;
        float fracY = yTop - oy;
        if (_snapAnchorToPixels && _snapDiscardSubpixelFrac)
        {
            fracX = 0f;
            fracY = 0f;
        }

        int vertStart = _pixelVerts.Count;
        _font.AppendString(text, ox, oy, scale, color, _pixelVerts, _uvs, _colors, _indices);

        float pixelW = Mathf.Max(1, cam.pixelWidth);
        float pixelH = Mathf.Max(1, screenH);
        float halfW = halfH * (pixelW / pixelH);
        Quaternion camRot = cam.transform.rotation;

        for (int v = vertStart; v < _pixelVerts.Count; v++)
        {
            Vector3 p = _pixelVerts[v];
            float sx = p.x + fracX;
            float sy = (screenH - p.y) - fracY;
            float lx = ((sx / pixelW) - 0.5f) * 2f * halfW;
            float ly = ((sy / pixelH) - 0.5f) * 2f * halfH;
            _worldVerts.Add(camRot * new Vector3(lx, ly, planeDepth));
        }
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

    private static float ResolveWorldUnitsPerPixel(Camera cam, float headDepth, float orbitDistance)
    {
        float dist = orbitDistance > 0.05f ? orbitDistance : headDepth;
        return GetWorldUnitsPerPixel(cam, dist);
    }

    private static float GetWorldUnitsPerPixel(Camera cam, float distanceAlongForward)
    {
        if (cam.orthographic)
        {
            return (cam.orthographicSize * 2f) / Mathf.Max(1, cam.pixelHeight);
        }

        float dist = Mathf.Max(0.05f, Mathf.Abs(distanceAlongForward));
        return (2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad))
               / Mathf.Max(1, cam.pixelHeight);
    }

    private void TryLogLocalPlayerDiag(
        Camera cam,
        PaintItem item,
        Vector3 screen,
        float ax,
        float ay,
        float pw,
        float halfH,
        float planeDepth,
        float nearClip,
        bool clamped)
    {
        float now = Time.unscaledTime;
        if (now < _diagNextTime)
        {
            return;
        }

        _diagNextTime = now + Mathf.Max(0.016f, _diagIntervalSec);

        Vector3 camPos = cam.transform.position;
        float headDelta = _diagHasPrev ? Vector3.Distance(item.World, _diagPrevHead) : 0f;
        float camDelta = _diagHasPrev ? Vector3.Distance(camPos, _diagPrevCamPos) : 0f;
        float zDelta = _diagHasPrev ? (screen.z - _diagPrevScreenZ) : 0f;
        float pwDelta = _diagHasPrev ? (pw - _diagPrevPw) : 0f;
        float axDelta = _diagHasPrev ? (ax - _diagPrevAx) : 0f;
        float ayDelta = _diagHasPrev ? (ay - _diagPrevAy) : 0f;

        // Only spam when something actually moves (orbit / micro-motion).
        bool interesting = !_diagHasPrev
            || camDelta > 0.00005f
            || headDelta > 0.00005f
            || Mathf.Abs(zDelta) > 0.00005f
            || Mathf.Abs(pwDelta) > 1e-8f
            || clamped;

        if (!interesting)
        {
            return;
        }

        float nameW = _font != null ? _font.MeasureWidth(item.Name, _pixelScale > 0f ? _pixelScale : 1f) : 0f;
        float xName = ax - nameW * 0.5f;
        float fracX = xName - Mathf.Floor(xName);

        string line =
            $"t={now:F3} name='{item.Name}' " +
            $"head=({item.World.x:F4},{item.World.y:F4},{item.World.z:F4}) dHead={headDelta:F6} " +
            $"cam=({camPos.x:F4},{camPos.y:F4},{camPos.z:F4}) dCam={camDelta:F6} " +
            $"scrRaw=({screen.x:F3},{screen.y:F3},{screen.z:F4}) " +
            $"scrRound=({ax:F0},{ay:F0}) dAx={axDelta:F1} dAy={ayDelta:F1} " +
            $"z={screen.z:F4} dZ={zDelta:F6} pw={pw:E4} dPw={pwDelta:E4} " +
            $"halfH={halfH:F6} plane={planeDepth:F4} near={nearClip:F4} clamp={(clamped ? 1 : 0)} " +
            $"fracX={fracX:F3} discardFrac={(_snapDiscardSubpixelFrac ? 1 : 0)} " +
            $"orbit={(CameraController.Instance != null ? CameraController.Instance.CurrentDistance : 0f):F3}";

        Debug.Log("[NameplateDiag/local] " + line);
        AppendDiagFile(line);

        _diagPrevHead = item.World;
        _diagPrevCamPos = camPos;
        _diagPrevScreenZ = screen.z;
        _diagPrevPw = pw;
        _diagPrevAx = ax;
        _diagPrevAy = ay;
        _diagHasPrev = true;
    }

    private void AppendDiagFile(string line)
    {
        if (!_diagLogToFile)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(_diagLogPath))
            {
                _diagLogPath = System.IO.Path.Combine(Application.persistentDataPath, "NameplateDiag_local.log");
                System.IO.File.WriteAllText(
                    _diagLogPath,
                    "# local player nameplate diag — orbit camera and watch dZ / dPw / dHead\n" +
                    "# path=" + _diagLogPath + "\n");
                Debug.Log("[NameplateDiag] writing file: " + _diagLogPath);
            }

            System.IO.File.AppendAllText(_diagLogPath, line + "\n");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[NameplateDiag] file write failed: " + ex.Message);
            _diagLogToFile = false;
        }
    }

    /// <summary>
    /// L2 DrawTargetName: world anchor = Actor.Location + (0,0,CollisionHeight)
    /// (UE capsule center + half-height = capsule top). Prefer CC/Capsule top; no bone bob.
    /// </summary>
    private Vector3 GetHeadWorldPos(Entry entry)
    {
        Transform target = entry.Target;
        if (target == null)
        {
            return Vector3.zero;
        }

        CharacterController cc = entry.CC;
        if (cc != null)
        {
            Vector3 localTop = cc.center + Vector3.up * (cc.height * 0.5f);
            return target.TransformPoint(localTop) + Vector3.up * _headHeightOffset;
        }

        CapsuleCollider capsule = entry.Capsule;
        if (capsule != null)
        {
            Vector3 localTop = capsule.center + Vector3.up * (capsule.height * 0.5f);
            return target.TransformPoint(localTop) + Vector3.up * _headHeightOffset;
        }

        // Feet GO + 2×CH: UE Location is capsule center (= feet+CH), name = Loc+CH.
        return target.position + Vector3.up * ResolveNameplateHeightFromFeet(entry);
    }

    /// <summary>
    /// Half-height in Unity meters. npcgrp is already /52.5; UserInfo often raw UU.
    /// </summary>
    private static float CollisionHeightToUnityMeters(float collisionHeight)
    {
        if (collisionHeight <= 0.0001f)
        {
            return 0.46f;
        }

        // Raw Interlude UU half-heights are typically ~3..40; converted meters are usually under ~1.
        if (collisionHeight > 2.5f)
        {
            return collisionHeight / 52.5f;
        }

        return collisionHeight;
    }

    private float ResolveNameplateHeightFromFeet(Entry entry)
    {
        float ch = 0.46f;
        Entity entity = entry.Entity;
        if (entity != null && entity.Appearance != null)
        {
            ch = CollisionHeightToUnityMeters(entity.Appearance.CollisionHeight);
        }

        return 2f * ch + _headHeightOffset;
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
