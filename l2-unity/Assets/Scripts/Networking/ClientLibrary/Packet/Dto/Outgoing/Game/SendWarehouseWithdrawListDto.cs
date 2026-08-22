using System.Collections.Generic;

public sealed class SendWarehouseWithdrawListDto : IOutgoingDto
{
    public List<Product> Items;

    public void WriteTo(PacketWriter writer)
    {
        if (Items == null || Items.Count == 0)
        {
            writer.WriteI(0);
            return;
        }

        writer.WriteI(Items.Count);
        foreach (var item in Items)
        {
            writer.WriteI(item.ObjId);
            writer.WriteI(item.Count);
        }
    }
}
