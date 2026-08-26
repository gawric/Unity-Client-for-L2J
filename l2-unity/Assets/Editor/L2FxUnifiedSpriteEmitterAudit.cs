using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class L2FxUnifiedSpriteEmitterAudit
{
    const string UnifiedShaderName = "L2/Effects/SpriteEmitter";
    const string ParticleSinglePath =
        "Assets/Resources/Data/Effects/it_power_striker/it_power_striker_ta/SpriteEmitter2.mat";

    static readonly string[] TargetMaterialPaths =
    {
        "Assets/Resources/Data/Effects/drop_item/e_u056_a/e_u056_a_ta/SpriteEmitter11.mat",
        "Assets/Resources/Data/Effects/drop_item/e_u056_a/e_u056_a_ta/SpriteEmitter12.mat",
        "Assets/Resources/Data/Effects/drop_item/e_u056_b/e_u056_b_ta/SpriteEmitter2.mat",
        "Assets/Resources/Data/Effects/drop_item/e_u056_b/e_u056_b_ta/SpriteEmitter6.mat",
        "Assets/Resources/Data/Effects/it_healing_potion/it_healing_potion_ta/SpriteEmitter7.mat",
        "Assets/Resources/Data/Effects/it_quick_step_potion/it_quick_step_potion_ta/SpriteEmitter7.mat",
        "Assets/Resources/Data/Effects/it_power_striker/it_power_striker_ta/SpriteEmitter2.mat",
        "Assets/Resources/Data/Effects/it_teleport_v1/it_teleport_v1_ca/SpriteEmitter2.mat",
        "Assets/Resources/Data/Effects/shot_atk_simple/shot_atk_simple_ta/SpriteEmitter2.mat",
        "Assets/Resources/Data/Effects/shot_N_atk_v1/shot_N_atk_v1_ta/SpriteEmitter324.mat",
        "Assets/Resources/Data/Effects/shot_N_atk_v1/shot_N_atk_v1_ta/SpriteEmitter325.mat",
        "Assets/Resources/Data/Effects/shot_N_atk_v1/shot_N_atk_v1_ta/SpriteEmitter326.mat"
    };

    [MenuItem("L2/Effects/Audit Unified SpriteEmitter Materials")]
    public static void AuditFromMenu()
    {
        Audit(true);
    }

    public static bool Audit(bool logSuccess)
    {
        var errors = new List<string>();
        Shader shader = Shader.Find(UnifiedShaderName);
        if (shader == null || !shader.isSupported)
            errors.Add($"Shader '{UnifiedShaderName}' is missing or unsupported.");

        foreach (string path in TargetMaterialPaths)
            ValidateMaterial(path, errors);

        if (errors.Count > 0)
        {
            Debug.LogError(
                $"[UnifiedSpriteEmitterAudit] FAILED ({errors.Count})\n" +
                string.Join("\n", errors));
            return false;
        }

        if (logSuccess)
            Debug.Log($"[UnifiedSpriteEmitterAudit] PASS ({TargetMaterialPaths.Length} materials)");
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
        ValidateIntegerRange(material, path, "_FullTlsShape", 0, 1, errors);
        ValidateIntegerRange(material, path, "_MotionMode", 0, 2, errors);
        ValidateIntegerRange(material, path, "_OrientationMode", 0, 1, errors);
        ValidateIntegerRange(material, path, "_PtvdMode", 0, 2, errors);
        ValidateIntegerRange(material, path, "_SizeMode", 0, 1, errors);
        ValidateIntegerRange(material, path, "_SpinMode", 0, 1, errors);
        ValidateIntegerRange(material, path, "_FlipbookMode", 0, 3, errors);
        ValidateIntegerRange(material, path, "_SrcBlend", 0, 10, errors);
        ValidateIntegerRange(material, path, "_DstBlend", 0, 10, errors);
        ValidateIntegerRange(material, path, "_ZWrite", 0, 1, errors);
        ValidateIntegerRange(material, path, "_Cull", 0, 2, errors);

        if (material.GetFloat("_TextureUSubdivisions") < 1f ||
            material.GetFloat("_TextureVSubdivisions") < 1f)
            errors.Add($"{path}: atlas subdivisions must be positive.");
        if (material.GetVector("_LifetimeRange").y <= 0f)
            errors.Add($"{path}: lifetime range is invalid.");
        if (material.renderQueue < (int)UnityEngine.Rendering.RenderQueue.Transparent)
            errors.Add($"{path}: effective render queue must be Transparent.");

        bool expectsInstancing = path != ParticleSinglePath;
        if (material.enableInstancing != expectsInstancing)
            errors.Add($"{path}: enableInstancing must be {expectsInstancing}.");
        if (material.GetTag("L2FxGpuInstancing", false, string.Empty) != "On")
            errors.Add($"{path}: L2FxGpuInstancing shader tag is missing.");
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
