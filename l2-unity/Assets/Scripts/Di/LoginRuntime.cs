using VContainer;

public sealed class LoginRuntime
{
    private readonly IObjectResolver _resolver;

    public LoginRuntime(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    public L2LoginUI Ui { get { return Get<L2LoginUI>() ?? L2LoginUI.Instance; } }
    public LoginWindow LoginWindow { get { return Get<LoginWindow>() ?? LoginWindow.Instance; } }
    public LicenseWindow License { get { return Get<LicenseWindow>() ?? LicenseWindow.Instance; } }
    public ServerSelectWindow Servers { get { return Get<ServerSelectWindow>() ?? ServerSelectWindow.Instance; } }
    public CharSelectWindow CharSelect { get { return Get<CharSelectWindow>() ?? CharSelectWindow.Instance; } }
    public CharCreationWindow CharCreate { get { return Get<CharCreationWindow>() ?? CharCreationWindow.Instance; } }
    public LoginCameraManager Camera { get { return Get<LoginCameraManager>() ?? LoginCameraManager.Instance; } }
    public CharacterSelector Selector { get { return Get<CharacterSelector>() ?? CharacterSelector.Instance; } }
    public CharacterCreator Creator { get { return Get<CharacterCreator>() ?? CharacterCreator.Instance; } }
    public GameClient Game { get { return Get<GameClient>() ?? IncomingPacketActions.Game; } }
    public LoginClient LoginClient { get { return Get<LoginClient>() ?? IncomingPacketActions.Login; } }
    public GameManager Manager { get { return Get<GameManager>() ?? IncomingPacketActions.Manager; } }

    private T Get<T>() where T : class
    {
        if (_resolver == null)
            return null;

        try
        {
            return _resolver.Resolve<T>();
        }
        catch
        {
            return null;
        }
    }
}
