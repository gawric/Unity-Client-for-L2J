using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractTab
{
   
    private bool _mainTab = false;
    protected VisualElement _tabContainer;
    protected VisualElement _tabHeader;
    protected VisualElement _contentContainer;
    public event Action<ITab , bool> EventSwitch;
    public event Action<int, ItemCategory, int> EventLeftClick;
    public string TabName { get { return _tabName; } }
    protected string _tabName = "Tab";
    public void SetMainTab(bool main)
    {
        _mainTab = main;
    }

    public void UnselectTabContainerClass()
    {
        if (_tabContainer != null && _tabHeader != null)
        {
            _tabContainer.AddToClassList("unselected-tab");
            _tabHeader.RemoveFromClassList("active");
        }
        else
        {
            Debug.LogError("TradeTab > RefreshTabContainerClass Not Found _tabContainer or _tabHeader");
        }

    }

    public void SelectTabContainerClass()
    {
        if (_tabContainer != null && _tabHeader != null)
        {
            _tabContainer.RemoveFromClassList("unselected-tab");
            _tabHeader.AddToClassList("active");
        }
        else
        {
            Debug.LogError("TradeTab > RefreshTabContainerClass Not Found _tabContainer or _tabHeader");
        }

    }

    protected virtual void OnEventSwitch(ITab tab , bool isTrade)
    {
        EventSwitch?.Invoke(tab , isTrade);
    }

    protected virtual void OnEventLeftClick(int itemId, ItemCategory category, int position)
    {
        EventLeftClick?.Invoke(itemId, category, position);
    }

    public bool GetMainTab()
    {
        return _mainTab;
    }




}
