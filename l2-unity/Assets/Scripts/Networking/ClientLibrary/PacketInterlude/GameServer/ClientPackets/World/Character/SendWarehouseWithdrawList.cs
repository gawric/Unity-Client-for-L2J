using System.Collections.Generic;
using UnityEngine;

public class SendWarehouseWithdrawList : ClientPacket
{
    public SendWarehouseWithdrawList(List<Product> sellList) : base((byte)GameInterludeClientPacketType.SendWareHouseWithDrawList)
    {
        if (sellList == null || sellList.Count == 0)
        {
            WriteI(0);
        }
        else
        {
            WriteI(sellList.Count);

            foreach (var item in sellList)
            {
                WriteI(item.ObjId);
                WriteI(item.Count);
            }
        }


        BuildPacketNoPad();
    }
}
