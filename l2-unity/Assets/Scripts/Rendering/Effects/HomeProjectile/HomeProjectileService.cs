using System.Collections.Generic;

public sealed class HomeProjectileService : IHomeProjectileService
{
    readonly HomeProjectileDualFlightRoots _dualFlightRoots;
    readonly HomeProjectileLauncher _launcher;

    public HomeProjectileService(
        HomeProjectileDualFlightRoots dualFlightRoots,
        HomeProjectileLauncher launcher)
    {
        _dualFlightRoots = dualFlightRoots;
        _launcher = launcher;
    }

    public void EnsureDualFlightRoots(BaseEffect instance, bool mirrorDualFlight)
    {
        _dualFlightRoots.Ensure(instance, mirrorDualFlight);
    }

    public bool Launch(
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config)
    {
        if (effect == null || context == null || context.CasterTransform == null || config == null)
        {
            return false;
        }

        _dualFlightRoots.Ensure(effect, config.mirrorDualFlight);
        return _launcher.Launch(effect, context, config);
    }

    public bool TryLaunchPart(
        CompositePrefabPart part,
        BaseEffect spawned,
        EffectResolveContext context)
    {
        CompositeHomeProjectileConfig config = part != null ? part.homeProjectile : null;
        if (config == null || !config.IsEnabled)
        {
            return false;
        }

        return Launch(spawned, context, config);
    }

    public void ProcessShootLaunches(
        CompositePrefabPart[] parts,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances,
        HashSet<CompositePrefabPart> launchedHomeProjectileParts,
        EffectResolveContext context)
    {
        if (parts == null || spawnedPartInstances == null || launchedHomeProjectileParts == null || context == null)
        {
            return;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            CompositePrefabPart part = parts[i];
            if (part == null || part.homeProjectile == null || !part.homeProjectile.ShouldLaunchOnShoot)
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

            if (TryLaunchPart(part, spawned, context))
            {
                launchedHomeProjectileParts.Add(part);
            }
        }
    }
}
