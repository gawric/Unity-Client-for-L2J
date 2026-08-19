using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class AppLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        RegisterIfFound<EventProcessor>(builder);
        RegisterIfFound<GameManager>(builder);
        RegisterIfFound<SceneLoader>(builder);
        RegisterIfFound<GameClient>(builder);
        RegisterIfFound<LoginClient>(builder);

        builder.Register<IncomingPacketAutoRegistry>(Lifetime.Singleton)
            .AsSelf()
            .As<IIncomingPacketAutoRegistry>()
            .As<INetworkHandlers>();
        builder.Register<OutgoingPacketAutoRegistry>(Lifetime.Singleton)
            .AsSelf()
            .As<IOutgoingPacketAutoRegistry>();
        builder.Register<InterludeProtocol>(Lifetime.Singleton).As<IProtocol>().AsSelf();
        builder.Register<PacketApplyQueue>(Lifetime.Singleton);
        builder.Register<NetworkDispatcher>(Lifetime.Singleton).As<INetworkDispatcher>().AsSelf();
        builder.Register<IncomingGameQueue>(Lifetime.Singleton);
        builder.Register<SendGameDataQueue>(Lifetime.Singleton);
        builder.Register<IncomingLoginDataQueue>(Lifetime.Singleton);
        builder.Register<SendLoginDataQueue>(Lifetime.Singleton);
        builder.Register<NetworkRuntime>(Lifetime.Singleton);
        builder.Register<AnimationManager>(Lifetime.Singleton).As<IAnimationManager>().AsSelf();
        RegisterDatTables(builder);

        builder.RegisterBuildCallback(container =>
        {
            App.Container = container;
            AnimationManager animations = container.Resolve<AnimationManager>();
            AnimationManager.Bind(animations);
            IncomingPacketActions.BindApp(
                container.Resolve<EventProcessor>(),
                container.Resolve<GameClient>(),
                container.Resolve<LoginClient>(),
                container.Resolve<GameManager>(),
                animations,
                container.Resolve<PacketApplyQueue>());
        });
    }

    private static void RegisterDatTables(IContainerBuilder builder)
    {
        builder.RegisterInstance(ItemTable.Instance);
        builder.RegisterInstance(ItemNameTable.Instance);
        builder.RegisterInstance(ItemStatDataTable.Instance);
        builder.RegisterInstance(ArmorgrpTable.Instance);
        builder.RegisterInstance(EtcItemgrpTable.Instance);
        builder.RegisterInstance(WeapongrpTable.Instance);
        builder.RegisterInstance(NpcgrpTable.Instance);
        builder.RegisterInstance(NpcNameTable.Instance);
        builder.RegisterInstance(QuestNameTable.Instance);
        builder.RegisterInstance(RecipeTable.Instance);
        builder.RegisterInstance(ActionNameTable.Instance);
        builder.RegisterInstance(AnimLeghtTable.Instance);
        builder.RegisterInstance(SysStringTable.Instance);
        builder.RegisterInstance(SkillNameTable.Instance);
        builder.RegisterInstance(SkillgrpTable.Instance);
        builder.RegisterInstance(ModelTable.Instance);
        builder.RegisterInstance(LogongrpTable.Instance);
        builder.RegisterInstance(SystemMessageTable.Instance);
    }

    private T ResolveComponent<T>() where T : MonoBehaviour
    {
        T local = GetComponent<T>();
        if (local != null)
            return local;
        return FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private void RegisterIfFound<T>(IContainerBuilder builder) where T : MonoBehaviour
    {
        T component = ResolveComponent<T>();
        if (component != null)
            builder.RegisterComponent(component);
    }
}
