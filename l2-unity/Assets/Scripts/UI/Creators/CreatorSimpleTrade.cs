using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class CreatorSimpleTrade : AbstractTabCreator, ICreatorSimpleTrade
{
    
    private const string containerTabName = "Data/UI/_Elements/Template/SimpleTab/TabContainer";
    private VisualElement _elementContainerTab;
    //240px
    private int _defaultMaximumWidthPx = 240;

    public void AddData(List<ItemInstance> allItems , bool checkInventory = false)
    {
        if(allItems != null)
        {
            _useTab.ClearAllSlots();
            _useTab.AddDataTrade(allItems , checkInventory);
        }

    }

    public void CreateSlots(VisualElement container, SlotType slotType, int countSlots = 96 , bool isDragged = false)
    {
        _elementContainerTab = container;
        container.Add(GetTemplateById(0));

        if(_elementContainerTab != null)
        {
            CreateSimpleTradeTab(container, slotType, countSlots,  isDragged);
        }
        else
        {
             Debug.LogError("CreatorSimpleTrade>CreateSlots : Element Container Tab is null");
        }

    }

 

    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        base.LoadAsset(loaderFunc, new string[1] { containerTabName });
    }
}
