public sealed class RequestQuestAbortDto : IOutgoingDto
{
    public int QuestId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(QuestId);
    }
}
