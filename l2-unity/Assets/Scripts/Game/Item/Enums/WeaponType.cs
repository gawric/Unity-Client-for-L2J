using System;
using UnityEngine;

public enum WeaponType : byte {
    none, //shield
    hand,
    sword,
    bigword,
    blunt,
    bigblunt,
    bow,
    dagger,
    fist,
    dual,
    dualfist,
    pole,
    shield,
    staff,
    mbook
}

public enum WeaponMaterial : byte
{
    None, //shield
    Wood,
    Bone
}

// hands: 244 Elven fighter fists

public class WeaponMaterialTypeParser
{
    public static WeaponMaterial Parce(string materils)
    {
        switch (materils)
        {
            case "wood":
                return WeaponMaterial.Wood;
            case "bone":
                return WeaponMaterial.Bone;
            default:
                return WeaponMaterial.None;
        }
    }
}
public class WeaponTypeParser {
    public static string GetWeaponAnim(WeaponType weaponType) {
        switch (weaponType) {
            case WeaponType.none:
                return "1HS";
            case WeaponType.fist:
            case WeaponType.hand:
                return "hand";
            case WeaponType.sword:
            case WeaponType.blunt:
            case WeaponType.dagger:
                return "1HS";
            case WeaponType.bigword:
            case WeaponType.bigblunt:
                return "2HS";
            case WeaponType.dual:
            case WeaponType.dualfist:
                return "dual";
            case WeaponType.pole:
            case WeaponType.staff:
                return "pole";
            case WeaponType.bow:
                return "bow";
            default:
                return "hand";
        }
    }

    public static WeaponType Parse(string type) {
        switch (type) {
            case "sword": // For 2H sword check at handness
                return WeaponType.sword;
            case "twohandsword":
                return WeaponType.blunt;
            case "blunt":
                return WeaponType.pole;
            case "buster":
                return WeaponType.dagger;
            case "dagger":
                return WeaponType.dual;
            case "staff":
                return WeaponType.bow;
            case "fist": // shield or hand check at handness
                return WeaponType.shield;
            case "twohandblunt":
                return WeaponType.fist;
            case "bow":
                return WeaponType.bow;
            default:
                return WeaponType.hand;
        }
    }

    public static string WeaponTypeName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.sword: // For 2H sword check at handness
                return "Sword";
            case WeaponType.blunt:
                return "Blunt";
            case WeaponType.pole:
                return "Pole";
            case WeaponType.dagger:
                return "Dagger";
            case WeaponType.dual:
                return "Dual Sword";
            case WeaponType.bow:
                return "Bow";
            case WeaponType.hand: // shield or hand check at handness
                return "Hand";
            case WeaponType.fist:
                return "Fist";
            case WeaponType.staff:
                return "Staff";
            case WeaponType.mbook:
                return "Magic book";
            default:
                return "None";
        }
    }


    public static int WeaponRange(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.none:
                return  40;
            case WeaponType.sword:
                return 40;
            case WeaponType.blunt:
                return 40;
            case WeaponType.dagger:
                return 40;
            case WeaponType.bow:
                return 500;
            case WeaponType.pole:
                return 66;
            case WeaponType.fist:
                return 40;
            case WeaponType.dualfist:
                return 40;
            case WeaponType.bigword:
                return 40;
            default:
                return 40;
        }
    }

        public static WeaponType ParseByHandness(int handness) {
        switch (handness) {
            case 0:
                return WeaponType.none;
            case 1:
                return WeaponType.sword;
            case 2:
                return WeaponType.bigword;
            case 3:
                return WeaponType.hand;
            case 4:
                return WeaponType.pole;
            case 5:
                return WeaponType.bow;
            case 6: // special
                return WeaponType.hand;
            case 7:
                return WeaponType.fist;
            case 8: // arbalet
                return WeaponType.hand;
            case 9: // rapier
                return WeaponType.hand;
            case 10: // dualdagger
                return WeaponType.hand;
            case 12: 
                return WeaponType.dagger;
            case 14: //staff
                return WeaponType.pole;
            default:
                return WeaponType.hand;

        }
    }
}
