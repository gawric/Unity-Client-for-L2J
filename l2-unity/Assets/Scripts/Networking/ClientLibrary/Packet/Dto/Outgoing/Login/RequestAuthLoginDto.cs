public sealed class RequestAuthLoginDto : IOutgoingDto
{
    public byte[] RsaBlock;
    public int Response;

    public void WriteTo(PacketWriter writer)
    {
        if (RsaBlock == null)
            return;

        writer.WriteB(RsaBlock);
        writer.WriteI(Response);
        writer.WriteI(0);
        writer.WriteI(0);
        writer.WriteI(0);
        writer.WriteI(0);
        writer.WriteI(8);
        writer.WriteI(0);
    }
}
