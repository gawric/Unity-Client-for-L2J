/// <summary>
/// Interlude GetCurSpeed on the ground.
/// Walk while moveTimer &lt;= 12/30 s when run mode is on.
/// NPCs skip that window and take max speed immediately.
/// Run mode off stays on walk speed.
/// </summary>
public static class L2PlayerMoveGait
{
    public const float WalkSeconds = 12f / 30f;

    public static bool IsWalkStart(float moveTimer, bool running, bool npc)
    {
        if (!running)
            return true;
        if (npc)
            return false;
        return moveTimer <= WalkSeconds;
    }

    public static float Speed(float moveTimer, bool running, bool npc, float walk, float run)
    {
        if (IsWalkStart(moveTimer, running, npc))
            return walk;
        return run;
    }
}
