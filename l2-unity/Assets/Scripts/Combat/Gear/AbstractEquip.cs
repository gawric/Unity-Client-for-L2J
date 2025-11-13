using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static L2Slot;

public abstract class AbstractEquip
{
    private Dictionary<ItemSlot, GearItem> _gearAnchors;

    protected void SetAnchor(Dictionary<ItemSlot, GearItem> gearAnchors)
    {
        _gearAnchors = gearAnchors;
    }

    protected void AddIfNotExists(ItemInstance item, ItemSlot type)
    {
        int objectId = GetObjectIdBySlotType(type);

        if (objectId != item.ObjectId)
        {
            AddGearSlotAssign(type, item);
        }
    }

    protected bool ContainsItemInTypes(ItemInstance item , ItemSlot[] types)
    {
        return types.Any(slot => GetObjectIdBySlotType(slot) == item.ObjectId);
    }

    public GearItem GetSlotByObjectId(int objectId)
    {
        return _gearAnchors.Values.FirstOrDefault(gear => gear.GetObjectId() == objectId);
    }

    protected bool GearSlotIsEmpty(ItemSlot slotType)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            return _gearAnchors[slotType].IsEmptyGearSlot();
        }
        return false;
    }

    protected ItemSlot GearSlotGetItemSlot(ItemSlot slotType)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            return _gearAnchors[slotType].GetItemType();
        }
        return ItemSlot.none;
    }

    protected void AddGearSlotAssign(ItemSlot slotType, ItemInstance item)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            _gearAnchors[slotType].Assign(item);
        }
    }

    protected void AddGearSlotAssignByIndexIcon(ItemSlot slotType, ItemInstance item , int indexImage)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            _gearAnchors[slotType].AssignImageAtIndex(item , indexImage);
        }
    }

    protected void AddGearSlotAssignByIndexIconAlpha(ItemSlot slotType, ItemInstance item, int indexImage)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            _gearAnchors[slotType].AssignImageAtIndeAlpha(item, indexImage);
        }
    }

    protected void AddGearSlotAssignAlpha(ItemSlot slotType, ItemInstance item , float alpha)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            _gearAnchors[slotType].AssignAlpha(item , alpha);
        }
    }

    protected int GetObjectIdBySlotType(ItemSlot slotType)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            return _gearAnchors[slotType].GetObjectId();
        }
        return 0;
    }

    protected int GetItemIdBySlotType(ItemSlot slotType)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            return _gearAnchors[slotType].GetItemId();
        }
        return 0;
    }
}
