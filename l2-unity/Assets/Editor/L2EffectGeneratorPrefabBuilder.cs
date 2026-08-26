#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class L2EffectGeneratorPrefabBuilder
{
    private const string LineageEffectsStaticMeshesFolder =
        "Assets/Resources/Data/StaticMeshes/LineageEffectsStaticmeshes";

    public static string GetPrefabAssetPath(L2EffectGeneratorFolderBuilder.PlannedFolder planned)
    {
        return planned.AssetPath + "/" + planned.FolderName + ".prefab";
    }

    public static bool TryCreateRootPrefab(
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        out string logLine,
        out string errorMessage)
    {
        logLine = null;
        errorMessage = null;

        if (planned == null)
        {
            errorMessage = "Planned folder entry is missing.";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(planned.AssetPath))
        {
            errorMessage = "Target folder does not exist: " + planned.AssetPath;
            return false;
        }

        string prefabPath = GetPrefabAssetPath(planned);
        string fullPrefabPath = GetFullProjectPath(prefabPath);
        string fullMetaPath = fullPrefabPath + ".meta";

        AssetDatabase.Refresh();

        if (TryGetExistingPrefabAsset(prefabPath, fullPrefabPath, out string existingReason))
        {
            logLine = planned.Label + ": " + existingReason + " -> " + prefabPath;
            return true;
        }

        TryRemoveOrphanPrefabMeta(prefabPath, fullPrefabPath, fullMetaPath);

        GameObject root = null;
        try
        {
            root = new GameObject(planned.FolderName);
            root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            L2Particle particle = root.AddComponent<L2Particle>();
            InitializeDefaultL2Particle(particle);

            bool saveSucceeded;
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(
                root,
                prefabPath,
                out saveSucceeded);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!saveSucceeded || prefabAsset == null)
            {
                errorMessage = BuildSaveFailureMessage(prefabPath, fullPrefabPath);
                return false;
            }

            logLine = planned.Label + ": prefab created -> " + prefabPath;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = "Failed to save prefab " + prefabPath + ": " + exception.Message;
            return false;
        }
        finally
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static bool TryGetExistingPrefabAsset(
        string prefabPath,
        string fullPrefabPath,
        out string reason)
    {
        Object mainAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);
        if (mainAsset != null)
        {
            reason = "prefab already exists";
            return true;
        }

        if (File.Exists(fullPrefabPath))
        {
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            mainAsset = AssetDatabase.LoadMainAssetAtPath(prefabPath);
            if (mainAsset != null)
            {
                reason = "prefab already exists";
                return true;
            }

            reason = "prefab file already exists";
            return true;
        }

        reason = null;
        return false;
    }

    private static void TryRemoveOrphanPrefabMeta(
        string prefabPath,
        string fullPrefabPath,
        string fullMetaPath)
    {
        if (File.Exists(fullPrefabPath) || !File.Exists(fullMetaPath))
        {
            return;
        }

        if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath)))
        {
            File.Delete(fullMetaPath);
            AssetDatabase.Refresh();
        }
    }

    private static string GetFullProjectPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }

    private static void InitializeDefaultL2Particle(L2Particle particle)
    {
        if (particle == null)
        {
            return;
        }

        SerializedObject serializedParticle = new SerializedObject(particle);
        SerializedProperty pooledEffectProperty = serializedParticle.FindProperty("_pooledEffect");
        if (pooledEffectProperty != null)
        {
            SetFloatIfPresent(pooledEffectProperty, "_effectDurationSec", 15f);
            SetFloatIfPresent(pooledEffectProperty, "_maximumInactiveTimeSec", 60f);
        }

        SerializedProperty particleGroupsProperty = serializedParticle.FindProperty("_particleGroups");
        if (particleGroupsProperty != null && particleGroupsProperty.isArray)
        {
            particleGroupsProperty.arraySize = 0;
        }

        serializedParticle.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloatIfPresent(SerializedProperty parent, string relativePropertyName, float value)
    {
        SerializedProperty property = parent.FindPropertyRelative(relativePropertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static string BuildSaveFailureMessage(string prefabPath, string fullPrefabPath)
    {
        if (!Directory.Exists(Path.GetDirectoryName(fullPrefabPath)))
        {
            return "Failed to save prefab because target directory is missing on disk: " + prefabPath;
        }

        return "Failed to save prefab: " + prefabPath +
               ". Check Console for importer or script errors on L2Particle.";
    }

    public static bool TryPopulateEmitterObjects(
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        out List<string> logLines,
        out string errorMessage)
    {
        logLines = new List<string>();
        errorMessage = null;

        if (planned == null)
        {
            errorMessage = "Planned folder entry is missing.";
            return false;
        }

        if (!L2EffectUcEmitterParser.TryParseFile(
                planned.SourceUcPath,
                out List<L2EffectUcEmitterParser.UcEmitterDefinition> emitters,
                out string parseError))
        {
            errorMessage = planned.Label + ": " + parseError;
            return false;
        }

        string prefabPath = GetPrefabAssetPath(planned);
        if (AssetDatabase.LoadMainAssetAtPath(prefabPath) == null)
        {
            errorMessage = planned.Label + ": prefab is missing, cannot add emitters -> " + prefabPath;
            return false;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            errorMessage = planned.Label + ": failed to load prefab contents -> " + prefabPath;
            return false;
        }

        try
        {
            L2Particle owner = prefabRoot.GetComponent<L2Particle>();
            if (owner == null)
            {
                errorMessage = planned.Label + ": prefab root is missing L2Particle -> " + prefabPath;
                return false;
            }

            int createdCount = 0;
            int updatedCount = 0;
            int createdMaterialCount = 0;
            var effectParts = new List<EffectPart>();

            for (int i = 0; i < emitters.Count; i++)
            {
                L2EffectUcEmitterParser.UcEmitterDefinition emitter = emitters[i];
                Transform existingTransform = FindDirectChild(prefabRoot.transform, emitter.EmitterName);
                GameObject emitterObject;
                bool wasCreated = false;

                if (existingTransform != null)
                {
                    emitterObject = existingTransform.gameObject;
                    updatedCount++;
                }
                else
                {
                    emitterObject = new GameObject(emitter.EmitterName);
                    emitterObject.transform.SetParent(prefabRoot.transform, false);
                    emitterObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    emitterObject.transform.localScale = Vector3.one;
                    createdCount++;
                    wasCreated = true;
                }

                EffectPart effectPart = ConfigureEmitterPart(owner, emitterObject, emitter);
                Mesh slotMesh = ResolveSlotMesh(emitter);
                Material[] emitterMaterials = EnsureEmitterMaterials(
                    planned.AssetPath,
                    emitter,
                    slotMesh,
                    out int materialsCreated,
                    out string materialConfiguration);
                createdMaterialCount += materialsCreated;

                int slotCount = EnsureParticleSlots(emitterObject.transform, emitter, emitterMaterials);
                AssignParticleRenderers(effectPart, emitterObject.transform);
                if (effectPart != null)
                {
                    effectParts.Add(effectPart);
                }

                string action = wasCreated ? "emitter created" : "emitter updated";
                string materialAction = materialsCreated > 0
                    ? ", materials created=" + materialsCreated
                    : emitterMaterials != null && emitterMaterials.Length > 0
                        ? ", materials reused"
                        : ", material missing";
                logLines.Add(
                    planned.Label + ": " + action + " -> " + emitter.EmitterName +
                    " (" + emitter.ClassName + ", " + DescribeEmitterPart(emitter.MaxParticles) +
                    ", slots=" + slotCount + ", slotName=" + emitter.ParticleSlotName + materialAction +
                    ", " + materialConfiguration +
                    ", delay=" + FormatUcDelay(emitter) +
                    ", cps=" + FormatUcCountPerSecond(emitter) +
                    ", duration=" + FormatUcDuration(emitter) + ")");
            }

            InitializeL2ParticleGroups(owner, effectParts);

            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            logLines.Insert(
                0,
                planned.Label + ": emitters in prefab created=" + createdCount +
                " updated=" + updatedCount + ", materials created=" + createdMaterialCount);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = planned.Label + ": failed to populate emitters in " + prefabPath + ": " + exception.Message;
            return false;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static EffectPart ConfigureEmitterPart(
        L2Particle owner,
        GameObject emitterObject,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (emitterObject == null || emitter == null)
        {
            return null;
        }

        if (emitter.MaxParticles <= 1)
        {
            ParticleGroup existingGroup = emitterObject.GetComponent<ParticleGroup>();
            if (existingGroup != null)
            {
                Object.DestroyImmediate(existingGroup);
            }

            ParticleSingle single = emitterObject.GetComponent<ParticleSingle>();
            if (single == null)
            {
                single = emitterObject.AddComponent<ParticleSingle>();
            }

            InitializeEmitterPart(single, owner, emitter);
            return single;
        }

        ParticleSingle existingSingle = emitterObject.GetComponent<ParticleSingle>();
        if (existingSingle != null)
        {
            Object.DestroyImmediate(existingSingle);
        }

        ParticleGroup group = emitterObject.GetComponent<ParticleGroup>();
        if (group == null)
        {
            group = emitterObject.AddComponent<ParticleGroup>();
        }

        InitializeEmitterPart(group, owner, emitter);
        return group;
    }

    private static void InitializeEmitterPart(
        MonoBehaviour emitterPart,
        L2Particle owner,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (emitterPart == null || emitter == null)
        {
            return;
        }

        SerializedObject serializedPart = new SerializedObject(emitterPart);
        SerializedProperty ownerProperty = serializedPart.FindProperty("_owner");
        if (ownerProperty != null)
        {
            ownerProperty.objectReferenceValue = owner;
        }

        SerializedProperty maxCountProperty = serializedPart.FindProperty("_maxCount");
        if (maxCountProperty != null)
        {
            maxCountProperty.intValue = Math.Max(1, emitter.MaxParticles);
        }

        if (emitterPart is ParticleGroup && emitter.MaxParticles > 1)
        {
            SerializedProperty cloneProperty = serializedPart.FindProperty("_cloneParticlesToMaxCount");
            if (cloneProperty != null)
            {
                cloneProperty.boolValue = true;
            }

            SerializedProperty gpuProperty = serializedPart.FindProperty("_useGpuInstancing");
            if (gpuProperty != null)
            {
                gpuProperty.boolValue = true;
            }

            if (string.Equals(emitter.ClassName, "MeshEmitter", StringComparison.OrdinalIgnoreCase) &&
                emitter.HasInitialParticlesPerSecond &&
                emitter.InitialParticlesPerSecond >= 100)
            {
                SerializedProperty burstProperty = serializedPart.FindProperty("_isBurstSpawning");
                if (burstProperty != null)
                {
                    burstProperty.boolValue = true;
                }
            }
        }

        if (emitter.HasInitialDelayRange)
        {
            SetFloatIfPresent(serializedPart, "_startDelay", emitter.InitialDelayMax);
        }

        if (emitter.HasInitialParticlesPerSecond)
        {
            SetIntIfPresent(serializedPart, "_countPerSecond", emitter.InitialParticlesPerSecond);
        }

        if (emitter.HasLifetimeRange)
        {
            SetFloatIfPresent(serializedPart, "_duration", ResolveUcDuration(emitter));
        }

        SerializedProperty particlesProperty = serializedPart.FindProperty("_particles");
        if (particlesProperty != null && particlesProperty.isArray)
        {
            particlesProperty.arraySize = 0;
        }

        serializedPart.ApplyModifiedPropertiesWithoutUndo();
    }

    private static float ResolveUcDuration(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        float life = Math.Max(emitter.LifetimeMin, emitter.LifetimeMax);
        float delay = emitter.HasInitialDelayRange ? Math.Max(emitter.InitialDelayMin, emitter.InitialDelayMax) : 0f;
        return life + delay;
    }

    private static void SetFloatIfPresent(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetIntIfPresent(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void AssignParticleRenderers(MonoBehaviour emitterPart, Transform emitterTransform)
    {
        if (emitterPart == null || emitterTransform == null)
        {
            return;
        }

        SerializedObject serializedPart = new SerializedObject(emitterPart);
        SerializedProperty particlesProperty = serializedPart.FindProperty("_particles");
        if (particlesProperty == null || !particlesProperty.isArray)
        {
            return;
        }

        particlesProperty.arraySize = emitterTransform.childCount;
        for (int i = 0; i < emitterTransform.childCount; i++)
        {
            Renderer renderer = emitterTransform.GetChild(i).GetComponent<Renderer>();
            particlesProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
        }

        serializedPart.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void InitializeL2ParticleGroups(L2Particle owner, List<EffectPart> effectParts)
    {
        if (owner == null)
        {
            return;
        }

        SerializedObject serializedOwner = new SerializedObject(owner);
        SerializedProperty groupsProperty = serializedOwner.FindProperty("_particleGroups");
        if (groupsProperty == null || !groupsProperty.isArray)
        {
            return;
        }

        groupsProperty.arraySize = effectParts.Count;
        for (int i = 0; i < effectParts.Count; i++)
        {
            groupsProperty.GetArrayElementAtIndex(i).objectReferenceValue = effectParts[i];
        }

        serializedOwner.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material[] EnsureEmitterMaterials(
        string effectAssetPath,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter,
        Mesh slotMesh,
        out int createdCount,
        out string configuration)
    {
        createdCount = 0;
        configuration = "material configuration skipped";
        if (string.IsNullOrWhiteSpace(effectAssetPath) || emitter == null ||
            string.IsNullOrWhiteSpace(emitter.EmitterName))
        {
            return null;
        }

        Shader shader = ResolveEmitterShader(emitter.ClassName);
        if (shader == null)
        {
            Debug.LogWarning(
                "L2 Effect Generator: failed to resolve shader for " + emitter.EmitterName +
                " (" + emitter.ClassName + ").");
            return null;
        }

        List<Texture2D> textures = L2EffectGeneratorMaterialConfigurator.ResolveTextures(
            emitter.TextureReference, slotMesh);
        bool isMesh = string.Equals(
            emitter.ClassName, "MeshEmitter", StringComparison.OrdinalIgnoreCase);
        int materialCount = isMesh && slotMesh != null
            ? Math.Max(1, slotMesh.subMeshCount)
            : 1;
        var materials = new Material[materialCount];
        var status = new List<string>();
        for (int i = 0; i < materialCount; i++)
        {
            string suffix = i == 0 ? string.Empty : "_sub" + i;
            string materialPath = effectAssetPath + "/" + emitter.EmitterName + suffix + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = emitter.EmitterName + suffix
                };
                AssetDatabase.CreateAsset(material, materialPath);
                createdCount++;
            }

            Texture2D texture = textures.Count > 0
                ? textures[Math.Min(i, textures.Count - 1)]
                : null;
            status.Add(L2EffectGeneratorMaterialConfigurator.Configure(
                material, emitter, slotMesh, texture));
            materials[i] = material;
        }

        configuration = string.Join("; ", status);
        return materials;
    }

    private static Shader ResolveEmitterShader(string className)
    {
        return L2EffectGeneratorMaterialConfigurator.ResolveShader(className);
    }

    private static int EnsureParticleSlots(
        Transform emitterTransform,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter,
        Material[] emitterMaterials)
    {
        if (emitterTransform == null || emitter == null)
        {
            return 0;
        }

        int targetCount = Math.Max(1, emitter.MaxParticles);
        string slotBaseName = string.IsNullOrWhiteSpace(emitter.ParticleSlotName)
            ? emitter.EmitterName
            : emitter.ParticleSlotName;

        while (emitterTransform.childCount > targetCount)
        {
            Transform lastChild = emitterTransform.GetChild(emitterTransform.childCount - 1);
            Object.DestroyImmediate(lastChild.gameObject);
        }

        int createdSlots = 0;
        for (int slotIndex = emitterTransform.childCount; slotIndex < targetCount; slotIndex++)
        {
            GameObject slotObject = new GameObject(GetParticleSlotName(slotBaseName, slotIndex));
            slotObject.transform.SetParent(emitterTransform, false);
            slotObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            slotObject.transform.localScale = Vector3.one;
            EnsureSlotRenderComponents(slotObject, emitter, emitterMaterials);
            createdSlots++;
        }

        for (int slotIndex = 0; slotIndex < emitterTransform.childCount; slotIndex++)
        {
            Transform slotTransform = emitterTransform.GetChild(slotIndex);
            slotTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            slotTransform.localScale = Vector3.one;
            EnsureSlotRenderComponents(slotTransform.gameObject, emitter, emitterMaterials);
        }

        return emitterTransform.childCount;
    }

    private static void EnsureSlotRenderComponents(
        GameObject slotObject,
        L2EffectUcEmitterParser.UcEmitterDefinition emitter,
        Material[] emitterMaterials)
    {
        if (slotObject == null)
        {
            return;
        }

        MeshFilter meshFilter = slotObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = slotObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = slotObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = slotObject.AddComponent<MeshRenderer>();
        }

        Mesh slotMesh = ResolveSlotMesh(emitter);
        if (slotMesh != null)
        {
            meshFilter.sharedMesh = slotMesh;
        }

        if (emitterMaterials != null && emitterMaterials.Length > 0)
        {
            int materialCount = slotMesh != null ? Math.Max(1, slotMesh.subMeshCount) : 1;
            var materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = emitterMaterials[Math.Min(i, emitterMaterials.Length - 1)];
            }
            meshRenderer.sharedMaterials = materials;
        }
    }

    private static Mesh ResolveSlotMesh(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (emitter != null &&
            string.Equals(emitter.ClassName, "MeshEmitter", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(emitter.StaticMeshReference))
        {
            Mesh staticMesh = TryResolveStaticMesh(emitter.StaticMeshReference);
            if (staticMesh != null)
            {
                return staticMesh;
            }
        }

        return GetDefaultSlotMesh();
    }

    private static Mesh TryResolveStaticMesh(string staticMeshReference)
    {
        int lastDotIndex = staticMeshReference.LastIndexOf('.');
        string meshFileName = lastDotIndex >= 0
            ? staticMeshReference.Substring(lastDotIndex + 1)
            : staticMeshReference;

        if (string.IsNullOrWhiteSpace(meshFileName))
        {
            return null;
        }

        string fbxAssetPath = LineageEffectsStaticMeshesFolder + "/" + meshFileName + ".fbx";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxAssetPath);
        if (mesh != null)
        {
            return mesh;
        }

        Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(fbxAssetPath);
        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (importedAssets[i] is Mesh importedMesh)
            {
                return importedMesh;
            }
        }

        return null;
    }

    private static Mesh _defaultSlotMesh;

    private static Mesh GetDefaultSlotMesh()
    {
        if (_defaultSlotMesh == null)
        {
            _defaultSlotMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        }

        return _defaultSlotMesh;
    }

    private static string GetParticleSlotName(string slotBaseName, int slotIndex)
    {
        if (slotIndex <= 0)
        {
            return slotBaseName;
        }

        return slotBaseName + " (" + slotIndex + ")";
    }

    private static string DescribeEmitterPart(int maxParticles)
    {
        return maxParticles <= 1 ? "ParticleSingle" : "ParticleGroup";
    }

    private static string FormatUcDelay(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        return emitter != null && emitter.HasInitialDelayRange
            ? emitter.InitialDelayMax.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "-";
    }

    private static string FormatUcCountPerSecond(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        return emitter != null && emitter.HasInitialParticlesPerSecond
            ? emitter.InitialParticlesPerSecond.ToString()
            : "-";
    }

    private static string FormatUcDuration(L2EffectUcEmitterParser.UcEmitterDefinition emitter)
    {
        if (emitter == null || !emitter.HasLifetimeRange)
        {
            return "-";
        }

        return ResolveUcDuration(emitter).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
