using UnityEngine;

public static class EffectShaderLifetimeHelper
{
    public static void Apply(Transform instanceTransform, bool enabled, float rangeMin, float rangeMax)
    {
        if (instanceTransform == null)
        {
            return;
        }

        Renderer[] renderers = instanceTransform.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] mats = renderer.materials;
            for (int j = 0; j < mats.Length; j++)
            {
                Material mat = mats[j];
                if (mat == null)
                {
                    continue;
                }

                if (mat.HasProperty("_HasLifetime"))
                {
                    mat.SetFloat("_HasLifetime", enabled ? 1f : 0f);
                }

                if (mat.HasProperty("_HasLifeTime"))
                {
                    mat.SetFloat("_HasLifeTime", enabled ? 1f : 0f);
                }

                if (enabled)
                {
                    Vector4 lifetimeRange = new Vector4(rangeMin, rangeMax, 0f, 0f);
                    if (mat.HasProperty("_LifetimeRange"))
                    {
                        mat.SetVector("_LifetimeRange", lifetimeRange);
                    }
                    if (mat.HasProperty("_LifeTimeRange"))
                    {
                        mat.SetVector("_LifeTimeRange", lifetimeRange);
                    }
                }
            }
        }
    }
}
