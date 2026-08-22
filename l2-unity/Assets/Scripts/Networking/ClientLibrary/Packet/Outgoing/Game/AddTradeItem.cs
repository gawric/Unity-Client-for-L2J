[OutgoingCommandPacket(typeof(AddTradeItemCommand))]
public sealed class AddTradeItem : OutgoingWirePacket<AddTradeItemDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.AddTradeItem;

    public AddTradeItem(AddTradeItemCommand command) : this(command.Trade, command.ObjectId, command.Count) { }

    public AddTradeItem(int trade, int objectId, int count)
    {
        Dto.TradeId = trade;
        Dto.ObjectId = objectId;
        Dto.Count = count;
    }
}
