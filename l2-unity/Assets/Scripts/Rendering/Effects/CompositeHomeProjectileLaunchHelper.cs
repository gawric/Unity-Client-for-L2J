using System.Collections.Generic;
using UnityEngine;

public static class CompositeHomeProjectileLaunchHelper
{
    public static bool IsEnabled(CompositePrefabPart part)
    {
        return GetLaunchMode(part) != ProjectileLaunchMode.Disabled;
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
        EffectResolveContext context,
        float playStartedAt,
        string debugPrefix)
    {
        if (part == null || spawned == null || context == null || context.CasterTransform == null)
        {
            return false;
        }

        CompositeHomeProjectileConfig config = GetConfig(part);
        if (config.launchMode == ProjectileLaunchMode.Disabled)
        {
            return false;
        }

        bool launched = HomeProjectileService.Launch(spawned, context, config, debugPrefix);
        if (!launched)
        {
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float now = Time.time;
        float elapsed = playStartedAt > 0f ? now - playStartedAt : -1f;
        Debug.Log(
            $"{debugPrefix} LaunchPartAsHomeProjectile part='{part.name}' at={now:F3}s elapsed={elapsed:F3}s " +
            $"home='{context.CasterTransform.name}' point={config.homeAttachmentPoint} arrive={config.arriveDistance:F2}m.");
#endif
        return true;
    }

    public static void ProcessShootLaunches(
        CompositePrefabPart[] parts,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances,
        HashSet<CompositePrefabPart> launchedHomeProjectileParts,
        EffectResolveContext context,
        float playStartedAt,
        string debugPrefix)
    {
        if (parts == null || spawnedPartInstances == null || launchedHomeProjectileParts == null || context == null)
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

            if (launchedHomeProjectileParts.Contains(part))
            {
                continue;
            }

            if (!spawnedPartInstances.TryGetValue(part, out BaseEffect spawned) || spawned == null)
            {
                continue;
            }

            if (TryLaunch(part, spawned, context, playStartedAt, debugPrefix))
            {
                launchedHomeProjectileParts.Add(part);
            }
        }
    }

    private static ProjectileLaunchMode GetLaunchMode(CompositePrefabPart part)
    {
        return GetConfig(part).launchMode;
    }

    private static CompositeHomeProjectileConfig GetConfig(CompositePrefabPart part)
    {
        return part != null && part.homeProjectile != null
            ? part.homeProjectile
            : new CompositeHomeProjectileConfig();
    }
}
