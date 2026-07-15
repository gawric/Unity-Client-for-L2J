#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorFolderBuilder
{
    public sealed class PlannedFolder
    {
        public string Label;
        public string FolderName;
        public string AssetPath;
        public string SourceUcPath;
    }

    public sealed class BuildResult
    {
        public bool Success;
        public string ErrorMessage;
        public List<PlannedFolder> Planned = new List<PlannedFolder>();
        public List<string> LogLines = new List<string>();
    }

    public static bool TryPlanFolders(
        string effectName,
        string effectRootPath,
        string ucTaPath,
        string ucCaPath,
        out BuildResult result)
    {
        result = new BuildResult();

        if (string.IsNullOrWhiteSpace(effectName))
        {
            result.ErrorMessage = "Effect name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(effectRootPath) || !AssetDatabase.IsValidFolder(effectRootPath))
        {
            result.ErrorMessage = "A valid effect root folder is required.";
            return false;
        }

        string normalizedEffectName = effectName.Trim();
        string normalizedRoot = effectRootPath.Replace('\\', '/').TrimEnd('/');

        if (!TryAddPlannedFolder(result, "TA", "_ta", normalizedEffectName, normalizedRoot, ucTaPath))
        {
            return false;
        }

        if (!TryAddPlannedFolder(result, "CA", "_ca", normalizedEffectName, normalizedRoot, ucCaPath))
        {
            return false;
        }

        if (result.Planned.Count == 0)
        {
            result.ErrorMessage = "Provide at least one UC file (_ta and/or _ca).";
            return false;
        }

        result.Success = true;
        return true;
    }

    public static BuildResult CreateFolders(
        string effectName,
        string effectRootPath,
        string ucTaPath,
        string ucCaPath)
    {
        if (!TryPlanFolders(effectName, effectRootPath, ucTaPath, ucCaPath, out BuildResult result))
        {
            return result;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < result.Planned.Count; i++)
            {
                PlannedFolder planned = result.Planned[i];
                if (AssetDatabase.IsValidFolder(planned.AssetPath))
                {
                    result.LogLines.Add(planned.Label + ": already exists -> " + planned.AssetPath);
                    continue;
                }

                string parentFolder = Path.GetDirectoryName(planned.AssetPath)?.Replace('\\', '/');
                string folderName = Path.GetFileName(planned.AssetPath);
                if (string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(folderName))
                {
                    result.Success = false;
                    result.ErrorMessage = "Invalid planned folder path: " + planned.AssetPath;
                    return result;
                }

                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    result.Success = false;
                    result.ErrorMessage = "Parent folder does not exist: " + parentFolder;
                    return result;
                }

                string createdGuid = AssetDatabase.CreateFolder(parentFolder, folderName);
                if (string.IsNullOrEmpty(createdGuid))
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to create " + planned.AssetPath + ".";
                    return result;
                }

                result.LogLines.Add(planned.Label + ": created -> " + planned.AssetPath);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        bool prefabFailures = false;
        string normalizedEffectName = effectName.Trim();
        string normalizedRoot = effectRootPath.Replace('\\', '/').TrimEnd('/');
        var successfulPlanned = new List<PlannedFolder>();

        for (int i = 0; i < result.Planned.Count; i++)
        {
            PlannedFolder planned = result.Planned[i];
            if (!L2EffectGeneratorPrefabBuilder.TryCreateRootPrefab(
                    planned,
                    out string prefabLogLine,
                    out string prefabError))
            {
                prefabFailures = true;
                result.LogLines.Add(planned.Label + ": ERROR " + prefabError);
                continue;
            }

            if (!string.IsNullOrEmpty(prefabLogLine))
            {
                result.LogLines.Add(prefabLogLine);
            }

            if (!L2EffectGeneratorPrefabBuilder.TryPopulateEmitterObjects(
                    planned,
                    out List<string> emitterLogLines,
                    out string emitterError))
            {
                prefabFailures = true;
                result.LogLines.Add(planned.Label + ": ERROR " + emitterError);
                continue;
            }

            successfulPlanned.Add(planned);
            for (int logIndex = 0; logIndex < emitterLogLines.Count; logIndex++)
            {
                result.LogLines.Add(emitterLogLines[logIndex]);
            }
        }

        if (!prefabFailures && successfulPlanned.Count > 0)
        {
            if (!L2EffectGeneratorCompositeBuilder.TryCreateOrUpdateComposite(
                    normalizedEffectName,
                    normalizedRoot,
                    successfulPlanned,
                    out string compositeLogLine,
                    out string compositeError))
            {
                prefabFailures = true;
                result.LogLines.Add("Composite: ERROR " + compositeError);
            }
            else if (!string.IsNullOrEmpty(compositeLogLine))
            {
                result.LogLines.Add("Composite: " + compositeLogLine);
            }
        }
        else if (!prefabFailures && successfulPlanned.Count == 0)
        {
            result.LogLines.Add("Composite: skipped (no child prefabs were built)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        result.Success = !prefabFailures;
        if (prefabFailures && string.IsNullOrEmpty(result.ErrorMessage))
        {
            result.ErrorMessage = "One or more prefabs failed to create. See Console log for details.";
        }

        return result;
    }

    private static bool TryAddPlannedFolder(
        BuildResult result,
        string label,
        string suffix,
        string effectName,
        string effectRootPath,
        string ucAssetPath)
    {
        if (string.IsNullOrWhiteSpace(ucAssetPath))
        {
            return true;
        }

        if (!ucAssetPath.EndsWith(".uc", StringComparison.OrdinalIgnoreCase))
        {
            result.ErrorMessage = label + " UC file must have a .uc extension: " + ucAssetPath;
            return false;
        }

        string folderName = effectName + suffix;
        string assetPath = effectRootPath + "/" + folderName;
        result.Planned.Add(new PlannedFolder
        {
            Label = label,
            FolderName = folderName,
            AssetPath = assetPath,
            SourceUcPath = ucAssetPath
        });
        return true;
    }
}
#endif
