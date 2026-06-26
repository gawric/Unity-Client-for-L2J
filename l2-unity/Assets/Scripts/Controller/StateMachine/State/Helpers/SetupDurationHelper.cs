using System;
using System.Collections.Generic;
using UnityEngine;

public class SetupDurationHelper
{
    public const int ScrollOfEscapeSkillId = 2013;

    public static bool IsLongCastSkill(MagicSkillUse useSkill)
    {
        return useSkill != null && useSkill.SkillId == ScrollOfEscapeSkillId;
    }

    public static void SetupLongCastDurationIfHitTimeNot0(MagicSkillUse useSkill, int objectId, Entity entity, AnimationCombo selfCombo)
    {
        if (useSkill.HitTime <= 0)
        {
            return;
        }

        string[] orderedCycle = BuildOrderedCycleForOverrideTiming(selfCombo.GetAnimCycle());
        float[] durations = AnimationManager.Instance.GetOverrideClipsDurations(objectId, orderedCycle);
        float shotEventTime = ResolveShotEventTime(objectId, orderedCycle);
        entity.SetupLongCastDuration(useSkill.HitTime, durations, shotEventTime, useSkill.TargetId);
    }

    public static void SetupDurationIfHitTimeNot0(MagicSkillUse useSkill, int objectId, Entity entity, AnimationCombo selfCombo)
    {

        if (useSkill.HitTime > 0)
        {
            string[] orderedSelfCycle = BuildOrderedCycleForOverrideTiming(selfCombo.GetAnimCycle());
            float[] durations_self = AnimationManager.Instance.GetOverrideClipsDurations(objectId, orderedSelfCycle);
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
            return AnimationManager.Instance.GetOverrideEventTimeByName(
                objectId,
                new[] { orderedCycle[1] },
                "OnAnimationShoot");
        }

        return AnimationManager.Instance.GetOverrideEventTimeByName(objectId, orderedCycle, "OnAnimationShoot");
    }
}
