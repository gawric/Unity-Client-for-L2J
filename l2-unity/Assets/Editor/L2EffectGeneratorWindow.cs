#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI for the new L2 effect generation pipeline.
/// Menu: Tools/L2 Effects/Effect Generator...
/// </summary>
public sealed class L2EffectGeneratorWindow : EditorWindow
{
    private const string PrefEffectName = "L2EffectGenerator.EffectName";
    private const string PrefEffectRootGuid = "L2EffectGenerator.EffectRootGuid";
    private const string PrefUcTaGuid = "L2EffectGenerator.UcTaGuid";
    private const string PrefUcCaGuid = "L2EffectGenerator.UcCaGuid";

    private string _effectName = string.Empty;
    private DefaultAsset _effectRootFolder;
    private DefaultAsset _ucTaFile;
    private DefaultAsset _ucCaFile;

    private Vector2 _scrollPosition;

    [MenuItem("Tools/L2 Effects/Effect Generator...")]
    public static void Open()
    {
        var window = GetWindow<L2EffectGeneratorWindow>("L2 Effect Generator");
        window.minSize = new Vector2(460f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        _effectName = EditorPrefs.GetString(PrefEffectName, string.Empty);
        _effectRootFolder = LoadAssetByGuid<DefaultAsset>(PrefEffectRootGuid);
        _ucTaFile = LoadAssetByGuid<DefaultAsset>(PrefUcTaGuid);
        _ucCaFile = LoadAssetByGuid<DefaultAsset>(PrefUcCaGuid);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PrefEffectName, _effectName ?? string.Empty);
        SaveAssetGuid(PrefEffectRootGuid, _effectRootFolder);
        SaveAssetGuid(PrefUcTaGuid, _ucTaFile);
        SaveAssetGuid(PrefUcCaGuid, _ucCaFile);
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("L2 Effect Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates subfolders, child prefabs, and {EffectName}_composite under Effect Root.\n" +
            "Emitter materials use the unified SpriteEmitter/MeshEmitter shaders and UC values.\n" +
            "UC _ta File (optional) -> {EffectName}_ta/{EffectName}_ta.prefab\n" +
            "UC _ca File (optional) -> {EffectName}_ca/{EffectName}_ca.prefab\n" +
            "At least one UC file is required. Composite includes only the parts that were generated.",
            MessageType.Info);
        EditorGUILayout.Space(8f);

        _effectName = EditorGUILayout.TextField("Effect Name", _effectName);

        DrawFolderField(
            "Effect Root Folder",
            ref _effectRootFolder,
            "Select the root folder where Unity assets for this effect will live.");

        DrawUcFileField(
            "UC _ta File",
            ref _ucTaFile,
            "Optional target-apply .uc source file. Creates {EffectName}_ta under the root folder.");

        DrawUcFileField(
            "UC _ca File",
            ref _ucCaFile,
            "Optional cast-apply .uc source file. Creates {EffectName}_ca under the root folder.");

        EditorGUILayout.Space(12f);
        DrawResolvedPaths();
        DrawPlannedFoldersPreview();

        EditorGUILayout.Space(16f);
        using (new EditorGUI.DisabledScope(!CanGenerateFolders()))
        {
            if (GUILayout.Button("Generate Effect Assets", GUILayout.Height(32f)))
            {
                GenerateEffectFolders();
            }
        }

        if (!CanGenerateFolders())
        {
            EditorGUILayout.HelpBox(
                "Provide effect name, root folder, and at least one UC file (_ta and/or _ca).",
                MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResolvedPaths()
    {
        EditorGUILayout.LabelField("Resolved Paths", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(GetFolderPath(_effectRootFolder), EditorStyles.textField, GUILayout.Height(18f));
        EditorGUILayout.SelectableLabel(GetAssetPath(_ucTaFile), EditorStyles.textField, GUILayout.Height(18f));
        EditorGUILayout.SelectableLabel(GetAssetPath(_ucCaFile), EditorStyles.textField, GUILayout.Height(18f));
    }

    private void DrawPlannedFoldersPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Planned Folders", EditorStyles.boldLabel);

        if (!L2EffectGeneratorFolderBuilder.TryPlanFolders(
                _effectName,
                GetFolderPath(_effectRootFolder),
                GetAssetPath(_ucTaFile),
                GetAssetPath(_ucCaFile),
                out L2EffectGeneratorFolderBuilder.BuildResult preview))
        {
            if (!string.IsNullOrWhiteSpace(_effectName) ||
                _effectRootFolder != null ||
                _ucTaFile != null ||
                _ucCaFile != null)
            {
                EditorGUILayout.HelpBox(preview.ErrorMessage, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("(fill fields above to preview folder names)");
            }

            return;
        }

        for (int i = 0; i < preview.Planned.Count; i++)
        {
            L2EffectGeneratorFolderBuilder.PlannedFolder planned = preview.Planned[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(planned.Label + " folder", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(planned.AssetPath, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel(
                L2EffectGeneratorPrefabBuilder.GetPrefabAssetPath(planned),
                EditorStyles.textField,
                GUILayout.Height(18f));
            EditorGUILayout.LabelField("From UC", planned.SourceUcPath);
            DrawPlannedEmittersPreview(planned.SourceUcPath);
            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawPlannedEmittersPreview(string ucAssetPath)
    {
        if (string.IsNullOrWhiteSpace(ucAssetPath))
        {
            return;
        }

        if (!L2EffectUcEmitterParser.TryParseFile(
                ucAssetPath,
                out List<L2EffectUcEmitterParser.UcEmitterDefinition> emitters,
                out string parseError))
        {
            EditorGUILayout.HelpBox(parseError, MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Emitters", EditorStyles.miniBoldLabel);
        for (int i = 0; i < emitters.Count; i++)
        {
            L2EffectUcEmitterParser.UcEmitterDefinition emitter = emitters[i];
            EditorGUILayout.LabelField(
                "  " + emitter.EmitterName,
                emitter.ClassName + ", " +
                (emitter.MaxParticles <= 1 ? "ParticleSingle" : "ParticleGroup") +
                ", slots=" + System.Math.Max(1, emitter.MaxParticles) +
                ", slotName=" + (string.IsNullOrWhiteSpace(emitter.ParticleSlotName)
                    ? emitter.EmitterName
                    : emitter.ParticleSlotName));
        }
    }

    private bool CanGenerateFolders()
    {
        return L2EffectGeneratorFolderBuilder.TryPlanFolders(
            _effectName,
            GetFolderPath(_effectRootFolder),
            GetAssetPath(_ucTaFile),
            GetAssetPath(_ucCaFile),
            out _);
    }

    private void GenerateEffectFolders()
    {
        L2EffectGeneratorFolderBuilder.BuildResult result = L2EffectGeneratorFolderBuilder.CreateFolders(
            _effectName,
            GetFolderPath(_effectRootFolder),
            GetAssetPath(_ucTaFile),
            GetAssetPath(_ucCaFile));

        for (int i = 0; i < result.LogLines.Count; i++)
        {
            string line = result.LogLines[i];
            if (line.IndexOf("ERROR", System.StringComparison.Ordinal) >= 0)
            {
                Debug.LogError("[L2EffectGenerator] " + line);
            }
            else
            {
                Debug.Log("[L2EffectGenerator] " + line);
            }
        }

        string dialogBody = result.LogLines.Count > 0
            ? string.Join("\n", result.LogLines)
            : result.ErrorMessage;

        if (!result.Success)
        {
            EditorUtility.DisplayDialog("L2 Effect Generator", dialogBody, "OK");
            return;
        }

        EditorUtility.DisplayDialog("L2 Effect Generator", dialogBody, "OK");
    }

    private static void DrawFolderField(string label, ref DefaultAsset folder, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        folder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent(label, tooltip),
            folder,
            typeof(DefaultAsset),
            false);

        if (GUILayout.Button("Browse", GUILayout.Width(70f)))
        {
            string selected = EditorUtility.OpenFolderPanel(label, "Assets", string.Empty);
            if (!string.IsNullOrEmpty(selected))
            {
                folder = LoadProjectFolder(selected);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (folder != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
        {
            EditorGUILayout.HelpBox("Selected object is not a project folder.", MessageType.Warning);
        }
    }

    private static void DrawUcFileField(string label, ref DefaultAsset ucFile, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        ucFile = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent(label, tooltip),
            ucFile,
            typeof(DefaultAsset),
            false);

        if (GUILayout.Button("Browse", GUILayout.Width(70f)))
        {
            string selected = EditorUtility.OpenFilePanel(label, Application.dataPath, "uc");
            if (!string.IsNullOrEmpty(selected))
            {
                ucFile = LoadProjectAsset(selected);
            }
        }

        EditorGUILayout.EndHorizontal();

        string path = GetAssetPath(ucFile);
        if (!string.IsNullOrEmpty(path) && !path.EndsWith(".uc", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorGUILayout.HelpBox("Selected file is not a .uc asset.", MessageType.Warning);
        }
    }

    private static string GetAssetPath(Object asset)
    {
        return asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
    }

    private static string GetFolderPath(DefaultAsset folder)
    {
        return folder == null ? string.Empty : AssetDatabase.GetAssetPath(folder);
    }

    private static DefaultAsset LoadProjectFolder(string absolutePath)
    {
        string projectRelative = ToProjectRelativePath(absolutePath);
        if (string.IsNullOrEmpty(projectRelative) || !AssetDatabase.IsValidFolder(projectRelative))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(projectRelative);
    }

    private static DefaultAsset LoadProjectAsset(string absolutePath)
    {
        string projectRelative = ToProjectRelativePath(absolutePath);
        if (string.IsNullOrEmpty(projectRelative))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(projectRelative);
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        absolutePath = absolutePath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (!absolutePath.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[L2EffectGenerator] Path is outside the Unity project: " + absolutePath);
            return null;
        }

        return "Assets" + absolutePath.Substring(dataPath.Length);
    }

    private static T LoadAssetByGuid<T>(string prefKey) where T : Object
    {
        string guid = EditorPrefs.GetString(prefKey, string.Empty);
        if (string.IsNullOrEmpty(guid))
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void SaveAssetGuid(string prefKey, Object asset)
    {
        string path = GetAssetPath(asset);
        string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        EditorPrefs.SetString(prefKey, guid ?? string.Empty);
    }
}
#endif
