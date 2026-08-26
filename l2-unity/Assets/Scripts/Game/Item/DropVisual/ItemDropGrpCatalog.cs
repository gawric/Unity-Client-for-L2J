public sealed class ItemDropGrpCatalog
{
    readonly ItemTable _items;

    public ItemDropGrpCatalog(ItemTable items)
    {
        _items = items;
    }

    public Abstractgrp ResolveGrp(int itemId)
    {
        ItemTable items = _items != null ? _items : ItemTable.Instance;
        if (items == null)
            return null;

        Weapon weapon = items.GetWeapon(itemId);
        if (weapon != null && weapon.Weapongrp != null)
            return weapon.Weapongrp;

        Armor armor = items.GetArmor(itemId);
        if (armor != null && armor.Armorgrp != null)
            return armor.Armorgrp;

        EtcItem etc = items.GetEtcItem(itemId);
        if (etc != null && etc.EtcItemgrp != null)
            return etc.EtcItemgrp;

        return null;
    }

    public string ResolveEquipModel(Abstractgrp grp)
    {
        if (grp is Weapongrp weapon)
            return weapon.Model;
        if (grp is EtcItemgrp etc)
            return etc.Model;
        return null;
    }

    public bool IsAdenaDropVisual(int itemId)
    {
        Abstractgrp grp = ResolveGrp(itemId);
        if (grp is EtcItemgrp etc && etc.ConsumeType == ConsumeCategory.Asset)
            return true;
        return IsCoinDropModel(grp != null ? grp.DropModel : null);
    }

    public bool IsStickWeapon(int itemId)
    {
        ItemTable items = _items != null ? _items : ItemTable.Instance;
        if (items == null)
            return false;

        Weapon weapon = items.GetWeapon(itemId);
        if (weapon == null || weapon.Weapongrp == null)
            return false;

        switch (weapon.Weapongrp.WeaponType)
        {
            case WeaponType.sword:
            case WeaponType.bigword:
            case WeaponType.dagger:
            case WeaponType.blunt:
            case WeaponType.bigblunt:
            case WeaponType.dual:
            case WeaponType.pole:
            case WeaponType.staff:
                return true;
            default:
                return false;
        }
    }

    public bool IsWeapon(int itemId)
    {
        ItemTable items = _items != null ? _items : ItemTable.Instance;
        return items != null && items.GetWeapon(itemId) != null;
    }

    public bool IsHerb(int itemId)
    {
        ItemName name = ItemNameTable.Instance != null
            ? ItemNameTable.Instance.GetItemName(itemId)
            : null;
        if (name != null && !string.IsNullOrEmpty(name.Name) &&
            name.Name.IndexOf("Herb", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        Abstractgrp grp = ResolveGrp(itemId);
        string drop = grp != null ? grp.DropModel : null;
        if (string.IsNullOrEmpty(drop))
            return itemId >= 8600 && itemId <= 8614 ||
                   itemId >= 8154 && itemId <= 8157 ||
                   itemId == 8952 || itemId == 8953;

        string lower = drop.ToLowerInvariant();
        return lower.IndexOf("drop_meat", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_magic_flower", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("herb", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_attack_speed_up", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_magic_speed_up", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_critical_up", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_move_up", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_warrior_set", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_priest_set", System.StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("drop_recovery_set", System.StringComparison.Ordinal) >= 0;
    }

    public static bool IsCoinDropModel(string dropModel)
    {
        if (string.IsNullOrEmpty(dropModel))
            return false;
        return dropModel.IndexOf("coin_m00", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               dropModel.IndexOf("coin00", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               dropModel.IndexOf("coin01", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
