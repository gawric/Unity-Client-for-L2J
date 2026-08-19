public sealed class RequestJoinPledgeDto : IOutgoingDto
{
    public int ObjectId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ObjectId);
        writer.WriteI(0);
    }
}
