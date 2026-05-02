using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ITab
{
    void UnselectTabContainerClass();
    void SelectTabContainerClass();
    void SetMainTab(bool main);

    public event Action<ITab , bool> EventSwitch;
    public event Action<int, ItemCategory, int> EventLeftClick;
    void OnClickLeftEvent(int position);

    void ClearSlots(List<ItemInstance> oldListItems);

   void  ClearAllSlots();

    void AddDataTrade(List<ItemInstance> allItems, bool isInventory = false);
    ItemInstance GetSlotByPosition(int position);

    public VisualElement GetContentElement();

    public string GetTabName();
}
