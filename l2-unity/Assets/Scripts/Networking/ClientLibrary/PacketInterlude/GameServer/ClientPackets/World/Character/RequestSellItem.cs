using System.Collections.Generic;
using UnityEngine;

public class RequestSellItem : ClientPacket
{
    public RequestSellItem(int listId, List<Product> sellList) : base((byte)GameInterludeClientPacketType.RequestSellItem)
    {
        WriteI(listId);

        if(sellList == null || sellList.Count == 0)
        {
            WriteI(0);
        }
        else
        {
            WriteI(sellList.Count);

            foreach (var item in sellList)
            {
                WriteI(item.ObjId);
                WriteI(item.ItemId);
                WriteI(item.Count);
            }
        }


        BuildPacketNoPad();
    }
}
