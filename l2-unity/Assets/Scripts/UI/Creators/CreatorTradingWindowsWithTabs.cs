using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class CreatorTradingWindowsWithTabs : AbstractCreator , ICreatorTradeTab
{
    public event Action<int> EventSwitchTabByIndexOfTab;
    private VisualTreeAsset[] _templates;
    //tabTemplate 0 , tabHeaderTemplate 1
    private readonly string[] loadTemplate = new string[2] { "Data/UI/_Elements/Game/Inventory/InventoryTab", "Data/UI/_Elements/Game/Inventory/InventoryTabHeader" };

    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        _templates = new VisualTreeAsset[loadTemplate.Length];

        for (int i = 0; i < loadTemplate.Length; i++)
        {
            _templates[i] = loaderFunc(loadTemplate[i]);
        }
    }

    public void CreateTradeTabs(VisualElement inventoryTabView , SlotType slotType = SlotType.Multisell , bool isDragged = false)
    {
        if(inventoryTabView == null)
        {
            Debug.LogWarning("CreatorTradingWindows > CreateTradeTabs: Not Found inventoryTabView ");
            return;
        }

        var tabTemplate = _templates[0];
        var tabHeaderTemplate = _templates[1];
        base.CreateTradeTabs(inventoryTabView, tabTemplate, tabHeaderTemplate , slotType, isDragged);
    }

    public void ClearSlots(List<ItemInstance> oldListItems)
    {
        base.ClearSlotsActiveTab(oldListItems);
    }

    public void SetClickActiveTab(int position)
    {
        if(_activeTab != null)
        {
            _activeTab.OnClickLeftEvent(position);
        }
    }

    public TradeTab GetMainTab()
    {
        foreach (TradeTab tab in _tabsTradeTabs)
        {
            if(tab != null & tab.GetMainTab() == true)
            {
                return tab;
            }
        }

        return null;
    }


    public void AddData(List<ItemInstance> allItems)
    {
        List<ItemInstance> allItemsInstance =  allItems.OfType<ItemInstance>().ToList();

        if (_activeTab != null)
        {
            _activeTab.AddDataTrade(allItemsInstance);
        }
    }

    public ItemInstance GetActiveByPosition(int position)
    {
        if(_activeTab != null)
        {
           return _activeTab.GetSlotByPosition(position);
        }

        return null;
    }

}
