[OutgoingCommandPacket(typeof(RequestDropItemCommand))]
public sealed class RequestDropItem : OutgoingWirePacket<RequestDropItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestDropItem;

    public RequestDropItem(RequestDropItemCommand command)
        : this(command.ObjectId, command.Count, command.X, command.Y, command.Z)
    {
    }

    public RequestDropItem(int objectId, int count, int x, int y, int z)
    {
        Dto.ObjectId = objectId;
        Dto.Count = count;
        Dto.X = x;
        Dto.Y = y;
        Dto.Z = z;
    }
}
