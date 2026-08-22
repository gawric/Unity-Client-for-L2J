[OutgoingCommandPacket(typeof(EnterWorldCommand))]
public sealed class EnterWorld : OutgoingWirePacket<EnterWorldDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.EnterWorld;
}
