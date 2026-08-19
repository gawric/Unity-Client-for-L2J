using System;
using UnityEngine;

/// <summary>
/// CrossFade for combat swings (jatk* / SpAtk*).
/// Always restarts (same-name re-attack must not be skipped).
/// 2HS needs a longer blend than 1H/locomotion 0.15s — run→jatk otherwise pops like an axe chop.
/// </summary>
public static class PlayerBasicAttackCrossFade
{
    public const float TwoHandedFixedDuration = 0.35f;

    public static float ResolveDuration(string stateName)
    {
        if (!string.IsNullOrEmpty(stateName) &&
            stateName.IndexOf("2HS", StringComparison.OrdinalIgnoreCase) >= 0)
            return TwoHandedFixedDuration;
        return LocomotionCrossFadeSettings.FixedDuration;
    }

    public static bool TryPlay(
        IAnimationController controller,
        string stateName,
        float? fixedDuration = null)
    {
        if (controller == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        float duration = fixedDuration ?? ResolveDuration(stateName);
        controller.CrossFadeInFixedTime(stateName, duration);
        return true;
    }
}
