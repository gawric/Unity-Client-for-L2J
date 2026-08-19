public sealed class RequestPledgePowerDto : IOutgoingDto
{
    public int Rank;
    public int Action;
    public int Privs;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(Rank);
        writer.WriteI(Action);
        writer.WriteI(Privs);
    }
}
