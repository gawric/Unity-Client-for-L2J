using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// TimeEnv0.INT hourly keys, or live TimeSweep CSV from GetTerrainAmbientColor.
/// Actor ambient stays in L2DayNightLut. This is lighting on geometry, not Yebis.
/// </summary>
public static class L2EnvWorldLut
{
    public struct Sample
    {
        public Color terrainAmbient;
        public Color staticMeshAmbient;
        public Color bspAmbient;
        public Color hsvTerrainLight;
        public Color hsvStaticMeshLight;
    }

    const string SweepLutFileName = "L2WorldAmbientLUT.csv";
    const string IntensityLutFileName = "L2TerrainIntensityLUT.csv";
    const string MetaFileName = "TimeEnv0.int";
    // Debugger pixel at 03:00 (open dirt, I=156). Used only if the intensity LUT is missing.
    const float DefaultTerrainIntensity01 = 156f / 255f;

    static readonly int TerrainAmbientId = Shader.PropertyToID("_L2TerrainAmbient");
    static readonly int StaticMeshAmbientId = Shader.PropertyToID("_L2StaticMeshAmbient");
    static readonly int BspAmbientId = Shader.PropertyToID("_L2BspAmbient");
    static readonly int ActorAmbientId = Shader.PropertyToID("_L2ActorAmbient");
    static readonly int HsvTerrainLightId = Shader.PropertyToID("_L2HsvTerrainLight");
    static readonly int HsvStaticMeshLightId = Shader.PropertyToID("_L2HsvStaticMeshLight");
    static readonly int FogColorId = Shader.PropertyToID("_L2FogColor");
    static readonly int FogEndId = Shader.PropertyToID("_L2FogEnd");
    static readonly int FogScaleId = Shader.PropertyToID("_L2FogScale");
    static readonly int FogEnableId = Shader.PropertyToID("_L2FogEnable");
    static readonly int TerrainColor0ScaleId = Shader.PropertyToID("_L2TerrainColor0Scale");
    static readonly int TerrainIntensityId = Shader.PropertyToID("_L2TerrainIntensity");
    static readonly int TerrainIntensityMapId = Shader.PropertyToID("_L2TerrainIntensityMap");
    static readonly int TerrainIntensityOriginId = Shader.PropertyToID("_L2TerrainIntensityOrigin");
    static readonly int TerrainIntensityMap1Id = Shader.PropertyToID("_L2TerrainIntensityMap1");
    static readonly int TerrainIntensityOrigin1Id = Shader.PropertyToID("_L2TerrainIntensityOrigin1");
    static readonly int TerrainIntensityMap2Id = Shader.PropertyToID("_L2TerrainIntensityMap2");
    static readonly int TerrainIntensityOrigin2Id = Shader.PropertyToID("_L2TerrainIntensityOrigin2");
    static readonly int TerrainLightDebugId = Shader.PropertyToID("_L2TerrainLightDebug");
    const bool ShowTerrainLightDebug = false;
    // PRIMARY terrain sun grid: L2unpacker Output/ExtractStoredIntensity.java.
    // It reads the 8x17x17 windows already stored on each TerrainSector.
    // L2TerrainIntensityGrid.bin is the emergency ray rebake (terrain-normals /
    // writeIntensityGrid). That bake misses BSP and hardcodes the sun.
    // Keep this flag on. Do not replace the stored bins with the rebake.
    const bool UseStoredClientIntensityGrid = true;
    // Original terrain windows from the client map. The generated 17_25 bake
    // lives in L2Unpacker/Beast and is not loaded.
    const bool LoadStoredIntensityMasks = true;
    const bool LoadBaked17_25Intensity = false;
    const string IntensityGridFileName = "L2TerrainIntensityGrid.bin";
    const string StoredIntensityGridFileName = "L2TerrainIntensityGrid_stored.bin";
    const string Baked17_25IntensityFileName = "L2TerrainIntensityGrid_17_25_baked.bin";
    const int IntensityGridSize = 257;
    // 17_25 vertex 0 in L2 units: X = -98304, Y = 229376. Cell is 128 UU.
    static readonly Vector4 IntensityOrigin = new Vector4(-98304f, 229376f, 128f, IntensityGridSize);
    static readonly int StaticMeshMaskSlot0Id = Shader.PropertyToID("_L2StaticMeshMaskSlot0");
    static readonly int StaticMeshMaskSlot1Id = Shader.PropertyToID("_L2StaticMeshMaskSlot1");
    static readonly int StaticMeshMaskBlendId = Shader.PropertyToID("_L2StaticMeshMaskBlend");
    static readonly int StaticMeshLightDirectionId = Shader.PropertyToID("_L2StaticMeshLightDirection");

