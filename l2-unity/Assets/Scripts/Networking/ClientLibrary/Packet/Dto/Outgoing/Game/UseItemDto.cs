public sealed class UseItemDto : IOutgoingDto
{
    public int ObjectId;
    public int CtrlPressed;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(CtrlPressed);
    }
}
