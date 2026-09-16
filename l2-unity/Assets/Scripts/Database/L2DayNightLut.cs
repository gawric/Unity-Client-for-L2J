using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// High Elf UL2NEnvManager color LUT (envType=0). Sample by world hours 0..24.
/// Byte RGB / 255, no sRGB decode — same as Engine getters.
/// </summary>
public static class L2DayNightLut
{
    public struct Sample
    {
        public float tHours;
        public int phase;
        public Color sky;
        public Color sun;
        public Color actorAmbient;
    }

    const string MetaFileName = "L2DayNightLUT.csv";

    static readonly List<Sample> Samples = new List<Sample>(320);
    // high_elf_soon.rdc EID 3067: midday raw #427CB0 -> final #62ADEA.
    // Stored as an additive display-LUT offset so brighter dawn colors do not
    // clip as aggressively as another RGB multiply.
    static readonly Color PostSkyDayOffset =
        new Color((0x62 - 0x42) / 255f, (0xAD - 0x7C) / 255f, (0xEA - 0xB0) / 255f, 0f);
    static bool _triedLoad;

    public static bool IsReady => Samples.Count >= 2;

    public static string PhaseName(int phase)
    {
        switch (phase)
        {
            case 0: return "morning";
            case 1: return "day";
            case 2: return "evening";
            case 3: return "night";
            default: return "phase" + phase;
        }
    }

    public static void EnsureLoaded()
    {
        if (_triedLoad)
            return;

        _triedLoad = true;
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta", MetaFileName);
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("[L2DayNightLut] File not found: " + dataPath + " — using DayNightCycle fallback colors.");
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
            // step,t_hours,t01,phase,phaseName,envType,skyRGB,sunRGB,actorRGB,sky01,sun01,actor01,hex
            if (c.Length < 24)
            {
                continue;
            }

            Samples.Add(new Sample
            {
                tHours = Parse(c[1]),
                phase = int.Parse(c[3], CultureInfo.InvariantCulture),
                sky = new Color(Parse(c[15]), Parse(c[16]), Parse(c[17]), 1f),
                sun = new Color(Parse(c[18]), Parse(c[19]), Parse(c[20]), 1f),
                actorAmbient = new Color(Parse(c[21]), Parse(c[22]), Parse(c[23]), 1f)
            });
        }

        Samples.Sort((a, b) => a.tHours.CompareTo(b.tHours));
    }

    public static Sample SampleAt(float worldHours)
    {
        if (!IsReady)
        {
            return default;
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

        if (i1 <= 0)
        {
            return Samples[0];
        }

        if (i1 >= Samples.Count)
        {
            return Samples[Samples.Count - 1];
        }

        var a = Samples[i1 - 1];
        var b = Samples[i1];
        float span = b.tHours - a.tHours;
        float u = span > 1e-6f ? (t - a.tHours) / span : 0f;

        return new Sample
        {
            tHours = t,
            phase = u < 0.5f ? a.phase : b.phase,
            sky = Color.Lerp(a.sky, b.sky, u),
            sun = Color.Lerp(a.sun, b.sun, u),
            actorAmbient = Color.Lerp(a.actorAmbient, b.actorAmbient, u)
        };
    }

    /// <summary>
    /// Applies the display-referred L2 post color to the sky material only.
    /// Night is unchanged; correction fades in 06–08 and out 22–24.
    /// No pow/gamma operation is used.
    /// </summary>
    public static Color ApplyPostSky(Color rawSky, float worldHours)
    {
        float dayWeight = PostSkyDayWeight(worldHours);
        Color result = rawSky + PostSkyDayOffset * dayWeight;
        result.r = Mathf.Clamp01(result.r);
        result.g = Mathf.Clamp01(result.g);
        result.b = Mathf.Clamp01(result.b);
        result.a = rawSky.a;
        return result;
    }

    static float PostSkyDayWeight(float worldHours)
    {
        float h = Mathf.Repeat(worldHours, 24f);
        if (h < 6f)
            return 0f;
        if (h < 8f)
            return Smooth01((h - 6f) * 0.5f);
        if (h < 22f)
            return 1f;
        return 1f - Smooth01((h - 22f) * 0.5f);
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
