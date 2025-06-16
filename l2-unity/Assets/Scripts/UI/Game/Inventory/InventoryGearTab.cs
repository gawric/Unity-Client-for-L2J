using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using static L2Slot;
using static UnityEditor.Progress;

[System.Serializable]
public class InventoryGearTab : L2Tab
{
    private Dictionary<int, ItemSlot> _gearCellIds;
    private Dictionary<ItemSlot, GearItem> _gearAnchors;
    [SerializeField] private int _selectedSlot = -1;
    private EquipGear _equip;

    public InventoryGearTab()
    {
        _gearAnchors = new Dictionary<ItemSlot, GearItem> ();
        _gearCellIds = new Dictionary<int, ItemSlot>();
        _equip = new EquipGear();
    }

    public override void Initialize(VisualElement chatWindowEle, VisualElement tabContainer, VisualElement tabHeader , bool initEmpty, bool isSwitchTab)
    {
        base.Initialize(chatWindowEle, tabContainer, tabHeader , initEmpty, isSwitchTab);

        _selectedSlot = -1;

        _gearAnchors?.Clear();
        InitEmptyGear();
        _equip.Initializing(_gearAnchors);

    }

  
    private void InitEmptyGear()
    {
        _gearAnchors = new Dictionary<ItemSlot, GearItem>
        {
            { ItemSlot.hat1, new GearItem(_windowEle.Q<VisualElement>("Hat1")  , ItemSlot.hat1 ) },
            { ItemSlot.hat2, new GearItem(_windowEle.Q<VisualElement>("Hat2")  , ItemSlot.hat2 ) },
            { ItemSlot.head, new GearItem(_windowEle.Q<VisualElement>("Helmet")  , ItemSlot.head ) },
            { ItemSlot.gloves, new GearItem(_windowEle.Q<VisualElement>("Gloves"), ItemSlot.gloves) },
            { ItemSlot.chest, new GearItem(_windowEle.Q<VisualElement>("Torso"), ItemSlot.chest) },
            { ItemSlot.feet, new GearItem(_windowEle.Q<VisualElement>("Boots"), ItemSlot.feet) },
            { ItemSlot.underwear, new GearItem(_windowEle.Q<VisualElement>("Underwear"), ItemSlot.underwear) },
            { ItemSlot.legs, new GearItem(_windowEle.Q<VisualElement>("Legs"), ItemSlot.legs) },
            { ItemSlot.belt, new GearItem(_windowEle.Q<VisualElement>("Belt"), ItemSlot.belt) },
            { ItemSlot.rhand, new GearItem(_windowEle.Q<VisualElement>("Rhand"), ItemSlot.rhand) },
            { ItemSlot.lhand, new GearItem(_windowEle.Q<VisualElement>("Lhand"), ItemSlot.lhand) },
            { ItemSlot.agation, new GearItem(_windowEle.Q<VisualElement>("Agathion"), ItemSlot.agation) },
            { ItemSlot.neck, new GearItem(_windowEle.Q<VisualElement>("Neck"), ItemSlot.neck) },
            { ItemSlot.rear, new GearItem(_windowEle.Q<VisualElement>("Rear"), ItemSlot.rear) },
            { ItemSlot.lear, new GearItem(_windowEle.Q<VisualElement>("Lear"), ItemSlot.lear) },
            { ItemSlot.boots, new GearItem(_windowEle.Q<VisualElement>("Boots"), ItemSlot.boots) },
            { ItemSlot.rfinger, new GearItem(_windowEle.Q<VisualElement>("Rring"), ItemSlot.rfinger) },
            { ItemSlot.lfinger, new GearItem(_windowEle.Q<VisualElement>("Lring"), ItemSlot.lfinger) },
            { ItemSlot.pendant, new GearItem(_windowEle.Q<VisualElement>("Pendant"), ItemSlot.pendant) }

        };

        CreateGearSlot(_gearAnchors);
    }

    private void CreateGearSlot(Dictionary<ItemSlot, GearItem> _gearAnchors)
    {
        List<GearItem> gearList = _gearAnchors.Values.ToList();
        for (int i = 0; i < gearList.Count; i++)
        {
            GearItem gearItem = gearList[i];
            _gearCellIds.Add(i, gearItem.GetSlotType());
            GearSlot slot = new GearSlot(i, gearItem.GetElement(), this, L2Slot.SlotType.Gear);
            gearItem.SetGearSlot(slot);
        }
    }
   

