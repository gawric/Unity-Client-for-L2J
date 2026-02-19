using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatorTabsWindows : AbstractCreator, ICreator
{
    private bool _useAllTabs;
    private ICreatorTables _creatorTable;
    private List<TableColumn> _dataColumn;
    public event Action<int> EventSwitchTabByIndexOfTab;

    public CreatorTabsWindows()
    {
        EventSwitchOut += OnSwitchEventOut;
    }
    public void AddData(List<ItemInstance> allItems)
    {
        throw new System.NotImplementedException();
    }

    public void AddOtherData(List<OtherModel> allItems)
    {
        throw new System.NotImplementedException();
    }

    public ItemInstance GetActiveByPosition(int position)
    {
        throw new System.NotImplementedException();
    }



    public void InsertTablesIntoContent(ICreatorTables creatorTable, List<TableColumn> dataColumn , bool useAllTabs)
    {
        VisualElement element = GetActiveContent();

        _useAllTabs = useAllTabs;
        _creatorTable = creatorTable;
        _dataColumn = dataColumn;

        if (element != null)
        {
            _creatorTable.InitTable(element);
            _creatorTable.CreateTable(dataColumn);
        }
        else
        {
            Debug.LogError("CreatorTabsWindows >>> InsertTablesIntoContent Not Found Content Element!!!");
        }

    }

    public void SetClickActiveTab(int position)
    {
        throw new System.NotImplementedException();
    }

    public void OnSwitchEventOut(ITab tab, bool isTrade)
    {
        if (_useAllTabs)
        {
           OnEventSwitchTab(FindTabByName(tab.GetTabName()));

           VisualElement element = GetActiveContent();

          _creatorTable.DestroyTable();
          element.Clear();
          _creatorTable.InitTable(element);
          _creatorTable.CreateTable(_dataColumn);
        }

    }

    public void OnEventSwitchTab(int tabIndex)
    {
     
        if (EventSwitchTabByIndexOfTab != null)
        {
            EventSwitchTabByIndexOfTab(tabIndex);
        }
    }

    public void RefreshDataColumns(List<TableColumn> dataColumns)
    {
        _dataColumn = dataColumns;
    }
    //Tabs do not own it, this is a separate element at the very bottom if you need to fill something with buttons or something else depending on the tab
    public void InsertFooterIntoContent(VisualElement footerElement , VisualElement root)
    {
        if(root != null && footerElement != null)
        {
            root.Clear();
            root.Add(footerElement);
        }
    }
}
