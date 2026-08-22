public sealed class RequestDestroyItemDto : IOutgoingDto
{
    public int ObjectId;
    public int Count;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(Count);
    }
}
