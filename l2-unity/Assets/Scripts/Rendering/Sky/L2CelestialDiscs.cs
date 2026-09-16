using System.Globalization;
using UnityEngine;

/// <summary>
/// L2 ANSun / ANMoon billboards: textures from High Elf RenderDoc, motion/size/color from celestial LUT.
/// Follows the camera using sweep-relative world offsets (same as EnvManager actor Location).
/// </summary>
[ExecuteInEditMode]
public class L2CelestialDiscs : MonoBehaviour
{
    const string SunTexPath = "Data/Clock/sun_texture_20335";
    const string MoonTexPath = "Data/Clock/moon_texture_20931";
    const string ShaderName = "L2/Sky/CelestialDisc";

    // Captured EID 1329: side 101.32 UU at moonDrawScale 4.5, clip-W ~244.
    const float MoonRefSideUu = 101.32f;
    const float MoonRefDrawScale = 4.5f;
    const float MoonRefDistanceUu = 244.33f;

    // Captured EID 1332: side 82.70 UU, clip-W ~338. DrawScale ~2 at daytime disc.
    const float SunRefSideUu = 82.70f;
    const float SunRefDrawScale = 2.0f;
    const float SunRefDistanceUu = 338.11f;

    const float VisibleScaleEpsilon = 0.02f;

    [SerializeField] Texture2D _sunTexture;
    [SerializeField] Texture2D _moonTexture;
    [SerializeField] bool _followCamera = true;
    [Tooltip("0 = just inside camera far clip so terrain/hills can occlude the disc. >80 overrides.")]
    [SerializeField] float _forceDistanceMeters = 0f;
    [SerializeField] bool _logTrajectory = true;
    [SerializeField] float _logInterval = 0.25f;
    [SerializeField] float _jumpAngleDeg = 3f;

    struct BodyTrack
    {
        public bool hasLast;
        public bool lastVisible;
        public bool lastFallback;
        public Vector3 lastPlaceDir;
        public Vector3 lastPos;
        public float lastHours;
    }

    Transform _sunTf;
    Transform _moonTf;
    MeshRenderer _sunMr;
    MeshRenderer _moonMr;
    Material _sunMat;
    Material _moonMat;
    Camera _cam;
    BodyTrack _sunTrack;
    BodyTrack _moonTrack;
    float _nextTrajLog;

    void OnEnable()
    {
        L2CelestialLut.EnsureLoaded();
        EnsureDiscs();
    }

    void OnDisable()
    {
        DestroyDisc(ref _sunTf, ref _sunMat);
        DestroyDisc(ref _moonTf, ref _moonMat);
        _sunMr = null;
        _moonMr = null;
        _sunTrack = default;
        _moonTrack = default;
    }

    void LateUpdate()
    {
        L2CelestialLut.EnsureLoaded();
        L2DayNightLut.EnsureLoaded();
        if (!L2CelestialLut.IsReady)
        {
            return;
        }

        WorldClock clock = WorldClock.Instance;
        if (clock == null)
        {
            clock = WorldClock.EnsurePersistent();
        }
        if (clock == null)
        {
            return;
        }

        EnsureDiscs();
        if (_sunTf == null || _moonTf == null)
        {
            return;
        }

        LateUpdateApplyLayer();

        var sample = L2CelestialLut.SampleAt(clock.WorldHours);
        Color sunTint = Color.white;
        if (L2DayNightLut.IsReady)
        {
            sunTint = L2DayNightLut.SampleAt(clock.WorldHours).sun;
        }

        Camera cam = ResolveCamera();
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

        bool periodic = Application.isPlaying && _logTrajectory && Time.unscaledTime >= _nextTrajLog;
        ApplyBody("SUN", ref _sunTrack, _sunTf, _sunMr, _sunMat, cam, camPos, clock.WorldHours, sample,
            sample.sunOffsetUnity, sample.sunDirUnity, sample.sunElevDeg,
            sample.sunDrawScale, sample.sunScale, SunRefSideUu, SunRefDrawScale, SunRefDistanceUu, sunTint, false, periodic);
        ApplyBody("MOON", ref _moonTrack, _moonTf, _moonMr, _moonMat, cam, camPos, clock.WorldHours, sample,
            sample.moonOffsetUnity, sample.moonDirUnity, sample.moonElevDeg,
            sample.moonDrawScale, sample.moonScale, MoonRefSideUu, MoonRefDrawScale, MoonRefDistanceUu,
            Color.white, true, periodic);
        if (periodic)
        {
            _nextTrajLog = Time.unscaledTime + Mathf.Max(0.05f, _logInterval);
        }
    }

