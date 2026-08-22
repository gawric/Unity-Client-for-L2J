
public class DropItemDto : IWireDto
{
    private int _objectId;
    public int ObjectId { get => _objectId; }
    private ItemInstance _item;
    public ItemInstance Item { get => _item; }


    

    public void ReadFrom(PacketReader reader)
    {
        _objectId = reader.ReadI();
        var itemId = reader.ReadI();
        var displayId = reader.ReadI();
        var x = reader.ReadI();
        var y = reader.ReadI();
        var z = reader.ReadI();
        var isStackable = reader.ReadI();
        var count = reader.ReadI();
    }
}
