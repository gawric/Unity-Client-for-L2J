[OutgoingCommandPacket(typeof(RequestServerLoginCommand))]
public sealed class RequestServerLogin : OutgoingWirePacket<RequestServerLoginDto>
{
    protected override byte Opcode => (byte)LoginClientPacketType.RequestServerLogin;

    public RequestServerLogin(RequestServerLoginCommand command) : this(command.ServerId, command.SessionKey1, command.SessionKey2) { }

    public RequestServerLogin(int serverId, int sessionKey1, int sessionKey2)
    {
        Dto.ServerId = serverId;
        Dto.SessionKey1 = sessionKey1;
        Dto.SessionKey2 = sessionKey2;
    }
}
