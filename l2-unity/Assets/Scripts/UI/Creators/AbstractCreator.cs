
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public abstract class AbstractCreator 
{
    protected ITab[] _tabsTradeTabs;
    protected ITab[] _tabsContentTabs;
    protected string[] _nameTabs;
    protected ITab _activeTab;
    public event Action<int, ItemCategory, int> EventLeftClick;
    public event Action<ITab , bool> EventSwitchOut;
    public void InitTradeTabs(string[] nameTabs)
    {
        _nameTabs = nameTabs;
        _tabsTradeTabs = new TradeTab[nameTabs.Length];
    }

    public void InitContentTabs(string[] nameTabs)
    {
        _nameTabs = nameTabs;
        _tabsContentTabs = new ContentTab[nameTabs.Length];
    }

    public int FindTabByName(string name)
    {
        for(int i=0; i < _tabsContentTabs.Length; i++)
        {
            ITab tab = _tabsContentTabs[i];
            if (tab.GetTabName() == name)
            {
                return i;
            }
        }
        return -1;
    }
    
    public void CreateTradeTabs(VisualElement inventoryTabView, VisualTreeAsset _tabTemplate, VisualTreeAsset _tabHeaderTemplate , SlotType slotType , bool isDragged)
    {
        VisualElement tabHeaderContainer = inventoryTabView.Q<VisualElement>("tab-header-container");
        VisualElement tabContainer = inventoryTabView.Q<VisualElement>("tab-content-container");

        if (inventoryTabView != null & _tabTemplate != null & _tabHeaderTemplate != null & tabContainer != null)
        {


            for (int i = _tabsTradeTabs.Count() - 1; i >= 0; i--)
            {
                string tabName = _nameTabs[i];

                VisualElement tabElement = _tabTemplate.CloneTree()[0];
                tabElement.name = tabName;
                tabElement.AddToClassList("unselected-tab");


                VisualElement tabHeaderElement = _tabHeaderTemplate.CloneTree()[0];
                tabHeaderElement.name = tabName;
                tabHeaderElement.Q<Label>().text = tabName;

                tabHeaderContainer.Add(tabHeaderElement);
                tabContainer.Add(tabElement);

                _tabsTradeTabs[i] = new TradeTab(tabName, 96, tabElement, tabHeaderElement, true , slotType , isDragged);

                _tabsTradeTabs[i].EventSwitch += OnSwitchEvent;
                _tabsTradeTabs[i].EventLeftClick += OnLeftClick;
            }
        }
        else
        {
            Debug.LogError("CreatorTradingWindows: Not Create Windows tabs!!!!");
        }

        if (_tabsTradeTabs.Length > 0)
        {
            SwitchTab(_tabsTradeTabs[0] , true , true);
        }

        SetMainTab(0, true , true);
    }

    public void ClearSlotsActiveTab(List<ItemInstance> oldListItems)
    {
        _activeTab.ClearSlots(oldListItems);
    }

    public void CreateTabs(VisualElement _inventoryTabView, VisualTreeAsset _tabTemplate, VisualTreeAsset _tabHeaderTemplate)
    {
        VisualElement tabHeaderContainer = _inventoryTabView.Q<VisualElement>("tab-header-container");
        VisualElement tabContainer = _inventoryTabView.Q<VisualElement>("tab-content-container");

        if (_inventoryTabView != null & _tabTemplate != null & _tabHeaderTemplate != null & tabContainer != null)
        {


            for (int i = _tabsContentTabs.Count() - 1; i >= 0; i--)
            {
                string tabName = _nameTabs[i];

                VisualElement tabElement = _tabTemplate.CloneTree()[0];


                //delete Scroll View
                tabElement.Clear();
                tabElement.Add(CreateEmtyContentVisualElement());

                tabElement.name = tabName;
                tabElement.AddToClassList("unselected-tab");




                VisualElement tabHeaderElement = _tabHeaderTemplate.CloneTree()[0];
                tabHeaderElement.name = tabName;
                tabHeaderElement.Q<Label>().text = tabName;

                tabHeaderContainer.Add(tabHeaderElement);
                tabContainer.Add(tabElement);

                _tabsContentTabs[i] = new ContentTab(tabName, tabElement, tabHeaderElement, true);

                _tabsContentTabs[i].EventSwitch += OnSwitchEvent;
                _tabsContentTabs[i].EventLeftClick += OnLeftClick;
            }
        }
        else
        {
            Debug.LogError("CreatorTradingWindows: Not Create Windows tabs!!!!");
        }

        if (_tabsContentTabs.Length > 0)
        {
            SwitchTab(_tabsContentTabs[0] , false , true);
        }

        SetMainTab(0, true , false);
    }

    public VisualElement CreateEmtyContentVisualElement()
    {
        var content = new VisualElement { name = "Content" };
        content.style.flexGrow = 1;
        content.style.flexShrink = 1;
        return content;
    }
    public void SwitchTab(ITab switchTo , bool isTrade , bool useEvent)
    {
        if (_activeTab != switchTo)
        {
            if (_activeTab != null) _activeTab.UnselectTabContainerClass();
            _activeTab = switchTo;
            if (_activeTab != null) _activeTab.SelectTabContainerClass();
            if(useEvent == true) EventSwitchOut?.Invoke(switchTo, isTrade);

        }
    }

    public void SetMainTab(int index, bool isMain , bool isTrade)
    {
        if (isTrade)
        {
            var tab = _tabsTradeTabs[index];
            tab.SetMainTab(isMain);
            EventSwitchOut?.Invoke(tab, isTrade);
        }
        else
        {
            var tab = _tabsContentTabs[index];
            tab.SetMainTab(isMain);
            EventSwitchOut?.Invoke(tab, isTrade);
        }

    }

    public void SwitchTab(int idTab , bool isTrade , bool useEvent)
    {
        if (isTrade)
        {
            if (_tabsTradeTabs == null) return;

            if (ArrayUtils.IsValidIndexArray(_tabsTradeTabs , idTab))
            {
                SwitchTab(_tabsTradeTabs[idTab], true , useEvent);
            }
        }
        else
        {
            if (_tabsContentTabs == null) return;

            if (ArrayUtils.IsValidIndexArray(_tabsContentTabs, idTab))
            {
                SwitchTab(_tabsContentTabs[idTab], false , useEvent);
            }
        }
    }
    public void OnSwitchEvent(ITab tab , bool isTrade)
    {
        if (isTrade)
        {
            if (_tabsTradeTabs.Length > 1)
            {
                if (tab != null)
                {
                    SwitchTab(tab , isTrade, true);
                }
            }
        }
        else
        {
            if (_tabsContentTabs.Length > 1)
            {
                if (tab != null)
                {
                    SwitchTab(tab , isTrade , true);
                }
            }
        }

    }

    public void OnLeftClick(int itemId, ItemCategory category, int position)
    {
        EventLeftClick?.Invoke(itemId, category, position);
    }

    public VisualElement GetActiveContent()
    {
       return  _activeTab.GetContentElement();
    }

    public string GetNameActiveTab()
    {
        return _activeTab.GetTabName();
    }

    
}



