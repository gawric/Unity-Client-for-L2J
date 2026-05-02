using UnityEngine;

public class RequestEnchantItem : ClientPacket
{
    public RequestEnchantItem(int _objectId) : base((byte)GameInterludeClientPacketType.RequestEnchantItem)
    {
        WriteI(_objectId);
        BuildPacket();
    }
}
