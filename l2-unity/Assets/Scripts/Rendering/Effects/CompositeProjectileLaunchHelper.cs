using UnityEngine;
using System.Collections.Generic;

public static class CompositeProjectileLaunchHelper
{
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

    public static bool ShouldLaunchOnShoot(CompositePrefabPart part)
    {
        return GetLaunchMode(part) == ProjectileLaunchMode.OnAnimationShoot;
    }

    public static bool ShouldLaunchImmediately(CompositePrefabPart part)
    {
        return GetLaunchMode(part) == ProjectileLaunchMode.Immediate;
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

    private static ProjectileImpactType GetImpactType(CompositePrefabPart part)
    {
        return GetProjectileConfig(part).impactType;
    }

    private static CompositeProjectileConfig GetProjectileConfig(CompositePrefabPart part)
    {
        return part != null && part.projectile != null ? part.projectile : new CompositeProjectileConfig();
    }
}
