using UnityEngine;

public class Appearing : ClientPacket
{
    public Appearing() : base((byte)GameInterludeClientPacketType.Appearing)
    {
        BuildPacket();
    }
}
