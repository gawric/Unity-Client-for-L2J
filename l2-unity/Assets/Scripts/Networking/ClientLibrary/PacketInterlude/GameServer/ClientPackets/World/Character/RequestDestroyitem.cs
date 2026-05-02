using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestDestroyItem : ClientPacket
{
   public RequestDestroyItem(int _objectId, int _count) : base((byte)GameInterludeClientPacketType.RequestDestroyItem)
    {
        WriteI(_objectId);
        WriteI(_count);

        BuildPacket();
    }
}
