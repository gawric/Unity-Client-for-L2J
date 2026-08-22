public sealed class ItemSendServer
{
    public readonly INetworkCommand Command;
    public readonly bool Encrypt;

    public ItemSendServer(INetworkCommand command, bool encrypt)
    {
        Command = command;
        Encrypt = encrypt;
    }
}
