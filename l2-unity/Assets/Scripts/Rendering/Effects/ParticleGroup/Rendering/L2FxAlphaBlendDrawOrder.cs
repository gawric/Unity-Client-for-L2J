using UnityEngine;

/// <summary>
/// AlphaBlend steam/puffs with high texture alpha punch whatever was drawn
/// underneath (dst*(1-a)+src*a). Keep those batches behind cores/meshes.
/// </summary>
public static class L2FxAlphaBlendDrawOrder
{
    public const int PuffSortingOrder = 0;
    public const int CoreSortingOrder = 1;

    static readonly int FlipbookModeId = Shader.PropertyToID("_FlipbookMode");

    public static void Apply(Component effectRoot)
    {
        if (effectRoot == null)
        {
            return;
        }

        ParticleGroupV2[] groups = effectRoot.GetComponentsInChildren<ParticleGroupV2>(true);
        if (groups == null || groups.Length < 2)
        {
            return;
        }

        AddLocationFromOtherEmitterProvider[] trails =
            effectRoot.GetComponentsInChildren<AddLocationFromOtherEmitterProvider>(true);

        bool anyPuff = false;
        bool anyCore = false;
        var isPuff = new bool[groups.Length];
        for (int i = 0; i < groups.Length; i++)
        {
            isPuff[i] = IsPuffGroup(groups[i], trails);
            if (isPuff[i])
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

        for (int i = 0; i < groups.Length; i++)
        {
            SetGroupSorting(
                groups[i],
                isPuff[i] ? PuffSortingOrder : CoreSortingOrder);
        }
    }

    public static bool IsPuffGroup(ParticleGroupV2 group, AddLocationFromOtherEmitterProvider[] trails)
    {
        if (group == null)
        {
            return false;
        }

        if (trails != null)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] != null && trails[i].ContainsTail(group))
                {
                    return true;
                }
            }
        }

        return HasBlendBetweenFlipbook(group);
    }

    public static void SetGroupSorting(ParticleGroupV2 group, int sortingOrder)
    {
        if (group == null)
        {
            return;
        }

        Renderer[] particles = group.CaptureAuthoring().particles;
        if (particles == null || particles.Length == 0)
        {
            particles = group.GetComponentsInChildren<Renderer>(true);
        }

        if (particles == null)
        {
            return;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            Renderer renderer = particles[i];
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }
    }

    static bool HasBlendBetweenFlipbook(ParticleGroupV2 group)
    {
        Renderer[] particles = group.CaptureAuthoring().particles;
        Renderer first = null;
        if (particles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                {
                    first = particles[i];
                    break;
                }
            }
        }

        if (first == null)
        {
            first = group.GetComponentInChildren<Renderer>(true);
        }

        if (first == null)
        {
            return false;
        }

        Material[] materials = first.sharedMaterials;
        if (materials == null)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material != null &&
                material.HasProperty(FlipbookModeId) &&
                material.GetFloat(FlipbookModeId) > 2.5f)
            {
                return true;
            }
        }

        return false;
    }
}
