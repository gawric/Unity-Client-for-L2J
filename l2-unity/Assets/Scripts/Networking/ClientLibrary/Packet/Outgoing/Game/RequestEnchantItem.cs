[OutgoingCommandPacket(typeof(RequestEnchantItemCommand))]
public sealed class RequestEnchantItem : OutgoingWirePacket<RequestEnchantItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestEnchantItem;

    public RequestEnchantItem(RequestEnchantItemCommand command) : this(command.ObjectId) { }

    public RequestEnchantItem(int objectId)
    {
        Dto.ObjectId = objectId;
    }
}
