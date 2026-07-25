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
    private const string TemplateCompositeAssetPath =
        "Assets/Resources/Data/Effects/L2_VFX_Pipeline_Sample/it_healing_potion_composite.prefab";

    private const string TemplateCompositeAssetName = "it_healing_potion_composite";

    private const string CaTemplateCompositeAssetPath =
        "Assets/Resources/Data/Effects/wh_heal/wh_heal_composite.prefab";

    private static readonly FieldInfo PartsField = typeof(CompositePrefabEffect).GetField(
        "_parts",
        BindingFlags.NonPublic | BindingFlags.Instance);

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

        if (!TryLoadTemplateComposite(
                out CompositePrefabEffect templateEffect,
                out string templatePathUsed,
                out string templateWarning))
        {
            errorMessage = templateWarning;
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateWarning))
        {
            templateWarning = null;
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

            CompositePrefabEffect compositeEffect = compositeRoot.GetComponent<CompositePrefabEffect>();
            if (compositeEffect == null)
            {
                compositeEffect = compositeRoot.AddComponent<CompositePrefabEffect>();
            }

            if (!TryApplyCompositeSettings(
                    compositeEffect,
                    templateEffect,
                    plannedFolders,
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
                      " -> " + compositePath + " (parts=" + plannedFolders.Count + ")";
            if (!string.IsNullOrEmpty(templatePathUsed))
            {
                logLine += ", template=" + templatePathUsed;
            }
            else if (!string.IsNullOrEmpty(templateWarning))
            {
                logLine += ", template=defaults (" + templateWarning + ")";
            }

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

    private static bool TryLoadTemplateComposite(
        out CompositePrefabEffect templateEffect,
        out string templatePathUsed,
        out string templateWarning)
    {
        templateEffect = null;
        templatePathUsed = null;
        templateWarning = null;

        string resolvedPath = ResolveTemplateCompositePath();
        if (string.IsNullOrEmpty(resolvedPath))
        {
            templateWarning = "sample composite not found, using default part settings";
            return true;
        }

        GameObject templateRoot = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
        if (templateRoot == null)
        {
            templateWarning = "sample composite not found, using default part settings";
            return true;
        }

        templateEffect = templateRoot.GetComponent<CompositePrefabEffect>();
        if (templateEffect == null)
        {
            templateWarning = "CompositePrefabEffect missing on " + resolvedPath + ", using default part settings";
            return true;
        }

        CompositePrefabPart[] templateParts = GetParts(templateEffect);
        if (templateParts == null || templateParts.Length == 0)
        {
            templateWarning = "sample composite has empty _parts, using default part settings";
            templateEffect = null;
            return true;
        }

        templatePathUsed = resolvedPath;
        return true;
    }

    private static string ResolveTemplateCompositePath()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TemplateCompositeAssetPath) != null)
        {
            return TemplateCompositeAssetPath;
        }

        string[] guids = AssetDatabase.FindAssets(TemplateCompositeAssetName + " t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            if (assetPath.EndsWith("/" + TemplateCompositeAssetName + ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }
        }

        return null;
    }

    private static bool TryApplyCompositeSettings(
        CompositePrefabEffect compositeEffect,
        CompositePrefabEffect templateEffect,
        IReadOnlyList<L2EffectGeneratorFolderBuilder.PlannedFolder> plannedFolders,
        out string errorMessage)
    {
        errorMessage = null;

        var builtParts = new CompositePrefabPart[plannedFolders.Count];
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

            CompositePrefabPart templatePart = ResolveTemplatePartForPlanned(templateEffect, planned);
            builtParts[i] = ClonePart(templatePart);
            builtParts[i].name = planned.FolderName;
            builtParts[i].prefab = partPrefab;
        }

        if (templateEffect != null)
        {
            CopyRootSettings(templateEffect, compositeEffect);
        }

        SetParts(compositeEffect, builtParts);
        EditorUtility.SetDirty(compositeEffect);
        return true;
    }

    private static CompositePrefabPart ResolveTemplatePartForPlanned(
        CompositePrefabEffect templateEffect,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        string partSuffix = GetPartSuffix(planned.FolderName);

        CompositePrefabPart matchedPart = TryFindMatchingTemplatePart(templateEffect, planned.FolderName, partSuffix);
        if (matchedPart != null)
        {
            return matchedPart;
        }

        if (partSuffix == "_ca")
        {
            CompositePrefabEffect caTemplateEffect = LoadCaTemplateComposite();
            matchedPart = TryFindMatchingTemplatePart(caTemplateEffect, null, "_ca");
            if (matchedPart != null)
            {
                return matchedPart;
            }

            return CreateDefaultCaTemplatePart();
        }

        return CreateDefaultTaTemplatePart();
    }

    private static string GetPartSuffix(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return null;
        }

        if (folderName.EndsWith("_ca", StringComparison.Ordinal))
        {
            return "_ca";
        }

        if (folderName.EndsWith("_ta", StringComparison.Ordinal))
        {
            return "_ta";
        }

        return null;
    }

    private static CompositePrefabPart TryFindMatchingTemplatePart(
        CompositePrefabEffect templateEffect,
        string exactName,
        string partSuffix)
    {
        CompositePrefabPart[] templateParts = GetParts(templateEffect);
        if (templateParts == null || templateParts.Length == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(exactName))
        {
            for (int i = 0; i < templateParts.Length; i++)
            {
                CompositePrefabPart part = templateParts[i];
                if (part != null && string.Equals(part.name, exactName, StringComparison.Ordinal))
                {
                    return part;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(partSuffix))
        {
            return templateParts[0];
        }

        for (int i = 0; i < templateParts.Length; i++)
        {
            CompositePrefabPart part = templateParts[i];
            if (part != null &&
                !string.IsNullOrWhiteSpace(part.name) &&
                part.name.EndsWith(partSuffix, StringComparison.Ordinal))
            {
                return part;
            }
        }

        return null;
    }

    private static CompositePrefabEffect LoadCaTemplateComposite()
    {
        GameObject templateRoot = AssetDatabase.LoadAssetAtPath<GameObject>(CaTemplateCompositeAssetPath);
        if (templateRoot == null)
        {
            return null;
        }

        return templateRoot.GetComponent<CompositePrefabEffect>();
    }

    private static CompositePrefabPart CreateDefaultTaTemplatePart()
    {
        return new CompositePrefabPart
        {
            attachmentPoint = EffectAttachmentPoint.CasterCenter,
            spawnTiming = CompositePartSpawnTiming.Immediate,
            passCastDataToPart = false,
            passShaderTargetPosition = true,
            shaderTargetAttachmentPoint = EffectAttachmentPoint.TargetPosition,
            useCastTimedLifetime = false,
            followResolvedTransform = false,
            inheritRotation = false,
            customHideTime = 0.5f,
            finalShaderLifetimeMin = 2f,
            finalShaderLifetimeMax = 2f
        };
    }

    private static CompositePrefabPart CreateDefaultCaTemplatePart()
    {
        return new CompositePrefabPart
        {
            attachmentPoint = EffectAttachmentPoint.CasterPosition,
            spawnTiming = CompositePartSpawnTiming.Immediate,
            passCastDataToPart = true,
            passShaderTargetPosition = false,
            useCastTimedLifetime = true,
            overrideContinuousLoop = true,
            continuousLoop = true,
            followResolvedTransform = false,
            inheritRotation = false,
            scale = 1.3f,
            customHideTime = 0.5f,
            finalShaderLifetimeMin = 2f,
            finalShaderLifetimeMax = 2f
        };
    }

    private static CompositePrefabPart[] GetParts(CompositePrefabEffect compositeEffect)
    {
        if (PartsField == null || compositeEffect == null)
        {
            return null;
        }

        return PartsField.GetValue(compositeEffect) as CompositePrefabPart[];
    }

    private static void SetParts(CompositePrefabEffect compositeEffect, CompositePrefabPart[] parts)
    {
        if (PartsField == null || compositeEffect == null)
        {
            return;
        }

        PartsField.SetValue(compositeEffect, parts);
    }

    private static void CopyRootSettings(CompositePrefabEffect templateEffect, CompositePrefabEffect compositeEffect)
    {
        if (ServerHitLifetimeTailField != null)
        {
            ServerHitLifetimeTailField.SetValue(compositeEffect, ServerHitLifetimeTailField.GetValue(templateEffect));
        }

        if (SkipDestroyCompositeField != null)
        {
            SkipDestroyCompositeField.SetValue(compositeEffect, SkipDestroyCompositeField.GetValue(templateEffect));
        }
    }

    private static CompositePrefabPart ClonePart(CompositePrefabPart source)
    {
        if (source == null)
        {
            return new CompositePrefabPart();
        }

        return JsonUtility.FromJson<CompositePrefabPart>(JsonUtility.ToJson(source));
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
