public sealed class RequestOustPledgeMemberDto : IOutgoingDto
{
    public string MemberName;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(MemberName);
    }
}
