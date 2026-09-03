using System.Collections.Generic;

public interface IHomeProjectileService
{
    void EnsureDualFlightRoots(BaseEffect instance, bool mirrorDualFlight);

    bool Launch(
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config);

    bool TryLaunchPart(
        CompositePrefabPart part,
        BaseEffect spawned,
        EffectResolveContext context);

    void ProcessShootLaunches(
        CompositePrefabPart[] parts,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances,
        HashSet<CompositePrefabPart> launchedHomeProjectileParts,
        EffectResolveContext context);
}
