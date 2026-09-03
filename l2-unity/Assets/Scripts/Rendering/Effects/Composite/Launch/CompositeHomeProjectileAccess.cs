using VContainer;

public static class CompositeHomeProjectileAccess
{
    public static IHomeProjectileService Resolve(ref IHomeProjectileService cached)
    {
        if (cached != null)
        {
            return cached;
        }

        if (App.GameContainer != null)
        {
            cached = App.GameContainer.Resolve<IHomeProjectileService>();
            return cached;
        }

        HomeProjectileDualFlightRoots dualRoots = new HomeProjectileDualFlightRoots();
        cached = new HomeProjectileService(
            dualRoots,
            new HomeProjectileLauncher(dualRoots, new DefaultEffectAttachmentResolver()));
        return cached;
    }
}
