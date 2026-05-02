using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsConverter
{
    private static StatsConverter _instance;
    public static StatsConverter Instance {
        get {
            if (_instance == null) {
                _instance = new StatsConverter();
            }
            return _instance;
        }
    }

    public float ConvertStat(Stat statType, float value) {
        switch (statType) {
            case Stat.SPEED:
                return ConvertSpeed(value);
            case Stat.PHYS_ATTACK_SPEED:
                return ConvertPAtkSpd(value);
            case Stat.MAGIC_ATTACK_SPEED:
                return ConvetMAtkSpd(value);
            case Stat.ANIM_RUN_SPEED:
                return ConvertAnimSpeed(value);
            case Stat.ANIM_MAGIC_RUN_SPEED:
                return ConvertAnimMagicSpeedL2j(value);
            case Stat.ANIM_ATTACK_SPEED:
                return ConvertAnimSpeedL2j(value);
            case Stat.ANIM_WALK_SPEED:
                return ConvertAnimWalkSpeedL2j(value);
            case Stat.RUN_SPEED_MONSTER:
                return ConvertSpeedPlus(value);
        }

        return value;
    }

    private float ConvertSpeed(float value) {
        return NumberUtils.ScaleToUnity(value);
    }

    private float ConvertSpeedPlus(float value)
    {
        return NumberUtils.ScaleToUnityPlus(value);
    }

    private float ConvertAnimSpeed(float value)
    {
        return NumberUtils.ScaleAnimSpeedToUnity(value);
    }

    private float ConvertAnimMagicSpeedL2j(float value)
    {
        return NumberUtils.ScaleAnimSpeedMagicToUnity(value);
    }

    private float ConvertAnimSpeedL2j(float value)
    {
        return NumberUtils.ScaleAnimSpeedL2jToUnity(value);
    }

    private float ConvertAnimWalkSpeedL2j(float value)
    {
        return NumberUtils.ScaleAnimWalkSpeedL2jToUnity(value);
    }

    private float ConvertPAtkSpd(float value) {
        if (value < 2) {
            return 2700;
        }
        //Debug.Log("Current atack speed convert " + (480000 / value));
        return (480000 / value);
    }

    private float ConvetMAtkSpd(float value) {
        //        if (skill.isMagic()) {
        //            return (int) ((skillTime * 333) / attacker.getMAtkSpd());
        //        }
        //        return (int) ((skillTime * 333) / attacker.getPAtkSpd());
        return value;
    }
}
