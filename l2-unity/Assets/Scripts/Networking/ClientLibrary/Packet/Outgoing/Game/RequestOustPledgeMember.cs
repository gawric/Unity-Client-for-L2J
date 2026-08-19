[OutgoingCommandPacket(typeof(RequestOustPledgeMemberCommand))]
public sealed class RequestOustPledgeMember : OutgoingWirePacket<RequestOustPledgeMemberDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestOustPledgeMember;

    public RequestOustPledgeMember(RequestOustPledgeMemberCommand command) : this(command.MemberName) { }

    public RequestOustPledgeMember(string memberName)
    {
        Dto.MemberName = memberName;
    }
}
