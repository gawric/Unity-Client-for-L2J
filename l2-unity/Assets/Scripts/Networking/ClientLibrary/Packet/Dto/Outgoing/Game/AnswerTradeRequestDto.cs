public sealed class AnswerTradeRequestDto : IOutgoingDto
{
    public int Response;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Response);
    }
}
