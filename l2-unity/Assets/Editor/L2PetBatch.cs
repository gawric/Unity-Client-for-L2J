#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class L2PetBatch
{
    static L2PetBatch()
    {
        EditorApplication.delayCall += BuildAll;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += BuildAll;
    }
    struct PetMesh
    {
        public string Package;
        public string Stem;
        public string Psk;
        public string Psa;
        public string[] Materials;
    }

    static readonly PetMesh[] Pets =
    {
        new PetMesh
        {
            Package = "LineageMonsters",
            Stem = "hatchling_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\SkeletalMesh\hatchling_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\MeshAnimation\hatchling_anim.psa",
            Materials = new[] { "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/hatchling_t00.mat" }
        },
        new PetMesh
        {
            Package = "LineageMonsters",
            Stem = "strider_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\SkeletalMesh\strider_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\MeshAnimation\strider_anim.psa",
            Materials = new[]
            {
                "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/strider_t00.mat",
                "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/strider_t01.mat"
            }
        },
        new PetMesh
        {
            Package = "LineageMonsters",
            Stem = "wyvern_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\SkeletalMesh\wyvern_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_1\LineageMonsters\MeshAnimation\wyvern_anim.psa",
            Materials = new[]
            {
                "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/wyvern_t00.mat",
                "Assets/Resources/Data/SysTextures/LineageMonstersTex/Materials/wyvern_t01.mat"
            }
        },
        new PetMesh
        {
            Package = "LineageMonsters2",
            Stem = "baby_buffalo_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\SkeletalMesh\baby_buffalo_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\MeshAnimation\baby_buffalo_anim.psa",
            Materials = new[] { "Assets/Resources/Data/SysTextures/LineageMonstersTex2/Materials/baby_buffalo_t00.mat" }
        },
        new PetMesh
        {
            Package = "LineageMonsters2",
            Stem = "baby_ostrich_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\SkeletalMesh\baby_ostrich_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\MeshAnimation\Baby_Ostrich_anim.psa",
            Materials = new[]
            {
                "Assets/Resources/Data/SysTextures/LineageMonstersTex2/Materials/baby_ostrich_t00.mat",
                "Assets/Resources/Data/SysTextures/LineageMonstersTex2/Materials/baby_ostrich_t01.mat"
            }
        },
        new PetMesh
        {
            Package = "LineageMonsters2",
            Stem = "baby_tiger_m00",
            Psk = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\SkeletalMesh\baby_tiger_m00.psk",
            Psa = @"C:\Users\hh-soft\Pictures\test_umodel\unpack\monster_anim\LineageMonsters2\MeshAnimation\baby_tiger_anim.psa",
            Materials = new[] { "Assets/Resources/Data/SysTextures/LineageMonstersTex2/Materials/baby_tiger_t00.mat" }
        }
    };

    [MenuItem("L2/Pets/Build Remaining Pets")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        int built = 0;
        for (int i = 0; i < Pets.Length; i++)
        {
            PetMesh pet = Pets[i];
            string folder = "Assets/Resources/Data/Animations/" + pet.Package + "/" + pet.Stem;
            string prefab = folder + "/" + pet.Stem + ".prefab";
            string wait = folder + "/" + pet.Stem + ".ao_Wait.anim";
            if (File.Exists(Abs(wait)) && File.Exists(Abs(prefab)))
            {
                UnityEngine.Debug.Log("[PetBatch] skip " + pet.Stem);
                continue;
            }

            if (!File.Exists(Abs(folder + "/" + pet.Stem + ".fbx")))
            {
                UnityEngine.Debug.LogError("[PetBatch] FBX missing " + pet.Stem);
                continue;
            }

            Build(pet);
            built++;
        }

        UnityEngine.Debug.Log("[PetBatch] built " + built);
    }

    static void Build(PetMesh pet)
    {
        string folder = "Assets/Resources/Data/Animations/" + pet.Package + "/" + pet.Stem;
        string fbx = folder + "/" + pet.Stem + ".fbx";
        string prefab = folder + "/" + pet.Stem + ".prefab";

        AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (src == null)
        {
            UnityEngine.Debug.LogError("[PetBatch] cannot load " + fbx);
            return;
        }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
        inst.name = pet.Stem;
        int entityLayer = LayerMask.NameToLayer("Entity");
        if (entityLayer >= 0)
            SetLayer(inst, entityLayer);
        AssignMaterials(inst, pet.Materials);

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

        PrefabUtility.SaveAsPrefabAsset(inst, prefab);
        Object.DestroyImmediate(inst);
        int written = WriteClipFiles(pet, fbx);
        StripAnimationsFromFbx(pet, fbx);
        L2MonsterPrefabBuilder.WireSingle(prefab);
        FitCapsule(prefab);
        UnityEngine.Debug.Log("[PetBatch] ready " + pet.Stem + " clips=" + written);
    }

    static void AssignMaterials(GameObject inst, string[] materialPaths)
    {
        var mats = new Material[materialPaths.Length];
        for (int i = 0; i < materialPaths.Length; i++)
            mats[i] = AssetDatabase.LoadAssetAtPath<Material>(materialPaths[i]);

        SkinnedMeshRenderer[] renderers = inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Material[] slots = renderers[r].sharedMaterials;
            if (slots == null || slots.Length == 0)
                continue;
            var next = new Material[slots.Length];
            var names = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                next[i] = i < mats.Length && mats[i] != null ? mats[i] : slots[i];
                names[i] = next[i] != null ? next[i].name : "null";
            }

            renderers[r].sharedMaterials = next;
            UnityEngine.Debug.Log("[PetBatch] slots " + inst.name + " " + string.Join(",", names));
        }
    }

    static int WriteClipFiles(PetMesh pet, string fbxPath)
    {
        ModelImporter importer = UnityEditor.AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
            return 0;

        ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
        if (takes == null || takes.Length == 0)
        {
            UnityEngine.Debug.LogError("[PetBatch] no takes " + pet.Stem);
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

        string folder = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        int written = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip src = assets[i] as AnimationClip;
            if (src == null || src.name.StartsWith("__preview__"))
                continue;
            string safe = src.name.Replace("|", "_").Replace("/", "_").Replace("\\", "_");
            string path = folder + "/" + pet.Stem + "." + safe + ".anim";
            if (File.Exists(Abs(path)))
                AssetDatabase.DeleteAsset(path);
            AnimationClip copy = Object.Instantiate(src);
            copy.name = pet.Stem + "." + safe;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = ShouldLoop(safe);
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
            written++;
        }

        AssetDatabase.SaveAssets();
        return written;
    }

    static void StripAnimationsFromFbx(PetMesh pet, string fbxPath)
    {
        string project = Directory.GetCurrentDirectory();
        string blender = @"C:\Users\hh-soft\Pictures\test_umodel\blemder_portable\blender-3.6.16-windows-x64\blender.exe";
        string script = Path.Combine(project, "Assets/Editor/L2EffectGenerator/Blender/psk_psa_to_fbx.py");
        string addon = Path.Combine(project, "Assets/Editor/L2EffectGenerator/Blender/io_import_scene_unreal_psa_psk_280.py");
        string fbx = Path.Combine(project, fbxPath);
        string reference = Path.Combine(project, "Assets/Resources/Data/Animations/LineageMonsters/wolf_m00/wolf.fbx");
        var start = new ProcessStartInfo();
        start.FileName = blender;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.Arguments =
            "-b -P \"" + script + "\" -- \"" + pet.Psk + "\" \"" + pet.Psa + "\" \"" + fbx + "\" \"" + addon + "\" \"" + reference + "\" mesh";
        using (Process process = Process.Start(start))
        {
            if (process != null)
                process.WaitForExit();
        }

        ModelImporter importer = UnityEditor.AssetImporter.GetAtPath(fbxPath) as ModelImporter;
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
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool has = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] == null)
                    continue;
                if (!has)
                {
                    bounds = skins[i].bounds;
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(skins[i].bounds);
                }
            }

            if (!has)
                return;

            Vector3 center = root.transform.InverseTransformPoint(bounds.center);
            float height = Mathf.Max(0.25f, bounds.size.y);
            float radius = Mathf.Max(0.08f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.55f);
            if (radius > height * 0.5f)
                radius = height * 0.5f;

            CharacterController body = root.GetComponent<CharacterController>();
            if (body != null)
            {
                body.height = height;
                body.radius = radius;
                body.center = new Vector3(0f, center.y, 0f);
            }

            Transform click = root.transform.Find("click_area");
            if (click != null)
            {
                click.localPosition = new Vector3(0f, center.y, 0f);
                click.localScale = new Vector3(
                    Mathf.Max(0.3f, bounds.size.x),
                    Mathf.Max(0.3f, bounds.size.y),
                    Mathf.Max(0.3f, bounds.size.z));
            }

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
