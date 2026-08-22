public sealed class RequestShortCutRegDto : IOutgoingDto
{
    public int TypeId;
    public int WorldSlot;
    public int Id;
    public int Level;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(TypeId);
        writer.WriteI(WorldSlot);
        writer.WriteI(Id);
        writer.WriteI(Level);
    }
}
