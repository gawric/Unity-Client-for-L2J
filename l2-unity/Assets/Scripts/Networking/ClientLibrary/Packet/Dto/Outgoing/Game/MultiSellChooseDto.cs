public sealed class MultiSellChooseDto : IOutgoingDto
{
    public int ListId;
    public int EntryId;
    public int Amount;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ListId);
        writer.WriteI(EntryId);
        writer.WriteI(Amount);
    }
}
