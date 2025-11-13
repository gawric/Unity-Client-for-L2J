using System;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class TradingSlot : L2DraggableSlot
{
    private TradingSlotModel _model;
    protected bool _empty = true;
    private AssignData _data;
    public event Action<int> EventLeftClick;
    public TradingSlot(TradingSlotModel model)
        : base(model.GetPosition(), model.GetSlotElement(), model.GetSlotType() , model.isDragged)
    {
        _model = model;
        _data = new AssignData();
        _empty = true;
    }

    public void AssignItem(ItemInstance item, bool isDisabled = false)
    {
        _slotElement.RemoveFromClassList("empty");
        _data.RefreshData(item);
        _data.RefreshDataItem(item);
        _empty = false;

        if (_model != null && _slotDragManipulator != null) SetDragged(_model.isDragged);
        SetDisabled(_slotDisabled , isDisabled);

        if (_slotElement != null)
        {
            AddImage(SlotBg, SlotElement, _data.GetItemId());
        }
        else
        {
            Debug.LogWarning("TradingSlot> AssignItem: Не критическая ошибка не смогли найти TradingSlotModel>SlotElement");
        }
    }

    public void SetDisabled(VisualElement slotDisabled , bool isDisabled)
    {
        if(slotDisabled != null)
        {
            if (!isDisabled)
            {
                slotDisabled.style.display = DisplayStyle.None;
            }
            else
            {
                slotDisabled.style.display = DisplayStyle.Flex;
            }
        }
    }

    public void SetDragged(bool isDragged)
    {
        if(_slotDragManipulator != null)
        {
            _slotDragManipulator.enabled = isDragged;
        }
    }

    public int GetItemId()
    {
        return _data.GetItemId();
    }

    public ItemCategory GetItemCategory()
    {
        return _data.GetItemCategory();
    }

    public ItemInstance GetItemInstance()
    {
        return _data.GetItemInstance();
    }

    public void AssignEmpty()
    {
        _empty = true;
        _data.ResetData();
        SetDisabled(_slotDisabled, false);

        if (_slotElement != null)
        {
            StyleBackground background = new StyleBackground(IconManager.Instance.GetInvetoryDefaultBackground());
            UpdateSlotBg(background);
        }
    }

    private void UpdateSlotBg(StyleBackground background)
    {
        if (SlotBg != null)
        {
            _slotBg.style.backgroundImage = background;
            _slotDragManipulator.enabled = false;

        }
        else
        {

            _slotElement.style.backgroundImage = null;
        }
    }


    private void AddImage(VisualElement slotBg , VisualElement slotElement , int id)
    {
        if (slotBg == null & slotElement == null) return;

        StyleBackground background = new StyleBackground(IconManager.Instance.GetIcon(id));
        slotBg.style.backgroundImage = background;
    }

    protected override void HandleLeftClick()
    {
        // TradeTab tab = model.GetTab();
        //if (tab != null)
        //{
            EventLeftClick?.Invoke(_position);
            //tab.SelectSlot(_position);
       /// }
    }

   

    protected override void HandleRightClick()
    {
        Debug.Log("Click Right TradingSLot");
    }

    protected override void HandleMiddleClick()
    {
        if (!_empty)
        {
            Debug.Log("Click Middle TradingSLot");
        }
    }
}







public class TradingSlotModel
{
  
    private VisualElement _slotElement;
    private SlotType _slotType;
    private int _position;
    private bool _isDragged;

    public TradingSlotModel(int position , bool isDragged ,  VisualElement slotElement , SlotType slotType)
    {
        _position = position;
        _slotElement = slotElement;
        _slotType = slotType;
        _isDragged = isDragged;
    }

 
    public VisualElement GetSlotElement(){ return _slotElement;}

    public SlotType GetSlotType() { return _slotType;}

    public bool isDragged { get { return _isDragged; } }

    public int GetPosition() { return _position; }
}
