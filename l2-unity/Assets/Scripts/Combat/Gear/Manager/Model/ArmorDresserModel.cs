
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ArmorDresserModel
{
    private readonly Dictionary<ArmorPart, (GameObject go, Armor armor)> _armorData;


    public ArmorDresserModel()
    {
        _armorData = new Dictionary<ArmorPart, (GameObject, Armor)>();
    }

    public void Update(ArmorPart part, GameObject go, Armor armor)
    {
        _armorData[part] = (go, armor);
    }

    public void UpdateGo(ArmorPart part, GameObject go)
    {
        if (_armorData.TryGetValue(part, out var current))
        {
            _armorData[part] = (go, current.armor);
        }
    }

    public void UpdateData(ArmorPart part, Armor armor)
    {
        if (_armorData.TryGetValue(part, out var current))
        {
            _armorData[part] = (current.go, armor);
        }
    }

    public GameObject GetGo(ArmorPart part)
    {
        return _armorData.TryGetValue(part, out var data) ? data.go : null;
    }



    public GameObject GetGo(ItemSlot slot)
    {
        if (slot == ItemSlot.chest)
        {
            return GetGo(ArmorPart.Torso);
        }else if (slot == ItemSlot.feet)
        {
            return GetGo(ArmorPart.Boots);
        }
            return null;
    }

    public Armor GetData(ItemSlot slot)
    {
        if (slot == ItemSlot.chest)
        {
            return GetData(ArmorPart.Torso);
        }
        else if (slot == ItemSlot.feet)
        {
            return GetData(ArmorPart.Boots);
        }
        return null;
    }




    public Armor GetData(ArmorPart part)
    {
        return _armorData.TryGetValue(part, out var data) ? data.armor : null;
    }




    public enum ArmorPart
    {
        Torso,
        Legs,
        FullArmor,
        Gloves,
        Boots,
        Unknow,
    }

    public static ItemSlot GetExtendedArmorPart(ItemSlot slot)
    {
        if(ItemSlot.boots == slot)
        {
            return ItemSlot.feet;
        }
        return slot;
    }

}