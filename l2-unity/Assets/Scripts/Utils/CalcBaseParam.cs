using System;
using UnityEngine;

public class CalcBaseParam
{

    // Константы для расчета скорости
    private const float SPEED_RANGE_1 = 8f;     // Скорость до 4 метров
    private const float SPEED_RANGE_2_MAX = 11f; // Максимальная скорость во втором диапазоне
    private const float SPEED_RANGE_3_MAX = 12f; // Максимальная скорость в третьем диапазоне

    private const float DISTANCE_SPLIT_1 = 4f;  // Первая точка разделения дистанции
    private const float DISTANCE_SPLIT_2 = 8f;  // Вторая точка разделения дистанции
    private const float DISTANCE_SPLIT_3 = 12f; // Третья точка разделения дистанции

    // Константы для времени атаки
    private const float MAX_ATTACK_TIME = 1000f; // Максимальное время атаки в миллисекундах

    public static  float CalculateTimeL2j(float patkSpeed)
    {
        return Math.Max(100, 500000 / patkSpeed);
    }


    public static float GetAnimatedSpeed(int pAtkSpd, float timeAtck)
    {
        return pAtkSpd / timeAtck;
    }
   
    public static float[] CalculateAttackAndFlightTimes(float distance, float baseAttackTimeMs)
    {
        // Определяем скорость в зависимости от дистанции
        float speed;
        if (distance <= DISTANCE_SPLIT_1)
        {
            speed = SPEED_RANGE_1;
        }
        else if (distance <= DISTANCE_SPLIT_2)
        {
            // Пропорция от 8 до 11 м/с между 4 и 8 метрами
            speed = SPEED_RANGE_1 + (distance - DISTANCE_SPLIT_1) *
                   ((SPEED_RANGE_2_MAX - SPEED_RANGE_1) / (DISTANCE_SPLIT_2 - DISTANCE_SPLIT_1));
        }
        else if (distance <= DISTANCE_SPLIT_3)
        {
            // Пропорция от 11 до 12 м/с между 8 и 12 метрами
            speed = SPEED_RANGE_2_MAX + (distance - DISTANCE_SPLIT_2) *
                   ((SPEED_RANGE_3_MAX - SPEED_RANGE_2_MAX) / (DISTANCE_SPLIT_3 - DISTANCE_SPLIT_2));
        }
        else
        {
            speed = SPEED_RANGE_3_MAX;
        }

        // Рассчитываем время полета
        float flightTime = TimeUtils.ConvertSecToMs(distance / speed);
        float attackTimeBase = baseAttackTimeMs - flightTime;
        float attackTime = Mathf.Clamp(attackTimeBase, 0, MAX_ATTACK_TIME);

        Debug.Log($"CalculateAttackAndFlightTimes: dist={distance}, speed={speed}, fly={flightTime}, atk={attackTime}, baseatk={attackTimeBase}");

        return new float[2] { attackTime, flightTime  };
   
    }





    public static float CalculateTimeReuseL2j(float reuse , float pAtkSpd)
    {
        if (reuse != 0)
            reuse = (reuse * 345) / pAtkSpd;

        return reuse;
    }
}
