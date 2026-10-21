using UnityEngine;

public enum SpecialSkillType
{
    None = 0,
    LevelUp = 99999,
    SoulshotNg = 2039,
    SpiritshotNg = 2047,
    BlessedSpiritshotNg = 2061,
}

/// <summary>
/// Soulshot and spiritshot skill ids that play a weapon charge.
/// Spiritshot 2047 and Blessed Spiritshot 2061 share the spirit visual row 2047.
/// </summary>
public static class WeaponChargeSkill
{
    public static bool IsCharge(int skillId)
    {
        return EffectId(skillId) > 0;
    }

    public static bool IsSpirit(int skillId)
    {
        return skillId == (int)SpecialSkillType.SpiritshotNg ||
               skillId == (int)SpecialSkillType.BlessedSpiritshotNg;
    }

    public static int EffectId(int skillId)
    {
        if (skillId == (int)SpecialSkillType.SoulshotNg)
            return (int)SpecialSkillType.SoulshotNg;
        if (IsSpirit(skillId))
            return (int)SpecialSkillType.SpiritshotNg;
        return 0;
    }

    public static void Play(EffectManager effects, int skillId, Transform weapon, Transform fallback)
    {
        int effectId = EffectId(skillId);
        Transform target = weapon != null ? weapon : fallback;
        // Debug.Log(
            // "[WEAPON_SHOT] skillId=" + skillId +
            // " effectId=" + effectId +
            // " spirit=" + IsSpirit(skillId) +
            // " weapon=" + (weapon != null ? weapon.name : "null") +
            // " target=" + (target != null ? target.name : "null") +
            // " effects=" + (effects != null));
        if (effects == null || effectId <= 0 || target == null)
            return;

        effects.PlayEffect(effectId, target);
    }
}