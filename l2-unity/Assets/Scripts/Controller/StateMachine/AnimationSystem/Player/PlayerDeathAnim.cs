using System;

/// <summary>
/// Player_Basic death / rebirth states (exact Animator names, no weapon suffix).
/// </summary>
public static class PlayerDeathAnim
{
    public const string Death = "death";
    public const string Rebirth = "rebirth";

    public static bool TryResolve(string animName, out string stateName)
    {
        stateName = null;
        if (string.IsNullOrEmpty(animName))
        {
            return false;
        }

        if (string.Equals(animName, Death, StringComparison.OrdinalIgnoreCase))
        {
            stateName = Death;
            return true;
        }

        if (string.Equals(animName, Rebirth, StringComparison.OrdinalIgnoreCase))
        {
            stateName = Rebirth;
            return true;
        }

        return false;
    }
}
