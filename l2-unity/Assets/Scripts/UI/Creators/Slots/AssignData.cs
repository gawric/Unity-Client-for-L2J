public class AssignData
{
    private ItemInstance _itemInstance;
    private AbstractItem _itemData;


    public void RefreshData(ItemInstance item)
    {
        _itemInstance = item;
    }

 

    public void RefreshDataItem(ItemInstance item)
    {
        if (item.ItemData != null)
        {
            _itemData = item.ItemData;
        }
    }


    public void ResetData()
    {
        _itemInstance = null;
        _itemData = null;
    }



    public int GetCount()
    {
        if (_itemInstance == null) return 0;
        return _itemInstance.Count;
    }

    public int GetObjectId()
    {
        if (_itemInstance == null) return 0;
        return _itemInstance.ObjectId;
    }

    public int GetEnchantLevel()
    {
        if (_itemInstance == null) return 0;
        return _itemInstance.EnchantLevel;
    }

    public int GetItemId()
    {
        if (_itemInstance == null) return 0;
        return _itemInstance.ItemId;
    }

    public ItemInstance GetItemInstance()
    {
        return _itemInstance;
    }



    public string GetName()
    {
        if (_itemData == null) return "";
        return _itemData.ItemName.Name;
    }

    public string Description()
    {
        if (_itemData == null) return "";
        return _itemData.ItemName.Description;
    }

    public string GetIcon()
    {
        if (_itemData == null) return "";
        return _itemData.Icon;
    }

    

    public ItemCategory GetItemCategory()
    {
        if (_itemInstance == null) return ItemCategory.None;
        return _itemInstance.Category;
    }


}
