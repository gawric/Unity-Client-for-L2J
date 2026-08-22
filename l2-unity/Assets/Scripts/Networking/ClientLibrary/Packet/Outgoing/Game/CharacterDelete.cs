[OutgoingCommandPacket(typeof(CharacterDeleteCommand))]
public sealed class CharacterDelete : OutgoingWirePacket<CharacterDeleteDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.CharacterDelete;

    public CharacterDelete(CharacterDeleteCommand command) : this(command.Slot) { }

    public CharacterDelete(int slot)
    {
        Dto.Slot = slot;
    }
}
