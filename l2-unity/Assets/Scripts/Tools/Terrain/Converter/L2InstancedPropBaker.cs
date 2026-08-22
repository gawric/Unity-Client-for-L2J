#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class L2InstancedPropBaker
{
    [MenuItem("L2/Map/Bake 17_25 DecoLayer To Instances")]
    public static void BakeSeventeenTwentyFiveDeco()
    {
        BakeDecoPrefab(
            "Assets/Resources/Data/Maps/17_25/17_25_DecoLayer.prefab",
            "Assets/Resources/Data/Maps/17_25/17_25_DecoInstances.asset");
    }

    [MenuItem("L2/Map/Bake l2_lobby DecoLayer To Instances")]
    public static void BakeLobbyDeco()
    {
        BakeDecoPrefab(
            "Assets/Resources/Data/Maps/l2_lobby/DecoLayer.prefab",
            "Assets/Resources/Data/Maps/l2_lobby/l2_lobby_DecoInstances.asset");
    }

    public static void BakeDecoPrefab(
        string prefabPath,
        string setPath,
        float maxDrawDistance = 30f,
        int layer = -1)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("L2InstancedPropBaker: missing " + prefabPath);
            return;
        }

        if (layer < 0)
        {
            layer = LayerMask.NameToLayer("Deco");
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = prefab.name;
        L2InstancedPropSet set = ReplaceHierarchyWithInstances(
            instance,
            setPath,
            castShadows: false,
            maxDrawDistance: maxDrawDistance,
            layer: layer);

        if (set == null)
        {
            Object.DestroyImmediate(instance);
            return;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "L2InstancedPropBaker: " +
            prefabPath +
            " is now instanced (" +
            set.instanceCount +
            " props, " +
            set.batches.Length +
            " batches).");
    }

    public static L2InstancedPropSet ReplaceHierarchyWithInstances(
        GameObject root,
        string assetPath,
        bool castShadows,
        float maxDrawDistance,
        int layer)
    {
        L2InstancedPropSet set = BakeFromRoot(root, assetPath, castShadows);
        if (set == null)
        {
            return null;
        }

        Transform rootTransform = root.transform;
        KeepRuntimeOnlyChildren(rootTransform);
        for (int i = rootTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = rootTransform.GetChild(i);
            if (child.name == "KeptLights")
            {
                continue;
            }

            Object.DestroyImmediate(child.gameObject);
        }

        MeshFilter[] leftoverFilters = root.GetComponents<MeshFilter>();
        for (int i = 0; i < leftoverFilters.Length; i++)
        {
            Object.DestroyImmediate(leftoverFilters[i]);
        }

        MeshRenderer[] leftoverRenderers = root.GetComponents<MeshRenderer>();
        for (int i = 0; i < leftoverRenderers.Length; i++)
        {
            Object.DestroyImmediate(leftoverRenderers[i]);
        }

        L2InstancedPropRenderer renderer = root.GetComponent<L2InstancedPropRenderer>();
        if (renderer == null)
        {
            renderer = root.AddComponent<L2InstancedPropRenderer>();
        }

        renderer.PropSet = set;
        renderer.MaxDrawDistance = maxDrawDistance;
        renderer.Layer = layer >= 0 ? layer : 0;
        return set;
    }

    private static void KeepRuntimeOnlyChildren(Transform root)
    {
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        AudioSource[] audio = root.GetComponentsInChildren<AudioSource>(true);
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        if (lights.Length == 0 && audio.Length == 0 && particles.Length == 0)
        {
            return;
        }

        Transform keep = root.Find("KeptLights");
        if (keep == null)
        {
            keep = new GameObject("KeptLights").transform;
            keep.SetParent(root, false);
        }

        ReparentKeepers(lights, keep);
        ReparentKeepers(audio, keep);
        ReparentKeepers(particles, keep);
    }

    private static void ReparentKeepers(Component[] components, Transform keep)
    {
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                continue;
            }

            Transform t = components[i].transform;
            t.SetParent(keep, true);
            MeshFilter filter = t.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = t.GetComponent<MeshRenderer>();
            if (filter != null)
            {
                Object.DestroyImmediate(filter);
            }

            if (meshRenderer != null)
            {
                Object.DestroyImmediate(meshRenderer);
            }
        }
    }

    public static L2InstancedPropSet BakeFromRoot(GameObject root, string assetPath, bool castShadows)
    {
        if (root == null)
        {
            Debug.LogError("L2InstancedPropBaker: root is null");
            return null;
        }

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        var groups = new Dictionary<long, List<Matrix4x4>>();
        var meshes = new Dictionary<long, Mesh>();
        var materials = new Dictionary<long, Material>();
        Matrix4x4 rootInverse = root.transform.worldToLocalMatrix;
        int skipped = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer meshRenderer = renderers[i];
            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || meshRenderer.sharedMaterial == null)
            {
                skipped++;
                continue;
            }

            if (meshRenderer.gameObject == root)
            {
                continue;
            }

            Mesh mesh = filter.sharedMesh;
            Material material = meshRenderer.sharedMaterial;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            long key = ((long)mesh.GetInstanceID() << 32) ^ (uint)material.GetInstanceID();
            if (!groups.TryGetValue(key, out List<Matrix4x4> matrices))
            {
                matrices = new List<Matrix4x4>(256);
                groups.Add(key, matrices);
                meshes.Add(key, mesh);
                materials.Add(key, material);
            }

            matrices.Add(rootInverse * meshRenderer.transform.localToWorldMatrix);
        }

        if (groups.Count == 0)
        {
            Debug.LogError("L2InstancedPropBaker: no mesh renderers under " + root.name);
            return null;
        }

        var batches = new L2InstancedPropBatch[groups.Count];
        int batchIndex = 0;
        int total = 0;
        foreach (var pair in groups)
        {
            batches[batchIndex++] = new L2InstancedPropBatch
            {
                mesh = meshes[pair.Key],
                material = materials[pair.Key],
                matrices = pair.Value.ToArray(),
                castShadows = castShadows
            };
            total += pair.Value.Count;
        }

        L2InstancedPropSet set = AssetDatabase.LoadAssetAtPath<L2InstancedPropSet>(assetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<L2InstancedPropSet>();
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");
            AssetDatabase.CreateAsset(set, assetPath);
        }

        set.batches = batches;
        set.instanceCount = total;
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "L2InstancedPropBaker: baked " +
            total +
            " instances / " +
            batches.Length +
            " batches → " +
            assetPath +
            (skipped > 0 ? " (skipped " + skipped + ")" : string.Empty));
        return set;
    }
}
#endif
