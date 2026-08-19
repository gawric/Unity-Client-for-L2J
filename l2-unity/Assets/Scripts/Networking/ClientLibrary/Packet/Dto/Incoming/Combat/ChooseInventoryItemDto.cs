public class ChooseInventoryItemDto : IWireDto
{

    private int _itemId;
    private ItemInstance _item;

    public ItemInstance Item { get { return _item; } }
    
    public void ReadFrom(PacketReader reader)
    {

        _itemId = reader.ReadI();
        _item =  new ItemInstance(-1, _itemId, ItemLocation.Inventory, -1, 1, ItemCategory.Item, false, ItemSlot.none, 0, 9999);
    }
}