    void ApplyBody(
        string body,
        ref BodyTrack track,
        Transform tf,
        MeshRenderer mr,
        Material mat,
        Camera cam,
        Vector3 camPos,
        float hours,
        L2CelestialLut.Sample sample,
        Vector3 lutOffset,
        Vector3 lutDir,
        float lutElevDeg,
        float drawScale,
        float visScale,
        float refSideUu,
        float refDrawScale,
        float refDistanceUu,
        Color tint,
        bool mirrorX,
        bool periodic)
    {
        bool visible = visScale > VisibleScaleEpsilon;
        if (mr != null && mr.enabled != visible)
        {
            mr.enabled = visible;
        }

        bool locYNeg = lutOffset.y < 0f;
        bool locTiny = lutOffset.sqrMagnitude < 0.01f;

        // Sweep actor Location has frozen/outlier keys (e.g. LUT 27, 50). Lerp through
        // those keys bounces the disc. Direction is smooth; L2 loc ≈ -dir when valid.
        Vector3 placeDir = lutDir.sqrMagnitude > 1e-8f ? lutDir.normalized : Vector3.up;
        bool flipped = visible && placeDir.y < 0f;
        string source = "DIR";
        if (flipped)
        {
            placeDir = -placeDir;
            source = "DIR_FLIP";
        }

        float skyDistance = ResolveSkyDistance(cam);

        Vector3 pos = camPos + placeDir * skyDistance;
        if (!_followCamera)
        {
            pos = placeDir * skyDistance;
        }

        LogTrajectory(body, ref track, hours, sample, visible, flipped, locYNeg, locTiny, source,
            lutOffset, lutDir, lutElevDeg, drawScale, visScale, placeDir, pos, camPos, periodic);

        if (!visible)
        {
            return;
        }

        tf.position = pos;

        if (cam != null)
        {
            // Screen-aligned: a world LookRotation(+up) tilts the quad and stretches the disc.
            tf.rotation = cam.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        }

        float sideUu = refSideUu * (drawScale / Mathf.Max(0.01f, refDrawScale));
        float distanceM = Vector3.Distance(pos, camPos);
        float refDistanceM = VectorUtils.ConvertL2UuToMeters(refDistanceUu);
        if (distanceM > 0.05f && refDistanceM > 0.05f)
        {
            sideUu *= distanceM / refDistanceM;
        }

        float sideM = VectorUtils.ConvertL2UuToMeters(sideUu);
        tf.localScale = new Vector3(mirrorX ? -sideM : sideM, sideM, 1f);

        if (mat != null)
        {
            mat.SetColor("_Color", tint);
        }
    }

    void LogTrajectory(
        string body,
        ref BodyTrack track,
        float hours,
        L2CelestialLut.Sample sample,
        bool visible,
        bool fallback,
        bool locYNeg,
        bool locTiny,
        string source,
        Vector3 lutOffset,
        Vector3 lutDir,
        float lutElevDeg,
        float drawScale,
        float visScale,
        Vector3 placeDir,
        Vector3 pos,
        Vector3 camPos,
        bool periodic)
    {
        if (!Application.isPlaying || !_logTrajectory)
        {
            track.hasLast = visible;
            track.lastVisible = visible;
            track.lastFallback = fallback;
            track.lastPlaceDir = placeDir;
            track.lastPos = pos;
            track.lastHours = hours;
            return;
        }

        float locMag = lutOffset.magnitude;
        Vector3 locDir = locMag > 1e-6f ? lutOffset / locMag : Vector3.zero;
        float locElev = locMag > 1e-6f ? Mathf.Asin(Mathf.Clamp(locDir.y, -1f, 1f)) * Mathf.Rad2Deg : 0f;
        Vector3 dirN = lutDir.sqrMagnitude > 1e-8f ? lutDir.normalized : Vector3.zero;
        float dirElev = dirN.sqrMagnitude > 1e-8f ? Mathf.Asin(Mathf.Clamp(dirN.y, -1f, 1f)) * Mathf.Rad2Deg : 0f;
        float placeElev = Mathf.Asin(Mathf.Clamp(placeDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        float placeAzim = Mathf.Atan2(placeDir.x, placeDir.z) * Mathf.Rad2Deg;
        float locVsDir = (locMag > 1e-6f && dirN.sqrMagnitude > 1e-8f) ? Vector3.Angle(locDir, dirN) : -1f;

        float angDelta = 0f;
        float hoursDelta = 0f;
        if (track.hasLast)
        {
            angDelta = Vector3.Angle(track.lastPlaceDir, placeDir);
            hoursDelta = hours - track.lastHours;
            if (hoursDelta < -12f)
            {
                hoursDelta += 24f;
            }
        }

        bool jump = track.hasLast && visible && track.lastVisible && angDelta >= _jumpAngleDeg;
        bool fallbackFlip = track.hasLast && visible && track.lastFallback != fallback;
        bool visFlip = track.hasLast && track.lastVisible != visible;
        if (periodic || jump || fallbackFlip || visFlip)
        {
            string tag = jump ? "JUMP " : (fallbackFlip ? "FLIP " : (visFlip ? "VIS  " : "     "));
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[L2Sky] {0}{1} {2} h={3:00.000} {4} vis={5} src={6} i={7}-{8} u={9:0.00} phase={10} " +
                "angΔ={11:0.00}° dh={12:0.0000}h locVsDir={13:0.0}° " +
                "lutEl={14:0.0} dirEl={15:0.0} locEl={16:0.0} placeEl={17:0.0} az={18:0.0} " +
                "visS={19:0.00} draw={20:0.00} locY={21:0.00} locMag={22:0.00} tiny={23} yNeg={24} fb={25} " +
                "loc=({26:0.00},{27:0.00},{28:0.00}) dir=({29:0.00},{30:0.00},{31:0.00}) " +
                "place=({32:0.00},{33:0.00},{34:0.00}) pos=({35:0.00},{36:0.00},{37:0.00}) dist={38:0.00}",
                tag, body, FormatClock(hours), hours, visible ? "ON" : "OFF", visible, source,
                sample.sampleA, sample.sampleB, sample.lerpU, sample.phase,
                angDelta, hoursDelta, locVsDir,
                lutElevDeg, dirElev, locElev, placeElev, placeAzim,
                visScale, drawScale, lutOffset.y, locMag, locTiny, locYNeg, fallback,
                lutOffset.x, lutOffset.y, lutOffset.z,
                lutDir.x, lutDir.y, lutDir.z,
                placeDir.x, placeDir.y, placeDir.z,
                pos.x, pos.y, pos.z,
                Vector3.Distance(pos, camPos)));
        }

        track.hasLast = true;
        track.lastVisible = visible;
        track.lastFallback = fallback;
        track.lastPlaceDir = placeDir;
        track.lastPos = pos;
        track.lastHours = hours;
    }

