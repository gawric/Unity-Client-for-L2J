using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICreatorVerticalScroll
{
    public event Action<int, ItemCategory, int> EventLeftClick;
    public event Action<int> EventSwitchTabByIndexOfTab;
    public void CreateTabs(VisualElement content, VisualTreeAsset empty, VisualTreeAsset templateItems);
    public void AddOtherData(List<OtherModel> allItems);

    public void HandleClickDown(MouseDownEvent evt, AcquireData item);
}
