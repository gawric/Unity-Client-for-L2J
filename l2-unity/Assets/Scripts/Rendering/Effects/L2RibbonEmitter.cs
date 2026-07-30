using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CPU port of URibbonEmitter trail buffer → triangle strip.
/// Prefers Sword_Tip / Sword_Base as a2/a3 (blade cross-section), else GetNew CS1.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class L2RibbonEmitter : EffectPart
{
    public enum WidthAxisSource
    {
        TransformRight = 0,
        TransformUp = 1,
        TransformForward = 2,
        CustomLocal = 3,
    }

    public enum SampleMode
    {
        AutoBladeEnds = 0,
        BladeEndsOnly = 1,
        BoneNormalScaleRatio = 2,
    }

    [Header("RibbonSet / GetNewRibbonPoint")]
    [SerializeField] private SampleMode _sampleMode = SampleMode.AutoBladeEnds;
    [Tooltip("UC ScaleRatio — used only for BoneNormalScaleRatio fallback.")]
    [SerializeField] private float _scaleRatio = 1.5f;
    [SerializeField] private float _l2ToUnityScale = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _edgeRatio = 0.5f;
    [SerializeField] private float _sampleRate = 0.002f;
    [SerializeField, Min(2)] private int _maxPoints = 160;
    [SerializeField] private int _minPoints = 20;
    [Tooltip("Max midpoint/tip gap between ribbon samples (m). Larger frame motion inserts arc sheets.")]
    [SerializeField] private float _maxSegmentMeters = 0.02f;
    [SerializeField, Min(0)] private int _maxSheetsPerSample = 48;
    [Tooltip("Slerp tip/base around weapon pivot instead of linear chords (needed for curved swings).")]
    [SerializeField] private bool _arcSheetInterpolation = true;
    [SerializeField] private WidthAxisSource _widthAxisSource = WidthAxisSource.TransformRight;
    [SerializeField] private Vector3 _customWidthAxisLocal = Vector3.right;
    [SerializeField] private Transform _sampleBone;
    [SerializeField] private string _autoBoneName = "Weapon_R_Bone";
    [SerializeField] private Transform _swordTip;
    [SerializeField] private Transform _swordBase;
    [SerializeField] private string _swordTipName = "Sword_Tip";
    [SerializeField] private string _swordBaseName = "Sword_Base";
    [Tooltip("If true, always use MeshFilter blade ends when available (never alternate with Sword_Tip/Base).")]
    [SerializeField] private bool _preferMeshBladeEnds = true;
    [Tooltip("If true, keep a2/a3 from flipping when tip/base sources disagree.")]
    [SerializeField] private bool _stabilizeEdgePolarity = true;

    [Header("Trail")]
    [Tooltip("False = store world edges (trail stays in world while effect root moves).")]
    [SerializeField] private bool _storeInOwnerLocal = false;
    [SerializeField] private float _minSampleDistance = 0.0001f;
    [SerializeField] private bool _fadeAlphaAlongTrail = true;
    [SerializeField, Range(0f, 1f)] private float _headAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _tailAlpha = 0.15f;
    [SerializeField, Range(0f, 2f)] private float _opacity = 1f;
    [Tooltip("Always keep a starter strip so something is visible before the sword swings.")]
    [SerializeField] private bool _seedStarterStrip = true;
    [SerializeField] private float _starterStripMeters = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool _debugLog = true;
    [Tooltip("Per-frame sword tip/base motion + ribbon head (compare to L2 RibbonSnapshot).")]
    [SerializeField] private bool _debugTraceMotion = true;
    [Tooltip("Dump first/last ribbon points + segment lengths when buffer changes.")]
    [SerializeField] private bool _debugTracePoints = true;
    [SerializeField] private float _debugLogInterval = 0.1f;
    [SerializeField] private int _debugTraceFirstFrames = 90;
    [SerializeField] private bool _drawGizmos = true;

    private struct RibbonPoint
    {
        public Vector3 A2;
        public Vector3 A3;
        public float A4;
        public float Time;
    }

    private readonly List<RibbonPoint> _points = new List<RibbonPoint>(96);
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private Vector3[] _verts;
    private Vector2[] _uvs;
    private Color[] _colors;
    private int[] _tris;
    private bool _playing;
    private float _sampleTimer;
    private float _nextDebugLogTime;
    private int _sampleAttempts;
    private int _sampleAccepted;
    private string _lastSampleSource = "?";
    private bool _loggedPlayOnce;
    private Transform _weaponRoot;
    private int _traceFrame;
    private bool _havePrevBlade;
    private Vector3 _prevTipWorld;
    private Vector3 _prevBaseWorld;
    private Vector3 _prevMidWorld;
    private float _playStartedAt;
    private int _lastLoggedPointCount = -1;
    private string _lastBladeResolveSource = "?";
    private Vector3 _lastTipWorld;
    private Vector3 _lastBaseWorld;

    public override void Initialize(EffectSettings settings, float baseSize)
    {
        base.Initialize(settings, baseSize);
        EnsureMesh();
    }

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        EnsureMesh();
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        Log(
            $"Setup owner='{(OwnerTarget != null ? OwnerTarget.name : "null")}' " +
            $"follow='{(FollowTarget != null ? FollowTarget.name : "null")}'");
    }

    public override void PlayPart()
    {
        EnsureMesh();
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        ResolveBladeAnchors();
        ApplyOpacityToMaterial();
        _points.Clear();
        _sampleTimer = 0f;
        _sampleAttempts = 0;
        _sampleAccepted = 0;
        _playing = true;
        _loggedPlayOnce = false;
        _nextDebugLogTime = 0f;
        _traceFrame = 0;
        _havePrevBlade = false;
        _playStartedAt = Time.time;
        _lastLoggedPointCount = -1;

        if (_meshFilter != null)
        {
            _meshFilter.sharedMesh = _mesh;
        }

        if (targetRenderer != null)
        {
            targetRenderer.enabled = true;
        }

        bool ok = TrySample(force: true);
        if (ok && _seedStarterStrip && _points.Count == 1)
        {
            SeedStarterStrip();
        }

        RebuildMesh();

        Shader shader = targetRenderer != null && targetRenderer.sharedMaterial != null
            ? targetRenderer.sharedMaterial.shader
            : null;
        Log(
            $"PlayPart okSample={ok} tip='{(_swordTip != null ? _swordTip.name : "null")}' " +
            $"base='{(_swordBase != null ? _swordBase.name : "null")}' " +
            $"bone='{(_sampleBone != null ? _sampleBone.name : "null")}' " +
            $"source={_lastSampleSource} points={_points.Count} " +
            $"owner='{(OwnerTarget != null ? OwnerTarget.name : "null")}' " +
            $"renderer={(targetRenderer != null ? targetRenderer.enabled.ToString() : "null")} " +
            $"mat={(targetRenderer != null && targetRenderer.sharedMaterial != null ? targetRenderer.sharedMaterial.name : "null")} " +
            $"shader={(shader != null ? shader.name : "null")} supported={(shader != null && shader.isSupported)} " +
            $"pos={transform.position}");
        _loggedPlayOnce = true;
    }

    private void SeedStarterStrip()
    {
        RibbonPoint head = _points[0];
        Vector3 blade = head.A2 - head.A3;
        Vector3 side = Vector3.Cross(blade.normalized, Vector3.up);
        if (side.sqrMagnitude < 1e-8f)
        {
            side = Vector3.Cross(blade.normalized, Vector3.right);
        }

        side = side.normalized * Mathf.Max(0.01f, _starterStripMeters);
        _points.Add(new RibbonPoint
        {
            A2 = head.A2 - side,
            A3 = head.A3 - side,
            A4 = head.A4,
            Time = head.Time - 0.001f,
        });
        Log($"SeedStarterStrip side={side.magnitude:F3} points={_points.Count} a4={head.A4:F3}");
    }

    public override void StopPart()
    {
        Log($"StopPart points={_points.Count} attempts={_sampleAttempts} accepted={_sampleAccepted}");
        _playing = false;
        _points.Clear();
        ClearMesh();
        if (targetRenderer != null)
        {
            targetRenderer.enabled = false;
        }
    }

    private void Awake()
    {
        EnsureMesh();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }

    private void OnDisable()
    {
        _playing = false;
    }

    private void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }

            _mesh = null;
        }
    }

    private void LateUpdate()
    {
        if (!_playing)
        {
            return;
        }

        if (!_loggedPlayOnce)
        {
            // Safety if PlayPart was skipped somehow.
            ResolveBladeAnchors();
            Log("LateUpdate playing but PlayPart flag missing — forcing first sample");
            TrySample(force: true);
            _loggedPlayOnce = true;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        // One animated pose per frame — sample once; large mid jumps get Lerp sheets.
        TrySample(force: false);

        if (_points.Count > 0)
        {
            RebuildMesh();
        }

        LogMotionAndPointsTrace();
    }

    private void LogMotionAndPointsTrace()
    {
        if (!_debugLog)
        {
            return;
        }

        _traceFrame++;
        bool burst = _traceFrame <= Mathf.Max(1, _debugTraceFirstFrames);
        bool intervalDue = Time.time >= _nextDebugLogTime;
        if (!burst && !intervalDue)
        {
            return;
        }

        if (intervalDue)
        {
            _nextDebugLogTime = Time.time + Mathf.Max(0.02f, _debugLogInterval);
        }

        float age = Time.time - _playStartedAt;
        Vector3 tip = _lastTipWorld;
        Vector3 hilt = _lastBaseWorld;
        Vector3 mid = (tip + hilt) * 0.5f;
        float bladeLen = Vector3.Distance(tip, hilt);

        float tipDelta = 0f;
        float baseDelta = 0f;
        float midDelta = 0f;
        Vector3 tipVel = Vector3.zero;
        Vector3 midVel = Vector3.zero;
        if (_havePrevBlade)
        {
            tipDelta = Vector3.Distance(tip, _prevTipWorld);
            baseDelta = Vector3.Distance(hilt, _prevBaseWorld);
            midDelta = Vector3.Distance(mid, _prevMidWorld);
            float dt = Mathf.Max(1e-5f, Time.deltaTime);
            tipVel = (tip - _prevTipWorld) / dt;
            midVel = (mid - _prevMidWorld) / dt;
        }

        if (_debugTraceMotion)
        {
            RibbonPoint head = _points.Count > 0 ? _points[0] : default;
            RibbonPoint tail = _points.Count > 0 ? _points[_points.Count - 1] : default;
            Vector3 headMid = _points.Count > 0
                ? L2FxRibbonGetPoint.EdgeMid(head.A2, head.A3)
                : Vector3.zero;
            float headWidth = _points.Count > 0 ? Vector3.Distance(head.A2, head.A3) : 0f;
            float trailLen = EstimateTrailMidLength();

            Log(
                $"MOTION f={_traceFrame} age={age:F3}s dt={Time.deltaTime:F4} source={_lastSampleSource}/{_lastBladeResolveSource} " +
                $"storeLocal={_storeInOwnerLocal} emitter={transform.position} " +
                $"tip={Fmt(tip)} base={Fmt(hilt)} mid={Fmt(mid)} bladeLen={bladeLen:F4} " +
                $"dTip={tipDelta:F4} dBase={baseDelta:F4} dMid={midDelta:F4} " +
                $"tipSpeed={tipVel.magnitude:F2} midSpeed={midVel.magnitude:F2} " +
                $"points={_points.Count} accepted={_sampleAccepted} " +
                $"headMid={Fmt(headMid)} headW={headWidth:F4} trailMidLen={trailLen:F3}");
        }

        if (_debugTracePoints && (_points.Count != _lastLoggedPointCount || burst || intervalDue))
        {
            _lastLoggedPointCount = _points.Count;
            LogPointsDump();
        }

        _prevTipWorld = tip;
        _prevBaseWorld = hilt;
        _prevMidWorld = mid;
        _havePrevBlade = bladeLen > 1e-6f || tip.sqrMagnitude > 0f;
    }

    private float EstimateTrailMidLength()
    {
        if (_points.Count < 2)
        {
            return 0f;
        }

        float len = 0f;
        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector3 a = L2FxRibbonGetPoint.EdgeMid(_points[i].A2, _points[i].A3);
            Vector3 b = L2FxRibbonGetPoint.EdgeMid(_points[i + 1].A2, _points[i + 1].A3);
            len += Vector3.Distance(a, b);
        }

        return len;
    }

    private void LogPointsDump()
    {
        int n = _points.Count;
        if (n == 0)
        {
            Log("POINTS empty");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
        sb.Append("POINTS n=").Append(n)
            .Append(" space=").Append(_storeInOwnerLocal ? "ownerLocal" : "world");

        int show = Mathf.Min(4, n);
        for (int i = 0; i < show; i++)
        {
            AppendPoint(sb, "H" + i, _points[i]);
        }

        if (n > show + 1)
        {
            sb.Append(" ...");
            AppendPoint(sb, "T0", _points[n - 1]);
            if (n > show + 2)
            {
                AppendPoint(sb, "T1", _points[n - 2]);
            }
        }
        else if (n > show)
        {
            AppendPoint(sb, "T0", _points[n - 1]);
        }

        // Segment lengths head→next few (detect jumps vs L2 continuous path).
        sb.Append(" seg=");
        int segShow = Mathf.Min(6, n - 1);
        for (int i = 0; i < segShow; i++)
        {
            float seg = Vector3.Distance(
                L2FxRibbonGetPoint.EdgeMid(_points[i].A2, _points[i].A3),
                L2FxRibbonGetPoint.EdgeMid(_points[i + 1].A2, _points[i + 1].A3));
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(seg.ToString("F3"));
        }

        Log(sb.ToString());
    }

    private static void AppendPoint(System.Text.StringBuilder sb, string label, RibbonPoint p)
    {
        Vector3 mid = L2FxRibbonGetPoint.EdgeMid(p.A2, p.A3);
        float w = Vector3.Distance(p.A2, p.A3);
        sb.Append(' ').Append(label)
            .Append(" a2=").Append(Fmt(p.A2))
            .Append(" a3=").Append(Fmt(p.A3))
            .Append(" mid=").Append(Fmt(mid))
            .Append(" a4=").Append(p.A4.ToString("F3"))
            .Append(" w=").Append(w.ToString("F3"));
    }

    private static string Fmt(Vector3 v)
    {
        return $"({v.x:F3},{v.y:F3},{v.z:F3})";
    }

    private void Log(string message)
    {
        if (!_debugLog)
        {
            return;
        }

        Debug.Log($"[L2RibbonEmitter] {name} {message}", this);
    }

    private void EnsureMesh()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "L2RibbonStrip" };
            _mesh.MarkDynamic();
        }

        if (_meshFilter != null && _meshFilter.sharedMesh != _mesh)
        {
            _meshFilter.sharedMesh = _mesh;
        }
    }

    private Transform ResolveSearchRoot()
    {
        if (OwnerTarget != null)
        {
            return OwnerTarget;
        }

        if (FollowTarget != null)
        {
            return FollowTarget;
        }

        if (PlayerEntity.Instance != null)
        {
            return PlayerEntity.Instance.transform;
        }

        return null;
    }

    private void ResolveBladeAnchors()
    {
        if (_swordTip != null && _swordBase != null && _weaponRoot != null)
        {
            return;
        }

        Transform searchRoot = ResolveSearchRoot();
        Transform weapon = _weaponRoot;

        if (PlayerEntity.Instance != null)
        {
            if (weapon == null)
            {
                weapon = PlayerEntity.Instance.GetWeaponTransform();
            }

            Transform[] points = PlayerEntity.Instance.GetSwordBasePoints();
            if (points != null)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] == null)
                    {
                        continue;
                    }

                    if (_swordBase == null && points[i].name == _swordBaseName)
                    {
                        _swordBase = points[i];
                    }

                    if (_swordTip == null && points[i].name == _swordTipName)
                    {
                        _swordTip = points[i];
                    }
                }
            }
        }

        if (weapon == null && searchRoot != null)
        {
            Transform bone = searchRoot.FindRecursive(_autoBoneName);
            if (bone != null)
            {
                // weapon_* usually under Weapon_R_Bone
                for (int i = 0; i < bone.childCount; i++)
                {
                    Transform child = bone.GetChild(i);
                    if (child.name.StartsWith("weapon_"))
                    {
                        weapon = child;
                        break;
                    }
                }

                if (weapon == null)
                {
                    weapon = bone;
                }
            }
        }

        Transform tipRoot = weapon != null ? weapon : searchRoot;
        if (tipRoot != null)
        {
            if (_swordTip == null)
            {
                _swordTip = tipRoot.Find(_swordTipName) ?? tipRoot.FindRecursive(_swordTipName);
            }

            if (_swordBase == null)
            {
                _swordBase = tipRoot.Find(_swordBaseName) ?? tipRoot.FindRecursive(_swordBaseName);
            }
        }

        if (_sampleBone == null && !string.IsNullOrEmpty(_autoBoneName) && searchRoot != null)
        {
            _sampleBone = searchRoot.FindRecursive(_autoBoneName);
        }

        if (_sampleBone == null && weapon != null)
        {
            _sampleBone = weapon;
        }

        _weaponRoot = weapon != null ? weapon : tipRoot;

        float markerLen = (_swordTip != null && _swordBase != null)
            ? Vector3.Distance(_swordTip.position, _swordBase.position)
            : 0f;
        float meshLen = 0f;
        if (_weaponRoot != null &&
            L2FxRibbonGetPoint.TryGetMeshBladeEnds(_weaponRoot, out Vector3 meshTip, out Vector3 meshHilt))
        {
            meshLen = Vector3.Distance(meshTip, meshHilt);
        }

        Log(
            $"ResolveBlade search='{(searchRoot != null ? searchRoot.name : "null")}' " +
            $"weapon='{(weapon != null ? weapon.name : "null")}' " +
            $"tip='{(_swordTip != null ? FullPath(_swordTip) : "null")}' " +
            $"base='{(_swordBase != null ? FullPath(_swordBase) : "null")}' " +
            $"bone='{(_sampleBone != null ? _sampleBone.name : "null")}' " +
            $"markerLen={markerLen:F3} meshLen={meshLen:F3}");
    }

    private static string FullPath(Transform t)
    {
        if (t == null)
        {
            return "null";
        }

        string path = t.name;
        Transform p = t.parent;
        int guard = 0;
        while (p != null && guard++ < 8)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }

        return path;
    }

    private void ApplyOpacityToMaterial()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        UpdateShaderFloat("_Opacity", _opacity);
    }

    private float ResolveScaleRatioUnity()
    {
        return _scaleRatio * Mathf.Max(0.0001f, _l2ToUnityScale);
    }

    private Transform ResolveSpace()
    {
        if (!_storeInOwnerLocal)
        {
            return null;
        }

        if (OwnerTarget != null)
        {
            return OwnerTarget;
        }

        if (FollowTarget != null)
        {
            return FollowTarget;
        }

        return transform;
    }

    /// <summary>
    /// Pivot for arc sheets — weapon root (hand) in the same space as stored a2/a3.
    /// Falls back to previous sample mid if weapon is missing.
    /// </summary>
    private Vector3 ResolveSheetPivot(Transform space)
    {
        Vector3 pivotWorld;
        if (_weaponRoot != null)
        {
            pivotWorld = _weaponRoot.position;
        }
        else if (_points.Count > 0)
        {
            Vector3 midLocal = L2FxRibbonGetPoint.EdgeMid(_points[0].A2, _points[0].A3);
            return midLocal;
        }
        else
        {
            pivotWorld = transform.position;
        }

        return space != null ? space.InverseTransformPoint(pivotWorld) : pivotWorld;
    }

    private Vector3 ResolveWidthAxisWorld(Transform bone)
    {
        switch (_widthAxisSource)
        {
            case WidthAxisSource.TransformUp:
                return bone.up;
            case WidthAxisSource.TransformForward:
                return bone.forward;
            case WidthAxisSource.CustomLocal:
                return bone.TransformDirection(_customWidthAxisLocal.sqrMagnitude > 1e-8f
                    ? _customWidthAxisLocal.normalized
                    : Vector3.right);
            default:
                return bone.right;
        }
    }

    private bool TryResolveBladeWorldEnds(out Vector3 tipWorld, out Vector3 hiltWorld, out string source)
    {
        tipWorld = default;
        hiltWorld = default;
        source = "none";

        bool haveMarkers = _swordTip != null && _swordBase != null;
        Vector3 markerTip = haveMarkers ? _swordTip.position : default;
        Vector3 markerHilt = haveMarkers ? _swordBase.position : default;
        float markerLenSq = haveMarkers ? (markerTip - markerHilt).sqrMagnitude : 0f;

        bool haveMesh = false;
        Vector3 meshTip = default;
        Vector3 meshHilt = default;
        Transform meshRoot = _weaponRoot;
        if (meshRoot == null && _swordTip != null)
        {
            meshRoot = _swordTip.parent != null ? _swordTip.parent : _swordTip;
        }

        if (_preferMeshBladeEnds && meshRoot != null &&
            L2FxRibbonGetPoint.TryGetMeshBladeEnds(meshRoot, out meshTip, out meshHilt))
        {
            haveMesh = true;
        }

        // Mesh bounds and Sword_Tip/Base often have opposite tip/hilt naming
        // (SwordSetup historically swapped GO names). Alternating sources flips
        // a2/a3 every frame (logs: dTip≈bladeLen, dMid≈0) and tears the ribbon.
        // Once mesh ends resolve, stick to them.
        if (haveMesh)
        {
            tipWorld = meshTip;
            hiltWorld = meshHilt;
            source = "MeshBounds";
            return true;
        }

        if (haveMarkers && markerLenSq > 1e-10f)
        {
            tipWorld = markerTip;
            hiltWorld = markerHilt;
            source = "SwordMarkers";
            return true;
        }

        return false;
    }

    private bool TryBuildEdges(out L2FxRibbonGetPoint.Edges edges)
    {
        edges = default;
        bool wantBlade = _sampleMode == SampleMode.AutoBladeEnds || _sampleMode == SampleMode.BladeEndsOnly;
        if (wantBlade && TryResolveBladeWorldEnds(out Vector3 tip, out Vector3 hilt, out string bladeSource))
        {
            edges = L2FxRibbonGetPoint.GetPointFromBladeEnds(tip, hilt, _edgeRatio);
            _lastSampleSource = bladeSource;
            _lastBladeResolveSource = bladeSource;
            _lastTipWorld = tip;
            _lastBaseWorld = hilt;
            return edges.A4 > 1e-5f;
        }

        if (_sampleMode == SampleMode.BladeEndsOnly)
        {
            _lastSampleSource = "BladeEndsMissing";
            return false;
        }

        Transform bone = _sampleBone;
        if (bone == null)
        {
            if (FollowTarget != null)
            {
                bone = FollowTarget;
            }
            else if (OwnerTarget != null)
            {
                bone = OwnerTarget;
            }
            else
            {
                bone = transform;
            }
        }

        edges = L2FxRibbonGetPoint.GetPointCoordSys1(
            bone.position,
            ResolveWidthAxisWorld(bone),
            ResolveScaleRatioUnity(),
            _edgeRatio);
        _lastSampleSource = "BoneNormal/" + bone.name;
        _lastBladeResolveSource = _lastSampleSource;
        _lastTipWorld = edges.A2;
        _lastBaseWorld = edges.A3;
        return true;
    }

    private bool TrySample(bool force)
    {
        _sampleAttempts++;
        ResolveBladeAnchors();

        if (!TryBuildEdges(out L2FxRibbonGetPoint.Edges edges))
        {
            if (_debugLog && (_sampleAttempts <= 3 || force))
            {
                Log($"TrySample FAIL source={_lastSampleSource} tipNull={_swordTip == null} baseNull={_swordBase == null}");
            }

            return false;
        }

        Transform space = ResolveSpace();
        Vector3 a2 = space != null ? space.InverseTransformPoint(edges.A2) : edges.A2;
        Vector3 a3 = space != null ? space.InverseTransformPoint(edges.A3) : edges.A3;

        if (_stabilizeEdgePolarity && _points.Count > 0)
        {
            L2FxRibbonGetPoint.Edges cur = new L2FxRibbonGetPoint.Edges
            {
                A2 = a2,
                A3 = a3,
                A4 = edges.A4,
            };
            L2FxRibbonGetPoint.Edges reference = new L2FxRibbonGetPoint.Edges
            {
                A2 = _points[0].A2,
                A3 = _points[0].A3,
                A4 = _points[0].A4,
            };
            L2FxRibbonGetPoint.Edges stable = L2FxRibbonGetPoint.StabilizeEdgePolarity(cur, reference);
            if (!Mathf.Approximately(stable.A2.x, a2.x) ||
                !Mathf.Approximately(stable.A2.y, a2.y) ||
                !Mathf.Approximately(stable.A2.z, a2.z))
            {
                if (_debugLog && _debugTraceMotion && (_sampleAccepted < 8 || _traceFrame < 12))
                {
                    Log($"POLARITY flip corrected source={_lastSampleSource}");
                }

                // World tip/base for MOTION logs must match stored a2/a3.
                _lastTipWorld = space != null ? space.TransformPoint(stable.A2) : stable.A2;
                _lastBaseWorld = space != null ? space.TransformPoint(stable.A3) : stable.A3;
            }

            a2 = stable.A2;
            a3 = stable.A3;
            edges.A2 = space != null ? space.TransformPoint(a2) : a2;
            edges.A3 = space != null ? space.TransformPoint(a3) : a3;
        }

        if (_points.Count > 0)
        {
            Vector3 mid = L2FxRibbonGetPoint.EdgeMid(a2, a3);
            Vector3 prevMid = L2FxRibbonGetPoint.EdgeMid(_points[0].A2, _points[0].A3);
            float midDist = Vector3.Distance(mid, prevMid);
            float tipDist = Vector3.Distance(a2, _points[0].A2);
            float baseDist = Vector3.Distance(a3, _points[0].A3);
            // Pure rotation around mid: tip/base move while mid stays — still need sheets.
            float travel = Mathf.Max(midDist, Mathf.Max(tipDist, baseDist));

            if (!force && travel < _minSampleDistance)
            {
                RibbonPoint head = _points[0];
                head.A2 = a2;
                head.A3 = a3;
                head.A4 = edges.A4;
                head.Time = Time.time;
                _points[0] = head;
                if (_debugLog && _debugTraceMotion && _traceFrame < 3)
                {
                    Log(
                        $"BUILD head-update travel={travel:F5} mid={midDist:F5} tip={tipDist:F5} " +
                        $"a2={Fmt(edges.A2)} a3={Fmt(edges.A3)} " +
                        $"tipW={Fmt(_lastTipWorld)} baseW={Fmt(_lastBaseWorld)}");
                }

                return true;
            }

            L2FxRibbonGetPoint.Edges newest = new L2FxRibbonGetPoint.Edges
            {
                A2 = a2,
                A3 = a3,
                A4 = edges.A4,
            };
            L2FxRibbonGetPoint.Edges previous = new L2FxRibbonGetPoint.Edges
            {
                A2 = _points[0].A2,
                A3 = _points[0].A3,
                A4 = _points[0].A4,
            };

            int sheets = L2FxRibbonGetPoint.CountInterpSheetsForEdges(
                previous,
                newest,
                _maxSegmentMeters,
                _maxSheetsPerSample);

            Vector3 pivot = ResolveSheetPivot(space);

            // Head = newest. Insert sheets between newest and previous head.
            RibbonPoint newestPt = new RibbonPoint
            {
                A2 = newest.A2,
                A3 = newest.A3,
                A4 = newest.A4,
                Time = Time.time,
            };
            _points.Insert(0, newestPt);

            for (int i = 1; i <= sheets; i++)
            {
                // t=0 at newest, t=1 at previous — same order as before.
                float t = (float)i / (sheets + 1);
                L2FxRibbonGetPoint.Edges sheet = _arcSheetInterpolation
                    ? L2FxRibbonGetPoint.LerpEdgesArc(newest, previous, t, pivot)
                    : L2FxRibbonGetPoint.LerpEdges(newest, previous, t);
                _points.Insert(i, new RibbonPoint
                {
                    A2 = sheet.A2,
                    A3 = sheet.A3,
                    A4 = sheet.A4,
                    Time = Time.time,
                });
            }

            while (_points.Count > _maxPoints)
            {
                _points.RemoveAt(_points.Count - 1);
            }

            _sampleAccepted++;
            if (_debugLog && (_debugTracePoints || _sampleAccepted <= 8 || sheets > 0))
            {
                float tipDelta = _havePrevBlade
                    ? Vector3.Distance(_lastTipWorld, _prevTipWorld)
                    : 0f;
                Log(
                    $"BUILD#{_sampleAccepted} mode=insert+sheets source={_lastSampleSource} " +
                    $"a2={Fmt(edges.A2)} a3={Fmt(edges.A3)} a4={edges.A4:F4} " +
                    $"|a2-a3|={Vector3.Distance(edges.A2, edges.A3):F4} " +
                    $"midDist={midDist:F4} sheets={sheets} points={_points.Count} " +
                    $"tipW={Fmt(_lastTipWorld)} baseW={Fmt(_lastBaseWorld)} tipStep={tipDelta:F4}");
            }

            return true;
        }

        RibbonPoint point = new RibbonPoint
        {
            A2 = a2,
            A3 = a3,
            A4 = edges.A4,
            Time = Time.time,
        };

        _points.Insert(0, point);
        while (_points.Count > _maxPoints)
        {
            _points.RemoveAt(_points.Count - 1);
        }

        _sampleAccepted++;
        if (_debugLog && (_debugTracePoints || _sampleAccepted <= 8 || force))
        {
            Log(
                $"BUILD#{_sampleAccepted} mode=first source={_lastSampleSource} " +
                $"a2={Fmt(edges.A2)} a3={Fmt(edges.A3)} a4={edges.A4:F4} " +
                $"|a2-a3|={Vector3.Distance(edges.A2, edges.A3):F4} points={_points.Count} " +
                $"tipW={Fmt(_lastTipWorld)} baseW={Fmt(_lastBaseWorld)}");
        }

        return true;
    }

    private void ClearMesh()
    {
        if (_mesh == null)
        {
            return;
        }

        _mesh.Clear();
    }

    private void RebuildMesh()
    {
        EnsureMesh();
        int n = _points.Count;
        if (n < 2)
        {
            ClearMesh();
            return;
        }

        int vertCount = n * 2;
        int triCount = (n - 1) * 6;
        EnsureBuffers(vertCount, triCount);

        Transform space = ResolveSpace();
        Transform meshXf = transform;

        for (int i = 0; i < n; i++)
        {
            RibbonPoint p = _points[i];
            Vector3 a2World = space != null ? space.TransformPoint(p.A2) : p.A2;
            Vector3 a3World = space != null ? space.TransformPoint(p.A3) : p.A3;
            Vector3 a2Local = meshXf.InverseTransformPoint(a2World);
            Vector3 a3Local = meshXf.InverseTransformPoint(a3World);

            int v = i * 2;
            _verts[v] = a3Local;
            _verts[v + 1] = a2Local;

            float along = n <= 1 ? 0f : (float)i / (n - 1);
            _uvs[v] = new Vector2(0f, along);
            _uvs[v + 1] = new Vector2(1f, along);

            float alpha = _fadeAlphaAlongTrail
                ? Mathf.Lerp(_headAlpha, _tailAlpha, along)
                : 1f;
            Color c = new Color(1f, 1f, 1f, alpha);
            _colors[v] = c;
            _colors[v + 1] = c;
        }

        int t = 0;
        for (int i = 0; i < n - 1; i++)
        {
            int v = i * 2;
            _tris[t++] = v;
            _tris[t++] = v + 2;
            _tris[t++] = v + 1;

            _tris[t++] = v + 1;
            _tris[t++] = v + 2;
            _tris[t++] = v + 3;
        }

        _mesh.Clear(false);
        var vertList = new List<Vector3>(vertCount);
        var uvList = new List<Vector2>(vertCount);
        var colorList = new List<Color>(vertCount);
        var triList = new List<int>(triCount);
        for (int i = 0; i < vertCount; i++)
        {
            vertList.Add(_verts[i]);
            uvList.Add(_uvs[i]);
            colorList.Add(_colors[i]);
        }

        for (int i = 0; i < triCount; i++)
        {
            triList.Add(_tris[i]);
        }

        _mesh.SetVertices(vertList);
        _mesh.SetUVs(0, uvList);
        _mesh.SetColors(colorList);
        _mesh.SetTriangles(triList, 0, false);
        _mesh.RecalculateBounds();
        Bounds expanded = _mesh.bounds;
        expanded.Expand(1f);
        _mesh.bounds = expanded;

        if (_debugLog && _sampleAccepted <= 3)
        {
            Log(
                $"RebuildMesh verts={vertCount} tris={triCount / 3} " +
                $"local0={_verts[0]} local1={_verts[1]} bounds={_mesh.bounds}");
        }
    }

    private void EnsureBuffers(int vertCount, int triCount)
    {
        if (_verts == null || _verts.Length < vertCount)
        {
            _verts = new Vector3[vertCount];
            _uvs = new Vector2[vertCount];
            _colors = new Color[vertCount];
        }

        if (_tris == null || _tris.Length < triCount)
        {
            _tris = new int[triCount];
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
        {
            return;
        }

        if (_swordTip != null && _swordBase != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_swordTip.position, _swordBase.position);
            Gizmos.DrawSphere(_swordTip.position, 0.02f);
            Gizmos.DrawSphere(_swordBase.position, 0.02f);
        }

        if (_points == null || _points.Count == 0)
        {
            return;
        }

        Transform space = ResolveSpace();
        Gizmos.color = Color.cyan;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 a2 = space != null ? space.TransformPoint(_points[i].A2) : _points[i].A2;
            Vector3 a3 = space != null ? space.TransformPoint(_points[i].A3) : _points[i].A3;
            Gizmos.DrawLine(a2, a3);
            if (i + 1 < _points.Count)
            {
                Vector3 a2n = space != null ? space.TransformPoint(_points[i + 1].A2) : _points[i + 1].A2;
                Vector3 a3n = space != null ? space.TransformPoint(_points[i + 1].A3) : _points[i + 1].A3;
                Gizmos.DrawLine(a2, a2n);
                Gizmos.DrawLine(a3, a3n);
            }
        }
    }
#endif
}
