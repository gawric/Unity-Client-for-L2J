using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RequestBuyItem : ClientPacket
{
    public RequestBuyItem(int listId , List<Product> buyList) : base((byte)GameInterludeClientPacketType.RequestBuyItem)
    {
        WriteI(listId);
        if (buyList == null || buyList.Count == 0)
        {
            WriteI(0);
        }
        else
        {
            WriteI(buyList.Count);

            foreach (var item in buyList)
            {
                WriteI(item.ItemId);
                WriteI(item.Count);
            }
        }


        BuildPacketNoPad();
    }
}
