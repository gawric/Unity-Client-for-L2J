public sealed class NetworkRuntime
{
    public IIncomingPacketAutoRegistry Packets { get; }
    public IProtocol Protocol { get; }
    public INetworkDispatcher Dispatcher { get; }
    public IncomingGameQueue IncomingGame { get; }
    public SendGameDataQueue SendGame { get; }
    public IncomingLoginDataQueue IncomingLogin { get; }
    public SendLoginDataQueue SendLogin { get; }
    public EventProcessor Events { get; }

    public NetworkRuntime(
        IIncomingPacketAutoRegistry packets,
        IProtocol protocol,
        INetworkDispatcher dispatcher,
        IncomingGameQueue incomingGame,
        SendGameDataQueue sendGame,
        IncomingLoginDataQueue incomingLogin,
        SendLoginDataQueue sendLogin,
        EventProcessor events)
    {
        Packets = packets;
        Protocol = protocol;
        Dispatcher = dispatcher;
        IncomingGame = incomingGame;
        SendGame = sendGame;
        IncomingLogin = incomingLogin;
        SendLogin = sendLogin;
        Events = events;
    }
}
