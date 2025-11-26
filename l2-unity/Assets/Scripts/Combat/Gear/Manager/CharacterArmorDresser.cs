using System;
using System.Collections.Generic;
using UnityEngine;
using static ModelTable;

public class CharacterArmorDresser
{
    private readonly Transform _containerTransform;
    private readonly Dictionary<ItemSlot, ArmorDresserModel> _equippedArmor;

    public Action<GameObject> OnDestroyGameObject;
    public Action<int> OnSyncMash;
    public Action<GameObject> OnAddSyncMash;
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
            [ItemSlot.chest] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Torso },
            [ItemSlot.fullarmor] = new List<ArmorDresserModel.ArmorPart> {
                ArmorDresserModel.ArmorPart.Torso,
                ArmorDresserModel.ArmorPart.Legs,
            },
            [ItemSlot.legs] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Legs},
            [ItemSlot.gloves] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Gloves},
            [ItemSlot.feet] = new List<ArmorDresserModel.ArmorPart> { ArmorDresserModel.ArmorPart.Boots },
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


        EquipNewArmor(slot, armor, armorPiece);
        OnSyncMash?.Invoke(1);
        armorPiece.SetActive(true);
    }

    public void UnequipArmorPiece(ItemSlot slot , int unequipId , Armor baseArmor, GameObject armorPiece)
    {

        ItemSlot slotArmor = GetExtendedOrGetCurrentArmorPart(slot, _equippedArmor);

        switch (slotArmor)
        {
            case ItemSlot.chest when HasEquipped(ItemSlot.chest , unequipId):
                //UnequipArmorParts(ItemSlot.chest, new[] { ArmorDresserModel.ArmorPart.Torso});
                EquipNewArmor(slotArmor , baseArmor, armorPiece);
                OnSyncMash?.Invoke(1);
                armorPiece.SetActive(true);
                break;

            case ItemSlot.feet when HasEquipped(ItemSlot.feet, unequipId):
                //UnequipArmorParts(ItemSlot.feet, new[] { ArmorDresserModel.ArmorPart.Boots });

                Debug.Log("Existing armor piece was " + destroy);
                EquipNewArmor(slotArmor, baseArmor, armorPiece);
                OnSyncMash?.Invoke(1);
                armorPiece.SetActive(true);
                break;

                // case ItemSlot.legs when HasEquipped(ItemSlot.fullarmor):
                //    UnequipArmorParts(ItemSlot.fullarmor, new[] { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.Legs });
                //    OnEquipArmor?.Invoke(ItemTable.NAKED_CHEST, ItemSlot.chest);
                //    break;

                // case ItemSlot.fullarmor:
                //    UnequipArmorParts(ItemSlot.fullarmor, new[] { ArmorDresserModel.ArmorPart.Torso, ArmorDresserModel.ArmorPart.Legs });
                //    break;
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

    public bool destroy = false;

    private void EquipNewArmor(ItemSlot slot , Armor armor, GameObject armorPiece)
    {
        var mainPart = GetMainArmorPart(slot);

        if (ArmorDresserModel.ArmorPart.Unknow == mainPart)
        {
            Debug.LogWarning($"CharacterArmorDresser: EquipNewArmor-> Unknown armor part detected for slot {slot}. Armor will not be equipped.");
            return;
        }

        var existingArmorPiece = GetGameObject(slot, mainPart);

        if (existingArmorPiece != null)
        {
            DefaultDestroy(existingArmorPiece);
        }

        //OnAddSyncMash?.Invoke(armorPiece);
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
            _ => ArmorDresserModel.ArmorPart.Unknow
        };
    }

    private bool HasEquipped(ItemSlot slot, int itemId)
    {
        if (_equippedArmor.ContainsKey(slot) && _equippedArmor[slot].GetGo(slot) != null)
        {

            var equippedArmor = _equippedArmor[slot].GetData(slot);
 
            return equippedArmor != null && equippedArmor.Id == itemId;
        }
        return false;
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
        //Debug.Log("Existing armor piece was not destroyed properly. 1");
        //ObjectPoolManager.Instance.ReturnToPool(ObjectType.Armor , go);
        OnDestroyGameObject?.Invoke(go);
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

    public ItemSlot GetExtendedOrGetCurrentArmorPart(ItemSlot slot, Dictionary<ItemSlot, ArmorDresserModel> equippedArmor)
    {
        return equippedArmor.ContainsKey(slot) ? slot : ArmorDresserModel.GetExtendedArmorPart(slot);
    }

    public ItemSlot GetExtendedOrGetCurrentArmorPart(ItemSlot slot)
    {
        return _equippedArmor.ContainsKey(slot) ? slot : ArmorDresserModel.GetExtendedArmorPart(slot);
    }
}




