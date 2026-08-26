#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// After LineageWeapons FBX import, bake loadable prefabs like the handmade swords:
/// mesh + URP Lit on the root, Sword_Tip / Sword_Base children. Does not overwrite existing prefabs.
/// </summary>
public class L2WeaponPrefabBuilder : AssetPostprocessor
{
    const string ModelsFolder = "Assets/Resources/Data/Animations/LineageWeapons/Models";
    const string PrefabFolder = "Assets/Resources/Data/Animations/LineageWeapons";
    const string TexFolder = "Assets/Resources/Data/SysTextures/LineageWeaponsTex";
    const string MatFolder = TexFolder + "/Materials";
    const string TemplateMat = MatFolder + "/small_sword_t00_wp.mat";
    static readonly string[] TexMapPaths =
    {
        "Assets/Scripts/Tools/Terrain/Blender/sword_tex_map.txt",
        "Assets/Scripts/Tools/Terrain/Blender/weapon_tex_map.txt",
    };

    static bool _queued;

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            string path = importedAssets[i].Replace('\\', '/');
            if (!path.StartsWith(ModelsFolder) || !path.EndsWith(".fbx"))
                continue;
            QueueBuild();
            return;
        }
    }

    [MenuItem("L2/Weapons/Build Missing Weapon Prefabs")]
    public static void BuildAllMenu()
    {
        int n = BuildMissingPrefabs();
        Debug.Log("[L2WeaponPrefabBuilder] built " + n + " prefabs");
    }

    public static void BuildAll()
    {
        BuildMissingPrefabs();
    }

    static void QueueBuild()
    {
        if (_queued)
            return;
        _queued = true;
        EditorApplication.delayCall += () =>
        {
            _queued = false;
            BuildMissingPrefabs();
        };
    }

    public static int BuildMissingPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(ModelsFolder) || !AssetDatabase.IsValidFolder(PrefabFolder))
            return 0;

        Dictionary<string, string> texMap = LoadTexMap();
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder });
        int built = 0;
        for (int i = 0; i < fbxGuids.Length; i++)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
            if (!fbxPath.EndsWith(".fbx"))
                continue;
            string name = Path.GetFileNameWithoutExtension(fbxPath);
            string prefabPath = PrefabFolder + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                continue;
            if (TryBuildPrefab(fbxPath, prefabPath, name, texMap))
                built++;
        }

        if (built > 0)
            AssetDatabase.SaveAssets();
        return built;
    }

    static bool TryBuildPrefab(
        string fbxPath,
        string prefabPath,
        string name,
        Dictionary<string, string> texMap)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (model == null)
        {
            Debug.LogWarning("[L2WeaponPrefabBuilder] no model at " + fbxPath);
            return false;
        }

        GameObject src = Object.Instantiate(model);
        src.name = name;
        MeshFilter srcFilter = src.GetComponentInChildren<MeshFilter>();
        if (srcFilter == null || srcFilter.sharedMesh == null)
        {
            Object.DestroyImmediate(src);
            Debug.LogWarning("[L2WeaponPrefabBuilder] no mesh in " + fbxPath);
            return false;
        }

        GameObject go = new GameObject(name);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = srcFilter.sharedMesh;
        mr.sharedMaterial = GetOrCreateMaterial(name, texMap);

        Transform srcMeshXf = srcFilter.transform;
        Transform tip = CopyOrCreateMarker(go.transform, src.transform, srcMeshXf, "Sword_Tip", true);
        Transform bladeBase = CopyOrCreateMarker(go.transform, src.transform, srcMeshXf, "Sword_Base", false);
        EnsureTipIsBladeEnd(tip, bladeBase);

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(src);
        Debug.Log("[L2WeaponPrefabBuilder] " + prefabPath);
        return true;
    }

    static Transform CopyOrCreateMarker(
        Transform destRoot,
        Transform srcRoot,
        Transform srcMeshXf,
        string markerName,
        bool isTip)
    {
        Transform existing = FindNamed(srcRoot, markerName);
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(destRoot, false);
        if (existing != null)
        {
            marker.transform.localPosition = srcMeshXf.InverseTransformPoint(existing.position);
            return marker.transform;
        }

        MeshFilter mf = destRoot.GetComponent<MeshFilter>();
        Mesh mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null)
            return marker.transform;

        Bounds b = mesh.bounds;
        Vector3 size = b.size;
        Vector3 min;
        Vector3 max;
        if (size.x >= size.y && size.x >= size.z)
        {
            min = new Vector3(b.min.x, b.center.y, b.center.z);
            max = new Vector3(b.max.x, b.center.y, b.center.z);
        }
        else if (size.z >= size.x && size.z >= size.y)
        {
            min = new Vector3(b.center.x, b.center.y, b.min.z);
            max = new Vector3(b.center.x, b.center.y, b.max.z);
        }
        else
        {
            min = new Vector3(b.center.x, b.min.y, b.center.z);
            max = new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // Handmade swords: Sword_Tip = more negative X (blade) after FBX import.
        marker.transform.localPosition = isTip ? min : max;
        return marker.transform;
    }

    static void EnsureTipIsBladeEnd(Transform tip, Transform bladeBase)
    {
        if (tip == null || bladeBase == null)
            return;
        Vector3 t = tip.localPosition;
        Vector3 b = bladeBase.localPosition;
        Vector3 delta = t - b;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
        {
            if (t.x > b.x)
            {
                tip.localPosition = b;
                bladeBase.localPosition = t;
            }
            return;
        }

        if (t.sqrMagnitude < b.sqrMagnitude)
        {
            tip.localPosition = b;
            bladeBase.localPosition = t;
        }
    }

    static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    static Material GetOrCreateMaterial(string meshName, Dictionary<string, string> texMap)
    {
        string texStem = null;
        if (texMap != null)
            texMap.TryGetValue(meshName, out texStem);
        if (string.IsNullOrEmpty(texStem))
            texStem = meshName.Replace("_m00_wp", "_t00_wp").Replace("_m00", "_t00");

        string matPath = MatFolder + "/" + texStem + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder(TexFolder, "Materials");

        Material template = AssetDatabase.LoadAssetAtPath<Material>(TemplateMat);
        Material mat = template != null
            ? new Material(template)
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = texStem;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder + "/" + texStem + ".png");
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    static Dictionary<string, string> LoadTexMap()
    {
        var map = new Dictionary<string, string>();
        for (int i = 0; i < TexMapPaths.Length; i++)
        {
            string full = Path.GetFullPath(TexMapPaths[i]);
            if (!File.Exists(full))
                continue;
            foreach (string raw in File.ReadAllLines(full))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                string[] parts = line.Split('\t');
                if (parts.Length < 2)
                    continue;
                map[parts[0].Trim()] = parts[1].Trim();
            }
        }
        return map;
    }
}
#endif
