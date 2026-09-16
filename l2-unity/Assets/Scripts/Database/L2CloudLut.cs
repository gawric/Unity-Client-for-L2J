using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Exact High Elf cloud ColorModifier values captured from
/// UL2NEnvManager::GetCloudColor. Channels 3..10 drive Cloud_Final sheets;
/// channel 11 drives the dark silhouette layer.
/// </summary>
public static class L2CloudLut
{
    public struct Sample
    {
        public float tHours;
        public Color sheet;
        public Color silhouette;
    }

    const string MetaFileName = "L2CloudLUT.csv";

    public static readonly Color SheetFallback =
        new Color(130f / 255f, 130f / 255f, 139f / 255f, 1f);
    public static readonly Color SilhouetteFallback =
        new Color(37f / 255f, 14f / 255f, 5f / 255f, 1f);

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
            Debug.LogWarning("[L2CloudLut] File not found: " + dataPath);
            return;
        }

        LoadCsv(File.ReadAllText(dataPath));
    }

    public static void LoadCsv(string csvText)
    {
        Samples.Clear();
        if (string.IsNullOrEmpty(csvText))
            return;

        var lines = csvText.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = lines[i].Split(',');
            if (c.Length < 7)
                continue;

            float t = Parse(c[0]);
            if (t >= 23.999f)
                continue;

            Samples.Add(new Sample
            {
                tHours = t,
                sheet = new Color(Parse(c[1]), Parse(c[2]), Parse(c[3]), 1f),
                silhouette = new Color(Parse(c[4]), Parse(c[5]), Parse(c[6]), 1f)
            });
        }

        Samples.Sort((a, b) => a.tHours.CompareTo(b.tHours));
        Debug.Log("[L2CloudLut] Loaded " + Samples.Count + " GetCloudColor keys from " + MetaFileName);
    }

    public static Sample SampleAt(float worldHours)
    {
        EnsureLoaded();
        if (Samples.Count == 0)
            return new Sample { sheet = SheetFallback, silhouette = SilhouetteFallback };
        if (Samples.Count == 1)
            return Samples[0];

        float t = Mathf.Repeat(worldHours, 24f);
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
            float along = t >= a.tHours
                ? t - a.tHours
                : (24f - a.tHours) + t;
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
            sheet = Color.Lerp(a.sheet, b.sheet, u),
            silhouette = Color.Lerp(a.silhouette, b.silhouette, u)
        };
    }

    static float Parse(string value) =>
        float.Parse(value, CultureInfo.InvariantCulture);
}
