public sealed class AddTradeItemDto : IOutgoingDto
{
    public int TradeId;
    public int ObjectId;
    public int Count;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(TradeId);
        writer.WriteI(ObjectId);
        writer.WriteI(Count);
    }
}
