using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public interface ICreator
{
    //public void InitTabs(string[] nameTabs);


    public void InitContentTabs(string[] nameTabs);

    public void CreateTabs(VisualElement _inventoryTabView, VisualTreeAsset _tabTemplate, VisualTreeAsset _tabHeaderTemplate);

    public void AddData(List<ItemInstance> allItems);

    public void AddOtherData(List<OtherModel> allItems);

    public event Action<int , ItemCategory , int> EventLeftClick;

    public event Action<int> EventSwitchTabByIndexOfTab;

    public void RefreshDataColumns(List<TableColumn>dataColumns);

    public void SetClickActiveTab(int position);

    public ItemInstance GetActiveByPosition(int position);

    public void InsertTablesIntoContent(ICreatorTables creatorTable , List<TableColumn> dataColumn, bool useAllTabs);

    public void InsertFooterIntoContent(VisualElement footerElement, VisualElement root);

    public void SwitchTab(int idTab, bool isTrade , bool useEvent);


}
