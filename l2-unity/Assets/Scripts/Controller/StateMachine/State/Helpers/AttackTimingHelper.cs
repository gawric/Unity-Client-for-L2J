using UnityEngine;

public static class AttackTimingHelper
{
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

    public static float ResolveServerLikeAttackDurationMs(PlayerEntity player)
    {
        float baseTimeAtkMs = CalcBaseParam.CalculateTimeL2j(player.Stats.BasePAtkSpeed);
        string weaponAnim = player.GetCurrentAnimName();
        if (string.IsNullOrEmpty(weaponAnim)) return baseTimeAtkMs / 2f;

        string lower = weaponAnim.ToLowerInvariant();
        if (lower.Contains("bow"))
        {
            return baseTimeAtkMs;
        }

        return baseTimeAtkMs / 2f;
    }

    public static float ResolveHitFractionByWeapon(PlayerEntity player)
    {
        string weaponAnim = player.GetCurrentAnimName();
        if (string.IsNullOrEmpty(weaponAnim)) return 0.88f;

        string lower = weaponAnim.ToLowerInvariant();
        if (lower.Contains("bow")) return 0.82f;
        if (lower.Contains("dual")) return 0.84f;
        if (lower.Contains("pole")) return 0.86f;
        if (lower.Contains("2hs")) return 0.90f;
        return 0.88f;
    }
}
