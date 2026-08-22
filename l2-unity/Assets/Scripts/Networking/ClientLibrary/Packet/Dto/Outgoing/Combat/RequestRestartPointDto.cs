public sealed class RequestRestartPointDto : IOutgoingDto
{
    public int PointType;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(PointType);
    }
}
