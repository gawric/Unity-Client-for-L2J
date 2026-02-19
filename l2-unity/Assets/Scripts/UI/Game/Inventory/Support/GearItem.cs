using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.UIElements;

public class GearItem
{
    private VisualElement _slotElement;
    private ItemSlot _itemSlot;
    private GearSlot _gearSlot;
    public GearItem(VisualElement slotElement , ItemSlot itemSlot)
    {
        _slotElement = slotElement;
        _itemSlot = itemSlot;

    }

    public ItemSlot GetSlotType()
    {
        return _itemSlot;
    }
    //SlotType is the name of the cell in the layout
    //and in ItemSlotType it can be a combined option for example lRHand or LRRing.And the layout can only be L or R
    public ItemSlot GetItemType()
    {
        if(_gearSlot.ItemInstance != null)
        {
            return _gearSlot.ItemInstance.BodyPart;
        }
        return ItemSlot.none;
    }
    public void SetGearSlot(GearSlot gearSlot)
    {
        _gearSlot = gearSlot;
    }

    public void SetSelectGear()
    {
        _gearSlot.SetSelected();
    }

    public void UnSelectGear()
    {
        _gearSlot.UnSelect();
    }


    public void Assign(ItemInstance item)
    {
        _gearSlot.AssignItem(item);
    }

    public void AssignImageAtIndex(ItemInstance item , int indexIcon)
    {
        _gearSlot.AssignItemAtIndexImage(item , indexIcon);
    }

    public void AssignImageAtIndeAlpha(ItemInstance item, int indexIcon)
    {
        _gearSlot.AssignItemAtIndexImage(item, indexIcon);
        _gearSlot.AddBlackOverlay();
    }

    public void AssignAlpha(ItemInstance item , float alpha)
    {
        _gearSlot.AssignItem(item , alpha);
        _gearSlot.AddBlackOverlay();
    }

    public void AssignEmpty()
    {
        _gearSlot.RemoveBlackOverlay();
        _gearSlot.AssignEmpty();
    }
    public bool IsEmptyGearSlot()
    {
        return _gearSlot.IsEmpty;
    }

    public int GetItemId()
    {
        return _gearSlot.ItemId;
    }

    public bool IsBow() => _gearSlot.GetUseElement() is ItemInstance item && item.IsBow();

    public bool IsArrow() => _gearSlot.GetUseElement() is ItemInstance item && item.IsArrow();
    public int GetObjectId()
    {
        return _gearSlot.ObjectId;
    }
    public VisualElement GetElement()
    {
        return _slotElement;
    }
}
