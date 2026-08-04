using UnityEngine;

/// <summary>
/// CrossFade policy for Monster_Basic states.
/// Locomotion (wait/walk/run/atkwait) skips when already playing; one-shots always restart.
/// </summary>
public static class MonsterCrossFade
{
    public static bool TryPlay(
        IAnimationController controller,
        string stateName,
        MonsterAnimState family,
        float? fixedDuration = null)
    {
        if (controller == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        if (!MonsterAnim.IsOneShot(family) &&
            PlayerLocomotionCrossFade.IsAlreadyPlaying(controller, stateName))
        {
            Debug.Log($"[MONSTER_CROSSFADE] SKIP already playing state={stateName}");
            return false;
        }

        float duration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
        controller.CrossFadeInFixedTime(stateName, duration);
        return true;
    }

    /// <summary>
    /// Unknown / legacy names (e.g. damageaction) — CrossFade by raw state name, no skip.
    /// </summary>
    public static bool TryPlayRaw(
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
