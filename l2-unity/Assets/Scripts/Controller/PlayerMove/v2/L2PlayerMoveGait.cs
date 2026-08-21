/// <summary>
/// Same walk-start as local PlayerController: PeriodicTimer 0.1s, walk while countTrigger &lt;= 3.
/// </summary>
public static class L2PlayerMoveGait
{
    public const int WalkTriggerMax = 3;

    public static bool IsWalkStart(int countTrigger, bool running)
    {
        if (!running)
            return true;
        return countTrigger <= WalkTriggerMax;
    }

    public static float Speed(int countTrigger, bool running, float walk, float run)
    {
        if (IsWalkStart(countTrigger, running))
            return walk;
        return run;
    }
}
