[OutgoingCommandPacket(typeof(RequestBypassToServerCommand))]
public sealed class RequestBypassToServer : OutgoingWirePacket<RequestBypassToServerDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestBypassToServer;

    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameOverwriteOpcode;

    public RequestBypassToServer(RequestBypassToServerCommand command) : this(command.Bypass) { }

    public RequestBypassToServer(string bypasscommand)
    {
        Dto.Command = bypasscommand;
    }
}
