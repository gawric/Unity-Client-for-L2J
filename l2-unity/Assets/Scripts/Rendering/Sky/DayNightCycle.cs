using AtmosphericHeightFog;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private Material _skyboxMaterial;

    private Light _mainLight;
    private WorldClock _clock;

    [Header("Sun/Moon appearances")]
    [SerializeField] private float _horizonOffsetDegree = 5f;
    [SerializeField] private float _mainLightRotY = 45f;
    [SerializeField] private Texture2D _sunTexture;
    [SerializeField] private Vector2 _sunTiling = new Vector2(1.5f, 1.5f);
    [SerializeField] private Texture2D _moonTexture;
    [SerializeField] private Vector2 _moonTiling = new Vector2(2.5f, 2.5f);

    [Header("Sky colors")]
    [SerializeField] private Color _dayColor = new Color(0f / 255f, 90f / 255f, 140f / 255f); // peak at sunriseEndTime
    [SerializeField] private Color _dawnColor = new Color(19f / 255f, 35f / 255f, 55f / 255f); // peak at sunriseStartTime  
    [SerializeField] private Color _duskColor = new Color(70f / 255f, 53f / 255f, 43f / 255f); // peak at sunsetStartTime
    [SerializeField] private Color _nightColor = new Color(1f / 255f, 1f / 255f, 2f / 255f) * -1f; // peak at sunsetEndTime

    [Header("Fog colors")]
    [SerializeField] private Color _dayFogColorStart = new Color(126f / 255f, 190f / 255f, 255f / 255f);
    [SerializeField] private Color _dayFogColorEnd = new Color(115f / 255f, 153f / 255f, 191f / 255f) * 0.7388527f;
    [SerializeField] private Color _nightFogColorStart = new Color(112f / 255f, 117f / 255f, 123f / 255f);
    [SerializeField] private Color _nightFogColorEnd = new Color(112f / 255f, 117f / 255f, 123f / 255f) * 0.7388527f;
    [SerializeField] private float _dayDirectionalIntensity = 0.250f;
    [SerializeField] private float _nightDirectionalIntensity = 0;

    [Header("Main light colors")]
    [SerializeField] private Color _mainLightDayColor = new Color(255f / 255f, 249f / 255f, 225f / 255f);
    [SerializeField] private Color _mainLightNightColor = new Color(101f / 255f, 110f / 255f, 152f / 255f);
    [SerializeField] private Color _mainLightduskColor = new Color(255f / 255f, 206f / 255f, 158f / 255f);
    [SerializeField] private Color _mainLightDawnColor = new Color(255f / 255f, 206f / 255f, 158f / 255f);
    [Header("Ambient light colors")]
    [SerializeField] private Color _ambientLightDayColor = new Color(166f / 255f, 156f / 255f, 135f / 255f);
    [SerializeField] private Color _ambientLightNightColor = new Color(42f / 255f, 42f / 255f, 40f / 255f);
    [SerializeField] private Color _ambientLightduskColor = new Color(82f / 255f, 65f / 255f, 41f / 255f);
    [SerializeField] private Color _ambientLightDawnColor = new Color(96f / 255f, 96f / 255f, 79f / 255f);

    [Header("Skybox cloud leftover (always off — L2 clouds are separate components)")]
    [SerializeField] private float _dayCloudsOpcacity = 0f;
    [SerializeField] private float _nightCloudsOpacity = 0f;
    [SerializeField] private float _dayHorizonCloudsOpcacity = 0f;
    [SerializeField] private float _nightHorizonCloudsOpacity = 0f;

    [Header("Ambient light intensity")]
    [SerializeField] private float _ambientMinIntensity = 0.2f;
    [SerializeField] private float _ambientMaxIntensity = 0.5f;

    [Header("Main light intensity")]
    [SerializeField] private float _mainLightMinIntensity = 0.4f;
    [SerializeField] private float _mainLightMaxIntensity = 1f;

    [Header("L2 High Elf color LUT")]
    [SerializeField] private bool _useL2ColorLut = true;

    [Header("L2 High Elf sun/moon discs")]
    [SerializeField] private bool _useL2Celestial = true;

    [Header("L2 High Elf haze ring")]
    [SerializeField] private bool _useL2HazeRing = true;

    [Header("L2 High Elf star field")]
    [SerializeField] private bool _useL2StarDome = true;

    [Header("L2 High Elf simple clouds")]
    [SerializeField] private bool _useL2CloudSimple = true;

    // EID 1566 myst strip: in the original capture it sits on top of simple
    // clouds as noise. Current port draws a solid colored card and the
    // intended effect is not visible — keep off until the combiner/placement
    // matches L2.
    [Header("L2 High Elf myst clouds (off — result not visible yet)")]
    [SerializeField] private bool _useL2CloudMyst = false;

    L2CelestialDiscs _celestialDiscs;
    L2HazeRing _hazeRing;
    L2StarDome _starDome;
    L2CloudSimple _cloudSimple;
    L2CloudMyst _cloudMyst;

    void OnEnable()
    {
        if (_useL2ColorLut)
        {
            L2DayNightLut.EnsureLoaded();
        }

        if (_useL2Celestial)
        {
            L2CelestialLut.EnsureLoaded();
            EnsureCelestialDiscs();
        }

        if (_useL2HazeRing)
        {
            L2HazeLut.EnsureLoaded();
            EnsureHazeRing();
        }

        if (_useL2StarDome)
        {
            EnsureStarDome();
        }

        StripLegacyCombinedClouds();

        if (_useL2CloudSimple)
        {
            EnsureCloudSimple();
        }

        if (_useL2CloudMyst && L2CloudMyst.PortEnabled)
        {
            EnsureCloudMyst();
        }
        else
        {
            DisableCloudMyst();
        }
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (_clock == null)
            {
                _clock = WorldClock.EnsurePersistent();
            }
            if (_clock == null)
            {
                _clock = GetComponent<WorldClock>();
            }
            if (_clock == null)
            {
                _clock = WorldClock.Instance;
            }
            if (_mainLight == null)
            {
                _mainLight = GetComponent<Light>();
            }
            if (_clock == null || _mainLight == null)
            {
                return;
            }
            if (_useL2Celestial)
            {
                L2CelestialLut.EnsureLoaded();
                EnsureCelestialDiscs();
            }

            if (_useL2HazeRing)
            {
                L2HazeLut.EnsureLoaded();
                EnsureHazeRing();
            }

            if (_useL2StarDome)
            {
                EnsureStarDome();
            }

            if (_useL2CloudSimple)
            {
                EnsureCloudSimple();
            }

            if (_useL2CloudMyst && L2CloudMyst.PortEnabled)
            {
                EnsureCloudMyst();
            }
            else
            {
                DisableCloudMyst();
            }

            bool usedL2LightDir = false;
            if (_useL2Celestial && L2CelestialLut.IsReady)
            {
                usedL2LightDir = ApplyL2CelestialLight();
            }

            if (!usedL2LightDir)
            {
                float mainLightLerpValue = _clock.Clock.dayRatio > 0 ? _clock.Clock.dayRatio : _clock.Clock.nightRatio;
                float sunRotation = Mathf.Lerp(0 - _horizonOffsetDegree, 180 + _horizonOffsetDegree, mainLightLerpValue);
                transform.eulerAngles = new Vector3(sunRotation, _mainLightRotY, 0);

                if (transform.eulerAngles.x < -1 || transform.eulerAngles.x > 181)
                {
                    _mainLight.intensity = 0;
                }
                else
                {
                    UpdateMainLightIntensity();
                }
            }

            ShareMainLightRotation();

            UpdateAmbientLightIntensity();

            if (_useL2ColorLut)
            {
                L2DayNightLut.EnsureLoaded();
            }

            if (_useL2ColorLut && L2DayNightLut.IsReady)
            {
                ApplyL2ColorLut();
            }
            else
            {
                UpdateSkyColor();
                UpdateFogColor();
                UpdateAmbientLightColor();
                UpdateLightColor();
            }

            UpdateCloudsOpacity();
            if (_useL2Celestial)
            {
                HideSkyboxSunMoon();
            }
            else if (_skyboxMaterial != null)
            {
                UpdateMainLightTexture();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

    }

    private void ApplyL2ColorLut()
    {
        var sample = L2DayNightLut.SampleAt(_clock.WorldHours);

        if (_skyboxMaterial != null)
        {
            // Flat L2 display sky: G1 fills the dome. Clouds / HDR G3 diluted the
            // LUT into dirty turquoise in unity_soon.rdc (#83A4BE vs #62ADEA).
            Color displaySky = L2DayNightLut.ApplyPostSky(sample.sky, _clock.WorldHours);
            ConfigureFlatSkyDome(displaySky);
        }

        L2FxSkyDebug.LogLut(_clock != null ? _clock.WorldHours : -1f, sample, _skyboxMaterial);

        _mainLight.color = sample.sun;

        RenderSettings.ambientSkyColor = sample.actorAmbient;

        if (HeightFogGlobal.Instance != null)
        {
            HeightFogGlobal.Instance.fogColorStart = sample.sky;
            HeightFogGlobal.Instance.fogColorEnd = sample.sky * 0.74f;
            float fogDir = sample.sun.r + sample.sun.g + sample.sun.b > 0.05f
                ? _dayDirectionalIntensity
                : _nightDirectionalIntensity;
            HeightFogGlobal.Instance.directionalIntensity = fogDir;
        }
    }

    private void ConfigureFlatSkyDome(Color skyColor)
    {
        if (_skyboxMaterial != null && _skyboxMaterial.HasProperty("_GradientColor1"))
            _skyboxMaterial.SetColor("_GradientColor1", skyColor);

        if (_skyboxMaterial != null && RenderSettings.skybox != _skyboxMaterial)
            RenderSettings.skybox = _skyboxMaterial;

        ApplyCameraSkyClear(skyColor);
    }

    private static void ApplyCameraSkyClear(Color skyColor)
    {
        // Edit-mode DayNightCycle must not rewrite login/lobby camera clears.
        if (!Application.isPlaying)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        // L2 sky is a color fill (GetSkyBoxColor). Skybox-clear only works if
        // Camera.RenderSkybox actually writes; otherwise previous frames trail.
        // SolidColor always clears. The MIT Unity skybox shader still sits on
        // RenderSettings.skybox for GI / a later cubemap.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = skyColor;
    }

    private void UpdateSkyColor()
    {
        Color skyColor = _skyboxMaterial.GetColor("_GradientColor1");
        if (_clock.Clock.dawnRatio > 0 && _clock.Clock.dawnRatio < 1)
        {
            skyColor = Color.Lerp(_nightColor, _dawnColor, _clock.Clock.dawnRatio);
        }
        if (_clock.Clock.brightRatio > 0)
        {
            if (_clock.Clock.brightRatio < 0.2f)
            {
                skyColor = Color.Lerp(_dawnColor, _dayColor, _clock.Clock.brightRatio / 0.2f);
            }
            else if (_clock.Clock.brightRatio < 1f)
            {
                skyColor = _dayColor;
            }
        }
        if (_clock.Clock.darkRatio > 0)
        {
            if (_clock.Clock.darkRatio < 0.05f)
            {
                skyColor = Color.Lerp(_duskColor, _nightColor, _clock.Clock.darkRatio / 0.05f);
            }
            else if (_clock.Clock.darkRatio < 1f)
            {
                skyColor = _nightColor;
            }
        }
        if (_clock.Clock.duskRatio > 0 && _clock.Clock.duskRatio < 1)
        {
            skyColor = Color.Lerp(_dayColor, _duskColor, _clock.Clock.duskRatio);
        }

        ConfigureFlatSkyDome(skyColor);
    }

    private void UpdateLightColor()
    {
        Color lightColor = _mainLight.color;
        if (_clock.Clock.dawnRatio > 0 && _clock.Clock.dawnRatio < 1)
        {
            lightColor = Color.Lerp(_mainLightNightColor, _mainLightDawnColor, _clock.Clock.dawnRatio);
        }
        if (_clock.Clock.brightRatio > 0)
        {
            if (_clock.Clock.brightRatio < 0.2f)
            {
                lightColor = Color.Lerp(_mainLightDawnColor, _mainLightDayColor, _clock.Clock.brightRatio / 0.2f);
            }
            else if (_clock.Clock.brightRatio < 1f)
            {
                lightColor = _mainLightDayColor;
            }
        }
        if (_clock.Clock.darkRatio > 0)
        {
            if (_clock.Clock.darkRatio < 0.05f)
            {
                lightColor = Color.Lerp(_mainLightduskColor, _mainLightNightColor, _clock.Clock.darkRatio / 0.05f);
            }
            else if (_clock.Clock.darkRatio < 1f)
            {
                lightColor = _mainLightNightColor;
            }
        }
        if (_clock.Clock.duskRatio > 0 && _clock.Clock.duskRatio < 1)
        {
            lightColor = Color.Lerp(_mainLightDayColor, _mainLightduskColor, _clock.Clock.duskRatio);
        }
        _mainLight.color = lightColor;
    }

    private void UpdateFogColor()
    {
        if (HeightFogGlobal.Instance == null)
        {
            return;
        }

        Color fogColorStart = HeightFogGlobal.Instance.fogColorStart;
        Color fogColorEnd = HeightFogGlobal.Instance.fogColorEnd;
        float directionalIntensity = HeightFogGlobal.Instance.directionalIntensity;
        if (_clock.Clock.dawnRatio > 0 && _clock.Clock.dawnRatio < 1)
        {
            fogColorStart = Color.Lerp(fogColorStart, _dayFogColorStart, _clock.Clock.dawnRatio);
            fogColorEnd = Color.Lerp(fogColorEnd, _dayFogColorEnd, _clock.Clock.dawnRatio);
            directionalIntensity = Mathf.Lerp(directionalIntensity, _dayDirectionalIntensity, _clock.Clock.dawnRatio);
        }
        if (_clock.Clock.brightRatio > 0 && _clock.Clock.brightRatio < 1)
        {
            fogColorStart = _dayFogColorStart;
            fogColorEnd = _dayFogColorEnd;
            directionalIntensity = _dayDirectionalIntensity;
        }
        if (_clock.Clock.duskRatio > 0 && _clock.Clock.duskRatio < 1)
        {
            fogColorStart = Color.Lerp(_dayFogColorStart, _nightFogColorStart, _clock.Clock.duskRatio);
            fogColorEnd = Color.Lerp(_dayFogColorEnd, _nightFogColorEnd, _clock.Clock.duskRatio);
            directionalIntensity = Mathf.Lerp(directionalIntensity, _nightDirectionalIntensity, _clock.Clock.duskRatio);
        }
        if (_clock.Clock.darkRatio > 0 && _clock.Clock.darkRatio < 1)
        {
            fogColorStart = _nightFogColorStart;
            fogColorEnd = _nightFogColorEnd;
            directionalIntensity = _nightDirectionalIntensity;
        }

        HeightFogGlobal.Instance.fogColorStart = fogColorStart;
        HeightFogGlobal.Instance.fogColorEnd = fogColorEnd;
        HeightFogGlobal.Instance.directionalIntensity = directionalIntensity;
    }

    private void UpdateAmbientLightIntensity()
    {
        // Ambient light intensity
        RenderSettings.ambientIntensity = AdjustIntensity(_ambientMinIntensity, _ambientMaxIntensity, _clock.Clock.dawnRatio, _clock.Clock.duskRatio); ;
    }

    private void UpdateAmbientLightColor()
    {
        // Ambient light intensity
        Color lightColor = RenderSettings.ambientSkyColor;

        if (_clock.Clock.dawnRatio > 0 && _clock.Clock.dawnRatio < 1)
        {
            lightColor = Color.Lerp(_ambientLightNightColor, _ambientLightDawnColor, _clock.Clock.dawnRatio);
        }
        if (_clock.Clock.brightRatio > 0)
        {
            if (_clock.Clock.brightRatio < 0.2f)
            {
                lightColor = Color.Lerp(_ambientLightDawnColor, _ambientLightDayColor, _clock.Clock.brightRatio / 0.2f);
            }
            else if (_clock.Clock.brightRatio < 1f)
            {
                lightColor = _ambientLightDayColor;
            }
        }
        if (_clock.Clock.darkRatio > 0)
        {
            if (_clock.Clock.darkRatio < 0.05f)
            {
                lightColor = Color.Lerp(_ambientLightduskColor, _ambientLightNightColor, _clock.Clock.darkRatio / 0.05f);
            }
            else if (_clock.Clock.darkRatio < 1f)
            {
                lightColor = _ambientLightNightColor;
            }
        }
        if (_clock.Clock.duskRatio > 0 && _clock.Clock.duskRatio < 1)
        {
            lightColor = Color.Lerp(_ambientLightDayColor, _ambientLightduskColor, _clock.Clock.duskRatio);
        }

        RenderSettings.ambientSkyColor = lightColor;
    }

    private void UpdateMainLightIntensity()
    {
        // Main light intensity
        _mainLight.intensity = AdjustIntensity(_mainLightMinIntensity, _mainLightMaxIntensity, _clock.Clock.dawnRatio, _clock.Clock.duskRatio);
    }

    private float AdjustIntensity(float minIntensity, float fullIntensity, float dawnRatio, float duskRatio)
    {
        if (duskRatio > 0)
        {
            return Mathf.Lerp(fullIntensity, minIntensity, duskRatio);
        }
        else
        {
            return Mathf.Lerp(minIntensity, fullIntensity, dawnRatio);
        }
    }

    private void UpdateCloudsOpacity()
    {
        if (_skyboxMaterial == null)
        {
            return;
        }

        // MIT skybox cloud planes stay off. L2 sheets are L2CloudSimple / L2CloudMyst.
        _skyboxMaterial.SetFloat("_Clouds_Opacity", 0f);
        _skyboxMaterial.SetFloat("_Horizon_Clouds_Opacity", 0f);
    }

    private void UpdateMainLightTexture()
    {
        // Update texture 
        if (_clock.Clock.dayRatio > 0)
        {
            _skyboxMaterial.SetTexture("_SunMoon", _sunTexture);
            _skyboxMaterial.SetTextureScale("_SunMoon", _sunTiling);
        }
        else
        {
            _skyboxMaterial.SetTexture("_SunMoon", _moonTexture);
            _skyboxMaterial.SetTextureScale("_SunMoon", _moonTiling);
        }
    }

    private void ShareMainLightRotation()
    {
        if (_skyboxMaterial == null)
        {
            return;
        }

        if (_skyboxMaterial.HasProperty("_MainLightForward"))
            _skyboxMaterial.SetVector("_MainLightForward", transform.forward);
        if (_skyboxMaterial.HasProperty("_MainLightUp"))
            _skyboxMaterial.SetVector("_MainLightUp", transform.up);
        if (_skyboxMaterial.HasProperty("_MainLightRight"))
            _skyboxMaterial.SetVector("_MainLightRight", transform.right);
    }

    void EnsureCelestialDiscs()
    {
        if (_celestialDiscs == null)
        {
            _celestialDiscs = GetComponent<L2CelestialDiscs>();
        }

        if (_celestialDiscs == null)
        {
            _celestialDiscs = gameObject.AddComponent<L2CelestialDiscs>();
        }

        if (!_celestialDiscs.enabled)
        {
            _celestialDiscs.enabled = true;
        }
    }

    void EnsureHazeRing()
    {
        _hazeRing = L2HazeRing.EnsureOn(gameObject);
    }

    void EnsureStarDome()
    {
        _starDome = L2StarDome.EnsureOn(gameObject);
    }

    void StripLegacyCombinedClouds()
    {
        L2CloudLayers.StripLegacyCloudChildren(transform);
        L2CloudLayers leftover = GetComponent<L2CloudLayers>();
        if (leftover != null)
        {
            if (Application.isPlaying)
                Destroy(leftover);
            else
                DestroyImmediate(leftover);
        }
    }

    void EnsureCloudSimple()
    {
        _cloudSimple = L2CloudSimple.EnsureOn(gameObject);
    }

    void EnsureCloudMyst()
    {
        _cloudMyst = L2CloudMyst.EnsureOn(gameObject);
    }

    void DisableCloudMyst()
    {
        L2CloudMyst myst = _cloudMyst != null ? _cloudMyst : GetComponent<L2CloudMyst>();
        if (myst == null)
            return;
        if (myst.enabled)
            myst.enabled = false;
        _cloudMyst = null;
    }

    bool ApplyL2CelestialLight()
    {
        if (_clock == null || _mainLight == null)
        {
            return false;
        }

        var celestial = L2CelestialLut.SampleAt(_clock.WorldHours);
        Vector3 dir = celestial.sunDirUnity;
        if (dir.sqrMagnitude < 1e-6f)
        {
            return false;
        }

        Vector3 forward = -dir.normalized;
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
        {
            up = Vector3.forward;
        }

        transform.rotation = Quaternion.LookRotation(forward, up);

        if (celestial.sunScale > 0.02f && celestial.sunElevDeg > 1f)
        {
            UpdateMainLightIntensity();
        }
        else
        {
            _mainLight.intensity = 0f;
            _mainLight.color = Color.black;
        }

        return true;
    }

    void HideSkyboxSunMoon()
    {
        if (_skyboxMaterial == null)
        {
            return;
        }

        if (_skyboxMaterial.HasProperty("_SunMoonSize"))
        {
            _skyboxMaterial.SetFloat("_SunMoonSize", 0f);
        }
    }
}
