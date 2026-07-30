using System;
using UnityEngine;

/// <summary>
/// CrossFade policy for player locomotion (wait / walk / run / atkwait).
/// <see cref="BaseAnimationController.CrossFadeInFixedTime"/> only launches the fade.
/// </summary>
public static class PlayerLocomotionCrossFade
{
    public static bool IsLocomotionStateName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.StartsWith("walk", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("run", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("wait", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("atkwait", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAlreadyPlaying(Animator animator, string stateName, int layer = 0)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
        if (current.IsName(stateName))
        {
            if (animator.IsInTransition(layer))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
                if (!next.IsName(stateName))
                {
                    return false;
                }
            }

            return true;
        }

        if (animator.IsInTransition(layer))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            if (next.IsName(stateName))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsAlreadyPlaying(IAnimationController controller, string stateName, int layer = 0)
    {
        return controller != null && IsAlreadyPlaying(controller.GetAnimator(), stateName, layer);
    }

    public static bool ShouldSkip(
        IAnimationController controller,
        string stateName,
        string recentAnimationName)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return true;
        }

        if (string.Equals(recentAnimationName, stateName, StringComparison.Ordinal))
        {
            return true;
        }

        return IsAlreadyPlaying(controller, stateName);
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

        if (IsAlreadyPlaying(controller, stateName))
        {
            Debug.Log($"[ANIM_CROSSFADE] SKIP already playing state={stateName}");
            return false;
        }

        if (IsLocomotionStateName(stateName))
        {
            controller.ReleasePriorityQueueIfBusy($"locomotion_crossfade:{stateName}");
        }

        float duration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
        controller.CrossFadeInFixedTime(stateName, duration);
        return true;
    }
}
