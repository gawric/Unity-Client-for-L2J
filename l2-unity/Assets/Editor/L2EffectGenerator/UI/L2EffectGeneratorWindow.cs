#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI for the L2 effect generation pipeline.
/// Menu: Tools/L2 Effects/Effect Generator...
/// Select an effect folder; the tool reads .uc files and the HF268 launch table,
/// then builds ParticleGroupV2 prefabs (unified HLSL) and a CompositeEffectV2.
/// </summary>
public sealed class L2EffectGeneratorWindow : EditorWindow
{
    private const string PrefEffectName = "L2EffectGenerator.EffectName";
    private const string PrefEffectRootGuid = "L2EffectGenerator.EffectRootGuid";
    private const string PrefSkillId = "L2EffectGenerator.SkillVisualId";
    private const string PrefBindToGlobalEffect = "L2EffectGenerator.BindToGlobalEffect";

    private string _effectName = string.Empty;
    private DefaultAsset _effectRootFolder;
    private int _skillVisualId;
    private bool _bindToGlobalEffect = true;
    private Vector2 _scrollPosition;
    private string _lastFolderPath = string.Empty;

    [MenuItem("Tools/L2 Effects/Effect Generator...")]
    public static void Open()
    {
        var window = GetWindow<L2EffectGeneratorWindow>("L2 Effect Generator");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    /// <summary>
    /// Batch entry: Unity.exe -executeMethod L2EffectGeneratorWindow.GenerateWindStrikeCli
    /// </summary>
    public static void GenerateWindStrikeCli()
    {
        GenerateCli(
            "Assets/Resources/Data/Effects/el_wind_strike",
            "el_wind_strike",
            1177);
    }

    /// <summary>
    /// Batch entry: Unity.exe -executeMethod L2EffectGeneratorWindow.GenerateHealCli
    /// </summary>
    public static void GenerateHealCli()
    {
        GenerateCli(
            "Assets/Resources/Data/Effects/wh_heal",
            "wh_heal",
            1011);
    }

    /// <summary>
    /// NPC deco: one Emitter class → one L2Particle under Resources/Data/Effects/deco.
    /// Unity.exe -batchmode -nographics -quit -executeMethod L2EffectGeneratorWindow.GenerateNpcDecoCli
    /// Optional: -decoName u_npc_id_buff
    /// </summary>
    public static void GenerateNpcDecoCli()
    {
        string effectName = ReadNamedArg("-decoName");
        if (string.IsNullOrWhiteSpace(effectName))
        {
            effectName = "u_npc_id_buff";
        }

        if (!GenerateDeco("Assets/Resources/Data/Effects/deco", effectName))
        {
            EditorApplication.Exit(1);
        }
    }

    public static void GenerateNpcDecoBuffMenu()
    {
        bool ok = GenerateDeco("Assets/Resources/Data/Effects/deco", "u_npc_id_buff");
        EditorUtility.DisplayDialog(
            "L2 Effect Generator",
            ok ? "Generated u_npc_id_buff under Data/Effects/deco." : "Failed. See Console.",
            "OK");
    }

    static bool GenerateDeco(string decoRoot, string effectName)
    {
        AssetDatabase.Refresh();
        L2EffectGeneratorFolderBuilder.BuildResult result =
            L2EffectGeneratorFolderBuilder.CreateDecoFromFolder(decoRoot, effectName);

        for (int i = 0; i < result.LogLines.Count; i++)
        {
            string line = result.LogLines[i];
            if (line.IndexOf("ERROR", System.StringComparison.Ordinal) >= 0)
            {
                Debug.LogError("[L2EffectGenerator][deco] " + line);
            }
            else
            {
                Debug.Log("[L2EffectGenerator][deco] " + line);
            }
        }

        if (!result.Success)
        {
            Debug.LogError("[L2EffectGenerator][deco] " + result.ErrorMessage);
            return false;
        }

        return true;
    }

    static string ReadNamedArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, System.StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                return args[i + 1];
            }

