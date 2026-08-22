[OutgoingCommandPacket(typeof(RequestDestroyItemCommand))]
public sealed class RequestDestroyItem : OutgoingWirePacket<RequestDestroyItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestDestroyItem;

    public RequestDestroyItem(RequestDestroyItemCommand command) : this(command.ObjectId, command.Count) { }

    public RequestDestroyItem(int objectId, int count)
    {
        Dto.ObjectId = objectId;
        Dto.Count = count;
    }
}
