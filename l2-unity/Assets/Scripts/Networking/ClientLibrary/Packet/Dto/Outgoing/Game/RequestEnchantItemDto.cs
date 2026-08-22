public sealed class RequestEnchantItemDto : IOutgoingDto
{
    public int ObjectId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
    }
}
