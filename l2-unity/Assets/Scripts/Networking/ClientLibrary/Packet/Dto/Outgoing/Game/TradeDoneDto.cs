public sealed class TradeDoneDto : IOutgoingDto
{
    public int Response;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Response);
    }
}
