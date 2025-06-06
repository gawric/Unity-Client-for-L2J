using System;
using System.Collections.Generic;
using UnityEngine;
using static L2Slot;
using static UnityEditor.Progress;

public class EquipGear : AbstractEquip
{
    private Dictionary<ItemSlot, GearItem> _gearAnchors;

    public void Initializing(Dictionary<ItemSlot, GearItem> gearAnchors)
    {
        _gearAnchors = gearAnchors;
        SetAnchor(gearAnchors);
    }
   public void Lear(ItemInstance item)
    {

        if (!GearSlotIsEmpty(ItemSlot.lear) && !GearSlotIsEmpty(ItemSlot.rear))
        {
            int itemId = GetItemIdBySlotType(ItemSlot.lear);

            if (item.ItemId != itemId)
            {
                AddIfNotExists(item, ItemSlot.lear);
            }
            else
            {
                AddIfNotExists(item, ItemSlot.rear);
            }
        }
        else
        {
            //checks that this is not a duplicate item.objectId since UpdateItems is often called when the part is already equipped
            if (!ContainsItemInTypes(item, new[] { ItemSlot.lear, ItemSlot.rear }))
             {
                if (GearSlotIsEmpty(item.BodyPart))
                {
                    AddIfNotExists(item, ItemSlot.lear);
                }
                else
                {

                    AddIfNotExists(item, ItemSlot.rear);
                }
             }
        }
    }


    public void LHand(ItemInstance item)
    {

        //AddGearEmptySlot(ItemSlot.rhand);
        if(GearSlotGetItemSlot(ItemSlot.rhand) == ItemSlot.lrhand || GearSlotGetItemSlot(ItemSlot.lhand) == ItemSlot.lrhand)
        {
            AddGearEmptySlot(ItemSlot.lhand);
            AddGearEmptySlot(ItemSlot.rhand);
        }
        AddGearSlotAssign(ItemSlot.lhand, item);
    }

    public void RHand(ItemInstance item)
    {
        if (GearSlotGetItemSlot(ItemSlot.rhand) == ItemSlot.lrhand || GearSlotGetItemSlot(ItemSlot.lhand) == ItemSlot.lrhand)
        {
            AddGearEmptySlot(ItemSlot.lhand);
            AddGearEmptySlot(ItemSlot.rhand);
        }

        AddGearSlotAssign(ItemSlot.rhand, item);
    }

    public void LRHand(ItemInstance item)
    {
        AddGearSlotAssignAlpha(ItemSlot.lhand, item, 0.8f);
        AddGearSlotAssign(ItemSlot.rhand, item);
    }

    public void FullArmor(ItemInstance item)
    {
        AddGearSlotAssign(ItemSlot.chest, item);
        AddGearSlotAssign(ItemSlot.legs, item);
    }

    public void LFinger(ItemInstance item)
    {
        if (!GearSlotIsEmpty(ItemSlot.lfinger) && !GearSlotIsEmpty(ItemSlot.rfinger))
        {
            int itemId = GetItemIdBySlotType(ItemSlot.lfinger);

            if (item.ItemId != itemId)
            {
                AddIfNotExists(item, ItemSlot.lfinger);
            }
            else
            {
                AddIfNotExists(item, ItemSlot.rfinger);
            }
        }
        else
        {
            //checks that this is not a duplicate item.objectId since UpdateItems is often called when the part is already equipped
            if (!ContainsItemInTypes(item, new[] { ItemSlot.lfinger, ItemSlot.rfinger }))
            {
                if (GearSlotIsEmpty(item.BodyPart))
                {
                    AddIfNotExists(item, ItemSlot.lfinger);
                }
                else
                {

                    AddIfNotExists(item, ItemSlot.rfinger);
                }
            }
        }
    }


    public void DefaultAssign(ItemInstance item)
    {
        //Debug.Log("Item id >>>>> " + item.ItemId);
        //Debug.Log("Slot ID  >>>>> " + item.Slot);
        //Debug.Log("BodyPart ID  >>>> " + item.BodyPart);
        ItemSlot slot = item.BodyPart;
        if (slot != ItemSlot.none)
        {
            if (_gearAnchors.ContainsKey(slot))
            {
                AddGearSlotAssign(slot, item);
            }
            else
            {
                Debug.LogError("GearSlots reInitialize not found assigned slots " + slot);
            }
        }
        else
        {
            Debug.LogError("Can't equip item, assigned slot is " + slot);
        }
    }


    public void EquipItemEmpty(ItemSlot bodyPart , ItemInstance item)
    {
        switch (bodyPart)
        {
            case ItemSlot.lrhand:
                AddGearEmptySlot(ItemSlot.lhand);
                AddGearEmptySlot(ItemSlot.rhand);
                break;

            case ItemSlot.fullarmor:
                AddGearEmptySlot(ItemSlot.chest);
                AddGearEmptySlot(ItemSlot.legs);
                break;

            case ItemSlot.lfinger:

                //if (!GearSlotIsEmpty(bodyPart))
                //{
                //    AddGearEmptySlot(ItemSlot.lfinger);
               // }
               // else
                //{
                   // AddGearEmptySlot(ItemSlot.rfinger);
                //}
                GearItem gearSlotFinger = GetSlotByObjectId(item.ObjectId);
                if (gearSlotFinger != null)
                {
                    AddGearEmptySlot(gearSlotFinger.GetSlotType());
                }

                break;

            case ItemSlot.lear:

                // if (!GearSlotIsEmpty(bodyPart))
                // {
                //     AddGearEmptySlot(ItemSlot.lear);
                // }
                // else
                // {
                //    AddGearEmptySlot(ItemSlot.rear);
                // }
                GearItem gearSlotLear = GetSlotByObjectId(item.ObjectId);
                if (gearSlotLear != null)
                {
                    AddGearEmptySlot(gearSlotLear.GetSlotType());
                }
                break;

            default:
                ItemSlot slot = bodyPart;
                if (slot != ItemSlot.none)
                {
                    if (_gearAnchors.ContainsKey(slot))
                    {
                        AddGearEmptySlot(slot);
                    }
                    else
                    {
                        Debug.LogError("GearSlots reInitialize not found assigned slots " + slot);
                    }
                }
                else
                {
                    Debug.LogError("Can't equip item, assigned slot is " + slot);
                }
                break;
        }
    }

    public void AddGearEmptySlot(ItemSlot slotType)
    {
        if (_gearAnchors.ContainsKey(slotType))
        {
            _gearAnchors[slotType].AssignEmpty();
        }
    }



}
