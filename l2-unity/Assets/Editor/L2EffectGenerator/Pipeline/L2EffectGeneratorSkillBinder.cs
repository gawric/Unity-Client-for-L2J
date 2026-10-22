#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorSkillBinder
{
    public const string EffectDatabaseAssetPath = "Assets/Scripts/Database/Effects/GlobalEffect.asset";

    public static bool TryBindComposite(
        int skillId,
        string compositePrefabPath,
        out string logLine,
        out string errorMessage)
    {
        logLine = null;
        errorMessage = null;

        if (skillId <= 0)
        {
            logLine = "skill bind skipped (no skill visual id)";
            return true;
        }

        if (string.IsNullOrWhiteSpace(compositePrefabPath))
        {
            errorMessage = "Composite prefab path is empty.";
            return false;
        }

        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(compositePrefabPath);
        if (prefabRoot == null)
        {
            errorMessage = "Composite prefab not found: " + compositePrefabPath;
            return false;
        }

        BaseEffect effect = prefabRoot.GetComponent<CompositeEffectV2>();
        if (effect == null)
        {
            effect = prefabRoot.GetComponent<BaseEffect>();
        }

        if (effect == null)
        {
            errorMessage = "Composite prefab has no BaseEffect: " + compositePrefabPath;
            return false;
        }

        EffectDatabase database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(EffectDatabaseAssetPath);
        if (database == null || database.effects == null)
        {
            errorMessage = "Effect database not found: " + EffectDatabaseAssetPath;
            return false;
        }

        int boundCount = 0;
        string compositeName = prefabRoot.name;
        bool isBowVariant =
            compositeName.EndsWith("_bow_composite", StringComparison.OrdinalIgnoreCase) ||
            compositePrefabPath.IndexOf("/soul_N_bow/", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isBowVariant)
        {
            logLine = "bow composite stays in weapon_shot.int, GlobalEffect.prefab unchanged: " +
                      compositePrefabPath;
            return true;
        }

        for (int i = 0; i < database.effects.Count; i++)
        {
            EffectDatabase.EffectData data = database.effects[i];
            if (data == null)
            {
                continue;
            }

            bool sameId = data.id == skillId;
            bool sameCompositeName = data.prefab != null &&
                string.Equals(data.prefab.gameObject.name, compositeName, StringComparison.OrdinalIgnoreCase);
            if (!sameId && !sameCompositeName)
            {
                continue;
            }

            data.prefab = effect;
            if (string.IsNullOrWhiteSpace(data.comment))
            {
                data.comment = PathFileName(compositePrefabPath);
            }

            boundCount++;
        }

        if (boundCount == 0)
        {
            logLine = "WARNING skill visual id " + skillId + " is missing from GlobalEffect.asset.";
            return true;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        logLine = "bound " + boundCount + " GlobalEffect row(s) including skill " +
                  skillId + " prefab -> " + compositePrefabPath;
        return true;
    }

    public const int EnterEventIdBase = 991000;
    public const string EnterEventSettingsPath =
        "Assets/Scripts/Database/Effects/Data/Settings/EnterEvent.asset";

    public static bool TryEnsureEnterEvent(
        string effectClass,
        string compositePrefabPath,
        out string logLine,
        out string errorMessage)
    {
        logLine = null;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(effectClass))
        {
            errorMessage = "Enter event effect class is empty.";
            return false;
        }

        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(compositePrefabPath);
        BaseEffect effect = prefabRoot != null ? prefabRoot.GetComponent<BaseEffect>() : null;
        if (effect == null)
        {
            errorMessage = "Enter event composite missing: " + compositePrefabPath;
            return false;
        }

        EffectDatabase database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(EffectDatabaseAssetPath);
        if (database == null || database.effects == null)
        {
            errorMessage = "Effect database not found: " + EffectDatabaseAssetPath;
            return false;
        }

        EffectSettings settings = AssetDatabase.LoadAssetAtPath<EffectSettings>(EnterEventSettingsPath);
        string comment = EnterEventTable.EffectCommentPrefix + effectClass;
        EffectDatabase.EffectData row = null;
        int nextId = EnterEventIdBase + 1;
        for (int i = 0; i < database.effects.Count; i++)
        {
            EffectDatabase.EffectData data = database.effects[i];
            if (data == null)
                continue;
            if (data.id >= EnterEventIdBase && data.id < EnterEventIdBase + 1000 && data.id >= nextId)
                nextId = data.id + 1;
            if (string.Equals(data.comment, comment, StringComparison.Ordinal))
                row = data;
        }

        if (row == null)
        {
            row = new EffectDatabase.EffectData();
            row.id = nextId;
            row.comment = comment;
            database.effects.Add(row);
        }

        row.prefab = effect;
        row.useWeaponShot = false;
        row.spiritShot = false;
        row.ignoreFlightTimeForCast = true;
        if (settings != null)
            row.settings = settings;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        logLine = "EnterEventgrp row id=" + row.id + " " + comment + " -> " + compositePrefabPath;
        return true;
    }

    private static string PathFileName(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return string.Empty;
        }

        int slash = Math.Max(assetPath.LastIndexOf('/'), assetPath.LastIndexOf('\\'));
        return slash >= 0 ? assetPath.Substring(slash + 1) : assetPath;
    }
}
#endif
