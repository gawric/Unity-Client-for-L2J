public sealed class ItemSendLogin
{
    public readonly INetworkCommand Command;

    public ItemSendLogin(INetworkCommand command)
    {
        Command = command;
    }
}
