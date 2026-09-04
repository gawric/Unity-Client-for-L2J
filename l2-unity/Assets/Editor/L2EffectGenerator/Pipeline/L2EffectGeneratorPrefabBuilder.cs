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
    private const string BeamStripMeshFolder =
        "Assets/Resources/Data/Shaders/Skills/Common/Decompile_Common/BeamEmitter_Reference";

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

        if (File.Exists(fullPrefabPath))
        {
            errorMessage = "prefab file exists on disk but Unity cannot import it: " + prefabPath;
            return false;
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
            GameObject prefabAsset =             PrefabUtility.SaveAsPrefabAsset(
                root,
                prefabPath,
                out saveSucceeded);

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

            reason = null;
            return false;
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
                out List<UcEmitterDefinition> emitters,
                out string parseError))
        {
            errorMessage = planned.Label + ": " + parseError;
            return false;
        }

        for (int skipIndex = emitters.Count - 1; skipIndex >= 0; skipIndex--)
        {
            UcEmitterDefinition skipEmitter = emitters[skipIndex];
            string skipName = skipEmitter != null ? skipEmitter.EmitterName : null;
            if (!L2EffectGeneratorAssetOverrides.ShouldSkipEmitter(planned.ClassName, skipName))
            {
                continue;
            }

            emitters.RemoveAt(skipIndex);
            logLines.Add(planned.Label + ": skipped emitter " + skipName);
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

            prefabRoot.transform.localScale = Vector3.one;

            int createdCount = 0;
            int updatedCount = 0;
            int createdMaterialCount = 0;
            var effectParts = new List<EffectPart>();

            for (int i = 0; i < emitters.Count; i++)
            {
                UcEmitterDefinition emitter = emitters[i];
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

                EffectPart effectPart = ConfigureEmitterPart(
                    owner, emitterObject, emitter, planned.UseCastWindow, planned.IsProjectile);
                Mesh slotMesh = ResolveSlotMesh(emitter);
                Material[] emitterMaterials = EnsureEmitterMaterials(
                    planned.AssetPath,
                    planned.ClassName,
                    planned.ExtendsClass,
                    emitter,
                    slotMesh,
                    out int materialsCreated,
                    out string materialConfiguration);
                createdMaterialCount += materialsCreated;

                int slotCount = EnsureParticleSlots(emitterObject.transform, emitter, emitterMaterials);
                EnsureBeamEmitterScripts(emitterObject, emitter, slotMesh);
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
                    " (" + emitter.ClassName + ", " +
                    "ParticleGroupV2" +
                    ", slots=" + slotCount + ", slotName=" + emitter.ParticleSlotName + materialAction +
                    ", " + materialConfiguration +
                    DescribeEssenceBind(emitter, slotMesh) +
                    ", delay=" + FormatUcDelay(emitter) +
                    ", cps=" + FormatUcCountPerSecond(emitter) +
                    ", duration=" + FormatUcDuration(emitter) + ")");
            }

            int prunedCount = PruneOrphanEmitterObjects(prefabRoot.transform, emitters);
            if (prunedCount > 0)
            {
                logLines.Add(planned.Label + ": pruned orphan emitters=" + prunedCount);
            }

            InitializeL2ParticleGroups(owner, effectParts);
            ApplyAlphaBlendDrawOrder(prefabRoot.transform, emitters);
            ApplyLocalDrawOrder(prefabRoot.transform, planned.ClassName, emitters);

            if (L2EffectGeneratorTrailProviderConfigurator.TryApplyHomeOrbTrailProvider(
                    prefabRoot,
                    planned,
                    emitters,
                    planned.Label,
                    out string trailProviderLogLine) &&
                !string.IsNullOrEmpty(trailProviderLogLine))
            {
                logLines.Add(trailProviderLogLine);
            }

            if (L2EffectGeneratorAddLocationProviderConfigurator.TryApply(
                    prefabRoot,
                    emitters,
                    planned.Label,
                    out string addLocationLogLine) ||
                !string.IsNullOrEmpty(addLocationLogLine))
            {
                if (!string.IsNullOrEmpty(addLocationLogLine))
                {
                    logLines.Add(addLocationLogLine);
                }
            }

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

    private static int PruneOrphanEmitterObjects(
        Transform prefabRoot,
        List<UcEmitterDefinition> emitters)
    {
        if (prefabRoot == null)
        {
            return 0;
        }

        var keepNames = new HashSet<string>(StringComparer.Ordinal);
        if (emitters != null)
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                if (emitters[i] != null && !string.IsNullOrEmpty(emitters[i].EmitterName))
                {
                    keepNames.Add(emitters[i].EmitterName);
                }
            }
        }

        int pruned = 0;
        for (int i = prefabRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = prefabRoot.GetChild(i);
            if (child == null || keepNames.Contains(child.name))
            {
                continue;
            }

            Object.DestroyImmediate(child.gameObject);
            pruned++;
        }

        return pruned;
    }

    private static EffectPart ConfigureEmitterPart(
        L2Particle owner,
        GameObject emitterObject,
        UcEmitterDefinition emitter,
        bool useCastWindow,
        bool isNSkillProjectile)
    {
        if (emitterObject == null || emitter == null)
        {
            return null;
        }

        ParticleSingle existingSingle = emitterObject.GetComponent<ParticleSingle>();
        if (existingSingle != null)
        {
            Object.DestroyImmediate(existingSingle);
        }

        ParticleGroup existingGroup = emitterObject.GetComponent<ParticleGroup>();
        if (existingGroup != null)
        {
            Object.DestroyImmediate(existingGroup);
        }

        ParticleStreamDriver existingDriver = emitterObject.GetComponent<ParticleStreamDriver>();
        if (existingDriver != null)
        {
            Object.DestroyImmediate(existingDriver);
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(emitterObject);

        ParticleGroupV2 group = emitterObject.GetComponent<ParticleGroupV2>();
        if (group == null)
        {
            group = emitterObject.AddComponent<ParticleGroupV2>();
        }

        InitializeEmitterPart(group, owner, emitter, useCastWindow, isNSkillProjectile);
        return group;
    }

    private static void InitializeEmitterPart(
        MonoBehaviour emitterPart,
        L2Particle owner,
        UcEmitterDefinition emitter,
        bool useCastWindow,
        bool isNSkillProjectile)
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

        int maxCount = Math.Max(1, emitter.MaxParticles);
        SerializedProperty maxCountProperty = serializedPart.FindProperty("_maxCount");
        if (maxCountProperty != null)
        {
            maxCountProperty.intValue = maxCount;
        }

        SetBoolIfPresent(serializedPart, "_cloneParticlesToMaxCount", maxCount > 1);
        SetBoolIfPresent(serializedPart, "_useGpuInstancing", maxCount > 1);
        SetIntIfPresent(
            serializedPart,
            "_coordinateSystem",
            (int)emitter.ResolveRuntimeCoordinateSystem());
        SetBoolIfPresent(serializedPart, "_forceContinuousSpawning", false);
        SetBoolIfPresent(serializedPart, "_preserveShaderTimeInContinuousLoop", false);
        SetBoolIfPresent(serializedPart, "_hasFixedDuration", !useCastWindow);
        SetBoolIfPresent(serializedPart, "_respawnDeadParticles", emitter.RespawnDeadParticles);
        // NSkillProjectile (home-flight m_u003_b, outbound _fl/_ra/_pr): actor
        // lives until FNMover / ProjectileManager destroys the GO. Short UC
        // lifetimes (SpriteEmitter5 0.01s @ 3000 pps) recycle for that trip.
        SetBoolIfPresent(serializedPart, "_hostOwnedEmission", isNSkillProjectile);
        SetBoolIfPresent(
            serializedPart,
            "_stretchParticleLifeToWindow",
            useCastWindow && emitter.MaxParticles <= 1 && !emitter.RespawnDeadParticles);

        bool burst = emitter.HasInitialParticlesPerSecond && emitter.InitialParticlesPerSecond >= 100;
        SetBoolIfPresent(serializedPart, "_isBurstSpawning", burst);

        if (emitter.HasRelativeWarmupTime)
        {
            SetFloatIfPresent(serializedPart, "_relativeWarmupTime", emitter.RelativeWarmupTime);
        }

        if (emitter.HasWarmupTicksPerSecond)
        {
            SetFloatIfPresent(serializedPart, "_warmupTicksPerSecond", emitter.WarmupTicksPerSecond);
        }

        SetBoolIfPresent(serializedPart, "_compareLogEnabled", true);

        if (emitter.HasInitialDelayRange)
        {
            SetFloatIfPresent(serializedPart, "_startDelay", emitter.InitialDelayMax);
        }

        if (emitter.HasInitialParticlesPerSecond)
        {
            SetIntIfPresent(serializedPart, "_countPerSecond", emitter.InitialParticlesPerSecond);
        }

        if (emitter.HasLifetimeRange || emitter.HasInferredLifetimeFromFades())
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

    private static float ResolveUcDuration(UcEmitterDefinition emitter)
    {
        emitter.ResolveLifetimeRange(out float lifetimeMin, out float lifetimeMax);
        float life = Math.Max(lifetimeMin, lifetimeMax);
        float delay = emitter.HasInitialDelayRange ? Math.Max(emitter.InitialDelayMin, emitter.InitialDelayMax) : 0f;
        return life + delay;
    }

    private static void SetBoolIfPresent(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
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

    private static void ApplyAlphaBlendDrawOrder(
        Transform prefabRoot,
        List<UcEmitterDefinition> emitters)
    {
        if (prefabRoot == null || emitters == null || emitters.Count < 2)
        {
            return;
        }

        bool anyPuff = false;
        bool anyCore = false;
        for (int i = 0; i < emitters.Count; i++)
        {
            if (IsAlphaBlendPuff(emitters[i]))
            {
                anyPuff = true;
            }
            else
            {
                anyCore = true;
            }
        }

        if (!anyPuff || !anyCore)
        {
            return;
        }

        for (int i = 0; i < emitters.Count; i++)
        {
            UcEmitterDefinition emitter = emitters[i];
            if (emitter == null || string.IsNullOrEmpty(emitter.EmitterName))
            {
                continue;
            }

            Transform child = FindDirectChild(prefabRoot, emitter.EmitterName);
            if (child == null)
            {
                continue;
            }

            int order = IsAlphaBlendPuff(emitter)
                ? L2FxAlphaBlendDrawOrder.PuffSortingOrder
                : L2FxAlphaBlendDrawOrder.CoreSortingOrder;
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null)
                {
                    renderers[r].sortingOrder = order;
                }
            }
        }
    }

    private static void ApplyLocalDrawOrder(
        Transform prefabRoot,
        string effectClassName,
        List<UcEmitterDefinition> emitters)
    {
        if (prefabRoot == null || emitters == null)
        {
            return;
        }

        for (int i = 0; i < emitters.Count; i++)
        {
            UcEmitterDefinition emitter = emitters[i];
            if (emitter == null ||
                !L2EffectGeneratorAssetOverrides.TryGetDrawOrder(
                    effectClassName, emitter, out var drawOrder))
            {
                continue;
            }

            Transform child = FindDirectChild(prefabRoot, emitter.EmitterName);
            if (child == null)
            {
                continue;
            }

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] == null)
                {
                    continue;
                }

                renderers[r].sortingOrder = drawOrder.SortingOrder;
                renderers[r].rendererPriority = drawOrder.SortingOrder;
            }
        }
    }

    private static bool IsAlphaBlendPuff(UcEmitterDefinition emitter)
    {
        if (emitter == null ||
            !string.Equals(emitter.ClassName, "SpriteEmitter", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(emitter.DrawStyle, "PTDS_AlphaBlend", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return emitter.BlendBetweenSubdivisions || emitter.AddLocationFromOtherEmitter >= 0;
    }

    private static Material[] EnsureEmitterMaterials(
        string effectAssetPath,
        string effectClassName,
        string extendsClass,
        UcEmitterDefinition emitter,
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
            emitter, slotMesh);
        bool isMesh = L2EffectGeneratorMaterialConfigurator.IsMeshEmitter(emitter.ClassName);
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
            if (material == null && File.Exists(ToAbsoluteAssetPath(materialPath)))
            {
                AssetDatabase.ImportAsset(materialPath, ImportAssetOptions.ForceSynchronousImport);
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }

            if (material == null)
            {
                if (File.Exists(ToAbsoluteAssetPath(materialPath)))
                {
                    AssetDatabase.DeleteAsset(materialPath);
                }

                material = new Material(shader)
                {
                    name = emitter.EmitterName + suffix
                };
                AssetDatabase.CreateAsset(material, materialPath);
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                createdCount++;
            }

            if (material == null)
            {
                status.Add("failed to create material " + materialPath);
                continue;
            }

            if (material.shader == null || material.shader.name != shader.name)
            {
                material.shader = shader;
            }
            Texture2D texture = textures.Count > 0
                ? textures[Math.Min(i, textures.Count - 1)]
                : null;
            status.Add(L2EffectGeneratorMaterialConfigurator.Configure(
                material,
                emitter,
                slotMesh,
                texture,
                effectClassName,
                extendsClass,
                i));
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
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
        UcEmitterDefinition emitter,
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
        UcEmitterDefinition emitter,
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

    private static void EnsureBeamEmitterScripts(
        GameObject emitterObject,
        UcEmitterDefinition emitter,
        Mesh stripMesh)
    {
        if (emitterObject == null ||
            emitter == null ||
            !string.Equals(emitter.ClassName, "BeamEmitter", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RemoveIncompatibleBeamBuilders(emitterObject);

        L2BeamStripMeshBuilder meshBuilder = emitterObject.GetComponent<L2BeamStripMeshBuilder>();
        if (meshBuilder == null)
        {
            meshBuilder = emitterObject.AddComponent<L2BeamStripMeshBuilder>();
        }

        int points = Math.Clamp(
            emitter.HighFrequencyPoints,
            2,
            L2BeamEmitterStripBuilder.MaxSegments + 1);

        ConfigureBeamStripHost(meshBuilder, points, stripMesh);
        meshBuilder.ApplyToFilters();

        MeshRenderer parentRenderer = emitterObject.GetComponent<MeshRenderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = false;
        }
    }

    private static void RemoveIncompatibleBeamBuilders(GameObject emitterObject)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(emitterObject);

        L2BeamEmitterStripBuilder leftoverStripBuilder =
            emitterObject.GetComponent<L2BeamEmitterStripBuilder>();
        if (leftoverStripBuilder != null)
        {
            Object.DestroyImmediate(leftoverStripBuilder);
        }

        L2MultiLayerBeamBuilder[] builders =
            emitterObject.GetComponentsInChildren<L2MultiLayerBeamBuilder>(true);
        for (int i = 0; i < builders.Length; i++)
        {
            if (builders[i] != null)
            {
                Object.DestroyImmediate(builders[i]);
            }
        }
    }

    private static void ConfigureBeamStripHost(
        MonoBehaviour host,
        int highFrequencyPoints,
        Mesh stripMesh)
    {
        if (host == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(host);
        SetIntIfPresent(serialized, "_highFrequencyPoints", highFrequencyPoints);
        SetFloatIfPresent(serialized, "_beamTextureUScale", 1f);
        SetFloatIfPresent(serialized, "_beamTextureVScale", 1f);
        SerializedProperty meshProperty = serialized.FindProperty("_stripMesh");
        if (meshProperty != null)
        {
            meshProperty.objectReferenceValue = stripMesh;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Mesh ResolveSlotMesh(UcEmitterDefinition emitter)
    {
        if (emitter != null &&
            string.Equals(emitter.ClassName, "BeamEmitter", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureBeamStripMesh(emitter.HighFrequencyPoints);
        }

        if (emitter != null &&
            L2EffectGeneratorMaterialConfigurator.IsMeshEmitter(emitter.ClassName) &&
            !string.IsNullOrWhiteSpace(emitter.StaticMeshReference))
        {
            Mesh staticMesh = TryResolveStaticMesh(emitter.StaticMeshReference);
            if (staticMesh != null)
            {
                return staticMesh;
            }

            Debug.LogWarning(
                "L2 Effect Generator: mesh missing for " + emitter.EmitterName +
                " (" + emitter.ClassName + ") ref=" + emitter.StaticMeshReference +
                " — slot would fall back to Quad and leave a geometry hole.");
        }

        return GetDefaultSlotMesh();
    }

    private static Mesh EnsureBeamStripMesh(int highFrequencyPoints)
    {
        int points = Math.Clamp(
            highFrequencyPoints,
            2,
            L2BeamEmitterStripBuilder.MaxSegments + 1);
        string meshPath = BeamStripMeshFolder + "/L2BeamEmitterStrip_HF" + points + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
        {
            return existing;
        }

        if (!AssetDatabase.IsValidFolder(BeamStripMeshFolder))
        {
            return L2BeamEmitterStripBuilder.Build(points, 1f, 1f, HideFlags.None);
        }

        Mesh mesh = L2BeamEmitterStripBuilder.Build(points, 1f, 1f, HideFlags.None);
        AssetDatabase.CreateAsset(mesh, meshPath);
        return AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    }

    private static string DescribeEssenceBind(UcEmitterDefinition emitter, Mesh slotMesh)
    {
        if (emitter == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (emitter.IsPolarShape)
        {
            parts.Add("polar");
        }
        else if (emitter.IsSphereShape)
        {
            parts.Add("sphere");
        }

        if (emitter.UseRevolution)
        {
            parts.Add("revolution");
        }

        if (string.Equals(emitter.UseDirectionAs, "PTDU_Forward", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("PTDU_Forward");
        }
        else if (string.Equals(emitter.UseDirectionAs, "PTDU_Normal", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("PTDU_Normal");
        }

        if (string.Equals(emitter.ClassName, "VertMeshEmitter", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(slotMesh != null ? "VertMesh=" + slotMesh.name : "VertMesh=MISSING");
        }

        if (emitter.HasRelativeWarmupTime || emitter.HasWarmupTicksPerSecond)
        {
            parts.Add("warmup");
        }

        int coordinateSystem = emitter.ResolveNativeCoordinateSystem();
        if (coordinateSystem != L2ParticleCoordinateSystemUtil.NativeRelative)
        {
            parts.Add("CS=" + coordinateSystem);
        }

        if (emitter.IndependentSprayAccel)
        {
            parts.Add("IndependentSprayAccel");
        }

        if (emitter.AddLocationFromOtherEmitter >= 0)
        {
            parts.Add("AddLocation=" + emitter.AddLocationFromOtherEmitter);
        }

        if (emitter.UseVelocityScale)
        {
            parts.Add("VelocityScale");
        }

        if (emitter.UseRevolutionScale)
        {
            parts.Add("RevolutionScale");
        }

        return parts.Count == 0 ? string.Empty : ", essence=" + string.Join("+", parts);
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

        Mesh mesh = LoadMeshFromProjectFile(meshFileName);
        if (mesh != null)
        {
            return mesh;
        }

        Mesh imported = L2EffectGeneratorViewerImport.TryImportMesh(staticMeshReference);
        if (imported != null)
        {
            return imported;
        }

        return LoadMeshFromProjectFile(meshFileName);
    }

    private static Mesh LoadMeshFromProjectFile(string meshFileName)
    {
        string[] paths =
        {
            LineageEffectsStaticMeshesFolder + "/" + meshFileName + ".asset",
            LineageEffectsStaticMeshesFolder + "/" + meshFileName + ".obj",
            LineageEffectsStaticMeshesFolder + "/" + meshFileName + ".fbx"
        };
        for (int p = 0; p < paths.Length; p++)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(paths[p]);
            if (mesh != null)
            {
                return mesh;
            }

            Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(paths[p]);
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i] is Mesh importedMesh)
                {
                    return importedMesh;
                }
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

    private static string ToAbsoluteAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(assetPath))
        {
            return assetPath;
        }

        string dataPath = Application.dataPath.Replace('\\', '/');
        string projectRoot = dataPath.EndsWith("/Assets", StringComparison.Ordinal)
            ? dataPath.Substring(0, dataPath.Length - "Assets".Length)
            : dataPath + "/";
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }

    private static string GetParticleSlotName(string slotBaseName, int slotIndex)
    {
        if (slotIndex <= 0)
        {
            return slotBaseName;
        }

        return slotBaseName + " (" + slotIndex + ")";
    }

    private static string FormatUcDelay(UcEmitterDefinition emitter)
    {
        return emitter != null && emitter.HasInitialDelayRange
            ? emitter.InitialDelayMax.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "-";
    }

    private static string FormatUcCountPerSecond(UcEmitterDefinition emitter)
    {
        return emitter != null && emitter.HasInitialParticlesPerSecond
            ? emitter.InitialParticlesPerSecond.ToString()
            : "-";
    }

    private static string FormatUcDuration(UcEmitterDefinition emitter)
    {
        if (emitter == null || (!emitter.HasLifetimeRange && !emitter.HasInferredLifetimeFromFades()))
        {
            return "-";
        }

        return ResolveUcDuration(emitter).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
