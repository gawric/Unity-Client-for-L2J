using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class LoginLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        RegisterIfFound<L2LoginUI>(builder);
        RegisterIfFound<LoginWindow>(builder);
        RegisterIfFound<LicenseWindow>(builder);
        RegisterIfFound<ServerSelectWindow>(builder);
        RegisterIfFound<CharSelectWindow>(builder);
        RegisterIfFound<CharCreationWindow>(builder);
        RegisterIfFound<LoginCameraManager>(builder);
        RegisterIfFound<CharacterSelector>(builder);
        RegisterIfFound<CharacterBuilder>(builder);
        RegisterIfFound<CharacterCreator>(builder);

        builder.Register<LoginRuntime>(Lifetime.Singleton);

        builder.RegisterBuildCallback(container =>
        {
            IncomingPacketActions.BindLogin(container.Resolve<LoginRuntime>());
        });
    }

    private void RegisterIfFound<T>(IContainerBuilder builder) where T : MonoBehaviour
    {
        T component = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (component != null)
            builder.RegisterComponent(component);
    }
}
