#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class L2PetWolfPrefab
{
    const string FbxPath = "Assets/Resources/Data/Animations/LineageMonsters/pet_wolf_a_m00/pet_wolf_a_m00.fbx";
    const string PrefabPath = "Assets/Resources/Data/Animations/LineageMonsters/pet_wolf_a_m00/pet_wolf_a_m00.prefab";
    const string MaterialPath = "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/wolf_t00.mat";
    const string WaitClipPath = "Assets/Resources/Data/Animations/LineageMonsters/pet_wolf_a_m00/pet_wolf_a_m00.ao_Wait.anim";

    static bool _busy;

    static L2PetWolfPrefab()
    {
        EditorApplication.delayCall += BuildIfMissing;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += BuildIfMissing;
    }

    static void BuildIfMissing()
    {
        if (_busy || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(Abs(PrefabPath)))
        {
            Build();
            return;
        }

        if (!File.Exists(Abs(WaitClipPath)))
            ExtractOnly();
    }

    [MenuItem("L2/Pets/Extract Pet Wolf Clips")]
    public static void ExtractOnly()
    {
        ExtractClipsAndWire();
    }

    [MenuItem("L2/Pets/Build Pet Wolf")]
    public static void Build()
    {
        if (_busy)
            return;
        _busy = true;
        try
        {
            AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (src == null)
            {
                UnityEngine.Debug.LogError("[PetWolf] FBX missing " + FbxPath);
                return;
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            inst.name = "pet_wolf_a_m00";
            int entityLayer = LayerMask.NameToLayer("Entity");
            if (entityLayer >= 0)
                SetLayer(inst, entityLayer);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null)
            {
                SkinnedMeshRenderer[] renderers = inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterial = mat;
            }

            if (inst.transform.Find("click_area") == null)
            {
                GameObject click = GameObject.CreatePrimitive(PrimitiveType.Cube);
                click.name = "click_area";
                click.transform.SetParent(inst.transform, false);
                click.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                click.transform.localScale = new Vector3(0.4f, 0.56f, 0.4f);
                int clickLayer = LayerMask.NameToLayer("EntityClick");
                if (clickLayer >= 0)
                    click.layer = clickLayer;
                click.tag = "Npc";
                MeshRenderer clickRenderer = click.GetComponent<MeshRenderer>();
                if (clickRenderer != null)
                    clickRenderer.enabled = false;
            }

            PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath);
            Object.DestroyImmediate(inst);
            int written = WriteClipFiles();
            StripAnimationsFromFbx();
            L2MonsterPrefabBuilder.WireSingle(PrefabPath);
            FitCapsule(PrefabPath);
            UnityEngine.Debug.Log("[PetWolf] prefab ready clips=" + written);
        }
        finally
        {
            _busy = false;
        }
    }

    static void ExtractClipsAndWire()
    {
        if (_busy)
            return;
        _busy = true;
        try
        {
            int written = WriteClipFiles();
            StripAnimationsFromFbx();
            L2MonsterPrefabBuilder.WireSingle(PrefabPath);
            FitCapsule(PrefabPath);
            UnityEngine.Debug.Log("[PetWolf] animation clips " + written);
        }
        finally
        {
            _busy = false;
        }
    }

    static int WriteClipFiles()
    {
        ModelImporter importer = UnityEditor.AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
            return 0;

        ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
        if (takes == null || takes.Length == 0)
        {
            UnityEngine.Debug.LogError("[PetWolf] FBX has no animation takes");
            return 0;
        }

        var defs = new ModelImporterClipAnimation[takes.Length];
        for (int i = 0; i < takes.Length; i++)
        {
            ModelImporterClipAnimation src = takes[i];
            string take = string.IsNullOrEmpty(src.takeName) ? src.name : src.takeName;
            var clip = new ModelImporterClipAnimation();
            clip.name = ShortName(take);
            clip.takeName = take;
            clip.firstFrame = src.firstFrame;
            clip.lastFrame = src.lastFrame;
            clip.loopTime = ShouldLoop(clip.name);
            defs[i] = clip;
        }

        importer.clipAnimations = defs;
        importer.importAnimation = true;
        importer.SaveAndReimport();

        string folder = Path.GetDirectoryName(FbxPath).Replace('\\', '/');
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
        int written = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip src = assets[i] as AnimationClip;
            if (src == null || src.name.StartsWith("__preview__"))
                continue;
            string safe = src.name.Replace("|", "_").Replace("/", "_").Replace("\\", "_");
            string path = folder + "/pet_wolf_a_m00." + safe + ".anim";
            if (File.Exists(Abs(path)))
                AssetDatabase.DeleteAsset(path);
            AnimationClip copy = Object.Instantiate(src);
            copy.name = "pet_wolf_a_m00." + safe;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = ShouldLoop(safe);
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
            written++;
        }

        AssetDatabase.SaveAssets();
        return written;
    }

    static void StripAnimationsFromFbx()
    {
        string project = Directory.GetCurrentDirectory();
        string blender = @"C:\Users\hh-soft\Pictures\test_umodel\blemder_portable\blender-3.6.16-windows-x64\blender.exe";
        string script = Path.Combine(project, "Assets/Editor/L2EffectGenerator/Blender/psk_psa_to_fbx.py");
        string addon = Path.Combine(project, "Assets/Editor/L2EffectGenerator/Blender/io_import_scene_unreal_psa_psk_280.py");
        string psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\SkeletalMesh\pet_wolf_a_m00.psk";
        string psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\MeshAnimation\pet_wolf_anim.psa";
        string fbx = Path.Combine(project, FbxPath);
        string reference = Path.Combine(project, "Assets/Resources/Data/Animations/LineageMonsters/wolf_m00/wolf.fbx");
        var start = new ProcessStartInfo();
        start.FileName = blender;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.Arguments =
            "-b -P \"" + script + "\" -- \"" + psk + "\" \"" + psa + "\" \"" + fbx + "\" \"" + addon + "\" \"" + reference + "\" mesh";
        using (Process process = Process.Start(start))
        {
            if (process != null)
                process.WaitForExit();
        }

        ModelImporter importer = UnityEditor.AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
            return;
        importer.importAnimation = false;
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();
    }

    static string ShortName(string take)
    {
        int bar = take.LastIndexOf('|');
        return bar >= 0 && bar + 1 < take.Length ? take.Substring(bar + 1) : take;
    }

    static bool ShouldLoop(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("death") && !n.Contains("deathwait"))
            return false;
        return n.Contains("wait") || n.Contains("walk") || n.Contains("run") || n.Contains("swim");
    }

    static string Abs(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }

    static void FitCapsule(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            CharacterController body = root.GetComponent<CharacterController>();
            if (body == null)
                return;
            body.height = 0.7f;
            body.radius = 0.15f;
            body.center = new Vector3(0f, 0.42f, 0f);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child.name == "click_area")
                continue;
            SetLayer(child.gameObject, layer);
        }
    }
}
#endif
