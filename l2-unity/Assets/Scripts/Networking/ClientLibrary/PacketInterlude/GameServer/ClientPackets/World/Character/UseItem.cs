using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItem : ClientPacket
{
    public UseItem(int _objectId , int _ctrlPressed) : base((byte)GameInterludeClientPacketType.UseItem)
    {
       

        WriteI(_objectId);
        WriteI(_ctrlPressed);

        BuildPacket();
    }
}
