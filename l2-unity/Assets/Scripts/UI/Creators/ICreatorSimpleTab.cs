using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICreatorSimpleTab
{
    public void InitContentTabs(string[] nameTabs);
    public void CreateTabs(VisualElement _inventoryTabView, VisualTreeAsset _tabTemplate, VisualTreeAsset _tabHeaderTemplate);
    public void SetContent(int idTemplate);
    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc , string[] loadTemplate);

    public event Action<int, ItemCategory, int> EventLeftClick;

    public event Action<int> EventSwitchTabByIndexOfTab;

    public void SetClickActiveTab(int position);

    public void SwitchTab(int idTab, bool isTrade, bool useEvent);

    public VisualElement[] GetVisualElements(string[] name);
}
