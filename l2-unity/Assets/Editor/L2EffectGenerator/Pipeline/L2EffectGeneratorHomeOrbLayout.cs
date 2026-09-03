#if UNITY_EDITOR
using System;
using System.Collections.Generic;

/// <summary>
/// Home-flight orbs: Independent sprite = world trail, remaining local sprite = core.
/// m_u003_b: SpriteEmitter2 PTCS_Independent life=0.333, SpriteEmitter5 local life=0.010.
/// </summary>
public static class L2EffectGeneratorHomeOrbLayout
{
    public const float UnrealUnitsToMeters = 1f / 52.5f;

    public static bool IsSpriteEmitter(UcEmitterDefinition emitter)
    {
        return emitter != null &&
               !string.IsNullOrEmpty(emitter.ClassName) &&
               emitter.ClassName.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsIndependentCoordinateSystem(UcEmitterDefinition emitter)
    {
        return emitter != null &&
               !string.IsNullOrEmpty(emitter.CoordinateSystem) &&
               emitter.CoordinateSystem.IndexOf("Independent", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static float ResolveLifetime(UcEmitterDefinition emitter)
    {
        if (emitter == null)
        {
            return 0f;
        }

        emitter.ResolveLifetimeRange(out float min, out float max);
        return Math.Max(min, max);
    }

    public static bool TryResolve(
        IReadOnlyList<UcEmitterDefinition> emitters,
        out UcEmitterDefinition trail,
        out UcEmitterDefinition core)
    {
        trail = null;
        core = null;
        if (emitters == null || emitters.Count == 0)
        {
            return false;
        }

        List<UcEmitterDefinition> sprites = new List<UcEmitterDefinition>();
        for (int i = 0; i < emitters.Count; i++)
        {
            if (IsSpriteEmitter(emitters[i]))
            {
                sprites.Add(emitters[i]);
            }
        }

        if (sprites.Count < 2)
        {
            return false;
        }

        for (int i = 0; i < sprites.Count; i++)
        {
            UcEmitterDefinition sprite = sprites[i];
            if (!IsIndependentCoordinateSystem(sprite))
            {
                continue;
            }

            if (trail == null || ResolveLifetime(sprite) > ResolveLifetime(trail))
            {
                trail = sprite;
            }
        }

        if (trail == null)
        {
            trail = PickLongestFadingSprite(sprites);
        }

        for (int i = 0; i < sprites.Count; i++)
        {
            UcEmitterDefinition sprite = sprites[i];
            if (sprite == trail)
            {
                continue;
            }

            if (core == null || IsBetterCore(sprite, core))
            {
                core = sprite;
            }
        }

        return trail != null && core != null && trail != core;
    }

    public static string DescribeRole(UcEmitterDefinition emitter, UcEmitterDefinition trail, UcEmitterDefinition core)
    {
        if (emitter == null)
        {
            return string.Empty;
        }

        if (trail != null && string.Equals(emitter.EmitterName, trail.EmitterName, StringComparison.Ordinal))
        {
            return "Independent trail";
        }

        if (core != null && string.Equals(emitter.EmitterName, core.EmitterName, StringComparison.Ordinal))
        {
            return "local core";
        }

        return IsIndependentCoordinateSystem(emitter) ? "Independent" : string.Empty;
    }

    static UcEmitterDefinition PickLongestFadingSprite(List<UcEmitterDefinition> sprites)
    {
        UcEmitterDefinition best = null;
        for (int i = 0; i < sprites.Count; i++)
        {
            UcEmitterDefinition sprite = sprites[i];
            if (best == null || ResolveLifetime(sprite) > ResolveLifetime(best))
            {
                best = sprite;
            }
        }

        return best;
    }

    static bool IsBetterCore(UcEmitterDefinition candidate, UcEmitterDefinition current)
    {
        float candidateLife = ResolveLifetime(candidate);
        float currentLife = ResolveLifetime(current);
        if (candidateLife + 1e-4f < currentLife)
        {
            return true;
        }

        if (Math.Abs(candidateLife - currentLife) > 1e-4f)
        {
            return false;
        }

        int candidateRate = candidate.HasInitialParticlesPerSecond ? candidate.InitialParticlesPerSecond : 0;
        int currentRate = current.HasInitialParticlesPerSecond ? current.InitialParticlesPerSecond : 0;
        return candidateRate > currentRate;
    }
}
#endif