    float ResolveSkyDistance(Camera cam)
    {
        float far = cam != null ? cam.farClipPlane : 500f;
        // Keep the quad inside the far plane; corners of a large billboard sit farther than the center.
        float autoFar = Mathf.Clamp(far * 0.88f, 80f, 4000f);
        if (_forceDistanceMeters > 80f)
        {
            return Mathf.Min(_forceDistanceMeters, far * 0.95f);
        }

        return autoFar;
    }

    static string FormatClock(float hours)
    {
        float t = hours % 24f;
        if (t < 0f)
        {
            t += 24f;
        }

        int sec = Mathf.Clamp((int)(t * 3600f), 0, 86399);
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", sec / 3600, (sec % 3600) / 60, sec % 60);
    }

    void EnsureDiscs()
    {
        if (_sunTexture == null)
        {
            _sunTexture = Resources.Load<Texture2D>(SunTexPath);
        }

        if (_moonTexture == null)
        {
            _moonTexture = Resources.Load<Texture2D>(MoonTexPath);
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            return;
        }

        if (_sunTf == null)
        {
            CreateDisc("L2SunDisc", _sunTexture, shader, out _sunTf, out _sunMr, out _sunMat);
        }

        if (_moonTf == null)
        {
            CreateDisc("L2MoonDisc", _moonTexture, shader, out _moonTf, out _moonMr, out _moonMat);
        }
    }

    void CreateDisc(string name, Texture2D tex, Shader shader, out Transform tf, out MeshRenderer mr, out Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DestroyImmediate(go.GetComponent<Collider>());

        tf = go.transform;
        tf.SetParent(null, true);
        ApplyCelestialLayer(go);

        mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.allowOcclusionWhenDynamic = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        mat = new Material(shader);
        mat.hideFlags = HideFlags.DontSave;
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_Color", Color.white);
        mr.sharedMaterial = mat;
    }

    void ApplyCelestialLayer(GameObject go)
    {
        if (L2FxCompositorRuntime.PreferGpuQueue)
            L2FxCompositorLayers.ApplyL2SkyLayer(go);
        else
            go.layer = 0;
    }

    void LateUpdateApplyLayer()
    {
        if (_sunTf != null)
            ApplyCelestialLayer(_sunTf.gameObject);
        if (_moonTf != null)
            ApplyCelestialLayer(_moonTf.gameObject);
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

    static void DestroyDisc(ref Transform tf, ref Material mat)
    {
        if (mat != null)
        {
            if (Application.isPlaying)
            {
                Destroy(mat);
            }
            else
            {
                DestroyImmediate(mat);
            }

            mat = null;
        }

        if (tf != null)
        {
            if (Application.isPlaying)
            {
                Destroy(tf.gameObject);
            }
            else
            {
                DestroyImmediate(tf.gameObject);
            }

            tf = null;
        }
    }
}
