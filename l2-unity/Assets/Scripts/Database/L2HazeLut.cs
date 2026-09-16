using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Haze Ring tint. Default is ColorModifier <c>HazeRing_Final</c> in
/// <c>L2_Skies.utx</c> (R=188 G=203 B=224, #BCCBE0). Optional CSV
/// <c>StreamingAssets/Data/Meta/L2HazeLUT.csv</c> from GetHazeColor sweep
/// overrides that constant when present.
/// </summary>
public static class L2HazeLut
{
    public struct Sample
    {
        public float tHours;
        public Color haze;
    }

    const string MetaFileName = "L2HazeLUT.csv";

    // HazeRing_Final Color=(B=224,G=203,R=188,A=255)
    public static readonly Color ColorModifierDefault =
        new Color(188f / 255f, 203f / 255f, 224f / 255f, 1f);

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
            Debug.LogWarning("[L2HazeLut] File not found: " + dataPath);
            return;
        }

        LoadCsv(File.ReadAllText(dataPath));
    }

    public static void LoadCsv(string csvText)
    {
        Samples.Clear();
        if (string.IsNullOrEmpty(csvText))
        {
            return;
        }

        var lines = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 4)
            {
                continue;
            }

            float tHours = Parse(c[0]);
            // TimeSweep last key t=24 is a wrap spike (#B349C0); skip it.
            if (tHours >= 23.999f)
            {
                continue;
            }

            Samples.Add(new Sample
            {
                tHours = tHours,
                haze = new Color(Parse(c[1]), Parse(c[2]), Parse(c[3]), 1f)
            });
        }

        Samples.Sort((a, b) => a.tHours.CompareTo(b.tHours));
        Debug.Log("[L2HazeLut] Loaded " + Samples.Count + " GetHazeColor keys from " + MetaFileName);
    }

    public static Color SampleAt(float worldHours)
    {
        EnsureLoaded();
        if (Samples.Count == 0)
        {
            return ColorModifierDefault;
        }

        if (Samples.Count == 1)
        {
            return Samples[0].haze;
        }

        float t = worldHours % 24f;
        if (t < 0f)
        {
            t += 24f;
        }

        int i1 = 0;
        while (i1 < Samples.Count && Samples[i1].tHours < t)
        {
            i1++;
        }

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

        return Color.Lerp(a.haze, b.haze, u);
    }

    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