            if (args[i].StartsWith(name + "=", System.StringComparison.OrdinalIgnoreCase))
            {
                return args[i].Substring(name.Length + 1);
            }
        }

        return null;
    }

    static void GenerateCli(string folder, string effectName, int skillVisualId)
    {
        L2EffectGeneratorFolderBuilder.BuildResult result = L2EffectGeneratorFolderBuilder.CreateFromFolder(
            folder,
            effectName,
            skillVisualId,
            true);

        for (int i = 0; i < result.LogLines.Count; i++)
        {
            Debug.Log("[L2EffectGenerator] " + result.LogLines[i]);
        }

        if (!result.Success)
        {
            Debug.LogError("[L2EffectGenerator] " + result.ErrorMessage);
        }
    }

    private void OnEnable()
    {
        _effectName = EditorPrefs.GetString(PrefEffectName, string.Empty);
        _effectRootFolder = LoadAssetByGuid<DefaultAsset>(PrefEffectRootGuid);
        _skillVisualId = EditorPrefs.GetInt(PrefSkillId, 0);
        _bindToGlobalEffect = EditorPrefs.GetBool(PrefBindToGlobalEffect, true);
        _lastFolderPath = GetFolderPath(_effectRootFolder);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PrefEffectName, _effectName ?? string.Empty);
        SaveAssetGuid(PrefEffectRootGuid, _effectRootFolder);
        EditorPrefs.SetInt(PrefSkillId, _skillVisualId);
        EditorPrefs.SetBool(PrefBindToGlobalEffect, _bindToGlobalEffect);
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("L2 Effect Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select the effect folder. The generator finds .uc files, builds child prefabs with " +
            "L2/Effects/SpriteEmitter, MeshEmitter, and BeamEmitter (Decompile_Common HLSL), then wires CompositeEffectV2 " +
            "from skill-effects.tsv (one composite part per unique TSV row). Emitters use ParticleGroupV2 " +
            "(stream + DrawBatch). Skill Visual ID 0 auto-detects only " +
            "when exactly one skill matches all UC classes. Wind Strike is 1177. Heal is 1011. " +
            "_ca casts immediately, _ave or BeamEmitter waits for ShootEvent on the target, _ta is hit. " +
            "Skill ID is the server skill (1147 Vampiric Touch). Launch rows use skill_visual_effect " +
            "from skillgrp (1147 → 1090).",
            MessageType.Info);
        EditorGUILayout.Space(8f);

        DrawFolderField(
            "Effect Folder",
            ref _effectRootFolder,
            "Folder that contains .uc files (root or one subfolder level).");

        string folderPath = GetFolderPath(_effectRootFolder);
        if (!string.Equals(folderPath, _lastFolderPath, System.StringComparison.OrdinalIgnoreCase))
        {
            _lastFolderPath = folderPath;
            if (!string.IsNullOrEmpty(folderPath))
            {
                _effectName = System.IO.Path.GetFileName(folderPath.TrimEnd('/'));
            }
        }

        _effectName = EditorGUILayout.TextField("Effect Name", _effectName);
        _skillVisualId = EditorGUILayout.IntField(
            new GUIContent(
                "Skill ID",
                "Server skill id for GlobalEffect bind (1147 = Vampiric Touch). " +
                "0 = auto-detect launch-table visual id. Changing the folder only updates Effect Name, not this field."),
            _skillVisualId);
        _bindToGlobalEffect = EditorGUILayout.Toggle(
            new GUIContent(
                "Bind to GlobalEffect",
                "Write the generated composite onto GlobalEffect.asset. Bind warnings do not fail generate."),
            _bindToGlobalEffect);

        EditorGUILayout.Space(8f);
        DrawTableStatus();
        DrawResolvedPaths();
        DrawPlannedFoldersPreview();

        EditorGUILayout.Space(16f);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate Effect Assets", GUILayout.Height(32f)))
            {
                GenerateEffect();
            }
        }

        if (!CanGenerate())
        {
            EditorGUILayout.HelpBox(GetGenerateBlockReason(), MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTableStatus()
    {
        if (L2EffectSkillLaunchTable.TryLoad(out string tableError))
        {
            EditorGUILayout.LabelField("Launch table", L2EffectSkillLaunchTable.ResolveTablePath());
            return;
        }

        EditorGUILayout.HelpBox(
            tableError + " Composite parts will fall back to suffix defaults (_ca cast, _ave shoot, _fl shoot, _ta hit).",
            MessageType.Warning);
    }

    private void DrawResolvedPaths()
    {
        EditorGUILayout.LabelField("Resolved Paths", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(GetFolderPath(_effectRootFolder), EditorStyles.textField, GUILayout.Height(18f));
        EditorGUILayout.LabelField(
            "Composite",
            string.IsNullOrWhiteSpace(_effectName)
                ? "(effect name)"
                : L2EffectGeneratorCompositeBuilder.GetCompositeAssetPath(
                    GetFolderPath(_effectRootFolder),
                    _effectName));
    }

    private void DrawPlannedFoldersPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Planned Parts", EditorStyles.boldLabel);

        if (!L2EffectGeneratorFolderBuilder.TryPlanFromFolder(
                GetFolderPath(_effectRootFolder),
                _effectName,
                _skillVisualId,
                out L2EffectGeneratorFolderBuilder.BuildResult preview))
        {
            if (_effectRootFolder != null || !string.IsNullOrWhiteSpace(_effectName))
            {
                EditorGUILayout.HelpBox(preview.ErrorMessage, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("(select an effect folder to preview)");
            }

            return;
        }

        EditorGUILayout.LabelField("Bind skill id", preview.SkillVisualId.ToString());
        int launchTableId = L2EffectSkillLaunchTable.ResolveLaunchTableId(preview.SkillVisualId);
        if (launchTableId != preview.SkillVisualId && launchTableId > 0)
        {
            EditorGUILayout.LabelField(
                "Launch table id",
                launchTableId + "  (skill_visual_effect for " + preview.SkillVisualId + ")");
        }

        if (preview.SkillVisualId <= 0)
        {
            EditorGUILayout.HelpBox(
                "No unique skill visual id. Composite parts will use suffix defaults unless you enter an id.",
                MessageType.Warning);
        }

        bool hasProjectileCompanion = false;
        for (int i = 0; i < preview.Planned.Count; i++)
        {
            if (preview.Planned[i].IsProjectile && !preview.Planned[i].IsHomeFlight)
            {
                hasProjectileCompanion = true;
                break;
            }
        }

        if (L2EffectGeneratorAssetOverrides.ShouldPrependSharedBodyToMindCa(preview.Planned))
        {
            DrawSharedBodyToMindCaPreview(preview.SkillVisualId, hasProjectileCompanion);
        }

        for (int i = 0; i < preview.Planned.Count; i++)
        {
            L2EffectGeneratorFolderBuilder.PlannedFolder planned = preview.Planned[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(planned.Label + "  " + planned.FolderName, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(planned.AssetPath, EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel(
                L2EffectGeneratorPrefabBuilder.GetPrefabAssetPath(planned),
                EditorStyles.textField,
                GUILayout.Height(18f));
            EditorGUILayout.LabelField("UC", planned.SourceUcPath);
            if (L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned))
            {
                EditorGUILayout.HelpBox(
                    "m_u003_b live lock (class name, not UC): " +
                    "speed=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedSpeed.ToString("0.###") +
                    " accel=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedAcceleration.ToString("0.##") +
                    " max=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedMaxSpeed.ToString("0.##") +
                    " side=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedPathSideOffset.ToString("0.##") +
                    " height=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedPathHeightOffset.ToString("0.##") +
                    " trail=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedTrailHistorySeconds.ToString("0.###") +
                    "s spark=" + L2EffectGeneratorAssetOverrides.M_u003_bLockedTrailSparkSizeMeters.ToString("0.###") +
                    "m dual=1",
                    MessageType.Info);
            }
            EditorGUILayout.LabelField(
                "Class",
                planned.ClassName +
                (string.IsNullOrEmpty(planned.ExtendsClass) ? string.Empty : " extends " + planned.ExtendsClass) +
                (planned.IsHomeFlight ? "  [home→caster]" : string.Empty) +
                (planned.IsTargetTrailer ? "  [trailer→target]" : string.Empty) +
                (planned.IsProjectile && !planned.IsHomeFlight ? "  [projectile]" : string.Empty) +
                (planned.HasBeamEmitter ? "  [beam→shoot/target]" : string.Empty));

            List<L2EffectSkillLaunchTable.LaunchRow> rows =
                L2EffectSkillLaunchTable.RowsForComposite(preview.SkillVisualId, planned.ClassName, planned.Suffix);
            if (rows.Count == 0)
            {
                CompositePart mapped = L2EffectSkillLaunchTable.CreateV2Part(
                    planned, null, hasProjectileCompanion);
                EditorGUILayout.LabelField(
                    "Launch",
                    planned.IsHomeFlight || planned.IsTargetTrailer
                        ? "table miss → UC defaults (shoot/target)"
                        : "table miss → suffix defaults");
                EditorGUILayout.LabelField("Composite", mapped.Describe());
            }
            else
            {
                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    L2EffectSkillLaunchTable.LaunchRow row = rows[rowIndex];
                    CompositePart mapped = L2EffectSkillLaunchTable.CreateV2Part(
                        planned, row, hasProjectileCompanion);
                    string rowLabel = rows.Count == 1 ? "Launch" : "Launch " + (rowIndex + 1);
                    EditorGUILayout.LabelField(
                        rowLabel,
                        row.Phase +
                        " attach=" + (row.HasAttachOn ? row.AttachOn.ToString() : "-") +
                        (row.HasSpawnDelay ? " delay=" + row.SpawnDelay.ToString("0.###") : string.Empty) +
                        (row.HasScale ? " scale=" + row.Scale.ToString("0.###") : string.Empty) +
                        (string.IsNullOrEmpty(row.Bone) ? string.Empty : " bone=" + row.Bone));
                    EditorGUILayout.LabelField(
                        rows.Count == 1 ? "Composite" : "Composite " + (rowIndex + 1),
                        mapped.Describe() +
                        (mapped.spawnDelaySeconds > 0f
                            ? " spawnDelay=" + mapped.spawnDelaySeconds.ToString("0.###")
                            : string.Empty));
                    string warning = L2EffectSkillLaunchTable.DescribeLaunchWarning(row);
                    if (!string.IsNullOrEmpty(warning))
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }
            }

            DrawPlannedEmittersPreview(planned);
            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawSharedBodyToMindCaPreview(int skillVisualId, bool hasProjectileCompanion)
    {
        L2EffectGeneratorFolderBuilder.PlannedFolder planned =
            L2EffectGeneratorAssetOverrides.CreateSharedBodyToMindCaPlanned();
        L2EffectSkillLaunchTable.LaunchRow row =
            L2EffectGeneratorAssetOverrides.ResolveSharedBodyToMindCaRow(skillVisualId);
        CompositePart mapped = L2EffectSkillLaunchTable.CreateV2Part(
            planned, row, hasProjectileCompanion);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("CA  " + planned.ClassName, EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(
            L2EffectGeneratorAssetOverrides.SharedBodyToMindCaPrefabPath,
            EditorStyles.textField,
            GUILayout.Height(18f));
        EditorGUILayout.HelpBox(
            "Shared from curse_poison. Detected m_u003_b + m_u003_c; TSV CA is m_u003_a (not in this folder).",
            MessageType.Info);
        if (row != null)
        {
            EditorGUILayout.LabelField(
                "Launch",
                row.Phase +
                " attach=" + (row.HasAttachOn ? row.AttachOn.ToString() : "-") +
                (row.HasScale ? " scale=" + row.Scale.ToString("0.###") : string.Empty));
        }

        EditorGUILayout.LabelField("Composite", mapped.Describe());
        EditorGUILayout.EndVertical();
    }

    private static void DrawPlannedEmittersPreview(L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        if (planned == null || string.IsNullOrWhiteSpace(planned.SourceUcPath))
        {
            return;
        }

        if (!L2EffectUcEmitterParser.TryParseFile(
                planned.SourceUcPath,
                out List<UcEmitterDefinition> emitters,
                out string parseError))
        {
            EditorGUILayout.HelpBox(parseError, MessageType.None);
            return;
        }

        List<UcEmitterDefinition> activeEmitters = new List<UcEmitterDefinition>();
        for (int i = 0; i < emitters.Count; i++)
        {
            UcEmitterDefinition emitter = emitters[i];
            if (emitter == null ||
                L2EffectGeneratorAssetOverrides.ShouldSkipEmitter(planned.ClassName, emitter.EmitterName))
            {
                continue;
            }

            activeEmitters.Add(emitter);
        }

        bool isHomeOrb = planned.IsHomeFlight ||
                         L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned);
        UcEmitterDefinition trail = null;
        UcEmitterDefinition core = null;
        if (isHomeOrb)
        {
            L2EffectGeneratorHomeOrbLayout.TryResolve(activeEmitters, out trail, out core);
        }

        if (trail != null && core != null)
        {
            EditorGUILayout.HelpBox(
                "Home orbs: trail=" + trail.EmitterName +
                " (" + (string.IsNullOrEmpty(trail.CoordinateSystem) ? "Independent" : trail.CoordinateSystem) +
                " life=" + L2EffectGeneratorHomeOrbLayout.ResolveLifetime(trail).ToString("0.###") +
                "s) core=" + core.EmitterName +
                " (life=" + L2EffectGeneratorHomeOrbLayout.ResolveLifetime(core).ToString("0.###") +
                "s) → HomeProjectileTrailVelocityProvider",
                MessageType.Info);
        }

        EditorGUILayout.LabelField("Emitters", EditorStyles.miniBoldLabel);
        for (int i = 0; i < emitters.Count; i++)
        {
            UcEmitterDefinition emitter = emitters[i];
            if (L2EffectGeneratorAssetOverrides.ShouldSkipEmitter(
                    planned.ClassName, emitter.EmitterName))
            {
                EditorGUILayout.LabelField(
                    "  " + emitter.EmitterName,
                    "skipped");
                continue;
            }

            string role = L2EffectGeneratorHomeOrbLayout.DescribeRole(emitter, trail, core);
            string coord = string.IsNullOrEmpty(emitter.CoordinateSystem)
                ? string.Empty
                : " " + emitter.CoordinateSystem;
            EditorGUILayout.LabelField(
                "  " + emitter.EmitterName,
                (string.IsNullOrEmpty(role) ? string.Empty : role + ", ") +
                emitter.ClassName + coord +
                ", ParticleGroupV2, slots=" + System.Math.Max(1, emitter.MaxParticles) +
                ", life=" + L2EffectGeneratorHomeOrbLayout.ResolveLifetime(emitter).ToString("0.###") +
                "s, slotName=" + (string.IsNullOrWhiteSpace(emitter.ParticleSlotName)
                    ? emitter.EmitterName
                    : emitter.ParticleSlotName));
        }
    }

    private bool CanGenerate()
    {
        return L2EffectGeneratorFolderBuilder.TryPlanFromFolder(
            GetFolderPath(_effectRootFolder),
            _effectName,
            _skillVisualId,
            out _);
    }

    private string GetGenerateBlockReason()
    {
        if (_effectRootFolder == null && string.IsNullOrWhiteSpace(_effectName))
        {
            return "Select a project folder that contains at least one .uc file.";
        }

        L2EffectGeneratorFolderBuilder.TryPlanFromFolder(
            GetFolderPath(_effectRootFolder),
            _effectName,
            _skillVisualId,
            out L2EffectGeneratorFolderBuilder.BuildResult preview);
        return string.IsNullOrEmpty(preview.ErrorMessage)
            ? "Select a project folder that contains at least one .uc file."
            : preview.ErrorMessage;
    }

    private void GenerateEffect()
    {
        L2EffectGeneratorFolderBuilder.BuildResult result = L2EffectGeneratorFolderBuilder.CreateFromFolder(
            GetFolderPath(_effectRootFolder),
            _effectName,
            _skillVisualId,
            _bindToGlobalEffect);

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
