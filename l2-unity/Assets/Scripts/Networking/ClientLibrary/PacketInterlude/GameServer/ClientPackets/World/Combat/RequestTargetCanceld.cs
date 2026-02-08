using UnityEngine;

public class RequestTargetCanceld : ClientPacket
{
    public RequestTargetCanceld() : base((byte)GameInterludeClientPacketType.RequestTargetCanceld)
    {
        WriteShort(0);
        BuildPacket();
    }
}
