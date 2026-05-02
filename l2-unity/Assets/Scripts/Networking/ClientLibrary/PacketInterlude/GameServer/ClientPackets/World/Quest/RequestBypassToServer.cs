using UnityEngine;

public class RequestBypassToServer : ClientPacket
{
    public RequestBypassToServer(string bypasscommand) : base((byte)GameInterludeClientPacketType.RequestBypassToServer)
    {
        WriteSOther(bypasscommand);
        BuildPacketManualType((byte)GameInterludeClientPacketType.RequestBypassToServer);
    }
}
