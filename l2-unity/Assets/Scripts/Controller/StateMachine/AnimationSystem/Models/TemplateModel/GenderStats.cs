using UnityEngine;

public class GenderStats
{
    public readonly float RunK;
    public readonly float WalkK;
    public readonly float TwoHandedBonus;

    public GenderStats(float run, float walk, float thBonus)
    {
        RunK = run;
        WalkK = walk;
        TwoHandedBonus = thBonus;
    }
}

