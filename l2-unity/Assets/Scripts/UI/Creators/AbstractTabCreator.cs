using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class AbstractTabCreator : AbstractLoaderTemplate 
{
    protected ITab _useTab;

    public void CreateSimpleTradeTab(VisualElement container , SlotType slotType, int countSlots , bool isDragged = false)
    {
        _useTab = new TradeTab("SimpleTan", countSlots, container, new VisualElement(), true, slotType, isDragged);
    }
}
