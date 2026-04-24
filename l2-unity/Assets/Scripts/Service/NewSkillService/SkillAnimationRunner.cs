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

   public async Task StartRun(string[] cycle , int objectId , IAnimationManager animationManager , Action onComplete = null)
    {
        foreach (string animName in cycle)
        {
            if(animName != "none")
            {
                //"SpAtk01" need "SpAtk01_"
                Debug.Log("SkillAnimationRunner>StartRun: animName " + animName);
                await animationManager.AsyncPlayAnimationTrigger(objectId, animName + "_" );
            }

        }

        onComplete?.Invoke();
    }

    public async Task StartRunOverride(string[] cycle, int objectId, IAnimationManager animationManager, Action onComplete = null)
    {
        float chainStartTime = Time.time;
        for (int i=0; i < cycle.Length; i++)
        {
            string animName = cycle[i];

            if (animName != "none")
            {
                string triggerName = GetTriggerName(i);
                string overrideAnimName = cycle[i];

                Debug.Log(
                    $"[SkillRunOverride] BEFORE await idx={i} trigger='{triggerName}' override='{overrideAnimName}' " +
                    $"sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");
                await animationManager.AsyncPlayAnimationRaceOverrides(objectId, triggerName  , overrideAnimName);
                Debug.Log(
                    $"[SkillRunOverride] AFTER await idx={i} trigger='{triggerName}' override='{overrideAnimName}' " +
                    $"sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");
            }

        }

        Debug.Log($"[SkillRunOverride] COMPLETE sinceChainStart={Time.time - chainStartTime:F3}s objectId={objectId}");
        onComplete?.Invoke();
    }

    private string GetTriggerName(int index)
    {
        if(index == 0)
        {
            return CAST_TRIGGER_MID_OVERRIDE;
        }
        else if (index == 1)
        {
            return CAST_TRIGGER_END_OVERRIDE;
        }
        else if (index == 2)
        {
            return CAST_TRIGGER_SHOT_OVERRIDE;
        }

        return "";
    }
}
