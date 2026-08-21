using System;
using System.Collections.Generic;
using UnityEngine;

public class SetupDurationHelper
{
    public const int ScrollOfEscapeSkillId = 2013;
    public static readonly HashSet<int> PotionSkillIds = new HashSet<int> { 2031  , 2011};

    public static bool IsLongCastSkill(MagicSkillUseDto useSkill)
    {
        return useSkill != null && useSkill.SkillId == ScrollOfEscapeSkillId;
    }

    public static bool IsUsePotion(MagicSkillUseDto useSkill)
    {
        return useSkill != null && PotionSkillIds.Contains(useSkill.SkillId);
    }

    public static void SetupLongCastDurationIfHitTimeNot0(MagicSkillUseDto useSkill, int objectId, Entity entity, AnimationCombo selfCombo)
    {
        if (useSkill.HitTime <= 0)
        {
            return;
        }

        string[] orderedCycle = BuildOrderedCycleForOverrideTiming(selfCombo.GetAnimCycle());
        float[] durations = IncomingPacketActions.Animations.GetOverrideClipsDurations(objectId, orderedCycle);
        float shotEventTime = ResolveShotEventTime(objectId, orderedCycle);
        entity.SetupLongCastDuration(useSkill.HitTime, durations, shotEventTime, useSkill.TargetId);
    }

    public static void SetupDurationIfHitTimeNot0(MagicSkillUseDto useSkill, int objectId, Entity entity, AnimationCombo selfCombo)
    {

        if (useSkill.HitTime > 0)
        {
            string[] orderedSelfCycle = BuildOrderedCycleForOverrideTiming(selfCombo.GetAnimCycle());
            float[] durations_self = IncomingPacketActions.Animations.GetOverrideClipsDurations(objectId, orderedSelfCycle);
            float shotEventTime_self = ResolveShotEventTime(objectId, orderedSelfCycle);
            entity.SetupTotalCastDuration(useSkill.HitTime, 0f, durations_self, shotEventTime_self, useSkill.TargetId);
        }
    }

    public static string[] BuildOrderedCycleForOverrideTiming(string[] rawCycle)
    {
        if (rawCycle == null || rawCycle.Length == 0)
        {
            return Array.Empty<string>();
        }

        List<string> valid = new List<string>();
        for (int i = 0; i < rawCycle.Length; i++)
        {
            string clipName = rawCycle[i];
            if (string.IsNullOrWhiteSpace(clipName))
            {
                continue;
            }

            if (string.Equals(clipName.Trim(), "none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            valid.Add(clipName.Trim());
        }

        // Keep timing order consistent with SkillAnimationRunner trigger plan.
        if (valid.Count == 2)
        {
            return new[] { valid[0], valid[1] };
        }

        return valid.ToArray();
    }

    public static float ResolveShotEventTime(int objectId, string[] orderedCycle)
    {
        if (orderedCycle == null || orderedCycle.Length == 0)
        {
            return 0f;
        }

        // For two-clip execution (CastEnd -> MagicShot), shoot event belongs to the second clip.
        if (orderedCycle.Length == 2)
        {
            return IncomingPacketActions.Animations.GetOverrideEventTimeByName(
                objectId,
                new[] { orderedCycle[1] },
                "OnAnimationShoot");
        }

        return IncomingPacketActions.Animations.GetOverrideEventTimeByName(objectId, orderedCycle, "OnAnimationShoot");
    }

    public static float ResolveMagicFlightTimeMs(Entity entity, int skillId, Transform target)
    {
        const float fallbackFlightMs = 1000f;

        if (EffectManager.Instance != null && EffectManager.Instance.database != null &&
            EffectManager.Instance.database.ShouldIgnoreFlightTimeForCast(skillId))
        {
            return 0f;
        }

        if (entity == null || target == null)
        {
            return fallbackFlightMs;
        }

        Vector3 startPos = entity.GetPositionRightHand();
        Vector3 aimPos = VectorUtils.GetCollision(startPos, target);
        float distance = Vector3.Distance(startPos, aimPos);
        if (distance <= 0f)
        {
            return fallbackFlightMs;
        }

        float flightSeconds = ProjectileFlightTimeCalculator.CalculateL2SkillFlightTimeSeconds(distance);
        return flightSeconds * 1000f;
    }
}
