#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class L2EffectGeneratorCompositeBuilder
{
    private static readonly FieldInfo ServerHitLifetimeTailField = typeof(CompositePrefabEffect).GetField(
        "_serverHitLifetimeTailSeconds",
        BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo SkipDestroyCompositeField = typeof(CompositePrefabEffect).GetField(
        "_skipDestroyCompositeByLifetime",
        BindingFlags.NonPublic | BindingFlags.Instance);

    public static string GetCompositeAssetPath(string effectRootPath, string effectName)
    {
        return effectRootPath.Replace('\\', '/').TrimEnd('/') + "/" + effectName.Trim() + "_composite.prefab";
    }

    public static bool TryCreateOrUpdateComposite(
        string effectName,
        string effectRootPath,
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders,
        int skillVisualId,
        out string logLine,
        out string errorMessage)
    {
        logLine = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(effectName))
        {
            errorMessage = "Effect name is required for composite prefab.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(effectRootPath) || !AssetDatabase.IsValidFolder(effectRootPath))
        {
            errorMessage = "A valid effect root folder is required for composite prefab.";
            return false;
        }

        if (plannedFolders == null || plannedFolders.Count == 0)
        {
            errorMessage = "No planned effect parts for composite prefab.";
            return false;
        }

        string compositeName = effectName.Trim() + "_composite";
        string compositePath = GetCompositeAssetPath(effectRootPath, effectName);
        bool compositeExists = AssetDatabase.LoadMainAssetAtPath(compositePath) != null;

        GameObject compositeRoot = compositeExists
            ? PrefabUtility.LoadPrefabContents(compositePath)
            : new GameObject(compositeName);

        try
        {
            if (compositeRoot == null)
            {
                errorMessage = "Failed to load composite prefab contents: " + compositePath;
                return false;
            }

            compositeRoot.name = compositeName;

            CompositeEffectV2 compositeEffect = compositeRoot.GetComponent<CompositeEffectV2>();
            if (compositeEffect == null)
            {
                CompositePrefabEffect legacy = compositeRoot.GetComponent<CompositePrefabEffect>();
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy);
                }

                compositeEffect = compositeRoot.AddComponent<CompositeEffectV2>();
            }

            if (!TryApplyCompositeSettings(
                    compositeEffect,
                    plannedFolders,
                    skillVisualId,
                    out int partCount,
                    out errorMessage))
            {
                return false;
            }

            bool saveSucceeded;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(compositeRoot, compositePath, out saveSucceeded);
            if (!saveSucceeded || savedPrefab == null)
            {
                errorMessage = "Failed to save composite prefab: " + compositePath;
                return false;
            }

            logLine = (compositeExists ? "composite updated" : "composite created") +
                      " -> " + compositePath +
                      " (V2, parts=" + partCount +
                      ", skill=" + skillVisualId + ")";
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = "Failed to build composite prefab " + compositePath + ": " + exception.Message;
            return false;
        }
        finally
        {
            if (compositeExists)
            {
                PrefabUtility.UnloadPrefabContents(compositeRoot);
            }
            else if (compositeRoot != null)
            {
                Object.DestroyImmediate(compositeRoot);
            }
        }
    }

    private static bool TryApplyCompositeSettings(
        CompositeEffectV2 compositeEffect,
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders,
        int skillVisualId,
        out int partCount,
        out string errorMessage)
    {
        errorMessage = null;
        partCount = 0;

        if (ServerHitLifetimeTailField != null)
        {
            ServerHitLifetimeTailField.SetValue(compositeEffect, 0f);
        }

        if (SkipDestroyCompositeField != null)
        {
            SkipDestroyCompositeField.SetValue(compositeEffect, false);
        }

        bool hasProjectileCompanion = false;
        for (int i = 0; i < plannedFolders.Count; i++)
        {
            if (plannedFolders[i] != null && plannedFolders[i].IsProjectile)
            {
                hasProjectileCompanion = true;
                break;
            }
        }

        var builtParts = new List<CompositePart>();
        for (int i = 0; i < plannedFolders.Count; i++)
        {
            L2EffectGeneratorFolderBuilder.PlannedFolder planned = plannedFolders[i];
            string childPrefabPath = L2EffectGeneratorPrefabBuilder.GetPrefabAssetPath(planned);
            BaseEffect partPrefab = LoadPartPrefab(childPrefabPath);
            if (partPrefab == null)
            {
                errorMessage = "Child prefab is missing or has no BaseEffect: " + childPrefabPath;
                return false;
            }

            List<L2EffectSkillLaunchTable.LaunchRow> rows =
                L2EffectSkillLaunchTable.RowsForComposite(skillVisualId, planned.ClassName, planned.Suffix);
            if (rows.Count == 0)
            {
                CompositePart part = L2EffectSkillLaunchTable.CreateV2Part(
                    planned, null, hasProjectileCompanion);
                part.prefab = partPrefab;
                builtParts.Add(part);
                continue;
            }

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                L2EffectSkillLaunchTable.LaunchRow row = rows[rowIndex];
                CompositePart part = L2EffectSkillLaunchTable.CreateV2Part(
                    planned, row, hasProjectileCompanion);
                part.prefab = partPrefab;
                if (rows.Count > 1)
                {
                    part.name = planned.FolderName + "_" + (rowIndex + 1);
                }

                string warning = L2EffectSkillLaunchTable.DescribeLaunchWarning(row);
                if (!string.IsNullOrEmpty(warning))
                {
                    Debug.LogWarning("[L2EffectGenerator] " + part.name + ": " + warning);
                }

                builtParts.Add(part);
            }
        }

        if (!TryWriteV2Parts(compositeEffect, builtParts.ToArray(), out errorMessage))
        {
            return false;
        }

        partCount = builtParts.Count;
        EditorUtility.SetDirty(compositeEffect);
        return true;
    }

    private static bool TryWriteV2Parts(
        CompositeEffectV2 compositeEffect,
        CompositePart[] parts,
        out string errorMessage)
    {
        errorMessage = null;
        SerializedObject serialized = new SerializedObject(compositeEffect);
        SerializedProperty v2Parts = serialized.FindProperty("_v2Parts");
        if (v2Parts == null)
        {
            errorMessage = "CompositeEffectV2._v2Parts field is missing; cannot write composite parts.";
            return false;
        }

        v2Parts.arraySize = parts.Length;
        for (int i = 0; i < parts.Length; i++)
        {
            v2Parts.GetArrayElementAtIndex(i).managedReferenceValue = parts[i];
        }

        SerializedProperty legacyParts = serialized.FindProperty("_parts");
        if (legacyParts != null)
        {
            legacyParts.arraySize = 0;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static BaseEffect LoadPartPrefab(string prefabAssetPath)
    {
        if (string.IsNullOrWhiteSpace(prefabAssetPath))
        {
            return null;
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", prefabAssetPath));
        if (!File.Exists(fullPath))
        {
            return null;
        }

        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
        if (prefabRoot == null)
        {
            return null;
        }

        BaseEffect baseEffect = prefabRoot.GetComponent<BaseEffect>();
        if (baseEffect != null)
        {
            return baseEffect;
        }

        return prefabRoot.GetComponent<L2Particle>();
    }
}
#endif
