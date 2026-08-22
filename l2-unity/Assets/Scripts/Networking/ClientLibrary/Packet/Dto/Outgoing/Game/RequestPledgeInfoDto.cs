public sealed class RequestPledgeInfoDto : IOutgoingDto
{
    public int ClanId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(ClanId);
    }
}
