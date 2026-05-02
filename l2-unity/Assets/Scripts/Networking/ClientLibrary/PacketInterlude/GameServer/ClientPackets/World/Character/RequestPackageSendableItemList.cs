using UnityEngine;

public class RequestPackageSendableItemList : ClientPacket
{
    public RequestPackageSendableItemList(int _objectId) : base((byte)GameInterludeClientPacketType.RequestPackageSendableItemList)
    {
        WriteI(_objectId);
        BuildPacket();
    }
}
