using UnityEngine;

/// <summary>
/// CrossFade policy for Monster_Basic states.
/// Locomotion (wait/walk/run/atkwait) and death skip when already playing.
/// Attack one-shots (atk / spatk) always restart.
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

        bool alreadyPlaying = PlayerLocomotionCrossFade.IsAlreadyPlaying(controller, stateName);
        if (alreadyPlaying &&
            (family == MonsterAnimState.Death || !MonsterAnim.IsOneShot(family)))
        {
            return false;
        }

        float duration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
        if (duration <= 0f)
        {
            Animator animator = controller.GetAnimator();
            if (animator == null)
                return false;
            int objectId = animator.GetInteger(AnimatorUtils.OBJECT_ID);
            int hash = Animator.StringToHash(stateName);
            bool hasState = animator.HasState(0, hash);
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
            Debug.Log(
                $"[ANIM_PLAY] id={objectId} snap state={stateName} hasState={hasState}");
            return true;
        }

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
