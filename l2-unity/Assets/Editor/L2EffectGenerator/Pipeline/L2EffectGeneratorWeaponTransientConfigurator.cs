#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorWeaponTransientConfigurator
{
    public static bool TryApply(
        GameObject prefabRoot,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        IReadOnlyList<UcEmitterDefinition> emitters,
        string label,
        out string logLine)
    {
        logLine = null;
        if (prefabRoot == null)
        {
            return false;
        }

        string className = planned != null ? planned.ClassName : prefabRoot.name;
        if (!WeaponTransientChargeBinder.MatchesChargeEffectName(className))
        {
            return false;
        }

        if (emitters == null || emitters.Count < WeaponTransientChargeBinder.MaxTravelingEmitters)
        {
            logLine = label + ": WeaponTransient skipped (native emitter indices 0 and 1 are missing)";
            return false;
        }

        WeaponTransientChargeBinder binder =
            prefabRoot.GetComponent<WeaponTransientChargeBinder>();
        if (binder == null)
        {
            binder = prefabRoot.AddComponent<WeaponTransientChargeBinder>();
        }

        SerializedObject serialized = new SerializedObject(binder);
        SerializedProperty travelingEmitters = serialized.FindProperty("_travelingEmitters");
        travelingEmitters.arraySize = WeaponTransientChargeBinder.MaxTravelingEmitters;
        for (int i = 0; i < WeaponTransientChargeBinder.MaxTravelingEmitters; i++)
        {
            ParticleGroupV2 group = FindDirectGroup(
                prefabRoot.transform,
                emitters[i] != null ? emitters[i].EmitterName : null);
            if (group == null)
            {
                logLine = label + ": WeaponTransient skipped (emitter index " + i + " is missing)";
                Object.DestroyImmediate(binder, true);
                return false;
            }

            travelingEmitters.GetArrayElementAtIndex(i).objectReferenceValue = group;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ForceAuthoredEmitterTiming(prefabRoot);

        logLine =
            label +
            ": WeaponTransientChargeBinder (explicit emitter indices 0,1; authored timing)";
        EditorUtility.SetDirty(prefabRoot);
        return true;
    }

    static void ForceAuthoredEmitterTiming(GameObject prefabRoot)
    {
        ParticleGroupV2[] groups =
            prefabRoot.GetComponentsInChildren<ParticleGroupV2>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            SerializedObject group = new SerializedObject(groups[i]);
            SerializedProperty fixedDuration = group.FindProperty("_hasFixedDuration");
            SerializedProperty stretch = group.FindProperty("_stretchParticleLifeToWindow");
            if (fixedDuration != null)
            {
                fixedDuration.boolValue = true;
            }
            if (stretch != null)
            {
                stretch.boolValue = false;
            }
            group.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static ParticleGroupV2 FindDirectGroup(Transform root, string emitterName)
    {
        if (root == null || string.IsNullOrEmpty(emitterName))
        {
            return null;
        }

        Transform child = root.Find(emitterName);
        return child != null ? child.GetComponent<ParticleGroupV2>() : null;
    }
}
#endif
