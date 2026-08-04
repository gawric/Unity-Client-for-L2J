using UnityEngine;

/// <summary>
/// CrossFade for combat swings (jatk* / SpAtk*).
/// Always restarts (same-name re-attack must not be skipped).
/// </summary>
public static class PlayerBasicAttackCrossFade
{
    public static bool TryPlay(
        IAnimationController controller,
        string stateName,
        float? fixedDuration = null)
    {
        if (controller == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        float duration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
        controller.CrossFadeInFixedTime(stateName, duration);
        return true;
    }
}
