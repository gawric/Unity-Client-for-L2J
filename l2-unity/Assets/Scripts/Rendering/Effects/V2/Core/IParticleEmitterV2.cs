using UnityEngine;

/// <summary>
/// Unified V2 emitter surface. ParticleGroup vs ParticleSingle is authoring
/// (rate, maxCount, respawn, stretch), not a second MonoBehaviour.
/// </summary>
public interface IParticleEmitterV2
{
    bool HasFixedDuration { get; }
    bool IsComplete { get; }
    float AuthoredDuration { get; }
    void SetEmissionWindow(float windowSeconds, EmitterStopMode stopMode);
    void SetStreamVisible(bool visible);
}

public static class ParticleEmitterV2
{
    public static IParticleEmitterV2[] InChildren(Component root, bool includeInactive = true)
    {
        if (root == null)
        {
            return System.Array.Empty<IParticleEmitterV2>();
        }

        return root.GetComponentsInChildren<IParticleEmitterV2>(includeInactive);
    }

    public static void SetEmissionWindow(
        Component root,
        float windowSeconds,
        EmitterStopMode stopMode,
        bool skipFixedDuration)
    {
        IParticleEmitterV2[] emitters = InChildren(root);
        for (int i = 0; i < emitters.Length; i++)
        {
            IParticleEmitterV2 emitter = emitters[i];
            if (emitter == null)
            {
                continue;
            }

            if (skipFixedDuration && emitter.HasFixedDuration)
            {
                continue;
            }

            emitter.SetEmissionWindow(windowSeconds, stopMode);
        }
    }

    public static bool TryAllComplete(Component root, out bool anyEnabled)
    {
        anyEnabled = false;
        IParticleEmitterV2[] emitters = InChildren(root);
        for (int i = 0; i < emitters.Length; i++)
        {
            IParticleEmitterV2 emitter = emitters[i];
            if (emitter is not Behaviour behaviour || !behaviour.isActiveAndEnabled)
            {
                continue;
            }

            anyEnabled = true;
            if (!emitter.IsComplete)
            {
                return false;
            }
        }

        return anyEnabled;
    }

    public static float MaxAuthoredDuration(Component root, float fallback)
    {
        float life = fallback;
        IParticleEmitterV2[] emitters = InChildren(root);
        for (int i = 0; i < emitters.Length; i++)
        {
            if (emitters[i] != null)
            {
                life = Mathf.Max(life, emitters[i].AuthoredDuration);
            }
        }

        return life;
    }

    public static void BindHostOwnedEmission(Component root)
    {
        if (root == null)
        {
            return;
        }

        ParticleGroupV2[] groups = root.GetComponentsInChildren<ParticleGroupV2>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i]?.BindHostOwnedEmission();
        }
    }

    public static void SetVisible(Component root, bool visible)
    {
        IParticleEmitterV2[] emitters = InChildren(root);
        for (int i = 0; i < emitters.Length; i++)
        {
            emitters[i]?.SetStreamVisible(visible);
        }
    }

    public static void StopAll(Component root)
    {
        if (root == null)
        {
            return;
        }

        EffectPart[] parts = root.GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] is IParticleEmitterV2)
            {
                parts[i].StopPart();
            }
        }
    }
}
