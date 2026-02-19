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
        for (int i=0; i < cycle.Length; i++)
        {
            string animName = cycle[i];

            if (animName != "none")
            {
                string triggerName = GetTriggerName(i);
                string overrideAnimName = cycle[i];

                Debug.Log("SkillAnimationRunner>StartRunOverride: animName " + animName + " overrideAnimName " + overrideAnimName);
                await animationManager.AsyncPlayAnimationRaceOverrides(objectId, triggerName  , overrideAnimName);
            }

        }

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
