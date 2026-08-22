public sealed class RequestTargetCanceldDto : IOutgoingDto
{


    public void WriteTo(PacketWriter writer)
    {
        writer.WriteShort(0);
    }
}
