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
                  skillId + " -> " + compositePrefabPath;
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
