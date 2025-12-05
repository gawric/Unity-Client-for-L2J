using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractArmorDresser
{
    private Dictionary<ItemSlot, List<ArmorDresserModel.ArmorPart>> _armorConfigs;
    protected  Dictionary<ItemSlot, IDresserModel> _equippedArmor;

    public List<ArmorDresserModel.ArmorPart> GetArmorPartsForSlot(ItemSlot slot)
    {
        return _armorConfigs[slot];
    }

    protected void InitializeArmorModels()
    {
        _armorConfigs = new Dictionary<ItemSlot, List<ArmorDresserModel.ArmorPart>>
        {
            [ItemSlot.chest] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Torso },
            [ItemSlot.fullarmor] = new List<CompositeArmorModel.ArmorPart> {
                ArmorDresserModel.ArmorPart.Torso,
                ArmorDresserModel.ArmorPart.Legs,
            },
            [ItemSlot.legs] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Legs },
            [ItemSlot.gloves] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Gloves },
            [ItemSlot.feet] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Boots },
        };

        foreach (var config in _armorConfigs)
        {

            IDresserModel armorModel = new ArmorDresserModel();
            if (ItemSlot.fullarmor == config.Key) armorModel = new CompositeArmorModel();

            foreach (var part in config.Value)
            {
                armorModel.Update(part, null, null);
            }
            _equippedArmor.Add(config.Key, armorModel);
        }
    }



    protected ArmorDresserModel.ArmorPart GetMainArmorPart(ItemSlot slot)
    {
        return slot switch
        {
            ItemSlot.chest => ArmorDresserModel.ArmorPart.Torso,
            ItemSlot.fullarmor => ArmorDresserModel.ArmorPart.FullArmor,
            ItemSlot.legs => ArmorDresserModel.ArmorPart.Legs,
            ItemSlot.gloves => ArmorDresserModel.ArmorPart.Gloves,
            ItemSlot.feet => ArmorDresserModel.ArmorPart.Boots,
            _ => ArmorDresserModel.ArmorPart.Unknow
        };
    }

    protected bool HasEquipped(ItemSlot slot, int itemId)
    {
        if (_equippedArmor.ContainsKey(slot) && _equippedArmor[slot].GetGo(slot) != null)
        {

            var equippedArmor = _equippedArmor[slot].GetData(slot);

            return equippedArmor != null && equippedArmor.Id == itemId;
        }
        return false;
    }


    protected void UpdateData(ItemSlot slot, ArmorDresserModel.ArmorPart part, Armor data)
    {
        if (_equippedArmor.ContainsKey(slot))
        {
            _equippedArmor[slot].UpdateData(part, data);
        }

    }

    protected void UpdateGo(ItemSlot slot, ArmorDresserModel.ArmorPart part, GameObject go)
    {
        if (_equippedArmor.ContainsKey(slot))
        {
            _equippedArmor[slot].UpdateGo(part, go);
        }

    }
    protected GameObject GetGameObject(ItemSlot slot, ArmorDresserModel.ArmorPart part)
    {
        if (_equippedArmor.ContainsKey(slot))
        {
            return _equippedArmor[slot].GetGo(part);
        }
        return null;
    }

    public bool IsArmorEquipped(Armor armor, ItemSlot slot)
    {
        if (armor == null)
            return false;

        if (_equippedArmor.ContainsKey(slot))
        {
            var equippedArmor = _equippedArmor[slot];
            var mainPart = GetMainArmorPart(slot);
            var currentArmor = equippedArmor.GetData(mainPart);
            return currentArmor != null && currentArmor.Id == armor.Id;
        }

        return false;
    }

    public bool IsFullPlateEquipped(ItemSlot slot)
    {
        if (_equippedArmor.ContainsKey(slot))
        {
            var equippedArmor = _equippedArmor[slot];
            var mainPartChest= GetMainArmorPart(ItemSlot.chest);
            var mainPartLegs = GetMainArmorPart(ItemSlot.legs);

            if (equippedArmor.GetData(mainPartChest) != null 
                || equippedArmor.GetData(mainPartLegs) != null) return true;
        }

        return false;
    }

    public ItemSlot GetExtendedOrGetCurrentArmorPart(ItemSlot slot, Dictionary<ItemSlot, IDresserModel> equippedArmor)
    {
        return equippedArmor.ContainsKey(slot) ? slot : ArmorDresserModel.GetExtendedArmorPart(slot);
    }

    public ItemSlot GetExtendedOrGetCurrentArmorPart(ItemSlot slot)
    {
        return _equippedArmor.ContainsKey(slot) ? slot : ArmorDresserModel.GetExtendedArmorPart(slot);
    }


}
