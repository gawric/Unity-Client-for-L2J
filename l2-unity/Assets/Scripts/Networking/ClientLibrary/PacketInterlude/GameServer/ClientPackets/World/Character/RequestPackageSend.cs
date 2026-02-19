using System.Collections.Generic;
using UnityEngine;

public class RequestPackageSend : ClientPacket
{
    public RequestPackageSend(int objectId, List<Product> buyList) : base((byte)GameInterludeClientPacketType.RequestPackageSend)
    {
        WriteI(objectId);

        if (buyList == null || buyList.Count == 0)
        {
            WriteI(0);
        }
        else
        {
            WriteI(buyList.Count);

            foreach (var item in buyList)
            {
                WriteI(item.ObjId);
                WriteI(item.Count);
            }
        }


        BuildPacketNoPad();
    }
}
