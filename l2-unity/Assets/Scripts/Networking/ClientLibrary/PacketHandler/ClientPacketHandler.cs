public abstract class ClientPacketHandler
{
    protected AsynchronousClient _client;

    public void SetClient(AsynchronousClient client)
    {
        _client = client;
    }

    public abstract void SendPacket(IOutgoingPacket packet);
}
