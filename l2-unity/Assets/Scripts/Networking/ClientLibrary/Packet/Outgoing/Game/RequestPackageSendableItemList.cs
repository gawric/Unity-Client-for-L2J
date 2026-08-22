[OutgoingCommandPacket(typeof(RequestPackageSendableItemListCommand))]
public sealed class RequestPackageSendableItemList : OutgoingWirePacket<RequestPackageSendableItemListDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestPackageSendableItemList;

    public RequestPackageSendableItemList(RequestPackageSendableItemListCommand command) : this(command.ObjectId) { }

    public RequestPackageSendableItemList(int objectId)
    {
        Dto.ObjectId = objectId;
    }
}
