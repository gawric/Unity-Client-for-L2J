using UnityEngine;
using System.Collections.Generic;

public static class CompositeProjectileLaunchHelper
{
    public static void ApplyPreShootVisibility(CompositePrefabPart part, BaseEffect spawned, string debugPrefix)
    {
        if (part == null || spawned == null || !ShouldHideUntilShoot(part))
        {
            return;
        }

        SetVisualsVisible(spawned.transform, false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{debugPrefix} HidePartUntilShoot part='{part.name}' launchMode=OnAnimationShoot.");
#endif
    }

    public static void RevealOnShootParts(
        CompositePrefabPart[] parts,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances)
    {
        if (parts == null || spawnedPartInstances == null)
        {
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null || !ShouldHideUntilShoot(part))
            {
                continue;
            }

            if (!spawnedPartInstances.TryGetValue(part, out BaseEffect spawned) || spawned == null)
            {
                continue;
            }

            RestartHiddenVisuals(spawned);
            SetVisualsVisible(spawned.transform, true);
        }
    }

    public static bool RequiresAnimationShootLaunch(CompositePrefabPart[] parts)
    {
        if (parts == null)
        {
            return false;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            if (ShouldLaunchOnShoot(parts[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if this composite must listen for the caster animation shoot event (projectile on shoot and/or parts spawned on shoot).
    /// </summary>
    public static bool RequiresAnimationShootEvent(CompositePrefabPart[] parts)
    {
        if (parts == null)
        {
            return false;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null)
            {
                continue;
            }

            if (ShouldLaunchOnShoot(part))
            {
                return true;
            }

            if (CompositeHomeProjectileLaunchHelper.ShouldLaunchOnShoot(part))
            {
                return true;
            }

            if (part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot)
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldLaunchOnShoot(CompositePrefabPart part)
    {
        return GetLaunchMode(part) == ProjectileLaunchMode.OnAnimationShoot;
    }

    public static bool ShouldLaunchImmediately(CompositePrefabPart part)
    {
        return GetLaunchMode(part) == ProjectileLaunchMode.Immediate;
    }

    public static bool IsProjectilePart(CompositePrefabPart part)
    {
        ProjectileLaunchMode mode = GetLaunchMode(part);
        return mode == ProjectileLaunchMode.Immediate || mode == ProjectileLaunchMode.OnAnimationShoot;
    }

    public static bool TryLaunch(
        CompositePrefabPart part,
        BaseEffect spawned,
        Transform targetTransform,
        float playStartedAt,
        string debugPrefix)
    {
        if (part == null || spawned == null || targetTransform == null || ProjectileManager.Instance == null)
        {
            return false;
        }

        Transform projectileTransform = spawned.transform;
        projectileTransform.SetParent(null, true);
        Vector3 startPos = projectileTransform.position;

        ProjectileData settings = BuildSettings(part, spawned.gameObject, startPos, targetTransform);
        ProjectileManager.Instance.LaunchProjectile(
            spawned.gameObject,
            startPos,
            targetTransform,
            settings);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Time.time;
        float elapsed = playStartedAt > 0f ? now - playStartedAt : -1f;
        Debug.Log(
            $"{debugPrefix} LaunchPartAsProjectile part='{part.name}' at={now:F3}s elapsed={elapsed:F3}s " +
            $"start={startPos} target='{targetTransform.name}' impactType={GetImpactType(part)}.");
#endif
        return true;
    }

    public static void ProcessShootLaunches(
        CompositePrefabPart[] parts,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances,
        HashSet<CompositePrefabPart> launchedProjectileParts,
        Transform targetTransform,
        float playStartedAt,
        string debugPrefix)
    {
        if (parts == null || spawnedPartInstances == null || launchedProjectileParts == null || targetTransform == null)
        {
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null || !ShouldLaunchOnShoot(part))
            {
                continue;
            }

            if (launchedProjectileParts.Contains(part))
            {
                continue;
            }

            if (!spawnedPartInstances.TryGetValue(part, out BaseEffect spawned) || spawned == null)
            {
                continue;
            }

            if (TryLaunch(part, spawned, targetTransform, playStartedAt, debugPrefix))
            {
                launchedProjectileParts.Add(part);
            }
        }
    }

    private static ProjectileData BuildSettings(
        CompositePrefabPart part,
        GameObject projectileObject,
        Vector3 startPos,
        Transform target)
    {
        CompositeProjectileConfig projectile = GetProjectileConfig(part);
        ProjectileData settings = projectile.settingsOverride != null
            ? new ProjectileData(projectile.settingsOverride)
            : new ProjectileData();

        settings.prefab = projectileObject;
        settings.transform = projectileObject.transform;
        settings.startPosition = startPos;
        settings.targetTransform = target;
        settings.impactType = projectile.impactType;
        return settings;
    }

    private static ProjectileLaunchMode GetLaunchMode(CompositePrefabPart part)
    {
        return GetProjectileConfig(part).launchMode;
    }

    private static bool ShouldHideUntilShoot(CompositePrefabPart part)
    {
        if (part != null && (part.spawnTiming == CompositePartSpawnTiming.OnHitCollider
                             || part.spawnTiming == CompositePartSpawnTiming.OnAnimationShoot))
        {
            // Collider-hit parts appear after hit; on-shoot-spawned parts do not exist yet at cast start.
            return false;
        }

        CompositeProjectileConfig config = GetProjectileConfig(part);
        return config.launchMode == ProjectileLaunchMode.OnAnimationShoot && !config.showBeforeAnimationShoot;
    }

    private static ProjectileImpactType GetImpactType(CompositePrefabPart part)
    {
        return GetProjectileConfig(part).impactType;
    }

    private static void RestartHiddenVisuals(BaseEffect spawned)
    {
        L2Particle particle = spawned as L2Particle;
        if (particle != null)
        {
            particle.ResetTimer();
        }
    }

    private static CompositeProjectileConfig GetProjectileConfig(CompositePrefabPart part)
    {
        return part != null && part.projectile != null ? part.projectile : new CompositeProjectileConfig();
    }

    public static void SetPartVisualsVisible(Transform root, bool visible)
    {
        SetVisualsVisible(root, visible);
    }

    private static void SetVisualsVisible(Transform root, bool visible)
    {
        if (root == null)
        {
            return;
        }

        ParticleEmitterV2.SetVisible(root, visible);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsOwnedByGpuStream(renderer))
            {
                continue;
            }

            renderer.enabled = visible;
        }

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];
            if (visible)
            {
                system.Play(true);
            }
            else
            {
                system.Pause(true);
                system.Clear(true);
            }
        }
    }

    private static bool IsOwnedByGpuStream(Renderer renderer)
    {
        ParticleStreamDriver driver = renderer.GetComponentInParent<ParticleStreamDriver>();
        if (driver != null && driver.IsGpuDraw)
        {
            return true;
        }

        ParticleGroupV2 groupV2 = renderer.GetComponentInParent<ParticleGroupV2>();
        return groupV2 != null && groupV2.IsGpuDraw;
    }
}
