public sealed class RequestJoinPartyDto : IOutgoingDto
{
    public string TargetName;
    public int PartyDistributionTypeId;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(TargetName);
        writer.WriteI(PartyDistributionTypeId);
    }
}
