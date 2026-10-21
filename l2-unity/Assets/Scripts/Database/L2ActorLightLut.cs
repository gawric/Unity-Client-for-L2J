using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Actor day curve. Keys are the engine getters over 0..23.99.
/// UL2NTimeLight::GetColorPlane picks the segment that contains the hour and
/// lerps it. The stored plane is already that color times brightness/255.
/// USkeletalMeshInstance::RenderPreProcess shifts the ambient byte right by 1.
/// _L2HsvActorLight.a is that brightness/255. It is not a second multiply.
/// DirectionalScale is not in GetColorPlane. One keltir frame had c250 = plane * 0.7.
/// </summary>
public static class L2ActorLightLut
{
    public struct Sample
    {
        public float tHours;
        public Color ambient;
        public Color ambientHalf;
        public Color sunPlane;
        public Color sunBase;
    }

    const string MetaFileName = "L2ActorLightLUT.csv";

    // Not applied by GetColorPlane. Kept here until a full-day c250/plane log says otherwise.
    public const float DirectionalScale = 0.7f;

    static readonly int ActorAmbientHalfId = Shader.PropertyToID("_L2ActorAmbientHalf");
    static readonly int HsvActorLightId = Shader.PropertyToID("_L2HsvActorLight");
    static readonly int HsvActorBaseId = Shader.PropertyToID("_L2HsvActorBase");
    static readonly int ActorDirectionalScaleId = Shader.PropertyToID("_L2ActorDirectionalScale");

    static readonly List<Sample> Samples = new List<Sample>(320);
    static bool _triedLoad;

    public static bool IsReady => Samples.Count >= 2;

    public static void EnsureLoaded()
    {
        if (_triedLoad)
            return;

        _triedLoad = true;
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta", MetaFileName);
        if (!File.Exists(dataPath))
        {
            // Debug.LogWarning("[L2ActorLightLut] File not found: " + dataPath);
            return;
        }

        LoadCsv(File.ReadAllText(dataPath));
        //if (Samples.Count >= 2)
        //    Debug.Log("[L2ActorLightLut] Loaded " + Samples.Count + " keys from " + MetaFileName);
    }

    public static void LoadCsv(string csvText)
    {
        Samples.Clear();
        if (string.IsNullOrEmpty(csvText))
            return;

        var lines = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 14)
                continue;

            float tHours = Parse(c[0]);
            if (tHours >= 23.999f)
                continue;

            Samples.Add(new Sample
            {
                tHours = tHours,
                ambient = new Color(Parse(c[1]), Parse(c[2]), Parse(c[3]), 1f),
                ambientHalf = new Color(Parse(c[4]), Parse(c[5]), Parse(c[6]), 1f),
                sunPlane = new Color(Parse(c[7]), Parse(c[8]), Parse(c[9]), Parse(c[10])),
                sunBase = new Color(Parse(c[11]), Parse(c[12]), Parse(c[13]), 1f)
            });
        }

        Samples.Sort((a, b) => a.tHours.CompareTo(b.tHours));
    }

    public static Sample SampleAt(float worldHours)
    {
        if (!IsReady)
            return default;

        float t = worldHours % 24f;
        if (t < 0f)
            t += 24f;

        int i1 = 0;
        while (i1 < Samples.Count && Samples[i1].tHours < t)
            i1++;

        Sample a;
        Sample b;
        float u;
        if (i1 <= 0 || i1 >= Samples.Count)
        {
            a = Samples[Samples.Count - 1];
            b = Samples[0];
            float span = (24f - a.tHours) + b.tHours;
            float along = t >= a.tHours ? (t - a.tHours) : ((24f - a.tHours) + t);
            u = span > 1e-6f ? along / span : 0f;
        }
        else
        {
            a = Samples[i1 - 1];
            b = Samples[i1];
            float span = b.tHours - a.tHours;
            u = span > 1e-6f ? (t - a.tHours) / span : 0f;
        }

        return new Sample
        {
            tHours = t,
            ambient = Color.Lerp(a.ambient, b.ambient, u),
            ambientHalf = Color.Lerp(a.ambientHalf, b.ambientHalf, u),
            sunPlane = Color.Lerp(a.sunPlane, b.sunPlane, u),
            sunBase = Color.Lerp(a.sunBase, b.sunBase, u)
        };
    }

    public static void PushGlobals(float worldHours)
    {
        EnsureLoaded();
        if (!IsReady)
            return;

        Sample sample = SampleAt(worldHours);
        Shader.SetGlobalColor(ActorAmbientHalfId, sample.ambientHalf);
        Shader.SetGlobalColor(HsvActorLightId, sample.sunPlane);
        Shader.SetGlobalColor(HsvActorBaseId, sample.sunBase);
        Shader.SetGlobalFloat(ActorDirectionalScaleId, DirectionalScale);
    }

    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
