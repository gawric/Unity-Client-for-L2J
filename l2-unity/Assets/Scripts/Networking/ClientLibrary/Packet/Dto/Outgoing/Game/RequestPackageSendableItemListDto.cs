public sealed class RequestPackageSendableItemListDto : IOutgoingDto
{
    public int ObjectId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
    }
}
