using System.Collections.Generic;

public sealed class RequestPreviewItemDto : IOutgoingDto
{
    public int Unknown;
    public int ListId;
    public List<Product> Items;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Unknown);
        writer.WriteI(ListId);
        if (Items == null || Items.Count == 0)
        {
            writer.WriteI(0);
            return;
        }

        writer.WriteI(Items.Count);
        foreach (var item in Items)
            writer.WriteI(item.ItemId);
    }
}
