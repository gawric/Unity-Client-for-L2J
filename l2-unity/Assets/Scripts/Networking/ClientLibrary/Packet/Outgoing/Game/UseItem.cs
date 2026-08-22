[OutgoingCommandPacket(typeof(UseItemCommand))]
public sealed class UseItem : OutgoingWirePacket<UseItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.UseItem;

    public UseItem(UseItemCommand command) : this(command.ObjectId, command.CtrlPressed) { }

    public UseItem(int objectId, int ctrlPressed)
    {
        Dto.ObjectId = objectId;
        Dto.CtrlPressed = ctrlPressed;
    }
}
