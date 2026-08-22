using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// World nameplates orchestrator — L2 canvas path: Project → screen-pixel glyphs →
/// <see cref="L2NameplateScreenBatch"/>. Titles + DrawTargetName hover/target/attack bubbles.
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
    [Tooltip("L2 canvas: lock plate to whole screen pixels (hysteresis).")]
    [SerializeField] private bool _snapAnchorToPixels = true;
    [SerializeField] private float _snapHysteresisPx = 0.75f;
    [SerializeField] private bool _snapDiscardSubpixelFrac = true;
    [SerializeField] private bool _drawTitles = true;
    [SerializeField] private float _titleGapPixels = 2f;
    [SerializeField] private bool _drawBubbles = true;
    [SerializeField] private string _atlasResourcePath = "Data/UI/Font/L2Lobby/l2_lobby_font_atlas";
    [SerializeField] private string _csvResourcePath = "Data/UI/Font/L2Lobby/ul2font_ascii";

    [SerializeField] public RaycastHit[] _entitiesInRange;

    private readonly List<NameplatePaintItem> _paintList = new List<NameplatePaintItem>(64);

    private NameplateBubbleResolver _bubbleResolver;
    private NameplateEntryStore _entryStore;
    private NameplatePixelSnap _pixelSnap;
    private NameplateTextDrawer _textDrawer;
    private NameplateBubbleDrawer _bubbleDrawer;

    private Transform _playerTransform;
    private bool _subscribed;

    private static NameplatesManager _instance;
    public static NameplatesManager Instance => _instance;

    public Camera Camera
    {
        get => _camera;
        set => _camera = value;
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

        _bubbleResolver = new NameplateBubbleResolver();
        _entryStore = new NameplateEntryStore(_bubbleResolver);
        _pixelSnap = new NameplatePixelSnap();
        _textDrawer = new NameplateTextDrawer();
        _bubbleDrawer = new NameplateBubbleDrawer();

        ApplySettingsToSubsystems();
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
        _entryStore?.Clear();
        _pixelSnap?.ClearAll();
        _textDrawer?.Dispose();
        _bubbleDrawer?.Dispose();

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
        _entryStore?.Remove(id, _pixelSnap);
    }

    private void ApplySettingsToSubsystems()
    {
        _pixelSnap.HysteresisPx = _snapHysteresisPx;
        _textDrawer.ConfigurePaths(_atlasResourcePath, _csvResourcePath);
        _textDrawer.PixelScale = _pixelScale;
        _textDrawer.TitleGapPixels = _titleGapPixels;
        _textDrawer.SnapAnchorToPixels = _snapAnchorToPixels;
        _textDrawer.SnapDiscardSubpixelFrac = _snapDiscardSubpixelFrac;
        _bubbleDrawer.Enabled = _drawBubbles;
    }

    private void EnsureResources()
    {
        ApplySettingsToSubsystems();
        _textDrawer.EnsureResources();
        _bubbleDrawer.EnsureResources();
    }

    private Camera ResolveCamera()
    {
        return _camera != null ? _camera : Camera.main;
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

        _entryStore.Discover(_entitiesInRange, _defaultNameColor);
        _entryStore.EnsureHoverAndTarget(_entityMask, _defaultNameColor);

        if (PlayerEntity.Instance != null)
        {
            _entryStore.UpsertEntry(PlayerEntity.Instance, _defaultNameColor);
        }

        _entryStore.RefreshVisibility(_playerTransform, _nameplateViewDistance, _pixelSnap);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Camera targetCam = ResolveCamera();
        if (cam == null || targetCam == null || cam != targetCam)
        {
            return;
        }

        EnsureResources();
        if (!_textDrawer.IsReady)
        {
            return;
        }

        _entryStore.BuildPaintList(_paintList, _drawTitles, _headHeightOffset);
        if (_paintList.Count == 0)
        {
            return;
        }

        _textDrawer.Draw(cam, _paintList, _pixelSnap);
        _bubbleDrawer.Draw(cam, _paintList);
    }
}
