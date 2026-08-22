[OutgoingCommandPacket(typeof(ProtocolVersionCommand))]
public sealed class ProtocolVersion : OutgoingWirePacket<ProtocolVersionDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.ProtocolVersion;

    public ProtocolVersion(ProtocolVersionCommand command) : this(command.Protocol) { }

    public ProtocolVersion(int protocol)
    {
        Dto.Protocol = protocol;
    }
}
