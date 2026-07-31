using UnityEngine;

/// <summary>
/// Builds <see cref="MagicCastData"/> for melee skill FX that match SpAtk wall-clock duration
/// (<see cref="MagicCastData.SkillAnimationDuration"/>).
/// </summary>
public static class SkillAnimationCastDataBuilder
{
    public static MagicCastData Build(Entity entity, int hitTimeMs, AnimationCombo animCombo)
    {
        MagicCastData castData = new MagicCastData
        {
            StartTime = Time.time,
            HitTime = hitTimeMs > 0 ? hitTimeMs / 1000f : 0f,
            FlightTime = 0f,
            SkillAnimationDuration = 0f
        };

        if (entity == null || hitTimeMs <= 0)
        {
            Debug.LogWarning(
                $"[SKILL_ANIM_FX] CAST_DATA skip entity/hit invalid " +
                $"entityNull={entity == null} hitMs={hitTimeMs}");
            return castData;
        }

        // Entity.Animator is often unset; SpAtk SMB runs on PlayerAnimationController's animator.
        Animator animator = null;
        if (AnimationManager.Instance is BaseAnimationManager animMgr)
        {
            PlayerAnimationController pac = animMgr.GetPlayerController(entity.IdentityInterlude.Id);
            if (pac != null)
            {
                animator = pac.GetAnimator();
            }
        }

        if (animator == null && PlayerAnimationController.Instance != null)
        {
            animator = PlayerAnimationController.Instance.GetAnimator();
        }

        if (animator == null)
        {
            animator = entity.Animator != null
                ? entity.Animator
                : entity.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogWarning(
                $"[SKILL_ANIM_FX] CAST_DATA no Animator objectId={entity.IdentityInterlude.Id} " +
                $"entityAnimatorNull={entity.Animator == null}");
            return castData;
        }

        string[] cycle = animCombo != null ? animCombo.GetAnimCycle() : null;
        string cycleName = cycle != null && cycle.Length > 0 ? cycle[0] : null;
        string spatkName = cycleName;
        if (!string.IsNullOrEmpty(cycleName) && entity is PlayerEntity playerEntity)
        {
            // Same as AnimationManager.GetFinalNameAnim: cycle + equipped weapon suffix (e.g. SpAtk01_1HS).
            spatkName = cycleName + playerEntity.GetEquippedWeaponName();
        }

        if (PlayerStateSpAtk.TryEstimateWallDuration(
                animator,
                spatkName,
                hitTimeMs,
                out float wallDuration,
                out string motionName))
        {
            castData.SkillAnimationDuration = wallDuration;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[SKILL_ANIM_FX] CAST_DATA now={Time.time:F3}s spatk='{spatkName}' motion='{motionName}' " +
                $"hitSec={castData.HitTime:F3}s skillAnimDur={wallDuration:F3}s animator='{animator.name}'");
#endif
        }
        else
        {
            int behaviourCount = animator.GetBehaviours<PlayerStateSpAtk>()?.Length ?? 0;
            Debug.LogWarning(
                $"[SKILL_ANIM_FX] CAST_DATA could not estimate SpAtk wall duration spatk='{spatkName}' " +
                $"hitMs={hitTimeMs} behaviours={behaviourCount} animator='{animator.name}' " +
                $"— FX will fall back to HitTime/settings lifetime.");
        }

        return castData;
    }
}
