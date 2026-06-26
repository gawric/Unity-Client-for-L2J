using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class AnimationManager : BaseAnimationManager , IAnimationManager
{
   
    private static AnimationManager _instance;
    private readonly HashSet<int> _awaitSubscribedObjectIds = new HashSet<int>();
    private readonly Dictionary<int, string> _expectedFinishNameByObjectId = new Dictionary<int, string>();
    private const string SP_TIME_ATK = "sptimeatk";
    private const string CAST_TRIGGER_MID = "CastMid";
    private const string CAST_TRIGGER_END = "CastEnd";
    private const string CAST_TRIGGER_SHOT = "MagicShot";
    private const string CAST_TRIGGER_END_2P = "CastEnd2P";
    private const string CAST_TRIGGER_SHOT_2P = "MagicShot2P";
    private const string CAST_TRIGGER_MID_LONG = "CastMidLong";
    private const string CAST_TRIGGER_END_LONG = "CastEndLong";
    private const string CAST_TRIGGER_SHOT_LONG = "MagicShotLong";
    private const string SHOT_SOURCE_RUNNER = "Runner";
    private const int OVERRIDE_AWAIT_WARN_TIMEOUT_MS = 5000;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Телеметрия: откуда пришёл shoot (параметр ивента на клипе) и что реально играет на animator layer0.
    /// Вызывается из <see cref="BaseAnimationController.OnAnimationShoot"/>.
    /// </summary>
    public static void LogAnimationShootFromAnimator(int objectId, string decision, string eventArg, string animatorDetail)
    {
        Debug.Log(
            "[ANIM_SHOOT_EVENT] " + decision +
            $" objectId={objectId} eventArg='{eventArg}' {animatorDetail} " +
            $"now={Time.time:F3}s frame={Time.frameCount}");
    }
#endif

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
        float startedAt = Time.time;
        string expectedFinishName = GetFinalNameAnim(objectId, triggerName);

      
        if (_tcsMap.TryGetValue(objectId, out var oldTcs))
        {
            oldTcs.TrySetResult(false);
        }

    
        var tcs = new TaskCompletionSource<bool>();
        _tcsMap[objectId] = tcs;
        _expectedFinishNameByObjectId[objectId] = expectedFinishName;

        AnimationModel model = GetModel(objectId);

        EnsureAwaitSubscribed(objectId, model);

        Debug.Log($"[AnimAwait] START objectId={objectId} trigger='{triggerName}' mode=default now={Time.time:F3}");
        PlayerAnimationTrigger(objectId, triggerName);

  
        await tcs.Task;
        _expectedFinishNameByObjectId.Remove(objectId);
        Debug.Log($"[AnimAwait] END objectId={objectId} trigger='{triggerName}' mode=default elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
    }

    public async Task AsyncPlayAnimationRaceOverrides(int objectId, string triggerName , string overrideAnimationName)
    {
        float startedAt = Time.time;
        string expectedFinishName = SkillAnimationDatabase.ResolveOverrideCompletionEventName(triggerName);
        if (_tcsMap.TryGetValue(objectId, out var oldTcs))
        {
            oldTcs.TrySetResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        _tcsMap[objectId] = tcs;
        _expectedFinishNameByObjectId[objectId] = expectedFinishName;
        AnimationModel model = GetModel(objectId);
        //Root Animator FDarkElf
        string animName = SkillAnimationDatabase.GetAnimationClipName(triggerName, "FDarkElf");

        model.GetController().ReplaceAnimClip(animName , overrideAnimationName);

        EnsureAwaitSubscribed(objectId, model);

        Debug.Log(
            $"[AnimAwait] START objectId={objectId} trigger='{triggerName}' override='{overrideAnimationName}' " +
            $"expectedFinish='{expectedFinishName}' mode=override now={Time.time:F3}");

        _ = WarnIfAwaitStuck(objectId, triggerName, expectedFinishName, startedAt);

        PlayerAnimationTrigger(objectId, triggerName , false);



        await tcs.Task;
        _expectedFinishNameByObjectId.Remove(objectId);
        Debug.Log(
            $"[AnimAwait] END objectId={objectId} trigger='{triggerName}' override='{overrideAnimationName}' " +
            $"mode=override elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
    }

    public async Task AsyncPlayLongCastLoopPhase(int objectId, string triggerName, string overrideAnimationName)
    {
        float startedAt = Time.time;
        string expectedFinishName = string.IsNullOrWhiteSpace(overrideAnimationName)
            ? triggerName
            : overrideAnimationName.Trim();

        AnimationModel model = GetModel(objectId);
        string animName = SkillAnimationDatabase.GetAnimationClipName(triggerName, "FDarkElf");
        model.GetController().ReplaceAnimClip(animName, overrideAnimationName);
        EnsureAwaitSubscribed(objectId, model);

        TaskCompletionSource<bool> loopTcs = LongCastCoordinator.RegisterLoopPhase(objectId);

        Debug.Log(
            $"[AnimAwait] START objectId={objectId} trigger='{triggerName}' override='{overrideAnimationName}' " +
            $"mode=long_loop now={Time.time:F3}");

        ResetLongCastTriggers(GetPlayerController(objectId));
        PlayerAnimationTrigger(objectId, triggerName, false);

        await loopTcs.Task;

        Debug.Log(
            $"[AnimAwait] END objectId={objectId} trigger='{triggerName}' mode=long_loop " +
            $"elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
    }

    public async Task AsyncAwaitOverrideAnimationFinish(int objectId, string expectedFinishNameOrTrigger)
    {
        float startedAt = Time.time;
        string sanitizedExpected = SkillAnimationDatabase.ResolveOverrideCompletionEventName(
            string.IsNullOrWhiteSpace(expectedFinishNameOrTrigger)
                ? string.Empty
                : expectedFinishNameOrTrigger.Trim());

        if (_tcsMap.TryGetValue(objectId, out TaskCompletionSource<bool> oldTcs))
        {
            oldTcs.TrySetResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        _tcsMap[objectId] = tcs;
        _expectedFinishNameByObjectId[objectId] = sanitizedExpected;

        AnimationModel model = GetModel(objectId);
        EnsureAwaitSubscribed(objectId, model);

        Debug.Log(
            $"[AnimAwait] START objectId={objectId} expectedFinish='{sanitizedExpected}' mode=await_finish now={Time.time:F3}");

        _ = WarnIfAwaitStuck(objectId, "await_finish", sanitizedExpected, startedAt);

        await tcs.Task;
        _expectedFinishNameByObjectId.Remove(objectId);

        Debug.Log(
            $"[AnimAwait] END objectId={objectId} expectedFinish='{sanitizedExpected}' mode=await_finish " +
            $"elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
    }

    private async Task WarnIfAwaitStuck(int objectId, string triggerName, string expectedFinishName, float startedAt)
    {
        await Task.Delay(OVERRIDE_AWAIT_WARN_TIMEOUT_MS);

        if (_expectedFinishNameByObjectId.TryGetValue(objectId, out string currentExpected) &&
            string.Equals(currentExpected, expectedFinishName, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                $"[AnimAwait] STUCK? objectId={objectId} trigger='{triggerName}' expectedFinish='{expectedFinishName}' " +
                $"elapsed={Time.time - startedAt:F3}s action=waiting_for_OnAnimationFinished");
        }
    }


    private void EnsureAwaitSubscribed(int objectId, AnimationModel model)
    {
        if (_awaitSubscribedObjectIds.Contains(objectId))
        {
            return;
        }

        model.SubscribeToInternalEvents();
        model.OnAnimationFinishedWithId += OnAnimationFinished;
        _awaitSubscribedObjectIds.Add(objectId);
    }
    public void OnAnimationFinished(string name, int objectId)
    {
        Debug.Log($"[AnimFinishedEvent] objectId={objectId} finishedName='{name}' now={Time.time:F3}");

        if (_expectedFinishNameByObjectId.TryGetValue(objectId, out string expectedName))
        {
            if (!string.Equals(name, expectedName, StringComparison.Ordinal) &&
                !string.Equals(SkillAnimationDatabase.ResolveOverrideCompletionEventName(name), expectedName, StringComparison.Ordinal))
            {
                Debug.Log(
                    $"[AnimFinishedEvent] IGNORE objectId={objectId} finishedName='{name}' expected='{expectedName}' now={Time.time:F3}");
                return;
            }
        }

        if (_tcsMap.TryGetValue(objectId, out var tcs))
        {
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

        bool isMagicShotTrigger = IsMagicShotTrigger(animationName);
        if (isMagicShotTrigger)
        {
            MagicCastData castData = entity != null ? entity.GetMagicCastData() : null;
            if (!MagicShotCoordinator.TryStartShot(objectId, castData, SHOT_SOURCE_RUNNER, out string coordinatorMessage))
            {
                Debug.Log($"{coordinatorMessage} trigger='{animationName}' action=skip");
                return;
            }

            Debug.Log($"{coordinatorMessage} trigger='{animationName}' action=run");
        }

        if (IsMagicCastTrigger(animationName))
        {
            // Prevent stale cast triggers from keeping previous cast states alive.
            controller.ResetAnimationTrigger(CAST_TRIGGER_MID);
            controller.ResetAnimationTrigger(CAST_TRIGGER_END);
            controller.ResetAnimationTrigger(CAST_TRIGGER_SHOT);
            controller.ResetAnimationTrigger(CAST_TRIGGER_END_2P);
            controller.ResetAnimationTrigger(CAST_TRIGGER_SHOT_2P);
            ResetLongCastTriggers(controller);
        }

        controller.ToggleAnimationTrigger(animationName);

        Debug.Log($"AnimationManager> start Async AnimationTrigger(  {entity} animation  {animationName}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsMagicShotTrigger(animationName) && entity != null)
        {
            MagicCastData cd = entity.GetMagicCastData();
            if (cd != null)
            {
                float globalSinceCast = Time.time - cd.StartTime;
                Debug.Log(
                    "[MAGIC_PROJECTILE_SYNC] AnimatorTriggerMagicShot " +
                    $"globalSinceCast={globalSinceCast:F3}s serverShoot={cd.serverTimeToShoot:F3}s " +
                    $"deltaToShoot={globalSinceCast - cd.serverTimeToShoot:F3}s trigger={animationName}");
            }
        }
#endif
    }

    private static void ResetLongCastTriggers(IAnimationController controller)
    {
        if (controller == null)
        {
            return;
        }

        controller.ResetAnimationTrigger(CAST_TRIGGER_MID_LONG);
        controller.ResetAnimationTrigger(CAST_TRIGGER_END_LONG);
        controller.ResetAnimationTrigger(CAST_TRIGGER_SHOT_LONG);
    }

    private static bool IsMagicCastTrigger(string animationName)
    {
        return animationName == CAST_TRIGGER_MID ||
               animationName == CAST_TRIGGER_END ||
               animationName == CAST_TRIGGER_SHOT ||
               animationName == CAST_TRIGGER_END_2P ||
               animationName == CAST_TRIGGER_SHOT_2P ||
               animationName == CAST_TRIGGER_MID_LONG ||
               animationName == CAST_TRIGGER_END_LONG ||
               animationName == CAST_TRIGGER_SHOT_LONG;
    }

    private static bool IsMagicShotTrigger(string animationName)
    {
        return animationName == CAST_TRIGGER_SHOT ||
               animationName == CAST_TRIGGER_SHOT_2P ||
               animationName == CAST_TRIGGER_SHOT_LONG ||
               animationName.StartsWith(CAST_TRIGGER_SHOT, StringComparison.Ordinal);
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

    public void ResetPlayerAnimatorSpeed(int objectId, float speed = 1f)
    {
        IAnimationController controller = GetPlayerController(objectId);
        if (controller == null)
        {
            Debug.LogWarning($"[AnimSpeed] reset skipped objectId={objectId} reason=no_controller");
            return;
        }

        controller.SetAnimatorSpeed(speed);
        Debug.Log($"[AnimSpeed] reset objectId={objectId} speed={speed:F3}");
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
