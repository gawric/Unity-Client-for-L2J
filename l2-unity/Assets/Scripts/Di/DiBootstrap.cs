using UnityEngine;
using VContainer;
using VContainer.Unity;

public static class DiBootstrap
{
    public static void EnsureAppScope()
    {
        if (Object.FindFirstObjectByType<AppLifetimeScope>() != null)
            return;

        GameObject go = new GameObject("AppLifetimeScope");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AppLifetimeScope>();
    }

    public static void EnsureGameScope()
    {
        World world = Object.FindFirstObjectByType<World>(FindObjectsInactive.Include);
        GameLifetimeScope existing = Object.FindFirstObjectByType<GameLifetimeScope>(FindObjectsInactive.Include);

        if (existing != null)
        {
            if (IsGameScopeReady(existing))
            {
                App.GameContainer = existing.Container;
                return;
            }

            Object.DestroyImmediate(existing.gameObject);
            App.GameContainer = null;
        }

        if (world == null)
            return;

        EnsureAppScope();
        AppLifetimeScope app = Object.FindFirstObjectByType<AppLifetimeScope>();
        GameObject go = new GameObject("GameLifetimeScope");
        using (LifetimeScope.EnqueueParent(app))
            go.AddComponent<GameLifetimeScope>();
    }

    public static void EnsureLoginScope()
    {
        if (Object.FindFirstObjectByType<LoginLifetimeScope>() != null)
            return;

        EnsureAppScope();
        AppLifetimeScope app = Object.FindFirstObjectByType<AppLifetimeScope>();
        L2LoginUI ui = Object.FindFirstObjectByType<L2LoginUI>(FindObjectsInactive.Include);
        GameObject go = new GameObject("LoginLifetimeScope");
        if (ui != null)
            go.transform.SetParent(ui.transform);
        using (LifetimeScope.EnqueueParent(app))
            go.AddComponent<LoginLifetimeScope>();
    }

    private static bool IsGameScopeReady(GameLifetimeScope scope)
    {
        if (scope == null || scope.Container == null)
            return false;

        try
        {
            return scope.Container.Resolve<World>() != null;
        }
        catch
        {
            return false;
        }
    }
}
