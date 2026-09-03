#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// skillgrp.dat: skill_id (server / GlobalEffect) → skill_visual_effect (skill-effects.tsv).
/// Vampiric Touch is skill 1147 with visual 1090.
/// </summary>
public static class L2EffectSkillgrpVisualMap
{
    public const string DefaultAssetPath = "Assets/StreamingAssets/Data/Meta/Skillgrp_Classic.txt";

    static readonly Regex SkillIdRegex = new Regex(@"skill_id=(\d+)", RegexOptions.Compiled);
    static readonly Regex VisualRegex = new Regex(@"skill_visual_effect=\[(\d+)\]", RegexOptions.Compiled);

    static Dictionary<int, int> _skillIdToVisual;
    static string _loadedPath;
    static DateTime _loadedWriteTimeUtc;

    public static bool TryGetVisualEffect(int skillId, out int visualEffectId)
    {
        visualEffectId = 0;
        if (skillId <= 0 || !TryLoad())
        {
            return false;
        }

        return _skillIdToVisual.TryGetValue(skillId, out visualEffectId) && visualEffectId > 0;
    }

    static bool TryLoad()
    {
        string assetPath = ResolveAssetPath();
        string fullPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath));
        if (!File.Exists(fullPath))
        {
            return _skillIdToVisual != null && _skillIdToVisual.Count > 0;
        }

        DateTime writeTime = File.GetLastWriteTimeUtc(fullPath);
        if (_skillIdToVisual != null &&
            string.Equals(_loadedPath, fullPath, StringComparison.OrdinalIgnoreCase) &&
            writeTime == _loadedWriteTimeUtc)
        {
            return true;
        }

        var map = new Dictionary<int, int>();
        string[] lines = File.ReadAllLines(fullPath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.IndexOf("skill_begin", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            Match skillMatch = SkillIdRegex.Match(line);
            Match visualMatch = VisualRegex.Match(line);
            if (!skillMatch.Success || !visualMatch.Success)
            {
                continue;
            }

            if (!int.TryParse(skillMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int skillId) ||
                !int.TryParse(visualMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int visualId))
            {
                continue;
            }

            if (skillId > 0 && visualId > 0)
            {
                map[skillId] = visualId;
            }
        }

        _skillIdToVisual = map;
        _loadedPath = fullPath;
        _loadedWriteTimeUtc = writeTime;
        return map.Count > 0;
    }

    static string ResolveAssetPath()
    {
        if (File.Exists(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", DefaultAssetPath))))
        {
            return DefaultAssetPath;
        }

        string[] guids = AssetDatabase.FindAssets("Skillgrp_Classic");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith("Skillgrp_Classic.txt", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return DefaultAssetPath;
    }
}
#endif
