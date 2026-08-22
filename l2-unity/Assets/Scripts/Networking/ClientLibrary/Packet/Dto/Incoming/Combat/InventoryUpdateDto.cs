
using System.Collections.Generic;
using UnityEngine;


public class InventoryUpdateDto : IWireDto
{
    private Dictionary<int , ItemInstance> items;
    private Dictionary<int, ItemInstance>  equipItems;
    
    private int indexItems = 0;
    private int indexEquipItems = 0;
    public Dictionary<int, ItemInstance> Items { get { return items; } }
    public Dictionary<int, ItemInstance> EquipItems { get { return equipItems; } }
    public void ReadFrom(PacketReader reader)
    {
        
        int size =  reader.ReadSh();

        items = new Dictionary<int, ItemInstance>(size);
        equipItems = new Dictionary<int, ItemInstance>();
        for (int i = 0; i < size; i++)
        {
            // Update type : 01-add, 02-modify, 03-remove
            int type = reader.ReadSh();

            int type1 = reader.ReadSh();
            int objectId = reader.ReadI();
            int displayId = reader.ReadI();
            int count = reader.ReadI();
           // Item Type 2 : 00-weapon, 01-shield/armor, 02-ring/earring/necklace, 03-questitem, 04-adena, 05-item
            int type2 = reader.ReadSh();
            // Filler (always 0)
            int customType1 = reader.ReadSh();
            int equipped = reader.ReadSh();
            int bodyPart = reader.ReadI();
            int enchant = reader.ReadSh();
            int customType2 = reader.ReadSh();
            int augmentationLevel = reader.ReadI();
            int mana = reader.ReadI();

            ItemLocation location = ItemLocation.Inventory;
            ItemCategory category = ItemsType.ParceCategory(type2);
            ItemSlot slot = ItemsType.ParceSlot(bodyPart);

            if (equipped == 1)
            {
                location = ItemLocation.Equipped;
                ItemInstance item = new ItemInstance(objectId, displayId, location, indexEquipItems++, count, category, equipped == 1, slot, enchant, 9999);
                item.LastChange = type;
                equipItems.Add(objectId, item);
            }
            else
            {
   
                var itemInstance  = new ItemInstance(objectId, displayId, location, indexItems++, count, category, equipped == 1, slot, enchant, 9999);
                itemInstance.LastChange = type;
                Debug.Log("Inventory Update s1 " + displayId + " count " + count + " flag modified " + type);
                items.Add(objectId, itemInstance);
            }

            
        }
    }


}


