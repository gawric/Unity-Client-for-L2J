using UnityEditor;
using UnityEngine;

public class WeaponClassifier
{
    public static WeaponType GetType(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (weaponType == WeaponType.hand)
        {
            weaponType = None(weaponType, weaponGrp, handness);
        }

        if (weaponType == WeaponType.sword)
        {
            weaponType = Sword(weaponType, weaponGrp, handness);
        }

        if (weaponType == WeaponType.blunt)
        {
            weaponType = Blunt(weaponType, weaponGrp, handness);
        }

        if (handness == 14)
        {
            weaponType = Staff(weaponType, weaponGrp, handness);
        }

        if (weaponType == WeaponType.shield)
        {
            weaponType = Shield(weaponType, weaponGrp, handness);
        }

        return weaponType;
    }

















    private static WeaponType None(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (handness == 0)
        {
            weaponType = WeaponType.none;
        }
        return weaponType;
    }
    private static WeaponType Sword(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (handness == 2)
        {
            weaponType = WeaponType.bigword;
        }
        else if (
              handness == 1 & weaponGrp.Material == ItemMaterial.paper
            | handness == 1 & weaponGrp.Material == ItemMaterial.steel & weaponGrp.IsMagicWeapon == 1)
        {
            weaponType = WeaponType.mbook;
        }
        else if (weaponGrp.Material == ItemMaterial.steel & weaponGrp.IsMagicWeapon == 0)
        {
            weaponType = WeaponType.sword;
        }

        return weaponType;
    }

    private static WeaponType Blunt(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (handness == 1)
        {
            weaponType = WeaponType.blunt;
        }
        return weaponType;
    }

    private static WeaponType Staff(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (weaponType == WeaponType.blunt & weaponGrp.Material == ItemMaterial.wood)
        {
            weaponType = WeaponType.staff;
        }
        else
        {
            weaponType = WeaponType.pole;
        }
        return weaponType;
    }

    private static WeaponType Shield(WeaponType weaponType, Weapongrp weaponGrp, int handness)
    {
        if (weaponGrp.Material == ItemMaterial.steel
          && handness == 0
          && weaponGrp.BodyPart == ItemSlot.lhand_shield)
        {
            weaponType = WeaponType.shield;
        }

        return weaponType;
    }
}
