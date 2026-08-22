public sealed class ClickActionDto : IOutgoingDto
{
    public int ObjectId;
    public int OriginX;
    public int OriginY;
    public int OriginZ;
    public int ActionId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(OriginX);
        writer.WriteI(OriginY);
        writer.WriteI(OriginZ);
        writer.WriteB((byte)ActionId);
    }
}
