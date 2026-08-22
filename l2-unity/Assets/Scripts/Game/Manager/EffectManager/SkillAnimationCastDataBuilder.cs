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

        Animator animator = null;
        if (IncomingPacketActions.Animations is BaseAnimationManager animMgr && entity.Identity != null)
        {
            IAnimationController registered = animMgr.GetRegisteredController(entity.Identity.Id);
            if (registered != null)
                animator = registered.GetAnimator();
        }

        if (animator == null && entity is PlayerEntity && PlayerAnimationController.Instance != null)
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
                $"[SKILL_ANIM_FX] CAST_DATA no Animator objectId={entity.Identity.Id} " +
                $"entityAnimatorNull={entity.Animator == null}");
            return castData;
        }

        string[] cycle = animCombo != null ? animCombo.GetAnimCycle() : null;
        string cycleName = cycle != null && cycle.Length > 0 ? cycle[0] : null;
        string spatkName = cycleName;
        if (!string.IsNullOrEmpty(cycleName))
        {
            string suffix = null;
            if (entity is PlayerEntity playerEntity)
                suffix = playerEntity.GetEquippedWeaponName();
            else if (entity is UserEntity userEntity)
                suffix = userEntity.WeaponAnim;
            if (!string.IsNullOrEmpty(suffix))
                spatkName = cycleName + suffix;
        }

        if (PlayerStateSpAtk.TryEstimateWallDuration(
                animator,
                spatkName,
                hitTimeMs,
                out float wallDuration,
                out _))
        {
            castData.SkillAnimationDuration = wallDuration;
        }
        else
        {
            Debug.LogWarning(
                $"[SKILL_ANIM_FX] CAST_DATA could not estimate SpAtk wall duration spatk='{spatkName}' " +
                $"hitMs={hitTimeMs} — FX will fall back to HitTime/settings lifetime.");
        }

        return castData;
    }
}
