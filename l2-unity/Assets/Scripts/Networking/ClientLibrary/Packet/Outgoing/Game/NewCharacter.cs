[OutgoingCommandPacket(typeof(NewCharacterCommand))]
public sealed class NewCharacter : OutgoingWirePacket<NewCharacterDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.NewCharacter;
}
