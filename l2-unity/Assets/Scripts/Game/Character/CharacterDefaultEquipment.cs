using UnityEngine;
using static ModelTable;
using static UnityEditor.Progress;

public class CharacterDefaultEquipment
{

    public static void EquipStarterGear(UserGear gear, PlayerInterludeAppearance appearance)
    {

        var armorSlots = new[] {
        (appearance.Chest, ItemSlot.chest, ItemTable.NAKED_CHEST),
        (appearance.Legs, ItemSlot.legs, ItemTable.NAKED_LEGS),
        (appearance.Gloves, ItemSlot.gloves, ItemTable.NAKED_GLOVES),
        (appearance.Feet, ItemSlot.feet, ItemTable.NAKED_BOOTS)
    };

        foreach (var (itemId, slot, nakedId) in armorSlots)
        {
            gear.EquipArmor(itemId != 0 ? itemId : nakedId, slot);
        }

        if (appearance.LHand != 0) gear.EquipWeapon(appearance.LHand, true);
        if (appearance.RHand != 0) gear.EquipWeapon(appearance.RHand, false);
    }

    public static int GetDefaultItemByItemSlot(ItemSlot slot)
    {
        return slot switch
        {
            ItemSlot.chest => ItemTable.NAKED_CHEST,
            ItemSlot.legs => ItemTable.NAKED_LEGS,
            ItemSlot.gloves => ItemTable.NAKED_GLOVES,
            ItemSlot.feet => ItemTable.NAKED_BOOTS,
            _ => 0 // Return 0 or throw exception for unknown slots
        };
    }

    public static Armor GetDefaultArmorByItemSlot(ItemSlot slot)
    {
        int itemId = GetDefaultItemByItemSlot(slot);
        return ItemTable.Instance.GetArmor(itemId);
    }

}
