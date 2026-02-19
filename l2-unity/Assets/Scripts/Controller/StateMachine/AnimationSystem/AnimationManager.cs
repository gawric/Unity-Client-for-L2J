using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class AnimationManager : BaseAnimationManager , IAnimationManager
{
   
    private static AnimationManager _instance;
    private const string SP_TIME_ATK = "sptimeatk";
    public static IAnimationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnimationManager();
            }
            return _instance;
        }
    }

    public void PlayAnimation(int objectId , string animationName, bool disableTriggerAfterStart)
    {
        IAnimationController controller = GetPlayerController(objectId);
        string finalAnimName = GetFinalNameAnim(objectId , animationName );

        Entity entity = GetEntity(objectId);
        DesableLastPlayerAnimationElseTrue(objectId, controller);


        SetRecentName(objectId , finalAnimName);
   
        controller.SetBool(finalAnimName, true, entity.name);
        Debug.Log($"AnimationManager> start bool name player  {entity.name} animation {finalAnimName}");
    }

    public void PlayAnimationTrigger(int objectId, string animationName)
    {
        IAnimationController controller = GetPlayerController(objectId);
        string triggerName = GetFinalNameAnim(objectId , animationName);
        Entity entity = GetEntity(objectId);
        DesableLastPlayerAnimationElseTrue(objectId, controller);
        controller.ToggleAnimationTrigger(triggerName);

        Debug.Log($"AnimationManager> start trigger name player  {entity.name} animation {triggerName}");
    }


    //Async Wait End Event
    public async Task AsyncPlayAnimationTrigger(int objectId, string triggerName)
    {

      
        if (_tcsMap.TryGetValue(objectId, out var oldTcs))
        {
            oldTcs.TrySetResult(false);
        }

    
        var tcs = new TaskCompletionSource<bool>();
        _tcsMap[objectId] = tcs;

        AnimationModel model = GetModel(objectId);

        ReturnAwait(model);

        PlayerAnimationTrigger(objectId, triggerName);

  
        await tcs.Task;
    }

    public async Task AsyncPlayAnimationRaceOverrides(int objectId, string triggerName , string overrideAnimationName)
    {
        if (_tcsMap.TryGetValue(objectId, out var oldTcs))
        {
            oldTcs.TrySetResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        _tcsMap[objectId] = tcs;
        AnimationModel model = GetModel(objectId);
        //Root Animator FDarkElf
        string animName = SkillAnimationDatabase.GetAnimationClipName(triggerName, "FDarkElf");

        model.GetController().ReplaceAnimClip(animName , overrideAnimationName);

        ReturnAwait(model);


        PlayerAnimationTrigger(objectId, triggerName , false);



        await tcs.Task;
    }


    private void ReturnAwait(AnimationModel model)
    {
        model.SubscribeToInternalEvents();
        model.OnAnimationFinishedWithId += OnAnimationFinished;
    }
    public void OnAnimationFinished(string name, int objectId)
    {
        if (_tcsMap.TryGetValue(objectId, out var tcs))
        {

            AnimationModel model = GetModel(objectId);
            if (model != null)
            {
                model.OnAnimationFinishedWithId -= OnAnimationFinished;
            }


            _tcsMap.Remove(objectId);

            tcs.TrySetResult(true);
        }
    }

    public void PlayerAnimationTrigger(int objectId , string animationName , bool useFinalName = true)
    {
        IAnimationController controller = GetPlayerController(objectId);
        if(useFinalName) animationName = GetFinalNameAnim(objectId, animationName);

        Entity entity = GetEntity(objectId);
        DesableLastPlayerAnimationElseTrue(objectId , controller);
        controller.ToggleAnimationTrigger(animationName);

        Debug.Log($"AnimationManager> start Async AnimationTrigger(  {entity} animation  {animationName}");
    }


    public void PlayMonsterAnimation(int objectId, string animationName)
    {
        IAnimationController controllerAnimator = GetMonsterController(objectId);
        DisableLastMonsterAnimationElseTrue(objectId, controllerAnimator, animationName);
        SetMonsterRecentName(objectId, animationName);
        controllerAnimator.SetBool(animationName, true);
    }
   
    public Dictionary<string, float> PlayerGetAllFloat(int objectId)
    {
        PlayerAnimationController controller = GetPlayerController(objectId);
        return controller.GetParametrs();
    }

    public void StopMonsterCurrentAnimation(int objectId, string animationName)
    {
        if (GetMonsterController(objectId) is { } controller)
        {
            controller.SetBool(animationName, false);
        }
        else
        {
            Debug.LogWarning($"AnimationManager->StopMonsterCurrentAnimation: Не критическая ошибка - animator not found for monster {objectId}. Animation: {animationName}");
        }
    }

    public void SetSpTimeAtk(int objectId, int timeAtk)
    {
       GetPlayerController(objectId)?.SetInt(SP_TIME_ATK , timeAtk);
    }

    public float[] GetOverrideClipsDurations(int objectId, string[]cycle)
    {
        IAnimationController controller = GetPlayerController(objectId);
        float[] durations = new float[cycle.Length];

        for(int i=0; i< cycle.Length; i++)
        {
            AnimationClip clip = SkillAnimationDatabase.GetOverrideClip(cycle[i], controller.GetAnimatorName());
            if(clip != null)
            {
                durations[i] = clip.length;
            }
        }

        return durations;
    }

    public float GetOverrideEventTimeByName(int objectId, string[] cycle, string eventName)
    {
        IAnimationController controller = GetPlayerController(objectId);

        for (int i = 0; i < cycle.Length; i++)
        {
            AnimationClip clip = SkillAnimationDatabase.GetOverrideClip(cycle[i], controller.GetAnimatorName());
            if (clip != null)
            {
                float timeStartEvent = controller.GetEventTimeByName(clip, eventName);
                if (timeStartEvent != 0) return timeStartEvent;
            }
        }
        return 0;
    }
}
