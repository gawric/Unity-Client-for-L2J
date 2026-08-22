public class GameClientPacketHandler : ClientPacketHandler
{
    public override void SendPacket(IOutgoingPacket packet)
    {
        _client.SendPacket(packet);
    }
}
