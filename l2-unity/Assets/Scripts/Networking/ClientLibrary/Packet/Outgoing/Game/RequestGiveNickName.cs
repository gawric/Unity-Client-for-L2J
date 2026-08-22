[OutgoingCommandPacket(typeof(RequestGiveNickNameCommand))]
public sealed class RequestGiveNickName : OutgoingWirePacket<RequestGiveNickNameDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestGiveNickName;

    public RequestGiveNickName(RequestGiveNickNameCommand command) : this(command.MemberName, command.Title) { }

    public RequestGiveNickName(string memberName, string title)
    {
        Dto.MemberName = memberName;
        Dto.Title = title;
    }
}
