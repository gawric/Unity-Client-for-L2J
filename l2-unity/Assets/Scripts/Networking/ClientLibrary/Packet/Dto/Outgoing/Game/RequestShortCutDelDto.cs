public sealed class RequestShortCutDelDto : IOutgoingDto
{
    public int WorldSlot;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(WorldSlot);
    }
}
