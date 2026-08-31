#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorFolderBuilder
{
    private static readonly string[] SuffixOrder =
    {
        "_ca", "_pr", "_cs", "_fl", "_ave", "_ta"
    };

    public sealed class PlannedFolder
    {
        public string Label;
        public string FolderName;
        public string AssetPath;
        public string SourceUcPath;
        public string Suffix;
        public string ClassName;
        public string ExtendsClass;
        public bool IsProjectile;
        public bool HasBeamEmitter;
        public float ProjectileSpeedUnreal;
        public bool HasProjectileSpeed;
        public bool CopyUcIntoFolder;
        public bool UseCastWindow = true;
    }

    public sealed class BuildResult
    {
        public bool Success;
        public string ErrorMessage;
        public string EffectName;
        public int SkillVisualId;
        public bool SkillIdAmbiguous;
        public List<int> SkillIdCandidates = new List<int>();
        public List<PlannedFolder> Planned = new List<PlannedFolder>();
        public List<string> LogLines = new List<string>();
    }

    public static bool TryPlanFromFolder(
        string effectRootPath,
        string effectNameOverride,
        int preferredSkillId,
        out BuildResult result)
    {
        result = new BuildResult();

        if (string.IsNullOrWhiteSpace(effectRootPath) || !AssetDatabase.IsValidFolder(effectRootPath))
        {
            result.ErrorMessage = "A valid effect root folder is required.";
            return false;
        }

        string normalizedRoot = effectRootPath.Replace('\\', '/').TrimEnd('/');
        string effectName = string.IsNullOrWhiteSpace(effectNameOverride)
            ? Path.GetFileName(normalizedRoot)
            : effectNameOverride.Trim();

        if (string.IsNullOrWhiteSpace(effectName))
        {
            result.ErrorMessage = "Effect name is required.";
            return false;
        }

        result.EffectName = effectName;
        List<string> ucPaths = DiscoverUcAssetPaths(normalizedRoot);
        if (ucPaths.Count == 0)
        {
            result.ErrorMessage = "No .uc files found in " + normalizedRoot + " (root or one subfolder level).";
            return false;
        }

        var classNames = new List<string>();
        for (int i = 0; i < ucPaths.Count; i++)
        {
            if (!TryAddPlannedFromUc(result, effectName, normalizedRoot, ucPaths[i]))
            {
                return false;
            }

            PlannedFolder planned = result.Planned[result.Planned.Count - 1];
            if (!string.IsNullOrEmpty(planned.ClassName))
            {
                classNames.Add(planned.ClassName);
            }
        }

        SortPlanned(result.Planned);
        L2EffectSkillLaunchTable.SkillResolveResult skillResolve =
            L2EffectSkillLaunchTable.ResolveSkillId(classNames, preferredSkillId);
        result.SkillVisualId = skillResolve.SkillId;
        result.SkillIdAmbiguous = skillResolve.Ambiguous;
        result.SkillIdCandidates = skillResolve.Candidates;
        if (skillResolve.Ambiguous)
        {
            result.ErrorMessage =
                "Ambiguous skill visual id (" + string.Join(", ", skillResolve.Candidates) +
                "). Enter Skill Visual ID before generating.";
            result.Success = false;
            return false;
        }

        result.Success = true;
        return true;
    }

    /// <summary>
    /// NPC deco: one Emitter class → one L2Particle. No skill id, no composite, no GlobalEffect bind.
    /// Prefab is written next to the .uc (deco/u_npc_id_buff/u_npc_id_buff.prefab).
    /// </summary>
    public static bool TryPlanDecoFromFolder(
        string effectRootPath,
        string effectNameOverride,
        out BuildResult result)
    {
        result = new BuildResult();

        if (string.IsNullOrWhiteSpace(effectRootPath) || !AssetDatabase.IsValidFolder(effectRootPath))
        {
            result.ErrorMessage = "A valid deco root folder is required.";
            return false;
        }

        string normalizedRoot = effectRootPath.Replace('\\', '/').TrimEnd('/');
        string effectName = string.IsNullOrWhiteSpace(effectNameOverride)
            ? Path.GetFileName(normalizedRoot)
            : effectNameOverride.Trim();

        if (string.IsNullOrWhiteSpace(effectName))
        {
            result.ErrorMessage = "Deco effect name is required.";
            return false;
        }

        result.EffectName = effectName;
        List<string> ucPaths = DiscoverUcAssetPaths(normalizedRoot);
        if (ucPaths.Count == 0)
        {
            result.ErrorMessage = "No .uc files found in " + normalizedRoot + " (root or one subfolder level).";
            return false;
        }

        for (int i = 0; i < ucPaths.Count; i++)
        {
            if (!TryAddPlannedFromUc(result, effectName, normalizedRoot, ucPaths[i]))
            {
                return false;
            }

            PlannedFolder planned = result.Planned[result.Planned.Count - 1];
            bool nameMatch =
                string.Equals(planned.ClassName, effectName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(ucPaths[i]), effectName, StringComparison.OrdinalIgnoreCase);
            if (!nameMatch)
            {
                result.Planned.RemoveAt(result.Planned.Count - 1);
                continue;
            }

            string ucFolder = Path.GetDirectoryName(ucPaths[i])?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(ucFolder))
            {
                planned.AssetPath = ucFolder;
            }

            planned.UseCastWindow = true;
            planned.Label = planned.ClassName;
        }

        if (result.Planned.Count == 0)
        {
            result.ErrorMessage =
                "No UC class named '" + effectName + "' under " + normalizedRoot + ".";
            return false;
        }

        result.SkillVisualId = 0;
        result.Success = true;
        return true;
    }

    public static BuildResult CreateDecoFromFolder(string effectRootPath, string effectNameOverride)
    {
        if (!TryPlanDecoFromFolder(effectRootPath, effectNameOverride, out BuildResult result))
        {
            return result;
        }

        return BuildPlannedPrefabs(effectRootPath, result, bindToGlobalEffect: false, createComposite: false);
    }

    public static BuildResult CreateFromFolder(
        string effectRootPath,
        string effectNameOverride,
        int preferredSkillId,
        bool bindToGlobalEffect = true)
    {
        if (!TryPlanFromFolder(effectRootPath, effectNameOverride, preferredSkillId, out BuildResult result))
        {
            return result;
        }

        return BuildPlannedPrefabs(effectRootPath, result, bindToGlobalEffect, createComposite: true);
    }

    static BuildResult BuildPlannedPrefabs(
        string effectRootPath,
        BuildResult result,
        bool bindToGlobalEffect,
        bool createComposite)
    {

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < result.Planned.Count; i++)
            {
                PlannedFolder planned = result.Planned[i];
                if (!AssetDatabase.IsValidFolder(planned.AssetPath))
                {
                    string parentFolder = Path.GetDirectoryName(planned.AssetPath)?.Replace('\\', '/');
                    string folderName = Path.GetFileName(planned.AssetPath);
                    if (string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(folderName))
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid planned folder path: " + planned.AssetPath;
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
                else
                {
                    result.LogLines.Add(planned.Label + ": already exists -> " + planned.AssetPath);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        bool prefabFailures = false;

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

            for (int logIndex = 0; logIndex < emitterLogLines.Count; logIndex++)
            {
                result.LogLines.Add(emitterLogLines[logIndex]);
            }
        }

        string compositePath = L2EffectGeneratorCompositeBuilder.GetCompositeAssetPath(
            effectRootPath.Replace('\\', '/').TrimEnd('/'),
            result.EffectName);

        if (!createComposite)
        {
            result.LogLines.Add("Composite: skipped (NPC deco — single L2Particle, no skill composite)");
            result.LogLines.Add("Skill bind: skipped");
        }
        else if (prefabFailures)
        {
            result.LogLines.Add("Composite: skipped (not all child prefabs succeeded)");
        }
        else if (result.Planned.Count == 0)
        {
            result.LogLines.Add("Composite: skipped (no child prefabs were built)");
        }
        else
        {
            if (!L2EffectGeneratorCompositeBuilder.TryCreateOrUpdateComposite(
                    result.EffectName,
                    effectRootPath.Replace('\\', '/').TrimEnd('/'),
                    result.Planned,
                    result.SkillVisualId,
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

            if (!prefabFailures && bindToGlobalEffect)
            {
                if (!L2EffectGeneratorSkillBinder.TryBindComposite(
                        result.SkillVisualId,
                        compositePath,
                        out string bindLog,
                        out string bindError))
                {
                    result.LogLines.Add("Skill bind: WARNING " +
                                        (string.IsNullOrEmpty(bindError) ? bindLog : bindError));
                }
                else if (!string.IsNullOrEmpty(bindLog))
                {
                    result.LogLines.Add("Skill bind: " + bindLog);
                }
            }
            else if (!bindToGlobalEffect)
            {
                result.LogLines.Add("Skill bind: skipped");
            }
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

    public static List<string> DiscoverUcAssetPaths(string effectRootPath)
    {
        var paths = new List<string>();
        string fullRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", effectRootPath));
        if (!Directory.Exists(fullRoot))
        {
            return paths;
        }

        AddUcFiles(paths, fullRoot, effectRootPath);
        string[] children = Directory.GetDirectories(fullRoot);
        for (int i = 0; i < children.Length; i++)
        {
            string folderName = Path.GetFileName(children[i]);
            if (folderName.IndexOf("deprecated", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            AddUcFiles(paths, children[i], effectRootPath + "/" + folderName);
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        return paths;
    }

    private static void AddUcFiles(List<string> paths, string fullFolder, string assetFolder)
    {
        string[] files = Directory.GetFiles(fullFolder, "*.uc");
        for (int i = 0; i < files.Length; i++)
        {
            string assetPath = assetFolder.Replace('\\', '/') + "/" + Path.GetFileName(files[i]);
            if (!paths.Contains(assetPath))
            {
                paths.Add(assetPath);
            }
        }
    }

    private static bool TryAddPlannedFromUc(
        BuildResult result,
        string effectName,
        string effectRootPath,
        string ucAssetPath)
    {
        if (!ucAssetPath.EndsWith(".uc", StringComparison.OrdinalIgnoreCase))
        {
            result.ErrorMessage = "UC file must have a .uc extension: " + ucAssetPath;
            return false;
        }

        string className = Path.GetFileNameWithoutExtension(ucAssetPath);
        string extendsClass = null;
        bool isProjectile = false;
        float projectileSpeed = 0f;
        bool hasProjectileSpeed = false;

        if (!L2EffectUcEmitterParser.TryParseFileInfo(
                ucAssetPath,
                out UcFileInfo ucInfo,
                out string parseError))
        {
            result.ErrorMessage = "UC parse failed: " + ucAssetPath + " — " + parseError;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(ucInfo.ClassName))
        {
            className = ucInfo.ClassName;
        }

        extendsClass = ucInfo.ExtendsClass;
        isProjectile = ucInfo.IsProjectile;
        hasProjectileSpeed = ucInfo.HasSpeed;
        projectileSpeed = ucInfo.Speed;

        bool hasBeamEmitter = ContainsBeamEmitter(ucInfo);
        string suffix = ResolveSuffix(effectName, className);
        string folderName = className;
        string assetPath = effectRootPath + "/" + folderName;

        result.Planned.Add(new PlannedFolder
        {
            Label = string.IsNullOrEmpty(suffix) ? className.ToUpperInvariant() : suffix.TrimStart('_').ToUpperInvariant(),
            FolderName = folderName,
            AssetPath = assetPath,
            SourceUcPath = ucAssetPath,
            Suffix = suffix,
            ClassName = className,
            ExtendsClass = extendsClass,
            IsProjectile = isProjectile,
            HasBeamEmitter = hasBeamEmitter,
            ProjectileSpeedUnreal = projectileSpeed,
            HasProjectileSpeed = hasProjectileSpeed,
            CopyUcIntoFolder = false,
            UseCastWindow = !L2EffectSkillLaunchTable.IsImpactSuffix(suffix) &&
                            !L2EffectSkillLaunchTable.IsShootVisualSuffix(suffix) &&
                            !hasBeamEmitter
        });
        return true;
    }

    static bool ContainsBeamEmitter(UcFileInfo ucInfo)
    {
        if (ucInfo == null || ucInfo.Emitters == null)
        {
            return false;
        }

        for (int i = 0; i < ucInfo.Emitters.Count; i++)
        {
            UcEmitterDefinition emitter = ucInfo.Emitters[i];
            if (emitter != null &&
                string.Equals(emitter.ClassName, "BeamEmitter", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveSuffix(string effectName, string className)
    {
        if (!string.IsNullOrEmpty(effectName) &&
            className.StartsWith(effectName, StringComparison.OrdinalIgnoreCase) &&
            className.Length > effectName.Length)
        {
            return className.Substring(effectName.Length);
        }

        int lastUnderscore = className.LastIndexOf('_');
        if (lastUnderscore > 0 && lastUnderscore < className.Length - 1)
        {
            return className.Substring(lastUnderscore);
        }

        return string.Empty;
    }

    private static void SortPlanned(List<PlannedFolder> planned)
    {
        planned.Sort((a, b) =>
        {
            int orderA = SuffixSortIndex(a.Suffix);
            int orderB = SuffixSortIndex(b.Suffix);
            int compare = orderA.CompareTo(orderB);
            return compare != 0
                ? compare
                : string.Compare(a.FolderName, b.FolderName, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static int SuffixSortIndex(string suffix)
    {
        for (int i = 0; i < SuffixOrder.Length; i++)
        {
            if (string.Equals(SuffixOrder[i], suffix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return SuffixOrder.Length;
    }
}
#endif
