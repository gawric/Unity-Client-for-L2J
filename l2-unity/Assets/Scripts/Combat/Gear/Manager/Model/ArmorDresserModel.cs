
using System.Collections.Generic;
using UnityEngine;

public class ArmorDresserModel
{
    private readonly Dictionary<ArmorPart, (GameObject go, Armor armor)> _armorData;

    public enum ArmorPart
    {
        Torso,
        Legs,
        FullArmor,
        Gloves,
        Boots
    }

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

    public Armor GetData(ArmorPart part)
    {
        return _armorData.TryGetValue(part, out var data) ? data.armor : null;
    }
}