using System.Collections.Generic;
using UnityEngine.UIElements;
using static L2Slot;


public class TradeTab : AbstractTab, ITab
{
    private TradingSlot[] _tradeSlots;
    private int _defaultCountSlot = 0;
    private CreateScroller _createScroller;

    private int _selectedSlot = -1;
    public TradeTab(string tabName , int countSlot , VisualElement tabContainer, VisualElement tabHeader, bool initEmpty , SlotType slotType , bool isDragged)
    {
        _createScroller = new CreateScroller();
        _tabName = tabName;
        _defaultCountSlot = countSlot;
        _tabHeader = tabHeader;
        _tabContainer = tabContainer;
        _contentContainer = tabContainer.Q<VisualElement>("Content");
        _createScroller.Start(tabContainer);
        CreateEmptyInventory(initEmpty , slotType , isDragged);
        OnRegisterClickTab(tabHeader);
    }


    public void OnRegisterClickTab(VisualElement tabHeader)
    {

            tabHeader.RegisterCallback<MouseDownEvent>(evt => {
                //EventSwitch?.Invoke(this);
                OnEventSwitch(this , true);
            }, TrickleDown.TrickleDown);
    }


    public void CreateEmptyInventory(bool initEmty , SlotType slotType , bool isDragged)
    {
        if (_contentContainer != null && initEmty)
        {
            _contentContainer.Clear();
            _tradeSlots = new TradingSlot[_defaultCountSlot];
            CreateSlots(_tradeSlots, _contentContainer , slotType , isDragged);
        }

    }



    private void CreateSlots(TradingSlot[] tradeSlots , VisualElement contentContainer , SlotType slotType , bool isDragged)
    {
        for (int i = 0; i < _tradeSlots.Length; i++)
        {
            VisualElement slotElement = CretaVisualElement();
            TradingSlot slot = CreateTradeSlot(new TradingSlotModel(i, isDragged, slotElement, slotType));
            slot.EventLeftClick += OnClickLeftEvent;
            contentContainer.Add(slotElement);
            tradeSlots[i] = slot;
        }
    }

    public void AddDataTrade(List<ItemInstance> allItems , bool checkInventory = false)
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            ItemInstance item = allItems[i];
            item.SetSlot(i);
            Assign(item, checkInventory, i);
        }
    }

    private void Assign(ItemInstance item , bool checkInventory,  int position)
    {

        if (!checkInventory)
        {
            _tradeSlots[position].AssignItem(item);
            return;
        }

        ItemInstance inventoryItem = PlayerInventory.Instance.GetItemByItemId(item.ItemId);

        if (inventoryItem != null && inventoryItem.Count >= item.Count)
        {
            _tradeSlots[position].AssignItem(item , false);
        }
        else
        {
            _tradeSlots[position].AssignItem(item, true);
        }
    }

    public void ClearSlots(List<ItemInstance> oldListItems)
    {
        for (int i = 0; i < oldListItems.Count; i++)
        {
            ItemInstance item = oldListItems[i];
            item.SetSlot(i);
            _tradeSlots[i].AssignEmpty();
        }
    }

    public void OnClickLeftEvent(int position)
    {
        SelectSlot(position);
        SetEventOutside(position);
    }

    public ItemInstance GetSlotByPosition(int position)
    {
        if(ArrayUtils.IsValidIndexArray(_tradeSlots, position))
        {
            return _tradeSlots[position].GetItemInstance();
        }

        return null;
    }

   
    public void ClearAllSlots()
    {
        if (_tradeSlots == null) return;

   
        if (_selectedSlot != -1 && ArrayUtils.IsValidIndexArray(_tradeSlots, _selectedSlot))
        {
            _tradeSlots[_selectedSlot].UnSelect();
            _selectedSlot = -1;
        }

     
        for (int i = 0; i < _tradeSlots.Length; i++)
        {
            if (_tradeSlots[i] != null)
            {
                _tradeSlots[i].AssignEmpty();
            }
        }
    }

    public void SelectSlot(int slotPosition)
    {
        if (_selectedSlot != -1)
        {
            //It happens that the panel will become smaller and the index may be larger than the current panel, so you need to check
            if (ArrayUtils.IsValidIndexArray(_tradeSlots, _selectedSlot))
            {
                _tradeSlots[_selectedSlot].UnSelect();
            }

        }
        _tradeSlots[slotPosition].SetSelected();
        _selectedSlot = slotPosition;
    }


    private void SetEventOutside(int slotPosition)
    {
        if (slotPosition != -1)
        {
            TradingSlot slot = _tradeSlots[slotPosition];
            if (slot != null)
            {
                OnEventLeftClick(slot.GetItemId(), slot.GetItemCategory(), slotPosition);
                //EventLeftClick?.Invoke(slot.GetItemId() , slot.GetItemCategory() , slotPosition);
            }
            
        }
    }




    private VisualElement CretaVisualElement()
    {
        return InventoryWindow.Instance.InventorySlotTemplate.Instantiate()[0];
    }

    private TradingSlot CreateTradeSlot(TradingSlotModel model)
    {
        return new TradingSlot(model);
    }

    public VisualElement GetContentElement()
    {
        return null;
    }

    public string GetTabName()
    {
        return _tabName;
    }
}
