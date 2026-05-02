using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsType 
{
    
    public static ItemCategory ParceCategory(int type2)
    {
        if (type2 == 0)
        {
            return ItemCategory.Weapon;
        }
        else if (type2 == 1)
        {
            return ItemCategory.ShieldArmor;
        }
        else if (type2 == 2)
        {
            return ItemCategory.Jewel;
        }
        else if (type2 == 3)
        {
            return ItemCategory.Quest;
        }
        else if (type2 == 4)
        {
            return ItemCategory.Adena;
        }
        else if (type2 == 5)
        {
            return ItemCategory.Item;
        }

        return ItemCategory.Item;

    }
    // Slot : 0006-lr.ear, 0008-neck, 0030-lr.finger, 0040-head, 0100-l.hand, 0200-gloves, 0400-chest, 0800-pants, 1000-feet, 4000-r.hand, 8000-r.hand
    public static ItemSlot ParceSlot(int slot)
    {
        switch (slot)
        {
            case 8:
                return ItemSlot.neck;
            case 64:
                return ItemSlot.head;
            case 100:
                return ItemSlot.lhand;
            case 512:
                return ItemSlot.gloves;
            case 1024:
                return ItemSlot.chest;
            case 128:
                return ItemSlot.rhand;
            case 6:
                return ItemSlot.lear;
            case 1000:
                return ItemSlot.feet;
            case 2048:
                return ItemSlot.legs;
            case 4096:
                return ItemSlot.boots;
            case 48:
                return ItemSlot.lfinger;
            case 256:
                return ItemSlot.lhand;
            case 16384:
                return ItemSlot.lrhand;
            case 32768: 
                return ItemSlot.fullarmor;

            default:
                return ItemSlot.none;
        }
    }
}
