using UnityEngine;

public static class ProjectileFlightTimeCalculator
{
    private const float SPEED_RANGE_1 = 8f;
    private const float SPEED_RANGE_2_MAX = 11f;
    private const float SPEED_RANGE_3_MAX = 12f;

    private const float DISTANCE_SPLIT_1 = 4f;
    private const float DISTANCE_SPLIT_2 = 8f;
    private const float DISTANCE_SPLIT_3 = 12f;

    public static float GetSpeed(float distance)
    {
        if (distance <= DISTANCE_SPLIT_1)
        {
            return SPEED_RANGE_1;
        }

        if (distance <= DISTANCE_SPLIT_2)
        {
            return SPEED_RANGE_1 + (distance - DISTANCE_SPLIT_1) *
                   ((SPEED_RANGE_2_MAX - SPEED_RANGE_1) / (DISTANCE_SPLIT_2 - DISTANCE_SPLIT_1));
        }

        if (distance <= DISTANCE_SPLIT_3)
        {
            return SPEED_RANGE_2_MAX + (distance - DISTANCE_SPLIT_2) *
                   ((SPEED_RANGE_3_MAX - SPEED_RANGE_2_MAX) / (DISTANCE_SPLIT_3 - DISTANCE_SPLIT_2));
        }

        return SPEED_RANGE_3_MAX;
    }

    public static float CalculateFlightTime(float distance, float speed, float hitOffsetSeconds)
    {
        float flightTime = distance / speed;
        return distance >= 4f
            ? Mathf.Max(flightTime - hitOffsetSeconds, 0.1f)
            : Mathf.Max(flightTime, 0.1f);
    }

    public static float CalculateFlightTimeByDistance(float distance, float hitOffsetSeconds)
    {
        float speed = GetSpeed(distance);
        return CalculateFlightTime(distance, speed, hitOffsetSeconds);
    }
}
