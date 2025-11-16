
public class AddTradeItem : ClientPacket
{
    private int _tradeId;
    private int _objectId;
    private int _count;

    public AddTradeItem(int trade, int objectId, int count) : base((byte)GameInterludeClientPacketType.AddTradeItem)
    {
        _tradeId = trade;
        _objectId = objectId;
        _count = count;
        WriteI(_tradeId);
        WriteI(_objectId);
        WriteI(_count);
        BuildPacket();
    }
}
