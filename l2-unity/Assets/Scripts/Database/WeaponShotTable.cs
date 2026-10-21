using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// StreamingAssets/Data/Meta/weapon_shot.int. Weapon sections name the prefab
/// played when EffectData.useWeaponShot is enabled. SpiritPrefab is used when
/// EffectData.spiritShot is also enabled.
/// </summary>
public sealed class WeaponShotTable
{
    const string RelativePath = "Data/Meta/weapon_shot.int";

    static WeaponShotTable _instance;

    readonly Dictionary<string, string> _prefabPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> _spiritPrefabPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, BaseEffect> _loaded =
        new Dictionary<string, BaseEffect>(StringComparer.OrdinalIgnoreCase);

    public static WeaponShotTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new WeaponShotTable();
            return _instance;
        }
    }

    WeaponShotTable()
    {
        Load();
    }

    public BaseEffect ResolvePrefab(Gear gear)
    {
        return ResolvePrefab(gear, false);
    }

    public BaseEffect ResolvePrefab(Gear gear, bool spirit)
    {
        string section = ResolveSection(gear);
        Dictionary<string, string> paths = spirit ? _spiritPrefabPaths : _prefabPaths;
        if (string.IsNullOrEmpty(section) || !paths.TryGetValue(section, out string resourcePath))
            return null;

        string cacheKey = (spirit ? "S:" : "P:") + section;
        if (_loaded.TryGetValue(cacheKey, out BaseEffect cached))
            return cached;

        BaseEffect prefab = Resources.Load<BaseEffect>(resourcePath);
        if (prefab == null)
        {
            // Debug.LogWarning("WeaponShotTable: prefab not found for [" + section + "] at " + resourcePath);
            return null;
        }

        _loaded[cacheKey] = prefab;
        return prefab;
    }

    void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, RelativePath);
        if (!File.Exists(path))
        {
            // Debug.LogWarning("WeaponShotTable: file not found: " + path);
            return;
        }

        string section = null;
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            int split = line.IndexOf('=');
            if (split <= 0 || string.IsNullOrEmpty(section))
                continue;

            string key = line.Substring(0, split).Trim();
            string value = line.Substring(split + 1).Trim();
            if (value.Length == 0)
                continue;
            if (key.Equals("Prefab", StringComparison.OrdinalIgnoreCase))
                _prefabPaths[section] = value;
            else if (key.Equals("SpiritPrefab", StringComparison.OrdinalIgnoreCase))
                _spiritPrefabPaths[section] = value;
        }
    }

    static string ResolveSection(Gear gear)
    {
        if (gear == null)
            return "HAND";

        WeaponType right = gear.RightHandWeaponType;
        WeaponType left = gear.LeftHandWeaponType;
        if (left == WeaponType.shield || left == WeaponType.arrow)
            left = WeaponType.none;

        if (right == WeaponType.bow || left == WeaponType.bow)
            return "BOW";
        if (right == WeaponType.dual || left == WeaponType.dual)
            return "DUAL";
        if (right == WeaponType.dagger || left == WeaponType.dagger)
            return "THROW";
        if (right == WeaponType.mbook || left == WeaponType.mbook)
            return "BOOK";
        if (right == WeaponType.fist || left == WeaponType.fist ||
            right == WeaponType.dualfist || left == WeaponType.dualfist)
            return "DUALFIST";
        if (IsBare(right) && IsBare(left))
            return "HAND";
        if (right == WeaponType.bigword || right == WeaponType.bigblunt ||
            left == WeaponType.bigword || left == WeaponType.bigblunt)
            return "2HS";
        if (right == WeaponType.pole || right == WeaponType.staff ||
            left == WeaponType.pole || left == WeaponType.staff)
            return "POLE";

        return "1HS";
    }

    static bool IsBare(WeaponType type)
    {
        return type == WeaponType.none || type == WeaponType.hand;
    }
}