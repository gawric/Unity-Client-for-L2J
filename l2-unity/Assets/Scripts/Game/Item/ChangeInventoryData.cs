using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ChangeInventoryData 
{
    //We remove items from the dictionary before we remove them in the Tab Inventory itself, so we put them in this temporary collection.
    //It will only store items that are removed as a result of moving from the inventory to the equip inventory and back.
    private List<ItemInstance> _obsoleteItemsInventory;
    private List<ItemInstance> _obsoleteItemsGear;
    private List<ReplaceGearData> _obsoleteItemsGearReplace;

    public ChangeInventoryData()
    {
        _obsoleteItemsInventory = new List<ItemInstance>();
        _obsoleteItemsGear = new List<ItemInstance>();
        _obsoleteItemsGearReplace = new List<ReplaceGearData>();
    }


    public List<ItemInstance> ItemsInventory()
    {
        return _obsoleteItemsInventory;
    }

    public List<ItemInstance> ItemsGears()
    {
        return _obsoleteItemsGear;
    }

    public List<ReplaceGearData> ItemsReplace()
    {
        return _obsoleteItemsGearReplace;
    }

    public bool IsReplaceSourceItem(int objectId)
    {
        return _obsoleteItemsGearReplace.Any(data => data.GetSource().ObjectId == objectId || data.GetGear().ObjectId == objectId);
    }
    public void AddRemoveInventory(ItemInstance item)
    {
        _obsoleteItemsInventory.Add(item);
    }

    public void AddRemoveGear(ItemInstance item)
    {
        _obsoleteItemsGear.Add(item);
    }

    public void AddReplaceGear(ItemInstance source , ItemInstance destination)
    {
        _obsoleteItemsGearReplace.Add(new ReplaceGearData(source , destination));
    }
    public void ClearData()
    {
        _obsoleteItemsInventory.Clear();
        _obsoleteItemsGear.Clear();
        _obsoleteItemsGearReplace.Clear();
    }

}
