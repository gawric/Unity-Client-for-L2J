public sealed class RequestJoinPartyDto : IOutgoingDto
{
    public string Name;
    public int DistributionType;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(Name);
        writer.WriteI(DistributionType);
    }
}

public sealed class RequestWithDrawalPartyDto : IOutgoingDto
{
    public void WriteTo(PacketWriter writer) { }
}
