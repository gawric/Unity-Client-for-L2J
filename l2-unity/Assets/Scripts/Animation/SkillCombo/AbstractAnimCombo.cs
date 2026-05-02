using UnityEngine;

public abstract class AbstractAnimCombo
{
   public float GetTotalAnimationTime(string[]allAnim)
   {
        return AnimLeghtTable.Instance.GetAllLeghtMs(allAnim) / 1000f;
   }

    public float ConvertHitTimeToSec(float hitTime)
    {
        return hitTime / 1000f;
    }

    public float ConvertHitTimeToMs(float hitTime)
    {
        return hitTime * 1000f;
    }

    //Random speed /min dist - min speed / max dist - max speed
    public float CalculateSpeed(float distance)
    {
        int max_speed = 14;
        int min_speed = 9;

        return max_speed - ((distance - 1) / (12 - 1)) * (max_speed - min_speed);
    }

}
