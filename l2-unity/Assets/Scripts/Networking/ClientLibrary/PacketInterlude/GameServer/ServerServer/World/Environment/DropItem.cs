
public class DropItem : ServerPacket
{
    private int _objectId;
    public int ObjectId { get => _objectId; }
    private ItemInstance _item;
    public ItemInstance Item { get => _item; }


    public DropItem(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _objectId = ReadI();
        var itemId = ReadI();
        var displayId = ReadI();
        var x = ReadI();
        var y = ReadI();
        var z = ReadI();
        var isStackable = ReadI();
        var count = ReadI();
    }
}
