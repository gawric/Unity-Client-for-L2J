using System.Collections.Generic;
using UnityEngine;

public class SendWarehouseDepositList : ClientPacket
{
    public SendWarehouseDepositList(List<Product> sellList) : base((byte)GameInterludeClientPacketType.SendWareHouseDepositList)
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
