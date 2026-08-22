public sealed class ProtocolVersionDto : IOutgoingDto
{
    public int Protocol;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Protocol);
    }
}
