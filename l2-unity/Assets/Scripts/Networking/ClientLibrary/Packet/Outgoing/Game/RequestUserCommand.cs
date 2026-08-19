[OutgoingCommandPacket(typeof(RequestUserCommandCommand))]
public sealed class RequestUserCommand : OutgoingWirePacket<RequestUserCommandDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.BypassUserCmd;

    public RequestUserCommand(RequestUserCommandCommand command) : this(command.Id) { }

    public RequestUserCommand(int idCommand)
    {
        Dto.CommandId = idCommand;
    }
}
