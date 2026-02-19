using UnityEngine;

public abstract class AbstractServerItem 
{
    protected int itemId;
    protected bool _isMultisellData = false;
    public void SetItemId(int itemId)
    {
        this.itemId = itemId;
    }
    public Armorgrp GetArmor()
    {
        return ArmorgrpTable.Instance.GetArmor(itemId);
    }
    public Weapongrp GetWeapon()
    {
        return WeapongrpTable.Instance.GetWeapon(itemId);
    }

    public EtcItemgrp GetEtcItem()
    {
        return EtcItemgrpTable.Instance.GetEtcItem(itemId);
    }


    public ConsumeCategory GetConsumeCategory()
    {
        EtcItemgrp etcgrp = EtcItemgrpTable.Instance.GetEtcItem(itemId);
        if (etcgrp != null) return etcgrp.ConsumeType;
        return ConsumeCategory.Normal;
    }

    public string GetTypeAccessoriesName()
    {
        Armorgrp armor = ArmorgrpTable.Instance.GetArmor(itemId);

        if (armor != null)
        {
            if (armor.BodyPart == ItemSlot.neck)
            {
                return "Necklace";
            }
            else if (armor.BodyPart == ItemSlot.rear | armor.BodyPart == ItemSlot.lear)
            {
                return "Earring";
            }
            else if (armor.BodyPart == ItemSlot.rfinger | armor.BodyPart == ItemSlot.lfinger)
            {
                return "Ring";
            }
        }


        return "";
    }

    public void SetMultiSell(bool data)
    {
        _isMultisellData = data;
    }
    public string GetItemDescription()
    {
        ItemName item = ItemNameTable.Instance.GetItemName(itemId);
        if (string.IsNullOrEmpty(item.Description) && !_isMultisellData) return "Not Found description";
        return item.Description;
    }

    public ItemName[] GetSets()
    {
        ItemName item = ItemNameTable.Instance.GetItemName(itemId);
        return item.GetSetsName();
    }

    public ItemSets[] GetSetsEffects()
    {
        ItemName item = ItemNameTable.Instance.GetItemName(itemId);
        return item.GetSetsEffect();
    }

    public string GetTypeArmorName()
    {
        Armorgrp armor = ArmorgrpTable.Instance.GetArmor(itemId);

        if (armor != null)
        {
            Debug.Log("Body part test > " + armor.BodyPart);
            if (armor.BodyPart == ItemSlot.legs)
            {
                if (armor.ArmorType == ArmorType.light) return "Lower Body / Light";
                if (armor.ArmorType == ArmorType.heavy) return "Lower Body / Heavy";

            }
            else if (armor.BodyPart == ItemSlot.chest)
            {
                if (armor.ArmorType == ArmorType.light) return "Upper Body / Light";
                if (armor.ArmorType == ArmorType.heavy) return "Upper Body / Heavy";
            }
            else if (armor.BodyPart == ItemSlot.boots)
            {
                return "Boots";
            }
            else if (armor.BodyPart == ItemSlot.feet)
            {
                return "Boots";
            }
            else if (armor.BodyPart == ItemSlot.head)
            {
                return "Helmet";
            }
            else if (armor.BodyPart == ItemSlot.fullarmor)
            {
                return "Fullbody / Heavy";
            }
            else if (armor.BodyPart == ItemSlot.gloves)
            {
                return "Gloves";
            }
        }


        return "";
    }

    public string GetTypeWeaponName()
    {
        Weapongrp weapon = WeapongrpTable.Instance.GetWeapon(itemId);

        if (weapon != null)
        {
            if (weapon.BodyPart == ItemSlot.lhand_shield)
            {
                return "Shield";
            }
        }


        return "";
    }

    public bool IsBow() => WeapongrpTable.Instance.GetWeapon(itemId)?.WeaponType == WeaponType.bow;

    public bool IsArrow() => WeaponTypeParser.Parse(EtcItemgrpTable.Instance.GetEtcItem(itemId)?.EtcItemType) == WeaponType.arrow;


    public bool IsDual() => WeapongrpTable.Instance.GetWeapon(itemId)?.WeaponType == WeaponType.dual;

    public string GetDescription()
    {
        return "";
    }



    public Abstractgrp GetOAbstractItem()
    {
        var armor = GetArmor();
        var weapon = GetWeapon();
        if (armor != null) return armor;
        if (weapon != null) return weapon;

        return null;
    }

    protected Texture2D GetGradeImage(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.none:
                return null;

            case ItemGrade.d:
                return IconManager.Instance.GetInterfaceIcon("grade_d");

            case ItemGrade.c:
                return IconManager.Instance.GetInterfaceIcon("grade_c");

            case ItemGrade.b:
                return IconManager.Instance.GetInterfaceIcon("grade_b");

            case ItemGrade.a:
                return IconManager.Instance.GetInterfaceIcon("grade_a");

            case ItemGrade.s:
                return IconManager.Instance.GetInterfaceIcon("grade_s");

            default:
                return null;
        }
    }

}
