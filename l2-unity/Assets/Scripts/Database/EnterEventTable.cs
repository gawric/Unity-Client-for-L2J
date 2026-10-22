using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// StreamingAssets/Data/Meta/EnterEventgrp.txt.
/// One row per npc id: rise mode, ground effect class, body animation, sound.
/// </summary>
public sealed class EnterEventTable
{
    public const string EffectCommentPrefix = "EnterEventgrp ";
    public struct Row
    {
        public int IsRise;
        public int SpawnType;
        public string EffectClass;
        public string AnimName;
        public string SoundName;
        public float SoundVolume;
        public float SoundRadius;
    }

    static EnterEventTable _instance;

    public static EnterEventTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new EnterEventTable();
            return _instance;
        }
    }

    const string RelativePath = "Data/Meta/EnterEventgrp.txt";

    readonly Dictionary<int, Row> _rows = new Dictionary<int, Row>();

    public int Count => _rows.Count;

    public void Initialize()
    {
        _rows.Clear();

        string dataPath = Path.Combine(Application.streamingAssetsPath, RelativePath);
        if (!File.Exists(dataPath))
            return;

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("id\t"))
                    continue;

                string[] cols = line.Split('\t');
                if (cols.Length < 9)
                    continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    continue;
                if (id <= 0)
                    continue;
                if (!int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int isRise))
                    continue;
                if (!int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int spawnType))
                    continue;

                float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float volume);
                float.TryParse(cols[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float radius);

                _rows[id] = new Row
                {
                    IsRise = isRise,
                    SpawnType = spawnType,
                    EffectClass = ClassName(cols[7]),
                    AnimName = cols[8].Trim(),
                    SoundName = CleanSound(cols[2]),
                    SoundVolume = volume,
                    SoundRadius = radius
                };
            }
        }
    }

    public bool TryGet(int npcId, out Row row)
    {
        if (_rows.Count == 0)
            Initialize();
        return _rows.TryGetValue(npcId, out row);
    }

    static string ClassName(string effect)
    {
        if (string.IsNullOrEmpty(effect))
            return string.Empty;
        effect = effect.Trim();
        int dot = effect.LastIndexOf('.');
        return dot >= 0 ? effect.Substring(dot + 1) : effect;
    }

    static string CleanSound(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        string sound = raw.Replace("\\0", string.Empty).Trim();
        if (sound.StartsWith("a,"))
            sound = sound.Substring(2);
        if (string.Equals(sound, "none", System.StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return sound;
    }
}
