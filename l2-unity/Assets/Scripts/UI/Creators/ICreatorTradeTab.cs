using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public interface ICreatorTradeTab 
{
    public void InitTradeTabs(string[] nameTabs);
    public void CreateTradeTabs(VisualElement inventoryTabView, SlotType slotType = SlotType.Multisell, bool isDragged = false);

    public void AddData(List<ItemInstance> allItems);

    public void SetClickActiveTab(int position);

    public ItemInstance GetActiveByPosition(int position);

    public void ClearSlots(List<ItemInstance> oldListItems);

    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc);

    public event Action<int, ItemCategory, int> EventLeftClick;


}
