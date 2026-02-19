using System.Collections.Generic;
using UnityEngine;

public class RequestPreviewItem : ClientPacket
{
    private int unk1 = 0;
    public RequestPreviewItem(int listId, List<Product> buyList) : base((byte)GameInterludeClientPacketType.RequestPreviewItem)
    {
        WriteI(unk1);
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
            }
        }


        //BuildPacketNoPad();
        BuildPacket();
    }
}
