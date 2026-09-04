#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorAddLocationProviderConfigurator
{
    public static bool TryApply(
        GameObject prefabRoot,
        IReadOnlyList<UcEmitterDefinition> emitters,
        string label,
        out string logLine)
    {
        logLine = null;
        if (prefabRoot == null || emitters == null || emitters.Count == 0)
        {
            return false;
        }

        var bindings = new List<AddLocationBinding>(4);
        for (int i = 0; i < emitters.Count; i++)
        {
            UcEmitterDefinition tail = emitters[i];
            if (tail == null ||
                L2EffectGeneratorMaterialConfigurator.IsBeamEmitter(tail.ClassName) ||
                tail.AddLocationFromOtherEmitter < 0 ||
                tail.AddLocationFromOtherEmitter >= emitters.Count)
            {
                continue;
            }

            UcEmitterDefinition source = emitters[tail.AddLocationFromOtherEmitter];
            ParticleGroupV2 tailGroup = FindGroup(prefabRoot.transform, tail.EmitterName);
            ParticleGroupV2 sourceGroup = source != null
                ? FindGroup(prefabRoot.transform, source.EmitterName)
                : null;
            if (tailGroup == null || sourceGroup == null)
            {
                logLine = label +
                          ": AddLocation skipped for " +
                          (tail.EmitterName ?? "?") +
                          " -> index " + tail.AddLocationFromOtherEmitter;
                continue;
            }

            bindings.Add(new AddLocationBinding
            {
                Tail = tailGroup,
                Source = sourceGroup,
                RevolutionsPerSecondZ = source != null
                    ? Midpoint(source.RevolutionsPerSecondRange.Z)
                    : 0f
            });
        }

        AddLocationFromOtherEmitterProvider provider =
            prefabRoot.GetComponent<AddLocationFromOtherEmitterProvider>();
        if (bindings.Count == 0)
        {
            if (provider != null)
            {
                Object.DestroyImmediate(provider, true);
            }

            return false;
        }

        if (provider == null)
        {
            provider = prefabRoot.AddComponent<AddLocationFromOtherEmitterProvider>();
        }

        SerializedObject serializedProvider = new SerializedObject(provider);
        SerializedProperty bindingsProperty = serializedProvider.FindProperty("_bindings");
        if (bindingsProperty == null)
        {
            logLine = label + ": AddLocation provider _bindings missing";
            return false;
        }

        bindingsProperty.ClearArray();
        for (int i = 0; i < bindings.Count; i++)
        {
            bindingsProperty.InsertArrayElementAtIndex(i);
            SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("tailEmitter").objectReferenceValue = bindings[i].Tail;
            binding.FindPropertyRelative("sourceEmitter").objectReferenceValue = bindings[i].Source;
            binding.FindPropertyRelative("revolutionsPerSecondZ").floatValue =
                bindings[i].RevolutionsPerSecondZ;
        }

        serializedProvider.ApplyModifiedPropertiesWithoutUndo();

        logLine = label +
                  ": AddLocationFromOtherEmitterProvider bindings=" +
                  bindings.Count;
        return true;
    }

    static ParticleGroupV2 FindGroup(Transform parent, string childName)
    {
        Transform child = FindDirectChild(parent, childName);
        return child != null ? child.GetComponent<ParticleGroupV2>() : null;
    }

    static Transform FindDirectChild(Transform parent, string childName)
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

    static float Midpoint(UcRange range)
    {
        return (range.Min + range.Max) * 0.5f;
    }

    struct AddLocationBinding
    {
        public ParticleGroupV2 Tail;
        public ParticleGroupV2 Source;
        public float RevolutionsPerSecondZ;
    }
}
#endif
