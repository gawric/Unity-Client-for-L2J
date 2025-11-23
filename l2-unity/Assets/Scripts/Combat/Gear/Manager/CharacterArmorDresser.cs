using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterArmorDresser
{
    private readonly Transform _containerTransform;
    private readonly Dictionary<ItemSlot, ArmorDresserModel> _equippedArmor;

    public Action<GameObject> OnDestroyGameObject;
    public Action<int> OnSyncMash;
    public Action<int, ItemSlot> OnEquipArmor;

    public CharacterArmorDresser(Transform containerTransform)
    {
        _containerTransform = containerTransform;
        _equippedArmor = new Dictionary<ItemSlot, ArmorDresserModel>();
        InitializeArmorModels();
    }

    private void InitializeArmorModels()
    {
        var armorConfigs = new Dictionary<ItemSlot, List<ArmorDresserModel.ArmorPart>>
        {
            [ItemSlot.chest] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.FullArmor },
            [ItemSlot.fullarmor] = new List<ArmorDresserModel.ArmorPart> {
                ArmorDresserModel.ArmorPart.Torso,
                ArmorDresserModel.ArmorPart.Legs,
                ArmorDresserModel.ArmorPart.FullArmor
            },
            [ItemSlot.legs] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Legs, ArmorDresserModel.ArmorPart.FullArmor },
            [ItemSlot.gloves] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Gloves, ArmorDresserModel.ArmorPart.FullArmor },
            [ItemSlot.feet] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Boots }
        };

        foreach (var config in armorConfigs)
        {
            var armorModel = new ArmorDresserModel();
            foreach (var part in config.Value)
            {
                armorModel.Update(part, null, null);
            }
            _equippedArmor.Add(config.Key, armorModel);
        }
    }

    public void SetArmorPiece(Armor armor, GameObject armorPiece, ItemSlot slot)
    {
        if (armor == null || armorPiece == null)
        {
            Debug.LogWarning("Invalid armor data provided");
            return;
        }


        
        if (IsArmorEquipped(armor, slot))
        {
            Debug.Log($"Armor {armor.Id} is already equipped in slot {slot}");
            return;
        }

        HandleArmorConflicts(slot);


        EquipNewArmor(armor, armorPiece, slot);


        OnSyncMash?.Invoke(1);
    }

    private void HandleArmorConflicts(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.chest when HasEquipped(ItemSlot.fullarmor):
                UnequipArmorParts(ItemSlot.fullarmor, new[] { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.Legs });
                OnEquipArmor?.Invoke(ItemTable.NAKED_LEGS, ItemSlot.legs);
                break;

            case ItemSlot.legs when HasEquipped(ItemSlot.fullarmor):
                UnequipArmorParts(ItemSlot.fullarmor, new[] { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.Legs });
                OnEquipArmor?.Invoke(ItemTable.NAKED_CHEST, ItemSlot.chest);
                break;

            case ItemSlot.fullarmor:
                UnequipArmorParts(ItemSlot.fullarmor, new[] { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.Legs });
                break;
        }
    }

    private void UnequipArmorParts(ItemSlot slot, ArmorDresserModel.ArmorPart[] parts)
    {
        foreach (var part in parts)
        {
            var go = GetGameObject(slot, part);
            if (go != null)
            {
                DefaultDestroy(go);
                UpdateData(slot, part, null);
            }
        }
    }

    private void EquipNewArmor(Armor armor, GameObject armorPiece, ItemSlot slot)
    {
        var mainPart = GetMainArmorPart(slot);
        armorPiece.transform.SetParent(_containerTransform, false);

        UpdateGo(slot, mainPart, armorPiece);
        UpdateData(slot, mainPart, armor);
    }

    private ArmorDresserModel.ArmorPart GetMainArmorPart(ItemSlot slot)
    {
        return slot switch
        {
            ItemSlot.chest => ArmorDresserModel.ArmorPart.Torso,
            ItemSlot.fullarmor => ArmorDresserModel.ArmorPart.FullArmor,
            ItemSlot.legs => ArmorDresserModel.ArmorPart.Legs,
            ItemSlot.gloves => ArmorDresserModel.ArmorPart.Gloves,
            ItemSlot.feet => ArmorDresserModel.ArmorPart.Boots,
            _ => ArmorDresserModel.ArmorPart.Torso
        };
    }

    private bool HasEquipped(ItemSlot slot)
    {
        return _equippedArmor.ContainsKey(slot) &&
               _equippedArmor[slot].GetGo(ArmorDresserModel.ArmorPart.FullArmor) != null;
    }

    private void UpdateData(ItemSlot slot, ArmorDresserModel.ArmorPart part, Armor data)
    {
        _equippedArmor[slot].UpdateData(part, data);
    }

    public void UpdateGo(ItemSlot slot, ArmorDresserModel.ArmorPart part, GameObject go)
    {
        _equippedArmor[slot].UpdateGo(part, go);
    }

    public GameObject GetGameObject(ItemSlot slot, ArmorDresserModel.ArmorPart part)
    {
        return _equippedArmor[slot].GetGo(part);
    }

    private void DefaultDestroy(GameObject go)
    {
        OnDestroyGameObject?.Invoke(go);
    }

    /// <summary>
    /// Checks if the specified armor is already equipped
    /// </summary>
    /// <param name="armor">Armor to check</param>
    /// <param name="slot">Slot to check in</param>
    /// <returns>True if the armor is already equipped, false otherwise</returns>
    public bool IsArmorEquipped(Armor armor, ItemSlot slot)
    {
        if (armor == null || !_equippedArmor.ContainsKey(slot))
            return false;

        var equippedArmor = _equippedArmor[slot];
        var mainPart = GetMainArmorPart(slot);

        // Get the currently equipped armor data
        var currentArmor = equippedArmor.GetData(mainPart);

        // Compare item IDs to check if it's the same armor
        return currentArmor != null && currentArmor.Id == armor.Id;
    }

}


