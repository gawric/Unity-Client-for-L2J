using UnityEngine;

public static class AttackTimingHelper
{
    // Player 1HS (server): HitTime/full = timeAtk, onHitTimer sAtk = timeAtk / 2 → fraction 0.5
    private const float SIMPLE_MELEE_HIT_OVER_FULL = 0.5f;

    /// <summary>
    /// Disabled: procedural spine/arm tilt toward the target. Keep for later.
    /// </summary>
    public static void RotateFaceToMonster(Entity entity)
    {
        // Transform monster = PlayerEntity.Instance.Target;
        // if (monster == null || entity == null) return;
        //
        // RotationService.Instance.RotateTowards(entity.transform, monster.position, () =>
        // {
        //     Entity monsterEntity = monster.GetComponent<Entity>();
        //     if (monsterEntity == null) return;
        //
        //     float monsterHeight = monsterEntity.Appearance.CollisionHeight;
        //     Vector3 monsterFacePosition = monster.position + Vector3.up * (monsterHeight * 0.8f);
        //
        //     Vector3 startPoint = entity.transform.position + Vector3.up * 1.5f;
        //     Vector3 lookDir = (monsterFacePosition - startPoint).normalized;
        //     float verticalAngle = Mathf.Asin(lookDir.y) * Mathf.Rad2Deg;
        //
        //     float spineAngle = Mathf.Clamp(verticalAngle * 0.4f, -15f, 10f);
        //     Vector3 spineRotation = new Vector3(0, 0, spineAngle);
        //
        //     float armAngle = Mathf.Clamp(verticalAngle * 0.3f, -20f, 10f);
        //     Vector3 armRotation = new Vector3(0, 0, armAngle);
        //
        //     if (entity is PlayerEntity playerEntity)
        //     {
        //         playerEntity.SetProceduralSpinePose(spineRotation);
        //         playerEntity.SetProceduralRightUpperArmPose(armRotation);
        //     }
        // });
    }

    /// <summary>
    /// Full attack cycle = Formulas.calculateTimeBetweenAttacks (500000 / pAtkSpd) ≈ HitTime.
    /// </summary>
    public static float ResolveServerLikeAttackDurationMs(PlayerEntity player)
    {
        return ResolveAttackCycleMs(player, null);
    }

    /// <summary>
    /// PlayerEntity uses UserInfo BasePAtkSpeed; UserEntity CharInfo only fills PAtkSpd.
    /// </summary>
    public static float ResolvePAtkSpd(Entity entity)
    {
        if (entity == null || entity.Stats == null)
            return 333f;

        if (entity is PlayerEntity && entity.Stats.BasePAtkSpeed > 1f)
            return entity.Stats.BasePAtkSpeed;

        if (entity.Stats.PAtkSpd > 1)
            return entity.Stats.PAtkSpd;

        if (entity.Stats.BasePAtkSpeed > 1f)
            return entity.Stats.BasePAtkSpeed;

        return 333f;
    }

    /// <summary>
    /// L2J timeAtk ms. Bow subtracts arrow flight from the draw window when distance is known.
    /// </summary>
    public static float ResolveAttackCycleMs(Entity entity, string animName)
    {
        float pAtk = ResolvePAtkSpd(entity);
        float baseTimeAtkMs = CalcBaseParam.CalculateTimeL2j(pAtk);

        if (string.IsNullOrEmpty(animName) ||
            animName.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return baseTimeAtkMs;
        }

        float distance = 0f;
        if (entity is PlayerEntity player)
        {
            if (player.Target != null)
                distance = player.TargetDistance();
        }
        else if (entity != null && entity.Target != null)
        {
            distance = VectorUtils.Distance2D(entity.transform.position, entity.Target.position);
        }

        if (distance <= 0.01f)
            return baseTimeAtkMs;

        return CalcBaseParam.CalculateAttackAndFlightTimes(distance, baseTimeAtkMs)[0];
    }

    /// <summary>
    /// Animator <c>patkspd</c>: play the whole clip over server timeAtk (not timeAtk/2).
    /// </summary>
    public static float ComputeLinearPAtkSpeed(float clipLengthSec, float cycleMs)
    {
        return clipLengthSec * 1000f / Mathf.Max(1f, cycleMs);
    }

    /// <summary>
    /// Fraction of full cycle when server fires onHitTimer.
    /// Player simple melee: doAttackHitSimple(..., timeAtk / 2) → 0.5.
    /// </summary>
    public static float ResolveHitFractionByWeapon(PlayerEntity player)
    {
        string weaponAnim = player.GetCurrentAnimName();
        if (string.IsNullOrEmpty(weaponAnim)) return SIMPLE_MELEE_HIT_OVER_FULL;

        string lower = weaponAnim.ToLowerInvariant();
        if (lower.Contains("bow")) return 0.82f;

        return SIMPLE_MELEE_HIT_OVER_FULL;
    }

    public static float ResolveServerLikeHitMs(PlayerEntity player)
    {
        return ResolveServerLikeAttackDurationMs(player) * ResolveHitFractionByWeapon(player);
    }
}