    public void UpdateEquipList(List<ItemInstance> modified, ChangeInventoryData changeInventoryData)
    {
        if(PlayerInventory.Instance.GetPlayerEquipInventory().Count == 0)
        {
            EquipEmptyAll();
        }

         for(int i = 0; i < modified.Count; i++)
         {
            ItemInstance currentSlotEquip = modified[i];
            if(currentSlotEquip.Equipped == true)
            {
                if (currentSlotEquip == null) return;
                if (changeInventoryData.IsReplaceSourceItem(currentSlotEquip.ObjectId)) return;
                EquipItem(currentSlotEquip);
            }
        }
    }
    public void SetEquipList(List<ItemInstance> equipItems)
    {
        if (equipItems == null) return;
        EquipList(equipItems);

        if (_selectedSlot != -1)
        {
            SelectSlot(_selectedSlot);
        }
    }

    private void EquipList(List<ItemInstance> equipItems)
    {
        for (int i = 0; i < equipItems.Count; i++)
        {
            ItemInstance itemInstance = equipItems[i];
            EquipItem(itemInstance);
        }
    }
    private void EquipEmptyAll()
    {
        foreach (KeyValuePair<ItemSlot, GearItem> entry in _gearAnchors)
        {
            GearItem gearItem = entry.Value;
            gearItem.AssignEmpty();
        }

    }

   
    private void EquipItem(ItemInstance item)
    {
        //Debug.Log("Body part staff " + item.BodyPart);
        switch (item.BodyPart)
        {
            case ItemSlot.lrhand:
                _equip.LRHand(item);
                break;

            case ItemSlot.fullarmor:
                _equip.FullArmor(item);
                break;
            case ItemSlot.lfinger:
                _equip.LFinger(item);
                break;

            case ItemSlot.lear:
                _equip.Lear(item);
                break;
            case ItemSlot.rhand:
                _equip.RHand(item);
                break;
            case ItemSlot.lhand:
                _equip.LHand(item);
                break;
            case ItemSlot.chest:
                _equip.Chest(item);
                break;
            case ItemSlot.legs:
                _equip.Legs(item);
                break;
            default:
                _equip.DefaultAssign(item);
                break;
        }
    }


    public void GetGearToSlot(ItemInstance item)
    {
        //Debug.Log("Body part staff " + item.BodyPart);
        switch (item.BodyPart)
        {
            case ItemSlot.lrhand:
                _equip.LRHand(item);
                break;

            case ItemSlot.fullarmor:
                _equip.FullArmor(item);
                break;
            case ItemSlot.lfinger:
                _equip.LFinger(item);
                break;

            case ItemSlot.lear:
                _equip.Lear(item);
                break;
            case ItemSlot.rhand:
                _equip.RHand(item);
                break;
            case ItemSlot.lhand:
                _equip.LHand(item);
                break;
            case ItemSlot.chest:
                _equip.Chest(item);
                break;
            case ItemSlot.legs:
                _equip.Legs(item);
                break;
            default:
                _equip.DefaultAssign(item);
                break;
        }
    }



    public void ModifiedRemove(ItemSlot bodyPart , ItemInstance item)
    {
        _equip.EquipItemEmpty(bodyPart , item);
    }

    public void ModifiedReplace(ItemInstance item)
    {
        EquipItem(item);
    }

 

    public override void SelectSlot(int slotPosition)
    {
        GearItem gearItem = ConvertIdToGearType(slotPosition);

        if (gearItem != null)
        {
            if (_selectedSlot != -1)
            {
                GearItem gearLastSelect = ConvertIdToGearType(_selectedSlot);
                gearLastSelect.UnSelectGear();
            }

            gearItem.SetSelectGear();
            _selectedSlot = slotPosition;
        }

    }

  
    public GearItem ConvertIdToGearType(int slotPosition)
    {
        if (_gearCellIds.ContainsKey(slotPosition))
        {
            ItemSlot slotSelect = _gearCellIds[slotPosition];
            GearItem gearitem =  _gearAnchors[slotSelect];
            return gearitem;
        }
        return null;
    }
}
