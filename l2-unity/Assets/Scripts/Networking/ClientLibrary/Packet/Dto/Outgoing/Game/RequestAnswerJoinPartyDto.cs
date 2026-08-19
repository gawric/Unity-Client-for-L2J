public sealed class RequestAnswerJoinPartyDto : IOutgoingDto
{
    public int Response;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Response);
    }
}