    // D3D9 render_state from mosque/terrain draws: linear fog mode 3.
    public const float FogEndUu = 12288f;
    public const float FogRangeUu = 8192f;

    static readonly List<KeyColor> TerrainAmbient = new List<KeyColor>(32);
    static readonly List<KeyColor> StaticMeshAmbient = new List<KeyColor>(32);
    static readonly List<KeyColor> BspAmbient = new List<KeyColor>(32);
    static readonly List<KeyHsv> HsvTerrainLight = new List<KeyHsv>(32);

    static readonly Regex ColorRe = new Regex(
        @"T\s*=\s*([0-9.]+)\s*,\s*R\s*=\s*([0-9.]+)\s*,\s*G\s*=\s*([0-9.]+)\s*,\s*B\s*=\s*([0-9.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex HsvRe = new Regex(
        @"T\s*=\s*([0-9.]+)\s*,\s*Hue\s*=\s*([0-9.]+)\s*,\s*Sat\s*=\s*([0-9.]+)\s*,\s*Bri\s*=\s*([0-9.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly List<SweepKey> SweepKeys = new List<SweepKey>(320);
    static readonly List<KeyFloat> TerrainIntensity = new List<KeyFloat>(320);
    static bool _triedLoad;

    public static bool IsReady =>
        SweepKeys.Count >= 2 || (TerrainAmbient.Count >= 2 && StaticMeshAmbient.Count >= 2);

    public static void EnsureLoaded()
    {
        if (_triedLoad)
            return;

        _triedLoad = true;
        string sweepPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta", SweepLutFileName);
        if (File.Exists(sweepPath))
        {
            LoadSweepCsv(File.ReadAllText(sweepPath));
            if (SweepKeys.Count >= 2)
            {
                // Debug.Log("[L2EnvWorldLut] Loaded " + SweepKeys.Count + " GetTerrainAmbient keys from " + SweepLutFileName);
                LoadIntensityLut();
                return;
            }
        }

        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta", MetaFileName);
        if (!File.Exists(dataPath))
        {
            // Debug.LogWarning("[L2EnvWorldLut] File not found: " + dataPath);
            LoadIntensityLut();
            return;
        }

        LoadInt(File.ReadAllText(dataPath));
        LoadIntensityLut();
    }

    static void LoadIntensityLut()
    {
        TerrainIntensity.Clear();
        string path = Path.Combine(Application.streamingAssetsPath, "Data/Meta", IntensityLutFileName);
        if (!File.Exists(path))
        {
            // Debug.LogWarning("[L2EnvWorldLut] Intensity LUT missing (" + IntensityLutFileName + "), using I=" + DefaultTerrainIntensity01.ToString("0.###"));
            return;
        }

        var lines = File.ReadAllText(path).Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 2)
                continue;
            float tHours = Parse(c[0]);
            if (tHours >= 23.999f)
                continue;
            TerrainIntensity.Add(new KeyFloat
            {
                tHours = tHours,
                value = Mathf.Clamp01(Parse(c[1]))
            });
        }