//old backup class

//using Org.BouncyCastle.Security;
//using System;
//using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
//using UnityEngine;

//public class CharacterArmorDresser
//{
//public Action<GameObject> OnDestroyGameObject;
//public Action<int> OnSyncMash;
//public Action<int, ItemSlot> OnEquipArmor;
//private Transform _containerTransform;

//private Dictionary<ItemSlot, ArmorModel> _equippedArmor = new Dictionary<ItemSlot, ArmorModel>();

// public CharacterArmorDresser(Transform containerTransform)
//{
////  _containerTransform = containerTransform;
// _equippedArmor = new Dictionary<ItemSlot, ArmorModel>();
// InitData();
//}

//private void InitData()
// {
//ArmorModel chest = new ArmorModel(new GameObject[2] { _torso , _fullarmor } , new Armor[2] { _torsoMeta , _fullarmorMeta });
// ArmorModel fullArmor = new ArmorModel(new GameObject[3] { _torso, _legs , _fullarmor }, new Armor[3] { _torsoMeta, _legsMeta , _fullarmorMeta });
// ArmorModel legs = new ArmorModel(new GameObject[2] { _legs, _fullarmor,  }, new Armor[2] { _legsMeta, _fullarmorMeta});
// ArmorModel gloves = new ArmorModel(new GameObject[2] { _gloves, _fullarmor, }, new Armor[2] { _glovesMeta, _fullarmorMeta });
// ArmorModel feet = new ArmorModel(new GameObject[1] { _boots, }, new Armor[1] { _bootsMeta });

//var chest = new ArmorModel();
//chest.Update(ArmorModel.ArmorPart.Torso, null, null);
//chest.Update(ArmorModel.ArmorPart.FullArmor, null, null);

//var fullArmor = new ArmorModel();
//fullArmor.Update(ArmorModel.ArmorPart.Torso, null, null);
//fullArmor.Update(ArmorModel.ArmorPart.Legs, null, null);
//fullArmor.Update(ArmorModel.ArmorPart.FullArmor, null, null);

//var legs = new ArmorModel();
//legs.Update(ArmorModel.ArmorPart.Legs, null, null);
//legs.Update(ArmorModel.ArmorPart.FullArmor, null, null);

//var gloves = new ArmorModel();
//gloves.Update(ArmorModel.ArmorPart.Gloves, null, null);
//gloves.Update(ArmorModel.ArmorPart.FullArmor, null, null);

//var feet = new ArmorModel();
//feet.Update(ArmorModel.ArmorPart.Boots, null, null);

//_equippedArmor.Add(ItemSlot.chest, chest);
//_equippedArmor.Add(ItemSlot.fullarmor, fullArmor);
//_equippedArmor.Add(ItemSlot.legs, legs);
//_equippedArmor.Add(ItemSlot.gloves, gloves);
//_equippedArmor.Add(ItemSlot.feet, feet);
//}

//public void SetArmorPiece(Armor armor, GameObject armorPiece, ItemSlot slot)
//{
//switch (slot)
// {
//case ItemSlot.chest:

// var _torso = GetGameObject(ItemSlot.chest, ArmorModel.ArmorPart.Torso);
//var _fullarmor = GetGameObject(ItemSlot.chest, ArmorModel.ArmorPart.FullArmor);

//if (_torso != null)
//{
//    DefaultDestroy(_torso);
//    UpdateData(ItemSlot.chest, ArmorModel.ArmorPart.Torso, null);
//}

//if (_fullarmor != null)
//{
// DefaultDestroy(_fullarmor);
// UpdateData(ItemSlot.chest, ArmorModel.ArmorPart.FullArmor, null);

