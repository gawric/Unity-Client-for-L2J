using System;

/// <summary>
/// Rider locomotion names. One body clip covers wait, walk and run.
/// The strider itself switches wait / run.
/// </summary>
public static class PlayerStriderAnim
{
    public const string State = "strider";

    public static bool TryResolve(string animName, out bool mountRunning)
    {
        mountRunning = false;
        if (string.IsNullOrEmpty(animName))
        {
            return false;
        }

        if (string.Equals(animName, AnimationNames.STRIDER_WAIT.Value, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(animName, AnimationNames.STRIDER_WALK.Value, StringComparison.Ordinal)
            || string.Equals(animName, AnimationNames.STRIDER_RUN.Value, StringComparison.Ordinal))
        {
            mountRunning = true;
            return true;
        }

        return false;
    }
}
