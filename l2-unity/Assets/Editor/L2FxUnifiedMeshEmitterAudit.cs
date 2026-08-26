using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class L2FxUnifiedMeshEmitterAudit
{
    const string UnifiedShaderName = "L2/Effects/MeshEmitter";

    static readonly string[] TargetMaterialPaths =
    {
        "Assets/Resources/Data/Effects/drop_item/e_u056_a/e_u056_a_ta/MeshEmitter6.mat",
        "Assets/Resources/Data/Effects/drop_item/e_u056_a/e_u056_a_ta/MeshEmitter6_cbui25.mat",
        "Assets/Resources/Data/Effects/drop_item/e_u056_a/e_u056_a_ta/MeshEmitter7.mat",
        "Assets/Resources/Data/Effects/it_healing_potion/it_healing_potion_ta/MeshEmitter0.mat",
        "Assets/Resources/Data/Effects/it_healing_potion/it_healing_potion_ta/MeshEmitter2.mat",
        "Assets/Resources/Data/Effects/it_quick_step_potion/it_quick_step_potion_ta/MeshEmitter0.mat",
        "Assets/Resources/Data/Effects/it_quick_step_potion/it_quick_step_potion_ta/MeshEmitter2.mat",
        "Assets/Resources/Data/Effects/it_power_striker/it_power_striker_ta/MeshEmitter2.mat",
        "Assets/Resources/Data/Effects/it_power_striker/it_power_striker_ta/MeshEmitter5.mat",
        "Assets/Resources/Data/Effects/it_teleport_v1/it_teleport_v1_ca/MeshEmitter0.mat",
        "Assets/Resources/Data/Effects/it_teleport_v1/it_teleport_v1_ca/MeshEmitter1.mat",
        "Assets/Resources/Data/Effects/it_teleport_v1/it_teleport_v1_ca/MeshEmitter2.mat",
        "Assets/Resources/Data/Effects/shot_atk_simple/shot_atk_simple_ta/MeshEmitter0.mat",
        "Assets/Resources/Data/Effects/shot_atk_simple/shot_atk_simple_ta/MeshEmitter1.mat",
        "Assets/Resources/Data/Effects/shot_N_atk_v1/shot_N_atk_v1_ta/MeshEmitter225.mat",
        "Assets/Resources/Data/Effects/shot_N_atk_v1/shot_N_atk_v1_ta/MeshEmitter226.mat"
    };

    [MenuItem("L2/Effects/Audit Unified MeshEmitter Materials")]
    public static void AuditFromMenu()
    {
        Audit(true);
    }

    public static bool Audit(bool logSuccess)
    {
        var errors = new List<string>();
        Shader unifiedShader = Shader.Find(UnifiedShaderName);
        if (unifiedShader == null || !unifiedShader.isSupported)
            errors.Add($"Shader '{UnifiedShaderName}' is missing or unsupported.");

        foreach (string path in TargetMaterialPaths)
            ValidateMaterial(path, errors);

        if (errors.Count > 0)
        {
            Debug.LogError(
                $"[UnifiedMeshEmitterAudit] FAILED ({errors.Count})\n" +
                string.Join("\n", errors));
            return false;
        }

        if (logSuccess)
            Debug.Log($"[UnifiedMeshEmitterAudit] PASS ({TargetMaterialPaths.Length} materials)");
        return true;
    }

    static void ValidateMaterial(string path, List<string> errors)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            errors.Add($"{path}: material is missing.");
            return;
        }

        if (material.shader == null || material.shader.name != UnifiedShaderName)
            errors.Add($"{path}: expected shader '{UnifiedShaderName}'.");

        if (!material.HasProperty("_MainTex") || material.GetTexture("_MainTex") == null)
            errors.Add($"{path}: _MainTex is missing.");

        ValidateIntegerRange(material, path, "_SpawnMode", 0, 3, errors);
        ValidateIntegerRange(material, path, "_MotionMode", 0, 2, errors);
        ValidateIntegerRange(material, path, "_TransformMode", 0, 1, errors);
        ValidateIntegerRange(material, path, "_SizeMode", 0, 3, errors);
        ValidateIntegerRange(material, path, "_SrcBlend", 0, 10, errors);
        ValidateIntegerRange(material, path, "_DstBlend", 0, 10, errors);
        ValidateIntegerRange(material, path, "_ZWrite", 0, 1, errors);
        ValidateIntegerRange(material, path, "_Cull", 0, 2, errors);

        // Deferred URP skips unlit Geometry/UniversalForward (mesh looks untextured).
        if (material.renderQueue < (int)UnityEngine.Rendering.RenderQueue.Transparent)
            errors.Add($"{path}: effective render queue must be Transparent (not Geometry).");

        bool expandsBounds = material.GetFloat("_ExpandShaderBounds") > 0.5f;
        if (expandsBounds == material.enableInstancing)
        {
            errors.Add(
                $"{path}: expanded ParticleSingle materials must disable instancing; " +
                "ParticleGroup materials must enable it.");
        }
    }

    static void ValidateIntegerRange(
        Material material,
        string path,
        string property,
        int min,
        int max,
        List<string> errors)
    {
        if (!material.HasProperty(property))
        {
            errors.Add($"{path}: {property} is missing.");
            return;
        }

        float value = material.GetFloat(property);
        int integer = Mathf.RoundToInt(value);
        if (!Mathf.Approximately(value, integer) || integer < min || integer > max)
            errors.Add($"{path}: {property}={value} is outside [{min}, {max}].");
    }
}
