using UnityEngine;

/// <summary>
/// CharInfo / UserEntity bow visual. Local player stays on <see cref="AbstractAttackEvents"/>.
/// </summary>
public sealed class BowArrowVisual
{
    public const int WoodenArrowId = 17;

    public void TryLoadArrow(UserEntity user, string animName)
    {
        if (user == null || !IsBowShot(user, animName))
            return;

        user.EquipArrow(WoodenArrowId);
        GameObject go = user.Gear != null ? user.Gear.GetGoEtcItem() : null;
        Debug.Log(
            $"[BOW_ARROW] LOAD id={(user.Identity != null ? user.Identity.Id : 0)} " +
            $"nick={user.Nick} anim={animName} equipped={(go != null)}");
    }

    public void TryShoot(UserEntity user, string animName)
    {
        if (user == null || !IsBowAnimName(animName))
            return;

        if (user.Gear != null && user.Gear.GetGoEtcItem() == null)
            user.EquipArrow(WoodenArrowId);

        GameObject go = user.Gear != null ? user.Gear.GetGoEtcItem() : null;
        Transform target = ResolveAimTarget(user);
        if (go == null || target == null)
        {
            Debug.LogWarning(
                $"[BOW_ARROW] SHOOT skip id={(user.Identity != null ? user.Identity.Id : 0)} " +
                $"anim={animName} go={(go != null)} target={(target != null)}");
            return;
        }

        Vector3 startPos = user.Gear.GetPositionRightHand();
        Vector3 aimPos = VectorUtils.GetCollision(startPos, target);
        float dist3d = Vector3.Distance(startPos, aimPos);
        float flyAccel = ProjectileFlightTimeCalculator.CalculateL2ArrowFlightTimeSeconds(dist3d);
        float avgSpeed = dist3d / Mathf.Max(flyAccel, 0.05f);

        ProjectileData settings = new ProjectileData(go, target, startPos, target);
        settings.impactType = ProjectileImpactType.ArrowStick;
        settings.speed = avgSpeed;
        settings.flytime = flyAccel;
        settings.lifetime = flyAccel;

        Debug.Log(
            $"[BOW_ARROW] SHOOT remote id={(user.Identity != null ? user.Identity.Id : 0)} " +
            $"anim={animName} dist3d={dist3d:F3} flyAccel={flyAccel:F3}s");

        if (ProjectileManager.Instance != null)
            ProjectileManager.Instance.LaunchProjectile(go, startPos, target, settings);
    }

    static bool IsBowShot(UserEntity user, string animName)
    {
        return IsBowAnimName(animName) || user.WeaponAnim == "bow";
    }

    static bool IsBowAnimName(string animName)
    {
        return !string.IsNullOrEmpty(animName) &&
               animName.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Transform ResolveAimTarget(Entity entity)
    {
        if (entity == null)
            return null;
        if (entity.AttackTarget != null)
            return entity.AttackTarget;
        if (entity.ActionSlot != null && entity.ActionSlot.Target != null)
            return entity.ActionSlot.Target.transform;
        return entity.Target;
    }
}
