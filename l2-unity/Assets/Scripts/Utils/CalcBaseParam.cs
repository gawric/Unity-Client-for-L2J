using System;
using UnityEngine;

public class CalcBaseParam
{
    private const float MAX_ATTACK_TIME = 1000f;

    public static float CalculateTimeL2j(float patkSpeed)
    {
        return Math.Max(100, 500000 / patkSpeed);
    }

    public static float GetAnimatedSpeed(int pAtkSpd, float timeAtck)
    {
        return pAtkSpd / timeAtck;
    }

    /// <summary>
    /// Attack split: [0]=draw/shoot window ms, [1]=flight ms (ANProjectile accel 3000).
    /// </summary>
    public static float[] CalculateAttackAndFlightTimes(float distance, float baseAttackTimeMs)
    {
        float flightTime = TimeUtils.ConvertSecToMs(
            ProjectileFlightTimeCalculator.CalculateL2ArrowFlightTimeSeconds(distance));
        float attackTimeBase = baseAttackTimeMs - flightTime;
        float attackTime = Mathf.Clamp(attackTimeBase, 0, MAX_ATTACK_TIME);

        Debug.Log(
            $"[BOW_ARROW] CalcAtkFly dist={distance:F3} flyMs={flightTime:F1} " +
            $"atkMs={attackTime:F1} baseMs={baseAttackTimeMs:F1} uuAccel=3000");

        return new float[2] { attackTime, flightTime };
    }
}
