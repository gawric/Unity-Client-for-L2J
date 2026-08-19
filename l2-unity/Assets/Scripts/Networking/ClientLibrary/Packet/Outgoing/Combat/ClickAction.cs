[OutgoingCommandPacket(typeof(ClickActionCommand))]
public sealed class ClickAction : OutgoingWirePacket<ClickActionDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.Action;

    public ClickAction(ClickActionCommand command) : this(command.ObjectId, command.OriginX, command.OriginY, command.OriginZ, command.ActionId) { }

    public ClickAction(int objectId, int originX, int originY, int originZ, int actionId)
    {
        Dto.ObjectId = objectId;
        Dto.OriginX = originX;
        Dto.OriginY = originY;
        Dto.OriginZ = originZ;
        Dto.ActionId = actionId;
    }
}
