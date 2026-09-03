#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class L2EffectGeneratorTrailProviderConfigurator
{
    public static bool TryApplyHomeOrbTrailProvider(
        GameObject prefabRoot,
        L2EffectGeneratorFolderBuilder.PlannedFolder planned,
        IReadOnlyList<UcEmitterDefinition> emitters,
        string label,
        out string logLine)
    {
        logLine = null;
        if (prefabRoot == null || planned == null)
        {
            return false;
        }

        bool isHomeOrb = planned.IsHomeFlight ||
                         L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned);
        if (!isHomeOrb)
        {
            return false;
        }

        if (!L2EffectGeneratorHomeOrbLayout.TryResolve(
                emitters,
                out UcEmitterDefinition trailEmitter,
                out UcEmitterDefinition coreEmitter))
        {
            logLine = label + ": trail provider skipped (need Independent trail + local core sprites)";
            return false;
        }

        Transform tailRoot = FindDirectChild(prefabRoot.transform, trailEmitter.EmitterName);
        Transform velocitySource = FindDirectChild(prefabRoot.transform, coreEmitter.EmitterName);
        if (tailRoot == null || velocitySource == null)
        {
            logLine = label +
                      ": trail provider skipped (missing " +
                      trailEmitter.EmitterName + "/" + coreEmitter.EmitterName + ")";
            return false;
        }

        HomeProjectileTrailVelocityProvider provider =
            prefabRoot.GetComponent<HomeProjectileTrailVelocityProvider>();
        if (provider == null)
        {
            provider = prefabRoot.AddComponent<HomeProjectileTrailVelocityProvider>();
        }

        float trailLifetime = L2EffectGeneratorHomeOrbLayout.ResolveLifetime(trailEmitter);
        if (L2EffectGeneratorAssetOverrides.IsM_u003_bHomeFlight(planned))
        {
            trailLifetime = L2EffectGeneratorAssetOverrides.M_u003_bLockedTrailHistorySeconds;
        }

        float sparkSizeMeters = trailEmitter.StartSizeRange.X.Min * L2EffectGeneratorHomeOrbLayout.UnrealUnitsToMeters;
        if (sparkSizeMeters < 0.001f)
        {
            sparkSizeMeters = L2EffectGeneratorAssetOverrides.M_u003_bLockedTrailSparkSizeMeters;
        }

        SerializedObject serializedProvider = new SerializedObject(provider);
        SerializedProperty bindingsProperty = serializedProvider.FindProperty("_bindings");
        if (bindingsProperty == null)
        {
            logLine = label + ": trail provider _bindings missing";
            return false;
        }

        bindingsProperty.ClearArray();
        bindingsProperty.InsertArrayElementAtIndex(0);
        SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(0);

        binding.FindPropertyRelative("name").stringValue = "trail";
        binding.FindPropertyRelative("tailRoot").objectReferenceValue = tailRoot;
        binding.FindPropertyRelative("velocitySource").objectReferenceValue = velocitySource;
        binding.FindPropertyRelative("velocitySourceName").stringValue = coreEmitter.EmitterName;
        binding.FindPropertyRelative("targetRenderers").ClearArray();
        binding.FindPropertyRelative("autoCollectChildren").boolValue = true;
        binding.FindPropertyRelative("followSourcePosition").boolValue = true;
        binding.FindPropertyRelative("placeRenderersOnHistory").boolValue = true;
        binding.FindPropertyRelative("historySeconds").floatValue = trailLifetime;
        binding.FindPropertyRelative("headLagPercent").floatValue =
            L2EffectGeneratorAssetOverrides.M_u003_bLockedTrailHeadLagPercent;
        binding.FindPropertyRelative("useCylinderSpread").boolValue = false;
        binding.FindPropertyRelative("cylinderRadiusHead").floatValue = 0.008f;
        binding.FindPropertyRelative("cylinderRadiusTail").floatValue = sparkSizeMeters;
        binding.FindPropertyRelative("cylinderRadiusPower").floatValue = 1.6f;
        binding.FindPropertyRelative("scaleOverTrail").boolValue = false;
        binding.FindPropertyRelative("particleScaleHead").floatValue = 1f;
        binding.FindPropertyRelative("particleScaleTail").floatValue = 1f;
        binding.FindPropertyRelative("particleScalePower").floatValue = 1.4f;
        binding.FindPropertyRelative("alongTrailPower").floatValue = 0.75f;
        binding.FindPropertyRelative("placeByParticleAge").boolValue = true;
        binding.FindPropertyRelative("trailTravelSeconds").floatValue = trailLifetime;
        binding.FindPropertyRelative("fadeAlphaOverTrail").boolValue = true;
        binding.FindPropertyRelative("trailFadeHead").floatValue = 1f;
        binding.FindPropertyRelative("trailFadeTail").floatValue = 0f;
        binding.FindPropertyRelative("trailFadePower").floatValue = 1.25f;
        binding.FindPropertyRelative("onlyActiveRenderers").boolValue = false;
        binding.FindPropertyRelative("convertWorldToLocal").boolValue = true;
        binding.FindPropertyRelative("localSpaceReference").objectReferenceValue = tailRoot;
        binding.FindPropertyRelative("velocityScale").floatValue = 0f;
        binding.FindPropertyRelative("rangeSpread").floatValue = 0.2f;
        binding.FindPropertyRelative("invertTrailDirection").boolValue = true;
        binding.FindPropertyRelative("trailSign").floatValue = 1f;

        SetFloatIfPresent(serializedProvider, "_minimumAxisRange", 0.002f);
        SetFloatIfPresent(serializedProvider, "_smoothing", 18f);
        SetBoolIfPresent(serializedProvider, "_debugLogs", false);
        serializedProvider.ApplyModifiedPropertiesWithoutUndo();

        logLine = label +
                  ": HomeProjectileTrailVelocityProvider " +
                  trailEmitter.EmitterName + " (Independent trail life=" + trailLifetime.ToString("0.###") +
                  "s) + " + coreEmitter.EmitterName + " (local core)";
        return true;
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

    static void SetFloatIfPresent(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    static void SetBoolIfPresent(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }
}
#endif
