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

    public void SetArmorPiece(Armor armor, GameObject armorPiece, ItemSlot slot  , Armor[] defaultArmor , GameObject[] defaultGo)
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

        if(ItemSlot.chest == slot | ItemSlot.legs == slot)
        {
            if (IsFullPlateEquipped(ItemSlot.fullarmor))
            {
                ResetFullArmor(ItemSlot.fullarmor, slot, defaultArmor, defaultGo);
                //OnSyncMash?.Invoke(1);
            }
            else
            {
                if (ItemSlot.chest == slot)
                {
                    DestroyIfNotUse(false, defaultGo);
                }
                else if(ItemSlot.legs == slot)
                {
                    DestroyIfNotUse(false, defaultGo);
                }
            }
       
        }
        else
        {
            DestroyIfNotUse(false , defaultGo);
        }


        EquipNewArmor(slot, armor, armorPiece);
        OnSyncMash?.Invoke(1);
        armorPiece.SetActive(true);
    }

    public void SetFullArmor(bool refresh , Armor armor, GameObject armorPiece, ItemSlot slotInFullArmor , ItemSlot fullPlate = ItemSlot.fullarmor)
    {
        if (armor == null || armorPiece == null)
        {
            Debug.LogWarning("Usergear->SetFullArmor: Invalid armor data provided");
            return;
        }

        if (!IsFullPlateEquipped(ItemSlot.fullarmor))
        {
            ResetArmorPiece(ItemSlot.chest);
            ResetArmorPiece(ItemSlot.legs);
        }

        EquipNewArmorFullPlate(armor, armorPiece, fullPlate, slotInFullArmor);

        if(refresh) OnSyncMash?.Invoke(1);
        armorPiece.SetActive(true);
    }

    public void UnequipArmorPiece(ItemSlot slot , int unequipId , Armor[] baseArmor, GameObject[] armorPiece)
    {
        ItemSlot slotArmor = GetExtendedOrGetCurrentArmorPart(slot, _equippedArmor);
        bool isUseDefault = false;
        switch (slotArmor)
        {
            case ItemSlot.chest:
            case ItemSlot.feet:
            case ItemSlot.legs:
            case ItemSlot.gloves:
                if (HasEquipped(slotArmor, unequipId))
                {
                    if(armorPiece != null)
                    {
                        var defaultPiece = armorPiece[0];
                        var defaultArmor = baseArmor[0];
                        EquipNewArmor(slotArmor, defaultArmor, defaultPiece);
                        OnSyncMash?.Invoke(1);
                        defaultPiece.SetActive(true);
                        isUseDefault = true;
                    }
                }
                else
                {
                    isUseDefault = false;
                }
               break;
            case ItemSlot.fullarmor:
                isUseDefault = true;
                if (HasEquipped(slotArmor, unequipId))
                {

                    UnequipFullPlateArmor(slot, slotArmor, baseArmor, armorPiece);
                }
                else
                {
                    isUseDefault = false;
                }
                    break;

            default:
                Debug.LogWarning($"UnequipArmorPiece: Slot {slotArmor} is not supported for unequipping");
                break;
        }

        DestroyIfNotUse(isUseDefault, armorPiece);
    }

    private void DestroyIfNotUse( bool isUse , GameObject[] listGameObject)
    {
        if(isUse == false)
        {
            foreach (GameObject go in listGameObject)
            {
                Destroy(go);
            }
        }

    }


    private void UnequipFullPlateArmor(ItemSlot slot, ItemSlot slotArmor, Armor[] baseArmor, GameObject[] armorPiece)
    {
        var pieceChest = armorPiece[0];
        var pieceLegs = armorPiece[1];

        var defaltArmorChest = baseArmor[0];
        var defaltArmorLegs = baseArmor[1];

        var mainPartChest = GetMainArmorPart(ItemSlot.chest);
        var mainPartLegs = GetMainArmorPart(ItemSlot.legs);

        Destroy(GetGameObject(slot, mainPartChest));
        Destroy(GetGameObject(slot, mainPartLegs));

        UpdateDataFullPlate(slot, mainPartChest, null, null);
        UpdateDataFullPlate(slot, mainPartLegs, null, null);


        EquipNewArmor(ItemSlot.chest, defaltArmorChest, pieceChest);
        EquipNewArmor(ItemSlot.legs, defaltArmorLegs, pieceLegs);

        OnSyncMash?.Invoke(1);
        pieceChest.SetActive(true);
        pieceLegs.SetActive(true);
    }

    public void ResetFullArmor( ItemSlot slot , ItemSlot useSlot , Armor[] defaultArmor, GameObject[] defaultGo)
    {

        var mainPartChest = GetMainArmorPart(ItemSlot.chest);
        var mainPartLegs = GetMainArmorPart(ItemSlot.legs);

        Debug.Log("Destroy test 1 chest " + " slot " + slot + " data " + mainPartChest.ToString() + " go " + GetGameObject(slot, mainPartChest));
        Debug.Log("Destroy test 2 legs " + " slot " + slot + " data " + mainPartLegs.ToString() + "go " + GetGameObject(slot, mainPartLegs));
        Destroy(GetGameObject(slot, mainPartChest));
        Destroy(GetGameObject(slot, mainPartLegs));

        UpdateDataFullPlate(slot, mainPartChest, null, null);
        UpdateDataFullPlate(slot, mainPartLegs, null, null);

        ResetToDefaultEquipment( useSlot, defaultArmor, defaultGo);
    }

    private void ResetToDefaultEquipment(ItemSlot useSlot , Armor[] defaultArmor, GameObject[] defaultGo)
    {
        //0 - m001_u
        //1 - m001_l
        if (defaultArmor != null && defaultGo != null)
        {
            if (ItemSlot.chest == useSlot)
            {
                EquipNewArmor(ItemSlot.legs, defaultArmor[1], defaultGo[1]);
                Destroy(defaultGo[0]);
                defaultGo[1].SetActive(true);
            }
            else if (ItemSlot.legs == useSlot)
            {
                EquipNewArmor(ItemSlot.chest, defaultArmor[0], defaultGo[0]);
                Destroy(defaultGo[1]);
                defaultGo[0].SetActive(true);
            }
        }


    }
    public void ResetArmorPiece(ItemSlot slot)
    {
        var mainPart = GetMainArmorPart(slot);
        Destroy(GetGameObject(slot, mainPart));
        UpdateGo(slot, mainPart, null);
        UpdateData(slot, mainPart, null);
    }
    
    private void EquipNewArmor(ItemSlot slot , Armor armor, GameObject armorPiece , bool isDelete = true)
    {
        var mainPart = GetMainArmorPart(slot);

        if (ArmorDresserModel.ArmorPart.Unknow == mainPart)
        {
            Debug.LogWarning($"CharacterArmorDresser: EquipNewArmor-> Unknown armor part detected for slot {slot}. Armor will not be equipped.");
            return;
        }
        Debug.Log("EquipNewArmor destroy " + GetGameObject(slot, mainPart));
        if(isDelete) Destroy(GetGameObject(slot, mainPart));
        SetParent(armorPiece);

        UpdateGo(slot, mainPart, armorPiece);
        UpdateData(slot, mainPart, armor);
    }

    private void EquipNewArmorFullPlate(Armor armor, GameObject armorPiece , ItemSlot slotFullPlater, ItemSlot insideFullPlate)
    {
        var mainPart = GetMainArmorPart(insideFullPlate);
        Destroy(GetGameObject(slotFullPlater, mainPart));

        SetParent(armorPiece);

        UpdateGo(slotFullPlater, mainPart, armorPiece);
        UpdateData(slotFullPlater, mainPart, armor);
    }

    private void SetParent(GameObject armorPiece)
    {
        if(armorPiece != null)
        {
            armorPiece.transform.SetParent(_containerTransform, false);
        }
        else
        {
            Debug.LogWarning("CharacterArmorDresser->SetParent: GameObject its null");
        }

    }

    public void UpdateDataFullPlate(ItemSlot slot, ArmorDresserModel.ArmorPart part, Armor armor , GameObject armorPiece)
    {
        UpdateGo(slot, part, armorPiece);
        UpdateData(slot, part, armor);
    }
    public void Destroy(GameObject existingArmorPiece)
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




