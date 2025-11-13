using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ContentTab : AbstractTab , ITab
{

    public ContentTab(string tabName, VisualElement tabContainer, VisualElement tabHeader, bool initEmpty)
    {
        _tabName = tabName;
        _tabHeader = tabHeader;
        _tabContainer = tabContainer;
        _contentContainer = tabContainer.Q<VisualElement>("Content");
        OnRegisterClickTab(tabHeader);
    }

  
    public VisualElement GetContentElement()
    {
        return _contentContainer;
    }

    public void AddDataTrade(List<ItemInstance> allItems, bool isInventory = false)
    {
        throw new NotImplementedException();
    }

    public ItemInstance GetSlotByPosition(int position)
    {
        throw new NotImplementedException();
    }

    public void OnClickLeftEvent(int position)
    {
        throw new NotImplementedException();
    }

    private void OnRegisterClickTab(VisualElement tabHeader)
    {
        tabHeader.RegisterCallback<MouseDownEvent>(evt => {
            OnEventSwitch(this , false);
        }, TrickleDown.TrickleDown);
    }

    public string GetTabName()
    {
        return _tabName;
    }

    public void ClearSlots(List<ItemInstance> oldListItems)
    {
        throw new NotImplementedException();
    }

    public void ClearAllSlots()
    {
        throw new NotImplementedException();
    }
}