        TerrainIntensity.Sort((a, b) => a.tHours.CompareTo(b.tHours));
        //if (TerrainIntensity.Count >= 2)
            //Debug.Log("[L2EnvWorldLut] Loaded " + TerrainIntensity.Count + " intensity keys from " + IntensityLutFileName);
    }

    public static void LoadSweepCsv(string csvText)
    {
        SweepKeys.Clear();
        if (string.IsNullOrEmpty(csvText))
            return;

        var lines = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 10)
                continue;

            float tHours = Parse(c[0]);
            if (tHours >= 23.999f)
                continue;

            var key = new SweepKey
            {
                tHours = tHours,
                terrain = new Color(Parse(c[1]), Parse(c[2]), Parse(c[3]), 1f),
                staticMesh = new Color(Parse(c[4]), Parse(c[5]), Parse(c[6]), 1f),
                bsp = new Color(Parse(c[7]), Parse(c[8]), Parse(c[9]), 1f),
                hsvTerrain = Color.black,
                hsvStatic = Color.black
            };
            if (c.Length >= 14)
                key.hsvTerrain = new Color(Parse(c[10]), Parse(c[11]), Parse(c[12]), 1f);
            if (c.Length >= 18)
                key.hsvStatic = new Color(Parse(c[14]), Parse(c[15]), Parse(c[16]), 1f);
            SweepKeys.Add(key);
        }

        SweepKeys.Sort((a, b) => a.tHours.CompareTo(b.tHours));
    }

    public static void LoadInt(string text)
    {
        TerrainAmbient.Clear();
        StaticMeshAmbient.Clear();
        BspAmbient.Clear();
        HsvTerrainLight.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        string section = "";
        var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = StripComment(lines[i]).Trim();
            if (line.Length == 0)
                continue;

            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                section = line.Substring(1, line.Length - 2);
                continue;
            }

            if (section == "TerrainAmbient")
                TryAddColor(TerrainAmbient, line);
            else if (section == "StaticMeshAmbient")
                TryAddColor(StaticMeshAmbient, line);
            else if (section == "BSPAmbient")
                TryAddColor(BspAmbient, line);
            else if (section == "HSVTerrainLight")
                TryAddHsv(HsvTerrainLight, line);
        }

        SortByTime(TerrainAmbient);
        SortByTime(StaticMeshAmbient);
        SortByTime(BspAmbient);
        HsvTerrainLight.Sort((a, b) => a.tHours.CompareTo(b.tHours));
    }

    public static Sample SampleAt(float worldHours)
    {
        float t = Repeat24(worldHours);
        if (SweepKeys.Count >= 2)
            return SampleSweep(t);

        return new Sample
        {
            terrainAmbient = SampleColor(TerrainAmbient, t),
            staticMeshAmbient = SampleColor(StaticMeshAmbient, t),
            bspAmbient = SampleColor(BspAmbient, t),
            hsvTerrainLight = SampleHsv(HsvTerrainLight, t),
            hsvStaticMeshLight = Color.black
        };
    }

    public static void PushDistanceFog(float worldHours)
    {
        L2HazeLut.EnsureLoaded();
        Shader.SetGlobalColor(FogColorId, L2HazeLut.SampleAt(worldHours));
        float uuToUnity = VectorUtils.L2UuToUnity;
        Shader.SetGlobalFloat(FogEndId, FogEndUu * uuToUnity);
        Shader.SetGlobalFloat(FogScaleId, uuToUnity > 1e-8f ? (1f / FogRangeUu) / uuToUnity : 0f);
        Shader.SetGlobalFloat(FogEnableId, 1f);
    }

    public static void PushGlobals(float worldHours, Color actorAmbient, Color sunColor)
    {
        PushDistanceFog(worldHours);
        EnsureLoaded();
        if (!IsReady)
            return;

        Sample sample = SampleAt(worldHours);
        Shader.SetGlobalColor(TerrainAmbientId, sample.terrainAmbient);
        Shader.SetGlobalColor(StaticMeshAmbientId, sample.staticMeshAmbient);
        Shader.SetGlobalColor(BspAmbientId, sample.bspAmbient);
        Shader.SetGlobalColor(ActorAmbientId, actorAmbient);
        Shader.SetGlobalColor(HsvTerrainLightId, sample.hsvTerrainLight);
        Shader.SetGlobalColor(HsvStaticMeshLightId, sample.hsvStaticMeshLight);

        // ActorStaticLight slots are centred at 01:30, 04:30, ... 22:30.
        float maskPosition = Repeat24(worldHours) / 3f - 0.5f;
        int slot0Unwrapped = Mathf.FloorToInt(maskPosition);
        int slot0 = ((slot0Unwrapped % L2StaticMeshSunVisibility.SlotCount) +
            L2StaticMeshSunVisibility.SlotCount) % L2StaticMeshSunVisibility.SlotCount;
        int slot1 = (slot0 + 1) % L2StaticMeshSunVisibility.SlotCount;
        Shader.SetGlobalFloat(StaticMeshMaskSlot0Id, slot0);
        Shader.SetGlobalFloat(StaticMeshMaskSlot1Id, slot1);
        Shader.SetGlobalFloat(StaticMeshMaskBlendId, maskPosition - Mathf.Floor(maskPosition));

        L2CelestialLut.EnsureLoaded();
        Vector3 sunDirection = L2CelestialLut.SampleAt(worldHours).sunDirUnity;
        Shader.SetGlobalVector(
            StaticMeshLightDirectionId,
            new Vector4(-sunDirection.x, -sunDirection.y, -sunDirection.z, 0f));

        Shader.SetGlobalFloat(TerrainColor0ScaleId, 1f);
        float intensity01 = TerrainIntensity.Count >= 2
            ? SampleFloat(TerrainIntensity, Repeat24(worldHours))
            : DefaultTerrainIntensity01;
        Shader.SetGlobalFloat(TerrainIntensityId, intensity01);
        PushIntensityGrid(Repeat24(worldHours), intensity01);

        Color gi = sample.terrainAmbient;
        _ = sunColor;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientSkyColor = gi;
        RenderSettings.ambientEquatorColor = gi;
        RenderSettings.ambientGroundColor = gi;
        RenderSettings.ambientIntensity = 1f;
    }

    static string StripComment(string line)
    {
        int cut = line.IndexOf(';');
        return cut >= 0 ? line.Substring(0, cut) : line;
    }

    static void TryAddColor(List<KeyColor> list, string line)
    {
        Match m = ColorRe.Match(line);
        if (!m.Success)
            return;
        list.Add(new KeyColor
        {
            tHours = Parse(m.Groups[1].Value),
            color = ByteRgb(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value)
        });
    }

    static void TryAddHsv(List<KeyHsv> list, string line)
    {
        Match m = HsvRe.Match(line);
        if (!m.Success)
            return;
        list.Add(new KeyHsv
        {
            tHours = Parse(m.Groups[1].Value),
            hue = Parse(m.Groups[2].Value),
            sat = Parse(m.Groups[3].Value),
            bri = Parse(m.Groups[4].Value)
        });
    }

    static Sample SampleSweep(float t)
    {
        int i1 = 0;
        while (i1 < SweepKeys.Count && SweepKeys[i1].tHours < t)
            i1++;

        SweepKey a;
        SweepKey b;
        float u;
        if (i1 <= 0 || i1 >= SweepKeys.Count)
        {
            a = SweepKeys[SweepKeys.Count - 1];
            b = SweepKeys[0];
            float span = (24f - a.tHours) + b.tHours;
            float along = t >= a.tHours ? (t - a.tHours) : ((24f - a.tHours) + t);
            u = span > 1e-6f ? along / span : 0f;
        }
        else
        {
            a = SweepKeys[i1 - 1];
            b = SweepKeys[i1];
            float span = b.tHours - a.tHours;
            u = span > 1e-6f ? (t - a.tHours) / span : 0f;
        }

        return new Sample
        {
            terrainAmbient = Color.Lerp(a.terrain, b.terrain, u),
            staticMeshAmbient = Color.Lerp(a.staticMesh, b.staticMesh, u),
            bspAmbient = Color.Lerp(a.bsp, b.bsp, u),
            hsvTerrainLight = Color.Lerp(a.hsvTerrain, b.hsvTerrain, u),
            hsvStaticMeshLight = Color.Lerp(a.hsvStatic, b.hsvStatic, u)
        };
    }

    static Color SampleColor(List<KeyColor> keys, float t)
    {
        if (keys == null || keys.Count == 0)
            return Color.black;
        if (keys.Count == 1)
            return keys[0].color;

        int i1 = 0;
        while (i1 < keys.Count && keys[i1].tHours < t)
            i1++;

        if (i1 <= 0)
            return keys[0].color;
        if (i1 >= keys.Count)
            return keys[keys.Count - 1].color;

        KeyColor a = keys[i1 - 1];
        KeyColor b = keys[i1];
        float span = b.tHours - a.tHours;
        float u = span > 1e-6f ? (t - a.tHours) / span : 0f;
        return Color.Lerp(a.color, b.color, u);
    }

    static float SampleFloat(List<KeyFloat> keys, float t)
    {
        if (keys == null || keys.Count == 0)
            return DefaultTerrainIntensity01;
        if (keys.Count == 1)
            return keys[0].value;

        int i1 = 0;
        while (i1 < keys.Count && keys[i1].tHours < t)
            i1++;

        KeyFloat a;
        KeyFloat b;
        float u;
        if (i1 <= 0 || i1 >= keys.Count)
        {
            a = keys[keys.Count - 1];
            b = keys[0];
            float span = (24f - a.tHours) + b.tHours;
            float along = t >= a.tHours ? (t - a.tHours) : ((24f - a.tHours) + t);
            u = span > 1e-6f ? along / span : 0f;
        }
        else
        {
            a = keys[i1 - 1];
            b = keys[i1];
            float span = b.tHours - a.tHours;
            u = span > 1e-6f ? (t - a.tHours) / span : 0f;
        }

        return Mathf.Lerp(a.value, b.value, u);
    }

    static Color SampleHsv(List<KeyHsv> keys, float t)
    {
        if (keys == null || keys.Count == 0)
            return Color.black;
        if (keys.Count == 1)
            return HsvToHdr(keys[0]);

        int i1 = 0;
        while (i1 < keys.Count && keys[i1].tHours < t)
            i1++;

        if (i1 <= 0)
            return HsvToHdr(keys[0]);
        if (i1 >= keys.Count)
            return HsvToHdr(keys[keys.Count - 1]);

        KeyHsv a = keys[i1 - 1];
        KeyHsv b = keys[i1];
        float span = b.tHours - a.tHours;
        float u = span > 1e-6f ? (t - a.tHours) / span : 0f;
        var mix = new KeyHsv
        {
            tHours = t,
            hue = Mathf.Lerp(a.hue, b.hue, u),
            sat = Mathf.Lerp(a.sat, b.sat, u),
            bri = Mathf.Lerp(a.bri, b.bri, u)
        };
        return HsvToHdr(mix);
    }

    static Color HsvToHdr(KeyHsv key)
    {
        float h = Mathf.Repeat(key.hue / 255f, 1f);
        float s = Mathf.Clamp01(key.sat / 255f);
        float v = key.bri / 255f;
        return Color.HSVToRGB(h, s, v, true);
    }

    static Color ByteRgb(string r, string g, string b)
    {
        return new Color(Parse(r) / 255f, Parse(g) / 255f, Parse(b) / 255f, 1f);
    }

    static void SortByTime(List<KeyColor> keys)
    {
        keys.Sort((a, b) => a.tHours.CompareTo(b.tHours));
    }

    static float Repeat24(float hours)
    {
        float t = hours % 24f;
        if (t < 0f)
            t += 24f;
        return t;
    }

    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    struct SweepKey
    {
        public float tHours;
        public Color terrain;
        public Color staticMesh;
        public Color bsp;
        public Color hsvTerrain;
        public Color hsvStatic;
    }

    struct KeyFloat
    {
        public float tHours;
        public float value;
    }

    struct KeyColor
    {
        public float tHours;
        public Color color;
    }

    struct KeyHsv
    {
        public float tHours;
        public float hue;
        public float sat;
        public float bri;
    }

    struct IntensitySlice
    {
        public float tHours;
        public byte[] intensity;
        public byte[] valid;
    }

    static readonly List<IntensitySlice> IntensitySlices = new List<IntensitySlice>(8);
    static Texture2D _intensityTex;
    static bool _gridTried;
    static bool _openIntensityPushed;
    static byte[] _unityTreePreview;
    static bool _unityTreePreviewUploaded;
    static int _gridSliceA = -2;
    static int _gridSliceB = -2;
    static byte _gridFill = 255;

    sealed class NeighborIntensityGrid
    {
        public string fileName;
        public Vector4 origin;
        public int textureId;
        public int originId;
        public readonly List<IntensitySlice> slices = new List<IntensitySlice>(8);
        public Texture2D tex;
        public bool tried;
        public int sliceA = -2;
        public int sliceB = -2;
        public byte fill = 255;
    }

    // Same ExtractStoredIntensity bins, one file per map. Not the ray rebake.
    // 16_25 shares the north edge with 17_25, so it is sampled first.
    static readonly NeighborIntensityGrid[] NeighborGrids =
    {
        new NeighborIntensityGrid
        {
            fileName = "L2TerrainIntensityGrid_16_25_stored.bin",
            origin = new Vector4(-131072f, 229376f, 128f, IntensityGridSize),
            textureId = TerrainIntensityMap1Id,
            originId = TerrainIntensityOrigin1Id
        },
        new NeighborIntensityGrid
        {
            fileName = "L2TerrainIntensityGrid_16_24_stored.bin",
            origin = new Vector4(-131072f, 196608f, 128f, IntensityGridSize),
            textureId = TerrainIntensityMap2Id,
            originId = TerrainIntensityOrigin2Id
        }
    };

    // TEMPORARY preview of Bake Unity Shadow Around Tree. White ground, only the
    // cells that bake wrote. The stored bin is not this file.
    public static void ShowUnityTreePreview(byte[] intensity)
    {
        int plane = IntensityGridSize * IntensityGridSize;
        if (intensity == null || intensity.Length != plane)
            return;
        _unityTreePreview = intensity;
        _unityTreePreviewUploaded = false;
        _openIntensityPushed = false;
        UploadUnityTreePreview();
    }

    static string UnityTreePreviewPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BakeOutput", "L2TerrainIntensityGrid_unitytree_preview.bin"));
    }

    static void EnsureUnityTreePreview()
    {
        if (_unityTreePreview != null)
            return;
        string path = UnityTreePreviewPath();
        if (!File.Exists(path))
            return;
        byte[] file = File.ReadAllBytes(path);
        if (file.Length != IntensityGridSize * IntensityGridSize)
            return;
        _unityTreePreview = file;
    }

    static void UploadUnityTreePreview()
    {
        EnsureUnityTreePreview();
        if (_unityTreePreview == null)
            return;

        int n = IntensityGridSize;
        var pixels = new byte[n * n * 4];
        for (int i = 0; i < n * n; i++)
        {
            pixels[i * 4] = _unityTreePreview[i];
            pixels[i * 4 + 1] = 255;
            pixels[i * 4 + 3] = 255;
        }

        if (_intensityTex == null || _intensityTex.width != n || _intensityTex.height != n)
        {
            if (_intensityTex != null)
                UnityEngine.Object.DestroyImmediate(_intensityTex);
            _intensityTex = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                name = "L2TerrainIntensityUnityTree",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        _intensityTex.SetPixelData(pixels, 0);
        _intensityTex.Apply(false, false);
        Shader.SetGlobalTexture(TerrainIntensityMapId, _intensityTex);
        Shader.SetGlobalVector(TerrainIntensityOriginId, IntensityOrigin);
        Shader.SetGlobalVector(TerrainIntensityOrigin1Id, Vector4.zero);
        Shader.SetGlobalVector(TerrainIntensityOrigin2Id, Vector4.zero);
        Shader.SetGlobalFloat(TerrainLightDebugId, 0f);
        _unityTreePreviewUploaded = true;
        _openIntensityPushed = true;
    }

    static void PushOpenIntensity()
    {
        Shader.SetGlobalFloat(TerrainLightDebugId, 0f);
        if (_openIntensityPushed && _intensityTex != null)
            return;
        _openIntensityPushed = true;
        if (_intensityTex == null)
        {
            _intensityTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                name = "L2TerrainIntensityOpen"
            };
            _intensityTex.SetPixel(0, 0, Color.white);
            _intensityTex.Apply(false, false);
        }

        Shader.SetGlobalTexture(TerrainIntensityMapId, _intensityTex);
        Shader.SetGlobalVector(TerrainIntensityOriginId, IntensityOrigin);
        Shader.SetGlobalVector(TerrainIntensityOrigin1Id, Vector4.zero);
        Shader.SetGlobalVector(TerrainIntensityOrigin2Id, Vector4.zero);
    }

    static bool _baked17Tried;
    static bool _bakedNeighborsCleared;

    static void PushIntensityGrid(float hours, float fallback01)
    {
        if (LoadBaked17_25Intensity)
        {
            PushBaked17_25(hours, fallback01);
            return;
        }

        if (!LoadStoredIntensityMasks)
        {
            EnsureUnityTreePreview();
            if (_unityTreePreview != null)
            {
                if (!_unityTreePreviewUploaded)
                    UploadUnityTreePreview();
                return;
            }

            PushOpenIntensity();
            return;
        }

        EnsureIntensityGrid();
        EnsureNeighborGrids();
        byte fill = (byte)Mathf.Clamp(Mathf.RoundToInt(fallback01 * 255f), 0, 255);
        int a;
        int b;
        float u;
        PickIntensitySlices(IntensitySlices, hours, out a, out b, out u);
        Shader.SetGlobalFloat(TerrainLightDebugId, ShowTerrainLightDebug ? 1f : 0f);
        bool primaryClean = _intensityTex != null && a == _gridSliceA && b == _gridSliceB && fill == _gridFill;
        bool neighborsClean = NeighborsClean(hours, fill);
        if (primaryClean && neighborsClean)
            return;

        if (!primaryClean)
            UploadPrimaryGrid(a, b, u, fill);
        if (!neighborsClean)
            UploadNeighborGrids(hours, fill);
    }

    static void UploadPrimaryGrid(int a, int b, float u, byte fill)
    {

        int n = IntensityGridSize;
        var pixels = new byte[n * n * 4];
        if (a < 0 || IntensitySlices.Count == 0)
        {
            for (int i = 0; i < n * n; i++)
            {
                pixels[i * 4] = fill;
                pixels[i * 4 + 3] = 255;
            }
        }
        else
        {
            IntensitySlice sa = IntensitySlices[a];
            IntensitySlice sb = IntensitySlices[b];
            for (int i = 0; i < n * n; i++)
            {
                bool va = sa.valid[i] != 0;
                bool vb = sb.valid[i] != 0;
                byte value = fill;
                byte valid = 0;
                if (va && vb)
                {
                    float mixed = sa.intensity[i] + (sb.intensity[i] - sa.intensity[i]) * u;
                    int truncated = (int)mixed;
                    if (truncated > 255)
                        truncated = 255;
                    if (truncated < 0)
                        truncated = 0;
                    value = (byte)truncated;
                    valid = 255;
                }
                else if (va)
                {
                    value = sa.intensity[i];
                    valid = 255;
                }
                else if (vb)
                {
                    value = sb.intensity[i];
                    valid = 255;
                }

                pixels[i * 4] = value;
                pixels[i * 4 + 1] = valid;
                pixels[i * 4 + 3] = 255;
            }
        }

        if (_intensityTex == null)
        {
            _intensityTex = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
            {
                name = "L2TerrainIntensityMap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            _intensityTex.hideFlags = HideFlags.HideAndDontSave;
        }

        _intensityTex.SetPixelData(pixels, 0);
        _intensityTex.Apply(false, false);
        Shader.SetGlobalTexture(TerrainIntensityMapId, _intensityTex);
        Shader.SetGlobalVector(TerrainIntensityOriginId, IntensityOrigin);
        _gridSliceA = a;
        _gridSliceB = b;
        _gridFill = fill;
    }

    static void PushBaked17_25(float hours, float fallback01)
    {
        Shader.SetGlobalFloat(TerrainLightDebugId, 0f);
        EnsureBaked17_25();
        if (!_bakedNeighborsCleared)
        {
            Shader.SetGlobalVector(TerrainIntensityOrigin1Id, Vector4.zero);
            Shader.SetGlobalVector(TerrainIntensityOrigin2Id, Vector4.zero);
            _bakedNeighborsCleared = true;
        }

        if (IntensitySlices.Count == 0)
        {
            PushOpenIntensity();
            return;
        }

        byte fill = (byte)Mathf.Clamp(Mathf.RoundToInt(fallback01 * 255f), 0, 255);
        int a;
        int b;
        float u;
        PickIntensitySlices(IntensitySlices, hours, out a, out b, out u);
        if (_intensityTex != null && a == _gridSliceA && b == _gridSliceB && fill == _gridFill)
            return;
        UploadPrimaryGrid(a, b, u, fill);
    }

    static void EnsureBaked17_25()
    {
        if (_baked17Tried)
            return;
        _baked17Tried = true;
        _gridTried = true;
        if (!ReadIntensityGrid(Baked17_25IntensityFileName, IntensitySlices))
            Debug.LogWarning("[L2EnvWorldLut] 17_25 baked intensity grid was not loaded.");
    }

    static void EnsureIntensityGrid()
    {
        if (_gridTried)
            return;
        _gridTried = true;
        string fileName = UseStoredClientIntensityGrid ? StoredIntensityGridFileName : IntensityGridFileName;
        string path = Path.Combine(Application.streamingAssetsPath, "Data/Meta", fileName);
        if (!File.Exists(path))
        {
            // Debug.LogWarning("[L2EnvWorldLut] Intensity grid missing (" + fileName + ")");
            return;
        }

        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 16 || file[0] != (byte)'L' || file[1] != (byte)'2' || file[2] != (byte)'I' || file[3] != (byte)'G')
            return;
        int version = BitConverter.ToInt32(file, 4);
        int n = BitConverter.ToInt32(file, 8);
        int count = BitConverter.ToInt32(file, 12);
        if (version != 2 || n != IntensityGridSize || count <= 0)
            return;

        int cursor = 16;
        int plane = n * n;
        for (int s = 0; s < count; s++)
        {
            if (cursor + 4 + plane * 2 > file.Length)
                break;
            float hours = BitConverter.ToSingle(file, cursor);
            cursor += 4;
            var intensity = new byte[plane];
            var valid = new byte[plane];
            System.Buffer.BlockCopy(file, cursor, intensity, 0, plane);
            cursor += plane;
            System.Buffer.BlockCopy(file, cursor, valid, 0, plane);
            cursor += plane;
            IntensitySlices.Add(new IntensitySlice { tHours = hours, intensity = intensity, valid = valid });
        }

        IntensitySlices.Sort((x, y) => x.tHours.CompareTo(y.tHours));
        // Debug.Log("[L2EnvWorldLut] Loaded " + IntensitySlices.Count + " intensity slices from " + fileName);
    }

    static void EnsureNeighborGrids()
    {
        for (int i = 0; i < NeighborGrids.Length; i++)
        {
            NeighborIntensityGrid grid = NeighborGrids[i];
            if (grid.tried)
                continue;
            grid.tried = true;
            if (!ReadIntensityGrid(grid.fileName, grid.slices))
                Shader.SetGlobalVector(grid.originId, Vector4.zero);
        }
    }

    static bool ReadIntensityGrid(string fileName, List<IntensitySlice> destination)
    {
        destination.Clear();
        string path = Path.Combine(Application.streamingAssetsPath, "Data/Meta", fileName);
        if (!File.Exists(path))
        {
            // Debug.LogWarning("[L2EnvWorldLut] Intensity grid missing (" + fileName + ")");
            return false;
        }

        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 16 || file[0] != (byte)'L' || file[1] != (byte)'2' || file[2] != (byte)'I' || file[3] != (byte)'G')
            return false;
        int version = BitConverter.ToInt32(file, 4);
        int n = BitConverter.ToInt32(file, 8);
        int count = BitConverter.ToInt32(file, 12);
        if (version != 2 || n != IntensityGridSize || count <= 0)
            return false;

        int cursor = 16;
        int plane = n * n;
        for (int s = 0; s < count; s++)
        {
            if (cursor + 4 + plane * 2 > file.Length)
                break;
            float hours = BitConverter.ToSingle(file, cursor);
            cursor += 4;
            var intensity = new byte[plane];
            var valid = new byte[plane];
            System.Buffer.BlockCopy(file, cursor, intensity, 0, plane);
            cursor += plane;
            System.Buffer.BlockCopy(file, cursor, valid, 0, plane);
            cursor += plane;
            destination.Add(new IntensitySlice { tHours = hours, intensity = intensity, valid = valid });
        }

        destination.Sort((x, y) => x.tHours.CompareTo(y.tHours));
        // Debug.Log("[L2EnvWorldLut] Loaded " + destination.Count + " intensity slices from " + fileName);
        return destination.Count > 0;
    }

    static bool NeighborsClean(float hours, byte fill)
    {
        for (int i = 0; i < NeighborGrids.Length; i++)
        {
            NeighborIntensityGrid grid = NeighborGrids[i];
            if (!grid.tried)
                return false;
            if (grid.slices.Count == 0)
                continue;
            int a;
            int b;
            float u;
            PickIntensitySlices(grid.slices, hours, out a, out b, out u);
            if (grid.tex == null || a != grid.sliceA || b != grid.sliceB || fill != grid.fill)
                return false;
        }
        return true;
    }

    static void UploadNeighborGrids(float hours, byte fill)
    {
        int n = IntensityGridSize;
        var pixels = new byte[n * n * 4];
        for (int g = 0; g < NeighborGrids.Length; g++)
        {
            NeighborIntensityGrid grid = NeighborGrids[g];
            if (grid.slices.Count == 0)
            {
                Shader.SetGlobalVector(grid.originId, Vector4.zero);
                continue;
            }

            int a;
            int b;
            float u;
            PickIntensitySlices(grid.slices, hours, out a, out b, out u);
            if (grid.tex != null && a == grid.sliceA && b == grid.sliceB && fill == grid.fill)
                continue;

            if (a < 0)
            {
                for (int i = 0; i < n * n; i++)
                {
                    pixels[i * 4] = fill;
                    pixels[i * 4 + 1] = 0;
                    pixels[i * 4 + 3] = 255;
                }
            }
            else
            {
                IntensitySlice sa = grid.slices[a];
                IntensitySlice sb = grid.slices[b];
                for (int i = 0; i < n * n; i++)
                {
                    bool va = sa.valid[i] != 0;
                    bool vb = sb.valid[i] != 0;
                    byte value = fill;
                    byte valid = 0;
                    if (va && vb)
                    {
                        float mixed = sa.intensity[i] + (sb.intensity[i] - sa.intensity[i]) * u;
                        int truncated = (int)mixed;
                        if (truncated > 255)
                            truncated = 255;
                        if (truncated < 0)
                            truncated = 0;
                        value = (byte)truncated;
                        valid = 255;
                    }
                    else if (va)
                    {
                        value = sa.intensity[i];
                        valid = 255;
                    }
                    else if (vb)
                    {
                        value = sb.intensity[i];
                        valid = 255;
                    }

                    pixels[i * 4] = value;
                    pixels[i * 4 + 1] = valid;
                    pixels[i * 4 + 3] = 255;
                }
            }

            if (grid.tex == null)
            {
                grid.tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
                {
                    name = grid.fileName,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                grid.tex.hideFlags = HideFlags.HideAndDontSave;
            }

            grid.tex.SetPixelData(pixels, 0);
            grid.tex.Apply(false, false);
            Shader.SetGlobalTexture(grid.textureId, grid.tex);
            Shader.SetGlobalVector(grid.originId, grid.origin);
            grid.sliceA = a;
            grid.sliceB = b;
            grid.fill = fill;
        }
    }

    static void PickIntensitySlices(List<IntensitySlice> slices, float hours, out int a, out int b, out float u)
    {
        a = -1;
        b = -1;
        u = 0f;
        int count = slices.Count;
        if (count == 0)
            return;
        if (count == 1)
        {
            a = b = 0;
            return;
        }

        // Slot centres are 1.5, 4.5, ... 22.5. Hour 1.0 is the pair 22.5 → 1.5,
        // alpha 5/6, the same wrap as UTerrainSector::UpdateShadow. Clamping to
        // the 1.5 slice alone leaves the lit path byte at 156 instead of 130.
        float first = slices[0].tHours;
        float last = slices[count - 1].tHours;
        if (hours < first || hours > last)
        {
            a = count - 1;
            b = 0;
            float timeA = last;
            float timeB = first + 24f;
            float time = hours < first ? hours + 24f : hours;
            float width = timeB - timeA;
            u = width > 1e-6f ? (time - timeA) / width : 0f;
            return;
        }

        int i1 = 1;
        while (i1 < count && slices[i1].tHours < hours)
            i1++;
        a = i1 - 1;
        b = i1;
        float span = slices[b].tHours - slices[a].tHours;
        u = span > 1e-6f ? (hours - slices[a].tHours) / span : 0f;
    }
}
