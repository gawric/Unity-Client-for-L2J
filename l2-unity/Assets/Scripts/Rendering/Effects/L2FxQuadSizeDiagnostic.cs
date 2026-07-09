using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// Reproduces sprite vertex-shader quad size in C# for calibration.
/// finalWidth(m) = quadSpan * [ SizeRange(UU) * 0.01 * effectScale * spriteScale * sizeScalePeak ] * objectScale
/// </summary>
public static class L2FxQuadSizeDiagnostic
{
    private const string MightTaToken = "wh_might_ta";
    private const string HealingPotionTaToken = "it_healing_potion_ta";
    // m_u004_b.uc actor DrawScale — reference for calibration, not read from material yet.
    private const float UcDrawScale = 0.05f;

    public static bool ShouldTrace(string groupName, L2Particle owner, Transform groupTransform)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return false;
        }

        bool isTargetGroup =
            groupName.IndexOf("SpriteEmitter0", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            groupName.IndexOf("SpriteEmitter2", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (!isTargetGroup)
        {
            return false;
        }

        return MatchesEffectToken(groupName, owner, groupTransform, MightTaToken)
            || MatchesEffectToken(groupName, owner, groupTransform, HealingPotionTaToken);
    }

    public static void Log(string groupName, Renderer renderer, float now)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] mats = renderer.sharedMaterials;
        Material mat = (mats != null && mats.Length > 0) ? mats[0] : null;
        if (mat == null || !mat.HasProperty("_SizeRange"))
        {
            return;
        }

        Vector4 sizeRange = mat.GetVector("_SizeRange");
        bool uniform = !mat.HasProperty("_UniformSize") || mat.GetFloat("_UniformSize") > 0.5f;
        float sizeMidUU = (sizeRange.x + sizeRange.y) * 0.5f;

        const float uuToUnity = 0.01f;
        float effectScale = SafePositiveProperty(mat, "_L2FxEffectScale");
        float spriteScale = SafePositiveProperty(mat, "_L2FxSpriteScale");
        float worldCalibration = SafePositiveProperty(mat, "_L2FxWorldCalibration");

        Transform quadTransform = renderer.transform;
        Vector3 localScale = quadTransform.localScale;
        Vector3 lossyScale = quadTransform.lossyScale;
        float parentRuntimeMul = localScale.x > 1e-6f ? lossyScale.x / localScale.x : lossyScale.x;

        Vector3 quadSpan = Vector3.one;
        MeshFilter mf = renderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            quadSpan = mf.sharedMesh.bounds.size;
        }

        float drawScale = mat.HasProperty("_L2FxDrawScale") ? mat.GetFloat("_L2FxDrawScale") : UcDrawScale;
        const float sizeScalePeak = 1f;

        // Safe mode: StartSize * 0.01 * K_world * lossyScale(DrawScale on root).
        float ucOnlyPeakM = sizeMidUU * uuToUnity * effectScale * sizeScalePeak * quadSpan.x;
        float ucWithDrawScaleM = ucOnlyPeakM * drawScale;
        float ucWithWorldCalM = ucWithDrawScaleM * worldCalibration;

        float baseSizeMidM = sizeMidUU * uuToUnity * effectScale * spriteScale * worldCalibration;
        float peakBaseM = baseSizeMidM * sizeScalePeak;
        float finalWidthM = quadSpan.x * peakBaseM * lossyScale.x;
        float finalHeightM = quadSpan.y * peakBaseM * lossyScale.y;
        float totalMulWidth = uuToUnity * effectScale * spriteScale * worldCalibration * sizeScalePeak * quadSpan.x * lossyScale.x;
        float manualTuningMul = spriteScale * lossyScale.x;

        Debug.Log(
            $"[MIGHT_TA_QUAD_SIZE] group='{groupName}' mat='{mat.name}' now={now:F3}s\n" +
            $"  UC proportion : _SizeRange(UU)=({sizeRange.x:F3}..{sizeRange.y:F3}) uniform={uniform} midUU={sizeMidUU:F3} sizeScalePeak={sizeScalePeak:F3}\n" +
            $"  UC-only (m)   : peak={ucOnlyPeakM:F5}  x DrawScale({drawScale:F3}) x K_world={ucWithWorldCalM:F4}\n" +
            $"  K_world         : {worldCalibration:F3}  (global UC->Unity calibration)\n" +
            $"  manual tuning : spriteScale={spriteScale:F3} quadLocalScale=({localScale.x:F3},{localScale.y:F3}) parentRuntimeMul={parentRuntimeMul:F3} lossyScale=({lossyScale.x:F3},{lossyScale.y:F3})\n" +
            $"  manualTuningMul : spriteScale*lossyScale = {manualTuningMul:F5}\n" +
            $"  FINAL quad(m) : width~={finalWidthM:F4} height~={finalHeightM:F4}  (= midUU * totalMul)\n" +
            $"  totalMul      : {totalMulWidth:F6}  (0.01 * effectScale * spriteScale * worldCal * sizeScalePeak * quadSpan * lossyScale)\n" +
            $"  frame={Time.frameCount}.");
    }

    private static bool MatchesEffectToken(string groupName, L2Particle owner, Transform groupTransform, string token)
    {
        if (!string.IsNullOrEmpty(groupName) &&
            groupName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (owner != null && !string.IsNullOrEmpty(owner.name) &&
            owner.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        Transform t = groupTransform;
        for (int depth = 0; t != null && depth < 16; depth++, t = t.parent)
        {
            if (!string.IsNullOrEmpty(t.name) &&
                t.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static float SafePositiveProperty(Material mat, string prop)
    {
        if (mat == null || !mat.HasProperty(prop))
        {
            return 1f;
        }

        float v = mat.GetFloat(prop);
        return v > 0f ? v : 1f;
    }

    private static float ComputeSizeScalePeak(Material mat)
    {
        if (mat == null || !mat.HasProperty("_UseSizeScale") || mat.GetFloat("_UseSizeScale") <= 0.5f)
        {
            return 1f;
        }

        string[] vals = { "_SizeScaleVal0", "_SizeScaleVal1", "_SizeScaleVal2", "_SizeScaleVal3", "_SizeScaleVal4" };
        int count = mat.HasProperty("_SizeScaleCount") ? Mathf.RoundToInt(mat.GetFloat("_SizeScaleCount")) : vals.Length;
        count = Mathf.Clamp(count, 1, vals.Length);

        float peak = 0f;
        for (int i = 0; i < count; i++)
        {
            if (mat.HasProperty(vals[i]))
            {
                peak = Mathf.Max(peak, mat.GetFloat(vals[i]));
            }
        }

        return peak > 0f ? peak : 1f;
    }
}
#endif
