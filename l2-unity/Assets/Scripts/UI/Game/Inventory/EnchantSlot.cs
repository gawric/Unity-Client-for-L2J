using UnityEngine;
using UnityEngine.UIElements;

public class EnchantSlot : InventorySlot
{
   
    public EnchantSlot(int position, VisualElement slotElement, L2Tab tab, SlotType slotType, bool rightMouseup) : base(position, slotElement, tab, slotType, rightMouseup)
    {
       
    }

    protected override void HandleLeftClick()
    {

       SetSelected();

    }

    protected override void HandleRightClick()
    {
        SetSelected();
    }

    public void UnSelectSlot()
    {
        UnSelect();
    }

}
