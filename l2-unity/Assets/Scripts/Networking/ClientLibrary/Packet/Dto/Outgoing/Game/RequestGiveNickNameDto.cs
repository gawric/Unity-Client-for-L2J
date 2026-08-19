public sealed class RequestGiveNickNameDto : IOutgoingDto
{
    public string MemberName;
    public string Title;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteOtherS(MemberName);
        writer.WriteOtherS(Title);
    }
}
