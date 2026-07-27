using UnityEngine;

public static class AttackTimingHelper
{
    // Player 1HS (server): HitTime/full = timeAtk, onHitTimer sAtk = timeAtk / 2 → fraction 0.5
    private const float SIMPLE_MELEE_HIT_OVER_FULL = 0.5f;

    public static void RotateFaceToMonster(Entity entity)
    {
        Transform monster = PlayerEntity.Instance.Target;
        if (monster == null || entity == null) return;

        RotationService.Instance.RotateTowards(entity.transform, monster.position, () =>
        {
            Entity monsterEntity = monster.GetComponent<Entity>();
            if (monsterEntity == null) return;

            float monsterHeight = monsterEntity.Appearance.CollisionHeight;
            Vector3 monsterFacePosition = monster.position + Vector3.up * (monsterHeight * 0.8f);

            Vector3 startPoint = entity.transform.position + Vector3.up * 1.5f;
            Vector3 lookDir = (monsterFacePosition - startPoint).normalized;
            float verticalAngle = Mathf.Asin(lookDir.y) * Mathf.Rad2Deg;

            float spineAngle = Mathf.Clamp(verticalAngle * 0.4f, -15f, 10f);
            Vector3 spineRotation = new Vector3(0, 0, spineAngle);

            float armAngle = Mathf.Clamp(verticalAngle * 0.3f, -20f, 10f);
            Vector3 armRotation = new Vector3(0, 0, armAngle);

            if (entity is PlayerEntity playerEntity)
            {
                playerEntity.SetProceduralSpinePose(spineRotation);
                playerEntity.SetProceduralRightUpperArmPose(armRotation);
            }
        });
    }

    /// <summary>
    /// Full attack cycle = Formulas.calculateTimeBetweenAttacks (500000 / pAtkSpd) ≈ HitTime.
    /// </summary>
    public static float ResolveServerLikeAttackDurationMs(PlayerEntity player)
    {
        return CalcBaseParam.CalculateTimeL2j(player.Stats.BasePAtkSpeed);
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
