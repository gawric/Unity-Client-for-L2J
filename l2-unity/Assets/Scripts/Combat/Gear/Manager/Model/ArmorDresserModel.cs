
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ArmorDresserModel : IDresserModel
{
    private readonly Dictionary<ArmorPart, (GameObject go, Armor armor)> _armorData;

    public ArmorDresserModel()
    {
        _armorData = new Dictionary<ArmorPart, (GameObject, Armor)>();
    }

    public virtual void Update(ArmorPart part, IEnumerable<GameObject> go, IEnumerable<Armor> armor)
    {
        _armorData[part] = (go == null || armor == null)? (null, null) : (go.First(), armor.First());
    }

    public virtual  void UpdateGo(ArmorPart part, GameObject go)
    {
        if (_armorData.TryGetValue(part, out var current))
        {
            _armorData[part] = (go, current.armor);
        }
    }

    public virtual void UpdateData(ArmorPart part, Armor armor)
    {
        if (_armorData.TryGetValue(part, out var current))
        {
            _armorData[part] = (current.go, armor);
        }
    }

    public virtual GameObject GetGo(ArmorPart part)
    {
        return _armorData.TryGetValue(part, out var data) ? data.go : null;
    }

    public virtual GameObject GetGo(ItemSlot slot)
    {
        return (GameObject)GetItemData(slot, true);
    }

    public virtual Armor GetData(ItemSlot slot)
    {
        return (Armor)GetItemData(slot, false);
    }

    private object GetItemData(ItemSlot slot, bool returnGameObject)
    {
        switch (slot)
        {
            case ItemSlot.chest:
                return returnGameObject ? (object)GetGo(ArmorPart.Torso) : GetData(ArmorPart.Torso);
            case ItemSlot.feet:
                return returnGameObject ? (object)GetGo(ArmorPart.Boots) : GetData(ArmorPart.Boots);
            case ItemSlot.legs:
                return returnGameObject ? (object)GetGo(ArmorPart.Legs) : GetData(ArmorPart.Legs);
            case ItemSlot.gloves:
                return returnGameObject ? (object)GetGo(ArmorPart.Gloves) : GetData(ArmorPart.Gloves);
            case ItemSlot.fullarmor:
                return returnGameObject ? (object)GetGo(ArmorPart.Torso) : GetData(ArmorPart.Torso);
            default:
                return null;
        }
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