using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// StreamingAssets/Data/Meta/AnimTween.txt.
/// Filled tween (wait/atkwait/walk/run/spwait) is used for CrossFade.
/// Empty rows keep Unity <see cref="LocomotionCrossFadeSettings.FixedDuration"/>.
/// </summary>
public sealed class AnimTweenTable
{
    private static AnimTweenTable _instance;

    public static AnimTweenTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AnimTweenTable();
            return _instance;
        }
    }

    const string RelativePath = "Data/Meta/AnimTween.txt";

    readonly Dictionary<string, float> _byMeshSequence =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, float> _bySequence =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, float> _byFamily =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    // Sequence source rate (AnimTween col "rate", e.g. 30/24/15 fps). Imported
    // Unity clips are keyed on a 24 fps timeline, so playback speed must be
    // rate/24 to reproduce the L2 wall-clock cycle length.
    readonly Dictionary<string, float> _rateByMeshSequence =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, float> _rateBySequence =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public int Count => _byMeshSequence.Count;

    public void Initialize()
    {
        _byMeshSequence.Clear();
        _bySequence.Clear();
        _byFamily.Clear();
        _rateByMeshSequence.Clear();
        _rateBySequence.Clear();

        string dataPath = Path.Combine(Application.streamingAssetsPath, RelativePath);
        if (!File.Exists(dataPath))
        {
            // Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("package\t"))
                    continue;

                string[] cols = line.Split('\t');
                if (cols.Length < 7)
                    continue;

                string mesh = cols[1];
                string sequence = cols[2];
                string tweenText = cols[6];
                if (string.IsNullOrEmpty(sequence) || string.IsNullOrEmpty(tweenText))
                    continue;
                if (!float.TryParse(tweenText, NumberStyles.Float, CultureInfo.InvariantCulture, out float tween))
                    continue;

                _byMeshSequence[mesh + "\0" + sequence] = tween;
                if (!_bySequence.ContainsKey(sequence))
                    _bySequence.Add(sequence, tween);

                // Store the sequence source rate (30/24/15…) so locomotion can scale
                // the 24 fps Unity clip by rate/24.
                if (float.TryParse(cols[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float rate) && rate > 0f)
                {
                    string key = mesh + "\0" + sequence;
                    if (!_rateByMeshSequence.ContainsKey(key))
                        _rateByMeshSequence.Add(key, rate);
                    if (!_rateBySequence.ContainsKey(sequence))
                        _rateBySequence.Add(sequence, rate);
                }

                string family = FamilyOf(sequence);
                if (family != null && !_byFamily.ContainsKey(family))
                    _byFamily.Add(family, tween);
            }
        }

        // Debug.Log("Successfully imported " + _byMeshSequence.Count + " animation tween(s)");
    }

    public bool TryGetTween(string sequence, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrEmpty(sequence))
            return false;
        if (_bySequence.TryGetValue(sequence, out seconds))
            return true;

        string family = FamilyOf(sequence);
        return family != null && _byFamily.TryGetValue(family, out seconds);
    }

    public bool TryGetTween(string mesh, string sequence, out float seconds)
    {
        if (!string.IsNullOrEmpty(mesh) && !string.IsNullOrEmpty(sequence)
            && _byMeshSequence.TryGetValue(mesh + "\0" + sequence, out seconds))
        {
            return true;
        }

        return TryGetTween(sequence, out seconds);
    }

    /// <summary>
    /// Source sequence rate (fps) from AnimTween col "rate". Falls back to the
    /// sequence family when the exact mesh+sequence is not listed.
    /// </summary>
    public float TryGetRate(string sequence, float fallback = 30f)
    {
        if (!string.IsNullOrEmpty(sequence) && _rateBySequence.TryGetValue(sequence, out float rate))
            return rate;
        return fallback;
    }

    public float TryGetRate(string mesh, string sequence, float fallback = 30f)
    {
        if (!string.IsNullOrEmpty(mesh) && !string.IsNullOrEmpty(sequence)
            && _rateByMeshSequence.TryGetValue(mesh + "\0" + sequence, out float rate))
            return rate;
        return TryGetRate(sequence, fallback);
    }

    /// <summary>
    /// wait / atkwait / walk / run / spwait. atkwait before wait.
    /// </summary>
    public static string FamilyOf(string sequence)
    {
        if (string.IsNullOrEmpty(sequence))
            return null;

        string n = sequence.ToLowerInvariant();
        if (n.StartsWith("atkwait"))
            return "atkwait";
        if (n.StartsWith("spwait"))
            return "spwait";
        if (n.StartsWith("wait"))
            return "wait";
        if (n.StartsWith("walk"))
            return "walk";
        if (n.StartsWith("run"))
            return "run";
        return null;
    }
}
