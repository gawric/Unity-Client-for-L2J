using UnityEngine;

/// <summary>
/// L2 <c>AEmitter::AdjustparticleLife(HitTime)</c> / <c>SetParticleLifeTimeRange</c>.
/// delta = targetLife - authoredMaxLife; LifetimeRange += delta; FadeOutStart += delta.
/// Keeps the authored fade-out tail length while the particle lives until HitTime.
/// SizeScale RelativeTime plateaus then look like a mid-life hold until FadeOut.
/// </summary>
public static class L2FxAdjustParticleLife
{
    static readonly int LifetimeRangeId = Shader.PropertyToID("_LifetimeRange");
    static readonly int FadeOutStartTimeId = Shader.PropertyToID("_FadeOutStartTime");
    static readonly int FadeoutStartTimeId = Shader.PropertyToID("_FadeoutStartTime");

    public static float ReadAuthoredMaxLife(Renderer renderer, float fallback)
    {
        if (renderer == null)
        {
            return Mathf.Max(0.01f, fallback);
        }

        Material[] shared = renderer.sharedMaterials;
        for (int i = 0; i < shared.Length; i++)
        {
            Material mat = shared[i];
            if (mat != null && mat.HasProperty(LifetimeRangeId))
            {
                return Mathf.Max(0.01f, mat.GetVector(LifetimeRangeId).y);
            }
        }

        return Mathf.Max(0.01f, fallback);
    }

    /// <summary>
    /// Apply stretch to runtime (instance) materials after shared→runtime copy.
    /// </summary>
    public static void ApplyToRenderer(Renderer renderer, float authoredMaxLife, float targetLife)
    {
        if (renderer == null)
        {
            return;
        }

        float authored = Mathf.Max(0.01f, authoredMaxLife);
        float target = Mathf.Max(0.01f, targetLife);
        float delta = target - authored;
        if (Mathf.Abs(delta) < 1e-4f)
        {
            return;
        }

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            ApplyToMaterial(materials[i], authored, target);
        }
    }

    public static void ApplyToMaterial(Material material, float authoredMaxLife, float targetLife)
    {
        if (material == null)
        {
            return;
        }

        float authored = Mathf.Max(0.01f, authoredMaxLife);
        float target = Mathf.Max(0.01f, targetLife);
        float delta = target - authored;
        if (Mathf.Abs(delta) < 1e-4f)
        {
            return;
        }

        if (material.HasProperty(LifetimeRangeId))
        {
            Vector4 range = material.GetVector(LifetimeRangeId);
            // L2 FRange::operator+=(delta) on Min and Max.
            float min = Mathf.Max(0.01f, range.x + delta);
            float max = Mathf.Max(min, range.y + delta);
            material.SetVector(LifetimeRangeId, new Vector4(min, max, 0f, 0f));
        }

        AddFloatIfPresent(material, FadeOutStartTimeId, delta);
        AddFloatIfPresent(material, FadeoutStartTimeId, delta);
    }

    static void AddFloatIfPresent(Material material, int propertyId, float delta)
    {
        if (!material.HasProperty(propertyId))
        {
            return;
        }

        material.SetFloat(propertyId, Mathf.Max(0f, material.GetFloat(propertyId) + delta));
    }
}