// OnEquipArmor?.Invoke(ItemTable.NAKED_LEGS, ItemSlot.legs);
//}
// _torso = armorPiece;
// var tr = _containerTransform;

//_torso.transform.SetParent(tr, false);
// UpdateGo(ItemSlot.chest, ArmorModel.ArmorPart.Torso, _torso);
//UpdateData(ItemSlot.chest, ArmorModel.ArmorPart.Torso, armor);

//break;
//case ItemSlot.fullarmor:
// var _torsoFull = GetGameObject(ItemSlot.fullarmor, ArmorModel.ArmorPart.Torso);
//var _fullArmor = GetGameObject(ItemSlot.fullarmor, ArmorModel.ArmorPart.FullArmor);
//var _legs = GetGameObject(ItemSlot.fullarmor, ArmorModel.ArmorPart.Legs);

//if (_torsoFull != null)
//{
// DefaultDestroy(_torsoFull);
//UpdateData(ItemSlot.fullarmor, ArmorModel.ArmorPart.Torso, null);
//}
//if (_legs != null)
//{
// DefaultDestroy(_legs);
// UpdateData(ItemSlot.fullarmor, ArmorModel.ArmorPart.Legs, null);
//}
//_fullarmor = armorPiece;
//var _fullarmorMeta = armor;
//_fullarmor.transform.SetParent(_containerTransform, false);

//UpdateGo(ItemSlot.fullarmor, ArmorModel.ArmorPart.FullArmor, _fullarmor);
//UpdateData(ItemSlot.fullarmor, ArmorModel.ArmorPart.FullArmor, _fullarmorMeta);

////break;
//case ItemSlot.legs:
//var _legs1 = GetGameObject(ItemSlot.legs, ArmorModel.ArmorPart.Legs);
///var _fullArmor1 = GetGameObject(ItemSlot.legs, ArmorModel.ArmorPart.FullArmor);

//if (_legs1 != null)
//{
// DefaultDestroy(_legs1);
// UpdateData(ItemSlot.legs, ArmorModel.ArmorPart.Legs, null);
// }
//if (_fullArmor1 != null)
// {
// DefaultDestroy(_fullArmor1);
// UpdateData(ItemSlot.legs, ArmorModel.ArmorPart.FullArmor, null);
// OnEquipArmor?.Invoke(ItemTable.NAKED_CHEST, ItemSlot.chest);
// EquipArmor(ItemTable.NAKED_CHEST, ItemSlot.chest);
// }
//_legs1 = armorPiece;
//_legs1.transform.SetParent(_containerTransform, false);

//UpdateGo(ItemSlot.legs, ArmorModel.ArmorPart.Legs, _legs1);
//UpdateData(ItemSlot.legs, ArmorModel.ArmorPart.Legs, armor);

//break;
//case ItemSlot.gloves:

// var _gloves = GetGameObject(ItemSlot.gloves, ArmorModel.ArmorPart.Gloves);


////if (_gloves != null)
// {
//  DefaultDestroy(_gloves);
// UpdateData(ItemSlot.gloves, ArmorModel.ArmorPart.Gloves, null);
//}

//_gloves = armorPiece;
//_gloves.transform.SetParent(_containerTransform, false);

//UpdateGo(ItemSlot.gloves, ArmorModel.ArmorPart.Gloves, _gloves);
//UpdateData(ItemSlot.gloves, ArmorModel.ArmorPart.Gloves, armor);

//break;
//case ItemSlot.feet:

// var _boots = GetGameObject(ItemSlot.feet, ArmorModel.ArmorPart.Boots);

//if (_boots != null)
//{
//    DefaultDestroy(_boots);
//    UpdateData(ItemSlot.feet, ArmorModel.ArmorPart.Boots, null);
// }
//_boots = armorPiece;
//_boots.transform.SetParent(_containerTransform, false);

// UpdateGo(ItemSlot.feet, ArmorModel.ArmorPart.Boots, _boots);
// UpdateData(ItemSlot.feet, ArmorModel.ArmorPart.Boots, armor);

//break;
//}

//OnSyncMash?.Invoke(1);
//}

//private void UpdateData(ItemSlot slot, ArmorModel.ArmorPart part, Armor data)
///{
//   _equippedArmor[slot].UpdateData(part, data);
//}

//public void UpdateGo(ItemSlot slot, ArmorModel.ArmorPart part, GameObject go)
//{
//   _equippedArmor[slot].UpdateGo(part, go);
//}

// public GameObject GetGameObject(ItemSlot slot, ArmorModel.ArmorPart part)
// {
//     return _equippedArmor[slot].GetGo(part);
// }

//private void DefaultDestroy(GameObject go)
//{
//    OnDestroyGameObject?.Invoke(go);
//}
//}



