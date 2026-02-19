using UnityEngine;
using UnityEngine.UIElements;

public class InventorySlot : L2DraggableSlot
{
    protected L2Tab _currentTab;
    private int _count;
    private long _remainingTime;
    private SlotClickSoundManipulator _slotClickSoundManipulator;
    private int _objectId;
    private ItemCategory _itemCategory;
    protected bool _empty = true;

    public int Count { get { return _count; } }
    public long RemainingTime { get { return _remainingTime; } }
    public ItemCategory ItemCategory { get { return _itemCategory; } }
    public int ObjectId { get { return _objectId; } }
    public int ItemId { get { return _id; } }

    public InventorySlot(int position, VisualElement slotElement, L2Tab tab, SlotType slotType , bool rightMouseup)
    : base(position, slotElement, slotType, false, rightMouseup)
    {
        _currentTab = tab;
        _empty = true;

        if (_slotClickSoundManipulator == null)
        {
            _slotClickSoundManipulator = new SlotClickSoundManipulator(_slotElement);
            _slotElement.AddManipulator(_slotClickSoundManipulator);
        }
       
    }

    public InventorySlot(int position, VisualElement slotElement, SlotType slotType)
    : base(position, slotElement, slotType, true, false)
    {
        _empty = true;
    }
 
    public void AssignEmpty()
    {
        //_slotElement.AddToClassList("empty");
        _empty = true;
        _id = 0;
        _name = "Unkown";
        _description = "Unkown item.";
        _itemInstance = null;
        _product = null;
        _objectId = 0;
        _enchantLevel = 0;
        if (_slotElement != null)
        {
            StyleBackground background = new StyleBackground(IconManager.Instance.GetInvetoryDefaultBackground());
            if(SlotBg != null)
            {
                _slotBg.style.backgroundImage = background;
                _slotDragManipulator.enabled = false;
                
            }
            else
            {
               
                _slotElement.style.backgroundImage = null;
            }

        }
    }



    public void RefreshPosition(int newPosition)
    {
        Position = newPosition;
    }

    private Product _product;
    private ItemInstance _itemInstance;
    public ItemInstance ItemInstance { get { return _itemInstance; } }
    public Product Product { get { return _product; } }
    public void AssignProduct(Product item)
    {
        _slotElement.RemoveFromClassList("empty");
        

        if (item != null)
        {
            _id = item.ItemId;
            _empty = false;
            _objectId = item.ObjId;
        }
        else
        {
            _id = 0;
            _objectId = 0;
            _name = "Unkown";
            _description = "Unkown item.";
            _empty = true;
        }

        if (_slotElement != null)
        { 
            StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(_id));
            _slotBg.style.backgroundImage = background;
            _slotDragManipulator.enabled = false;
        }

