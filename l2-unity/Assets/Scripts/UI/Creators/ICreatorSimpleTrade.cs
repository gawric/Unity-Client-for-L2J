using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public interface ICreatorSimpleTrade
{
    public void CreateSlots(VisualElement container, SlotType slotType, int countSlots = 96, bool isDragged = false);
    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc);
    public void AddData(List<ItemInstance> allItems, bool checkInventory = false);
}
