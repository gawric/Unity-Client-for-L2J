[OutgoingCommandPacket(typeof(RequestServerListCommand))]
public sealed class RequestServerList : OutgoingWirePacket<RequestServerListDto>
{
    protected override byte Opcode => (byte)LoginClientPacketType.RequestServerList;

    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.Login;
    protected override int LoginExtraZeroBytes => 0;

    public RequestServerList(RequestServerListCommand command) : this(command.SessionKey1, command.SessionKey2) { }

    public RequestServerList(int sessionKey1, int sessionKey2)
    {
        Dto.SessionKey1 = sessionKey1;
        Dto.SessionKey2 = sessionKey2;
    }
}
