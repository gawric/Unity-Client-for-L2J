
public class GetItem : ServerPacket
{
    private int _playerId;
    public int PlayerId { get => _playerId; }
    private ItemInstance _item;
    public ItemInstance Item { get => _item; }


    public GetItem(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _playerId = ReadI();
        var objectId = ReadI();
        var x = ReadI();
        var y = ReadI();
        var z = ReadI();
    }
}
