[OutgoingCommandPacket(typeof(CharacterSelectCommand))]
public sealed class CharacterSelect : OutgoingWirePacket<CharacterSelectDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.CharacterSelect;

    public CharacterSelect(CharacterSelectCommand command) : this(command.Slot) { }

    public CharacterSelect(int slot)
    {
        Dto.Slot = slot;
    }
}
