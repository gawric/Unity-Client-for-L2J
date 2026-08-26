public sealed class RequestDropItemDto : IOutgoingDto
{
    public int ObjectId;
    public int Count;
    public int X;
    public int Y;
    public int Z;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(Count);
        writer.WriteI(X);
        writer.WriteI(Y);
        writer.WriteI(Z);
    }
}
