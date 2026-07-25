using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class SkillAnimationRunner
{
    private const string CAST_TRIGGER_MID_OVERRIDE = "CastMid";
    private const string CAST_TRIGGER_END_OVERRIDE = "CastEnd";
    private const string CAST_TRIGGER_SHOT_OVERRIDE = "MagicShot";
    private const string CAST_TRIGGER_END_2P = "CastEnd2P";
    private const string CAST_TRIGGER_SHOT_2P = "MagicShot2P";
    private const string CAST_TRIGGER_MID_LONG = "CastMidLong";
    private const string CAST_TRIGGER_END_LONG = "CastEndLong";
    private const string CAST_TRIGGER_SHOT_LONG = "MagicShotLong";
    private const string NONE_ANIMATION_NAME = "none";

   public async Task StartRun(string[] cycle , int objectId , IAnimationManager animationManager , Action onComplete = null)
    {
        if (cycle == null)
        {
            Debug.LogWarning($"[SkillRun] cycle is null objectId={objectId}");
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"[SkillRun] START objectId={objectId} cycleLength={cycle.Length}");

        for (int i = 0; i < cycle.Length; i++)
        {
            string animName = cycle[i];
            if (ShouldSkipAnimation(animName))
            {
                Debug.Log($"[SkillRun] SKIP objectId={objectId} idx={i} rawAnim='{animName}' reason=none_or_empty");
                continue;
            }

            string sanitizedAnimName = animName.Trim();
            string triggerToPlay = sanitizedAnimName + "_";
     
            await animationManager.AsyncPlayAnimationTrigger(objectId, triggerToPlay);

        }

        onComplete?.Invoke();
    }

    public async Task StartRunOverride(string[] cycle, int objectId, IAnimationManager animationManager, Action onComplete = null)
    {
        if (cycle == null)
        {
            Debug.LogWarning($"[SkillRunOverride] cycle is null objectId={objectId}");
            onComplete?.Invoke();
            return;
        }

        float chainStartTime = Time.time;
        Debug.Log($"[SkillRunOverride] START objectId={objectId} cycleLength={cycle.Length}");
        var validAnimations = new List<string>();
        for (int i = 0; i < cycle.Length; i++)
        {
            string animName = cycle[i];
            bool shouldSkip = ShouldSkipAnimation(animName);
            Debug.Log($"[SkillRunOverride] STEP objectId={objectId} idx={i} rawAnim='{animName}' shouldSkip={shouldSkip}");
            if (!shouldSkip)
            {
                validAnimations.Add(animName.Trim());
            }
            else
            {
                Debug.Log($"[SkillRunOverride] SKIP objectId={objectId} idx={i} rawAnim='{animName}' reason=none_or_empty");
            }
        }

        string[] triggerPlan = BuildTriggerPlan(validAnimations.Count);
        Debug.Log($"[SkillRunOverride] PLAN objectId={objectId} validCount={validAnimations.Count} triggers={string.Join("->", triggerPlan)}");

        for (int logicalIndex = 0; logicalIndex < validAnimations.Count; logicalIndex++)
        {
            string overrideAnimName = validAnimations[logicalIndex];
            string triggerName = triggerPlan[logicalIndex];

            if (string.IsNullOrEmpty(triggerName))
            {
                Debug.LogWarning($"[SkillRunOverride] SKIP objectId={objectId} logicalIdx={logicalIndex} override='{overrideAnimName}' reason=empty_trigger_name");
                continue;
            }

            Debug.Log(
                $"[SkillRunOverride] BEFORE await logicalIdx={logicalIndex} trigger='{triggerName}' override='{overrideAnimName}' " +
                $"sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");
            await animationManager.AsyncPlayAnimationRaceOverrides(objectId, triggerName  , overrideAnimName);
            Debug.Log(
                $"[SkillRunOverride] AFTER await logicalIdx={logicalIndex} trigger='{triggerName}' override='{overrideAnimName}' " +
                $"sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");

            if (logicalIndex == 0 && validAnimations.Count == 2)
            {
                await HoldInCastEndPoseBeforeShot(objectId);
            }
            }

        Debug.Log($"[SkillRunOverride] COMPLETE sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");
        onComplete?.Invoke();
    }

    public async Task StartRunLongOverride(string[] cycle, int objectId, IAnimationManager animationManager, Action onComplete = null)
    {
        if (cycle == null)
        {
            Debug.LogWarning($"[SkillRunLongOverride] cycle is null objectId={objectId}");
            onComplete?.Invoke();
            return;
        }

        float chainStartTime = Time.time;
        var validAnimations = new List<string>();
        for (int i = 0; i < cycle.Length; i++)
        {
            string animName = cycle[i];
            if (!ShouldSkipAnimation(animName))
            {
                validAnimations.Add(animName.Trim());
            }
        }

        if (validAnimations.Count < 3)
        {
            Debug.LogWarning(
                $"[SkillRunLongOverride] expected 3 clips, got {validAnimations.Count} objectId={objectId} — fallback to override path");
            await StartRunOverride(cycle, objectId, animationManager, onComplete);
            return;
        }

        string midClip = validAnimations[0];
        string endClip = validAnimations[1];
        string shotClip = validAnimations[2];

        Debug.Log(
            $"[SkillRunLongOverride] START objectId={objectId} mid='{midClip}' end='{endClip}' shot='{shotClip}'");

        Debug.Log($"[SkillRunLongOverride] phase=CastMidLong objectId={objectId}");
        await animationManager.AsyncPlayAnimationRaceOverrides(objectId, CAST_TRIGGER_MID_LONG, midClip);
        Debug.Log($"[SkillRunLongOverride] phase=CastMidLong done sinceStart={Time.time - chainStartTime:F3}s objectId={objectId}");

        Task shotAwaitTask = animationManager.AsyncAwaitOverrideAnimationFinish(objectId, CAST_TRIGGER_SHOT_LONG);
        Debug.Log($"[SkillRunLongOverride] phase=CastEndLong loop start objectId={objectId}");
        await animationManager.AsyncPlayLongCastLoopPhase(objectId, CAST_TRIGGER_END_LONG, endClip);
        Debug.Log($"[SkillRunLongOverride] phase=CastEndLong loop done sinceStart={Time.time - chainStartTime:F3}s objectId={objectId}");

        await shotAwaitTask;
        Debug.Log($"[SkillRunLongOverride] phase=MagicShotLong done COMPLETE sinceStart={Time.time - chainStartTime:F3}s objectId={objectId}");

        onComplete?.Invoke();
    }

    private static async Task HoldInCastEndPoseBeforeShot(int objectId)
    {
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null)
        {
            return;
        }

        MagicCastData castData = player.GetMagicCastData();
        if (castData == null || castData.TwoClipCastEndHoldSeconds <= 0f)
        {
            return;
        }

        int holdMs = Mathf.RoundToInt(castData.TwoClipCastEndHoldSeconds * 1000f);
        Debug.Log(
            $"[SkillRunOverride] CastEnd2P pose hold objectId={objectId} holdSec={castData.TwoClipCastEndHoldSeconds:F3} " +
            $"before MagicShot2P");
        await Task.Delay(holdMs);
    }

    private static bool ShouldSkipAnimation(string animName)
    {
        if (string.IsNullOrWhiteSpace(animName))
        {
            return true;
        }

        return string.Equals(animName.Trim(), NONE_ANIMATION_NAME, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] BuildTriggerPlan(int validAnimationsCount)
    {
        if (validAnimationsCount <= 0)
        {
            return Array.Empty<string>();
        }

        if (validAnimationsCount == 1)
        {
            // Single animation fallback: use CastEnd because it is the safest cast completion state.
            return new[] { CAST_TRIGGER_END_OVERRIDE };
        }

        if (validAnimationsCount == 2)
        {
            // Two-clip cast uses dedicated animator states with their own transitions.
            return new[] { CAST_TRIGGER_END_2P, CAST_TRIGGER_SHOT_2P };
        }

        // Default full chain for 3+ clips.
        string[] plan = new string[validAnimationsCount];
        plan[0] = CAST_TRIGGER_MID_OVERRIDE;
        plan[1] = CAST_TRIGGER_END_OVERRIDE;
        plan[2] = CAST_TRIGGER_SHOT_OVERRIDE;

        for (int i = 3; i < validAnimationsCount; i++)
        {
            plan[i] = CAST_TRIGGER_SHOT_OVERRIDE;
        }

        return plan;
    }
}
