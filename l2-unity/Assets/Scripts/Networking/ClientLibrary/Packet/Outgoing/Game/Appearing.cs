[OutgoingCommandPacket(typeof(AppearingCommand))]
public sealed class Appearing : OutgoingWirePacket<AppearingDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Appearing;
}
