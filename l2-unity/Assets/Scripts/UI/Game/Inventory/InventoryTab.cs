using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;
using static PlayerInventory;


[System.Serializable]
public class InventoryTab : L2Tab
{
    private InventorySlot[] _inventorySlots;
    [SerializeField] private int _selectedSlot = -1;
    private VisualElement _contentContainer;
    public List<ItemCategory> _filteredCategories;
    public List<ItemCategory> GetFilterCategories {  get { return _filteredCategories; } }
    public bool MainTab { get; internal set; }

    public override void Initialize(VisualElement chatWindowEle, VisualElement tabContainer, VisualElement tabHeader, bool initEmpty, bool isSwitchTab)
    {
        base.Initialize(chatWindowEle, tabContainer, tabHeader, initEmpty, isSwitchTab);

        _selectedSlot = -1;
        _contentContainer = tabContainer.Q<VisualElement>("Content");
        CreateEmptyInventory(initEmpty);
    }

    public void UpdateInventorySlots(InventorySlot[] inventory)
    {
        _inventorySlots = null;
        _inventorySlots = inventory;
    }


    private void CreateEmptyInventory(bool initEmty)
    {
        if (_contentContainer != null && initEmty)
        {

            _contentContainer.Clear();

            int slotCount = InventoryWindow.PLAYER_INVENTORY_SIZE;
            _inventorySlots = new InventorySlot[slotCount];
            //SlotType slotType = GetSlotType(MainTab);

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                if (i == _inventorySlots.Length - 1)
                {
                   // Debug.Log(" slot int i " + i);
                    VisualElement disabledElement = CreateVisualElementDisabled();
                    _contentContainer.Add(disabledElement);
                }
                else
                {
                    VisualElement slotElement = CretaVisualElement();
                    InventorySlot slot = CreateInventorySlot(i, slotElement, SlotType.Inventory);
                    _contentContainer.Add(slotElement);
                   _inventorySlots[i] = slot;
                }

            }
            UpdateInventorySlots(_inventorySlots);
        }

    }

    private VisualElement CretaVisualElement()
    {
        return InventoryWindow.Instance.InventorySlotTemplate.Instantiate()[0];
    }

    private VisualElement CreateVisualElementDisabled()
    {
        VisualElement slotElement = CretaVisualElement();
        slotElement.AddToClassList("inventory-slot");
        slotElement.AddToClassList("disabled");
        return slotElement;
    }
    private InventorySlot CreateInventorySlot(int i , VisualElement slotElement , SlotType slotType)
    {
        return new InventorySlot(i, slotElement, this, slotType , false);
    }


    //temporarily disabling
    private void UpdatePadSlots()
    {
        int slotCount = InventoryWindow.Instance.SlotCount;

        // Add disabled slot to fill up the window
        int rowLength = 9;
        if (InventoryWindow.Instance.Expanded)
        {
            rowLength = 12;
        }

        int padSlot = 0;
        if (slotCount < 8 * rowLength)
        {
            padSlot = 8 * rowLength - slotCount;
        }
        else if (slotCount % rowLength != 0)
        {
            padSlot = rowLength - slotCount % rowLength;
        }

        for (int i = 0; i < padSlot; i++)
        {
            VisualElement slotElement = InventoryWindow.Instance.InventorySlotTemplate.Instantiate()[0];
            slotElement.AddToClassList("inventory-slot");
            slotElement.AddToClassList("disabled");
            _contentContainer.Add(slotElement);
        }
    }
    public void SetItemList(List<ItemInstance> allItems)
    {

        if(allItems != null && allItems.Count > 0)
        {
            AddDataInventory(allItems);
            AddAllEmptyInventory(allItems.Count);
        }
        else
        {
            AddAllEmptyInventory(0);
        }
    }

    private void AddDataInventory(List<ItemInstance> allItems)
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            ItemInstance item = allItems[i];
            item.SetSlot(i);
            //Debug.Log("AssignItem Set Inventory>>>> " + item.ItemId + " ObjectId " + item.ObjectId + " Add Slot " + item.Slot);
            _inventorySlots[i].AssignItem(item);
        }
    }
    public void AddAllEmptyInventory(int size)
    {
        int start_i = size;

        for (int i = start_i; i < _inventorySlots.Length; i++)
        {
            var slot = _inventorySlots[i];

            if (slot != null)
            {
                slot.AssignEmpty();
            }
               
        }
    }
    public void UpdateItemList(List<ItemInstance> removeAndAdd , List<ItemInstance> modified , ChangeInventoryData changeInventoryData)
    {
        AddAndRemove(removeAndAdd);
        Modified(modified , changeInventoryData);

        if (_selectedSlot != -1)
        {
            SelectSlot(_selectedSlot);
        }
    }

    public void ModifiedRemove(ItemInstance item)
    {
        InventorySlot i_slot1 = GetInventorySlot(item.ObjectId);

        if (i_slot1 != null)
        {
            i_slot1.AssignEmpty();
            ShiftElements.ShiftElementsLeft(_inventorySlots, i_slot1.Position);
        }
        else
        {
            Debug.Log("Shift element number null " + i_slot1);
        }
    }

    public void AddItem(ItemInstance item)
    {
        InventorySlot i_slot1 = GetEmptyInventorySlot();
        i_slot1.AssignItem(item);
    }

   

    private void AddAndRemove(List<ItemInstance> removeAndAdd)
    {

        for (int i = 0; i < removeAndAdd.Count; i++)
        {
            ItemInstance item = removeAndAdd[i];
            if (item.LastChange == (int)InventoryChange.ADDED)
            {   
                if (!PlayerInventory.Instance.IsContaineInventory(item.ObjectId))
                {
                    //Debug.Log("ADD Inventory Slot slot-position update inventory <<<<< " + item.Slot + " position " + item + " object id " + item.ObjectId);

                    InventorySlot i_slot = _inventorySlots[item.Slot];

                    if (i_slot != null)
                    {
                        i_slot.AssignItem(item);
                    }
         
                }
                else
                {
                    Debug.Log("ADD And Rmove Count Not ADD ObjectID " + item.ObjectId);
                }
          
            }
            else if(item.LastChange == (int)InventoryChange.REMOVED)
            {
                InventorySlot slot = GetInventorySlot(item.ObjectId);
                if (slot != null)
                {
                    _inventorySlots[slot.Position].AssignEmpty();
                    ShiftElements.ShiftElementsLeft(_inventorySlots, slot.Position);
                }
            }
        }

    }

    private void Modified(List<ItemInstance> modified , ChangeInventoryData changeInventoryData)
    {
        for (int i = 0; i < modified.Count; i++)
        {
            ItemInstance currentItem = modified[i];
            if (currentItem.LastChange == (int)InventoryChange.MODIFIED)
            {
                InventorySlot oldSlot = GetInventorySlot(currentItem.ObjectId);
                //Replace Inventory->Equip and Equip->Inventory elsewhere in the code, we skip it here
                if(!changeInventoryData.IsReplaceSourceItem(currentItem.ObjectId))
                {
                    if (oldSlot != null)
                    {
                        UpdateSlot(oldSlot, currentItem);

                    }
                    else
                    {
                        InventorySlot i_slot = _inventorySlots[currentItem.Slot];

                        if (i_slot != null)
                        {
                            i_slot.AssignItem(currentItem);
                        }
                    }
                }

              

            }
        }
    }

    private void UpdateSlot(InventorySlot slot , ItemInstance currentItem)
    {

        if (slot != null)
        {
            _inventorySlots[slot.Position].AssignItem(currentItem);
        }
    }

   
 

    public InventorySlot GetInventorySlot(int objectId)
    {

        return _inventorySlots?
                               .Where(slot => slot != null && slot.ObjectId == objectId)
                               .FirstOrDefault();
    }

 


    public ItemInstance GetInventorySlotByPosition(int position)
    {
        if(ArrayUtils.IsValidIndexArray(_inventorySlots, position))
        {
            return _inventorySlots[position].ItemInstance;
        }
       return null;
    }


   

    public int GetEmptySlot()
    {
        InventorySlot slot =  _inventorySlots?
                              .Where(slot => slot != null && slot.Id == 0)
                              .FirstOrDefault();
        return slot.Position;
    }

    public InventorySlot GetEmptyInventorySlot()
    {
        return  _inventorySlots?
                              .Where(slot => slot != null && slot.Id == 0)
                              .FirstOrDefault();
    }

    public InventorySlot GetFreeSlot()
    {
        //return _inventorySlots.FirstOrDefault(slot => slot.ObjectId == 0);

        return _inventorySlots?
                               .Where(slot => slot.IsEmpty)
                               .FirstOrDefault();
    }

    public override void SelectSlot(int slotPosition)
    {
        if (_selectedSlot != -1)
        {
            //It happens that the panel will become smaller and the index may be larger than the current panel, so you need to check
            if (ArrayUtils.IsValidIndexArray(_inventorySlots, _selectedSlot)){
                _inventorySlots[_selectedSlot].UnSelect();
            }

        }
        _inventorySlots[slotPosition].SetSelected();
        _selectedSlot = slotPosition;
    }

    public void UnSelectSlot()
    {
        if (_selectedSlot != -1)
        {
            _inventorySlots[_selectedSlot].UnSelect();
        }
    }

    public void ResetSelectSlot()
    {
        _selectedSlot = -1;
    }


    protected override void OnGeometryChanged()
    {
    }

    protected override void OnSwitchTab()
    {
        if (InventoryWindow.Instance.SwitchTab(this))
        {
            AudioManager.Instance.PlayUISound("window_open");
        }
    }

    protected override void RegisterAutoScrollEvent()
    {
    }

    public void SetSelectedSlot(int selected)
    {
        _selectedSlot = selected;
    }

    public void SetFilterCategories(List<ItemCategory> filteredCategories)
    {
        _filteredCategories = filteredCategories;
    }
}
