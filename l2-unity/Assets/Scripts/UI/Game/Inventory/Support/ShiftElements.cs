using UnityEngine;

public class ShiftElements
{
    public static void ShiftElementsLeft(InventorySlot[] inventorySlots, int newPosition)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i == newPosition)
            {
                var currentSlot = inventorySlots[i];
                var nextSlot = GetNextSlot(i, inventorySlots);
                HandleCurrentPosition(inventorySlots, currentSlot, nextSlot);
            }
            else if (i > newPosition)
            {
                var currentSlot = inventorySlots[i];
                var nextSlot = GetNextSlot(i, inventorySlots);
                if (nextSlot == null) return;
                HandleSlotsAfterCurrent(inventorySlots, currentSlot, nextSlot);
            }
        }
    }



    private static void HandleCurrentPosition(InventorySlot[] inventorySlots, InventorySlot currentSlot, InventorySlot nextSlot)
    {
        object data = nextSlot.GetUseElement();
        inventorySlots[currentSlot.Position].AssignUniversal(data);
       // Debug.Log("next Element 1 ");
        if (nextSlot.IsEmpty)
        {
            //Debug.Log("next Element 1  empty");
            nextSlot.ManualHideToolTips();
            inventorySlots[currentSlot.Position].AssignEmpty();
        }
    }


    private static void HandleSlotsAfterCurrent(InventorySlot[] inventorySlots, InventorySlot currentSlot, InventorySlot nextSlot)
    {
        ElseNextSlotEmpty(inventorySlots, nextSlot, currentSlot);
    }


    private static void ElseNextSlotEmpty(InventorySlot[] inventorySlots, InventorySlot nextSlot, InventorySlot slot)
    {
        if (nextSlot.IsEmpty)
        {
            ///Debug.Log("next Element 2  empty");
            inventorySlots[slot.Position].AssignEmpty();
        }
        else
        {
            object data  = nextSlot.GetUseElement();
            inventorySlots[slot.Position].AssignUniversal(data);
        }
    }

    private static InventorySlot GetNextSlot(int i, InventorySlot[] _inventorySlotsBuy)
    {
        return (i + 1 < _inventorySlotsBuy.Length) ? _inventorySlotsBuy[i + 1] : null;
    }


}
