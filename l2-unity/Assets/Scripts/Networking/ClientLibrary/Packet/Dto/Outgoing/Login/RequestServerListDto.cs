public sealed class RequestServerListDto : IOutgoingDto
{
    public int SessionKey1;
    public int SessionKey2;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(SessionKey1);
        writer.WriteI(SessionKey2);
        writer.WriteI(0);
        writer.WriteI(0);
    }
}