        _product = item;
    }


    public object GetUseElement()
    {
        if(_product != null)
        {
            return _product;
        }

        return _itemInstance;
    }
    public void ManualHideToolTips()
    {
        ToolTipSimple.Instance.ManualHide();
    }

    public void AssignUniversal(object data)
    {
        if (data != null)
        {
            if (data.GetType() == typeof(Product))
            {
                AssignProduct((Product)data);
            }
            else if (data.GetType() == typeof(ItemInstance))
            {
                AssignItem((ItemInstance)data);
            }
        }
       
    }
    public void AssignItem(ItemInstance item)
    {
        _slotElement.RemoveFromClassList("empty");

        AddDataItem(item);
        _itemInstance = item;
        _count = item.Count;
        _objectId = item.ObjectId;
        _enchantLevel = item.EnchantLevel;
        _id = item.ItemId;
        _remainingTime = item.RemainingTime;
        _empty = false;

        if (_slotElement != null)
        {
            AddImage(this);
            AddTooltip(item);
            _slotDragManipulator.enabled = true;
        }
        else
        {
            Debug.Log("Не критическая ошибка не смогли найти InventorySlot>SlotElement");
        }
    }

    public void AssignItem(ItemInstance item , float alpha)
    {
        _slotElement.RemoveFromClassList("empty");

        AddDataItem(item);
        _itemInstance = item;
        _count = item.Count;
        _objectId = item.ObjectId;
        _id = item.ItemId;
        _remainingTime = item.RemainingTime;
        _empty = false;
        _enchantLevel = item.EnchantLevel;

        if (_slotElement != null)
        {
            AddImageAlpha(this, alpha);
            AddTooltip(item);
            _slotDragManipulator.enabled = true;
        }
        else
        {
            Debug.Log("Не критическая ошибка не смогли найти InventorySlot>SlotElement");
        }
    }

    public void AssignItemAtIndexImage(ItemInstance item , int indexImage)
    {
        _slotElement.RemoveFromClassList("empty");

        AddDataItem(item);
        _itemInstance = item;
        _count = item.Count;
        _objectId = item.ObjectId;
        _id = item.ItemId;
        _remainingTime = item.RemainingTime;
        _enchantLevel = item.EnchantLevel;
        _empty = false;

        if (_slotElement != null)
        {
            AddImageAtIndex(this , indexImage);
            AddTooltip(item);
            _slotDragManipulator.enabled = true;
        }
        else
        {
            Debug.Log("Не критическая ошибка не смогли найти InventorySlot>SlotElement");
        }
    }

    private void AddDataItem(ItemInstance item)
    {
        if (item.ItemData != null)
        {
            _id = item.ItemData.Id;
            _name = item.ItemData.ItemName.Name;
            _description = item.ItemData.ItemName.Description;
            _icon = item.ItemData.Icon;
            _objectId = item.ObjectId;

            
            _itemCategory = item.Category;
        }
        else
        {
            Debug.LogWarning($"Item data is null for item {item.ItemId}.");
            _id = 0;
            _name = "Unkown";
            _description = "Unkown item.";
            _icon = "";
            _objectId = -1;
            _itemCategory = ItemCategory.Item;
        }

    }

    private void AddImage(InventorySlot slot)
    {
        if (SlotBg == null & _slotElement == null) return;

        if (slot.GetType() == typeof(GearSlot))
        {
            StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(_id));
            _slotElement.style.backgroundImage = background;
        }
        else
        {
            if (SlotBg == null) return;
            StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(_id));
            _slotBg.style.backgroundImage = background;
        }
    }

    private void AddImageAtIndex(InventorySlot slot , int indexIamge)
    {
        if (SlotBg == null & _slotElement == null) return;

        if (slot.GetType() == typeof(GearSlot))
        {
            StyleBackground background = new StyleBackground(IconManager.Instance.GetOtherIcon(_id , indexIamge));
            _slotElement.style.backgroundImage = background;
        }
        else
        {
            if (SlotBg == null) return;
            StyleBackground background = new StyleBackground(IconManager.Instance.GetOtherIcon(_id, indexIamge));
            _slotBg.style.backgroundImage = background;


        }
    }





    //I'm not touching the alpha channel yet, but it might come in handy in the future
    private void AddImageAlpha(InventorySlot slot , float alpha)
    {
        if (SlotBg == null & _slotElement == null) return;

        if (slot.GetType() == typeof(GearSlot))
        {
            StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(_id));
            _slotElement.style.backgroundImage = background;
        }
        else
        {
            if (SlotBg == null) return;
            StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(_id));
            _slotBg.style.backgroundImage = background;
        }
    }

   

    private void AddTooltip(ItemInstance item)
    {
        string tooltipText = $"{_name} ({_count})";
        if (item.Category == ItemCategory.Weapon ||
            item.Category == ItemCategory.Jewel ||
            item.Category == ItemCategory.ShieldArmor)
        {
            tooltipText = _name;
        }

        if (_tooltipManipulator != null)
        {
            _tooltipManipulator.SetText(tooltipText);
        }
    }

    public override void ClearManipulators()
    {
        base.ClearManipulators();

        if (_slotClickSoundManipulator != null)
        {
            _slotElement.RemoveManipulator(_slotClickSoundManipulator);
            _slotClickSoundManipulator = null;
        }
    }
    public bool IsEmpty { get { return _empty; } }
    protected override void HandleLeftClick()
    {
        if (_currentTab != null)
        {
            _currentTab.SelectSlot(_position);
        }
    }

    protected override void HandleRightClick()
    {
        UseItem();
    }

    protected override void HandleMiddleClick()
    {
        if (!_empty)
        {
            PlayerInventory.Instance.DestroyItem(_objectId, 1);
        }
    }

    public virtual void UseItem()
    {
        if (!_empty)
        {
            if (!PlayerInventory.Instance.UseItem(_objectId))
            {
                AssignEmpty();
            }
            
        }

    }
}
