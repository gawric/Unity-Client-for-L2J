using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// High Elf UL2NEnvManager celestial LUT: sun/moon dirs, scales, colors, world offsets from sweep camera.
/// Sample by world hours 0..24.
/// UE (X,Y,Z-up) → Unity via VectorUtils: (Y, Z, X).
/// </summary>
public static class L2CelestialLut
{
    public struct Sample
    {
        public float tHours;
        public int phase;
        public bool isDay;
        public Vector3 sunDirUnity;
        public Vector3 moonDirUnity;
        public Vector3 sunOffsetUnity;
        public Vector3 moonOffsetUnity;
        public float sunElevDeg;
        public float moonElevDeg;
        public float sunDrawScale;
        public float moonDrawScale;
        public float sunScale;
        public float moonScale;
        public Color moonColor;
        public int sampleA;
        public int sampleB;
        public float lerpU;
    }

    const string MetaFileName = "L2CelestialLUT.csv";

    // Sweep camera from high_elf_moon/sun InverseView (same session).
    static readonly Vector3 SweepCameraUe = new Vector3(324.684f, 264435f, 24536f);

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
            Debug.LogWarning("[L2CelestialLut] File not found: " + dataPath);
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
            if (c.Length < 39)
            {
                continue;
            }

            Vector3 sunDirUe = new Vector3(Parse(c[4]), Parse(c[5]), Parse(c[6]));
            Vector3 moonDirUe = new Vector3(Parse(c[9]), Parse(c[10]), Parse(c[11]));
            Vector3 sunLocUe = new Vector3(Parse(c[20]), Parse(c[21]), Parse(c[22]));
            Vector3 moonLocUe = new Vector3(Parse(c[23]), Parse(c[24]), Parse(c[25]));

            Samples.Add(new Sample
            {
                tHours = Parse(c[1]),
                phase = int.Parse(c[2], CultureInfo.InvariantCulture),
                isDay = Parse(c[3]) > 0.5f,
                sunDirUnity = DirUeToUnity(sunDirUe),
                moonDirUnity = DirUeToUnity(moonDirUe),
                sunOffsetUnity = VectorUtils.ConvertPosToUnity(sunLocUe - SweepCameraUe),
                moonOffsetUnity = VectorUtils.ConvertPosToUnity(moonLocUe - SweepCameraUe),
                sunElevDeg = Parse(c[7]),
                moonElevDeg = Parse(c[12]),
                sunDrawScale = Parse(c[26]),
                moonDrawScale = Parse(c[27]),
                sunScale = Parse(c[28]),
                moonScale = Parse(c[29]),
                moonColor = new Color(Parse(c[36]) / 255f, Parse(c[37]) / 255f, Parse(c[38]) / 255f, 1f)
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
            Sample first = Samples[0];
            first.sampleA = 0;
            first.sampleB = 0;
            first.lerpU = 0f;
            return first;
        }

        if (i1 >= Samples.Count)
        {
            var last = Samples[Samples.Count - 1];
            var first = Samples[0];
            float span = (24f - last.tHours) + first.tHours;
            float u = span > 1e-6f ? (t - last.tHours) / span : 0f;
            return Lerp(last, first, u, t, Samples.Count - 1, 0);
        }

        var a = Samples[i1 - 1];
        var b = Samples[i1];
        float s = b.tHours - a.tHours;
        float k = s > 1e-6f ? (t - a.tHours) / s : 0f;
        return Lerp(a, b, k, t, i1 - 1, i1);
    }

    static Sample Lerp(Sample a, Sample b, float u, float t, int indexA, int indexB)
    {
        return new Sample
        {
            tHours = t,
            phase = u < 0.5f ? a.phase : b.phase,
            isDay = u < 0.5f ? a.isDay : b.isDay,
            sunDirUnity = Vector3.Slerp(SafeDir(a.sunDirUnity), SafeDir(b.sunDirUnity), u),
            moonDirUnity = Vector3.Slerp(SafeDir(a.moonDirUnity), SafeDir(b.moonDirUnity), u),
            sunOffsetUnity = Vector3.Lerp(a.sunOffsetUnity, b.sunOffsetUnity, u),
            moonOffsetUnity = Vector3.Lerp(a.moonOffsetUnity, b.moonOffsetUnity, u),
            sunElevDeg = Mathf.Lerp(a.sunElevDeg, b.sunElevDeg, u),
            moonElevDeg = Mathf.Lerp(a.moonElevDeg, b.moonElevDeg, u),
            sunDrawScale = Mathf.Lerp(a.sunDrawScale, b.sunDrawScale, u),
            moonDrawScale = Mathf.Lerp(a.moonDrawScale, b.moonDrawScale, u),
            sunScale = Mathf.Lerp(a.sunScale, b.sunScale, u),
            moonScale = Mathf.Lerp(a.moonScale, b.moonScale, u),
            moonColor = Color.Lerp(a.moonColor, b.moonColor, u),
            sampleA = indexA,
            sampleB = indexB,
            lerpU = u
        };
    }

    static Vector3 DirUeToUnity(Vector3 ue)
    {
        Vector3 u = VectorUtils.ConvertToUnityUnscaled(ue);
        return u.sqrMagnitude > 1e-8f ? u.normalized : Vector3.forward;
    }

    static Vector3 SafeDir(Vector3 d)
    {
        return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
    }

    static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
