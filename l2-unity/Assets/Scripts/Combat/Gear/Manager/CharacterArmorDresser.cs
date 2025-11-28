using System;
using System.Collections.Generic;
using UnityEngine;
using static ModelTable;

public class CharacterArmorDresser : AbstractArmorDresser 
{
    private readonly Transform _containerTransform;
    public Action<GameObject> OnDestroyGameObject;
    public Action<int> OnSyncMash;
    public Action<GameObject> OnAddSyncMash;
    public Action<int, ItemSlot> OnEquipArmor;

    public CharacterArmorDresser(Transform containerTransform)
    {
        _containerTransform = containerTransform;
        _equippedArmor = new Dictionary<ItemSlot, IDresserModel>();
        InitializeArmorModels();
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

    public void SetFullArmor(bool refresh , Armor armor, GameObject armorPiece, ItemSlot slotInFullArmor , ItemSlot fullPlate = ItemSlot.fullarmor)
    {
        if (armor == null || armorPiece == null)
        {
            Debug.LogWarning("Invalid armor data provided");
            return;
        }

        //if (IsArmorEquipped(armor, fullPlate))
        //{
        //    Debug.Log($"Armor {armor.Id} is already equipped in slot {slotInFullArmor}");
         //   return;
       // }

        EquipNewArmorFullPlate(armor, armorPiece , fullPlate, slotInFullArmor);
        if(refresh) OnSyncMash?.Invoke(1);
        armorPiece.SetActive(true);
    }

    public void UnequipArmorPiece(ItemSlot slot , int unequipId , Armor baseArmor, GameObject armorPiece)
    {
        ItemSlot slotArmor = GetExtendedOrGetCurrentArmorPart(slot, _equippedArmor);
        switch (slot)
        {
            case ItemSlot.chest:
            case ItemSlot.feet:
            case ItemSlot.legs:
            case ItemSlot.gloves:
                if (HasEquipped(slotArmor, unequipId))
                {
                    EquipNewArmor(slotArmor, baseArmor, armorPiece);
                    OnSyncMash?.Invoke(1);
                    armorPiece.SetActive(true);
                }
                break;

            default:
                Debug.LogWarning($"UnequipArmorPiece: Slot {slot} is not supported for unequipping");
                break;
        }
    }


    private void EquipNewArmor(ItemSlot slot , Armor armor, GameObject armorPiece)
    {
        var mainPart = GetMainArmorPart(slot);

        if (ArmorDresserModel.ArmorPart.Unknow == mainPart)
        {
            Debug.LogWarning($"CharacterArmorDresser: EquipNewArmor-> Unknown armor part detected for slot {slot}. Armor will not be equipped.");
            return;
        }

        Destroy(GetGameObject(slot, mainPart));

        //OnAddSyncMash?.Invoke(armorPiece);
        SetParent(armorPiece);

        UpdateGo(slot, mainPart, armorPiece);
        UpdateData(slot, mainPart, armor);
    }

    private void EquipNewArmorFullPlate(Armor armor, GameObject armorPiece , ItemSlot slotFullPlater, ItemSlot insideFullPlate)
    {
        var mainPart = GetMainArmorPart(insideFullPlate);
        Destroy(GetGameObject(insideFullPlate, mainPart));

        //OnAddSyncMash?.Invoke(armorPiece);
        SetParent(armorPiece);

        UpdateGo(slotFullPlater, mainPart, armorPiece);
        UpdateData(slotFullPlater, mainPart, armor);
    }

    private void SetParent(GameObject armorPiece)
    {
        armorPiece.transform.SetParent(_containerTransform, false);
    }
    private void Destroy(GameObject existingArmorPiece)
    {
        if (existingArmorPiece != null)
        {
            DefaultDestroy(existingArmorPiece);
        }

    }

    private void DefaultDestroy(GameObject go)
    {
        OnDestroyGameObject?.Invoke(go);
    }



}




