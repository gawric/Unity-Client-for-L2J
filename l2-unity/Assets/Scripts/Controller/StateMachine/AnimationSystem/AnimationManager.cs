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
            if (_instance == null && App.HasContainer)
            {
                try
                {
                    _instance = App.Resolve<AnimationManager>();
                }
                catch
                {
                }
            }

            if (_instance == null)
                _instance = new AnimationManager();
            return _instance;
        }
    }

    public static void Bind(AnimationManager manager)
    {
        if (manager != null)
            _instance = manager;
    }

    public void PlayAnimation(int objectId , string animationName, bool disableTriggerAfterStart)
    {
        if (GetModel(objectId) == null)
            return;

        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return;

        // Bare names without weapon suffix (death / rebirth) — before GetFinalNameAnim appends _1HS etc.
        if (PlayerDeathAnim.TryResolve(animationName, out string bareStateName))
        {
            PlayExactAnimatorState(objectId, bareStateName);
            return;
        }

        string finalAnimName = GetFinalNameAnim(objectId , animationName );

        Entity entity = GetEntity(objectId);

        // Locomotion: wait_/walk_/run_/atkwait_* → CrossFadeInFixedTime (bool graph bypass).
        if (PlayerLocomotionAnim.TryResolve(finalAnimName, out string locoStateName, out PlayerLocomotionFamily family))
        {
            // Dedicated AtkWait path — breakpoint here to see callers / skip vs play.
            if (family == PlayerLocomotionFamily.AtkWait)
            {
                HandleAtkWaitCrossFade(objectId, controller, locoStateName);
                return;
            }

            if (PlayerLocomotionCrossFade.ShouldSkip(controller, locoStateName, GetCurrentAnimationName(objectId)))
            {
                SetRecentName(objectId, locoStateName);
                Debug.Log(
                    $"AnimationManager> skip crossfade {family} player {entity.name} " +
                    $"state={locoStateName} reason=already_playing");
                return;
            }

            DesableLastPlayerAnimationElseTrue(objectId, controller);
            SetRecentName(objectId, locoStateName);
            // MagicShot leaves animator.speed scaled (SpeedShot); wait must not keep that rate.
            controller.SetAnimatorSpeed(1f);
            float waitExitDuration = LocomotionCrossFadeSettings.ResolveExitDuration(controller);
            PlayerLocomotionCrossFade.TryPlay(controller, locoStateName, waitExitDuration);
            Debug.Log(
                $"AnimationManager> start crossfade {family} player {entity.name} " +
                $"state={locoStateName} duration={waitExitDuration:F3}s");
            return;
        }

        DesableLastPlayerAnimationElseTrue(objectId, controller);
        SetRecentName(objectId , finalAnimName);
   
        controller.SetBool(finalAnimName, true, entity.name);
        Debug.Log($"AnimationManager> start bool name player  {entity.name} animation {finalAnimName}");
    }

    /// <summary>
    /// Lobby character select / create pawns (no World Entity id).
    /// Full name e.g. wait_hand, walk_1HS — same CrossFade path as in-world locomotion.
    /// </summary>
    public void PlayLobbyLocomotion(IAnimationController controller, string stateOrPrefixWithWeapon)
    {
        if (controller == null || string.IsNullOrEmpty(stateOrPrefixWithWeapon))
        {
            return;
        }

        AnimationEventForwarder.BindAnimator(controller);

        if (!PlayerLocomotionAnim.TryResolve(stateOrPrefixWithWeapon, out string stateName, out PlayerLocomotionFamily family))
        {
            Debug.LogWarning(
                $"AnimationManager> lobby locomotion unresolved '{stateOrPrefixWithWeapon}'");
            return;
        }

        // Always play — lobby has no recent-name cache; force pose on place / walk stop.
        controller.CrossFadeInFixedTime(stateName, LocomotionCrossFadeSettings.FixedDuration);
        Debug.Log(
            $"AnimationManager> start crossfade lobby {family} state={stateName} " +
            $"duration={LocomotionCrossFadeSettings.FixedDuration:F3}s");
    }

    /// <summary>
    /// AtkWait only. Does not skip on "already playing" — code owns the pose after attack
    /// (animator exit-transition + PlayAnimation used to race and sometimes skip).
    /// </summary>
    private void HandleAtkWaitCrossFade(
        int objectId,
        IAnimationController controller,
        string atkWaitStateName)
    {
        DesableLastPlayerAnimationElseTrue(objectId, controller);
        SetRecentName(objectId, atkWaitStateName);

        // Leave attack playback rate so atkwait does not stay frozen at last swing speed.
        controller.SetAnimatorSpeed(1f);
        controller.SetPAtkSpeed(1f);

        // Always CrossFade — even if animator already in atkwait (stale transition / same pose).
        // Dual jatk often still mid-clip when wall cycle ends — longer blend softens the cut.
        float duration = LocomotionCrossFadeSettings.ResolveExitDuration(controller);
        controller.CrossFadeInFixedTime(atkWaitStateName, duration);
    }

    public void PlayAnimationTrigger(int objectId, string animationName)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return;

        string triggerName = GetFinalNameAnim(objectId , animationName);
        Entity entity = GetEntity(objectId);
        DesableLastPlayerAnimationElseTrue(objectId, controller);

        // Basic melee (jatk*) and SpAtk* → CrossFade; MagicShot/Cast stay on Trigger.
        if (TryPlayCombatCrossFade(objectId, controller, entity, triggerName))
        {
            return;
        }

        controller.ToggleAnimationTrigger(triggerName);
        SetRecentName(objectId, triggerName);

        Debug.Log($"AnimationManager> start trigger name player  {entity.name} animation {triggerName}");
    }

    /// <summary>
    /// jatk01/02/03_* and SpAtk01/02_* → CrossFade + priority lock.
    /// </summary>
    private bool TryPlayCombatCrossFade(
        int objectId,
        IAnimationController controller,
        Entity entity,
        string resolvedStateName)
    {
        string stateName = null;
        string family = null;
        if (PlayerBasicAttackAnim.TryResolve(resolvedStateName, out stateName))
        {
            family = "basicAtk";
        }
        else if (PlayerSpAtkAnim.TryResolve(resolvedStateName, out stateName))
        {
            family = "spAtk";
        }
        else
        {
            return false;
        }

        SetRecentName(objectId, stateName);
        if (family == "basicAtk")
            ApplyLinearMeleePAtkSpeed(objectId, stateName, -1f);
        float fade = PlayerBasicAttackCrossFade.ResolveDuration(stateName);
        PlayerBasicAttackCrossFade.TryPlay(controller, stateName, fade);
        Debug.Log(
            $"AnimationManager> start crossfade {family} player {entity.name} " +
            $"state={stateName} duration={fade:F3}s");
        return true;
    }


    //Async Wait End Event
    public async Task<bool> AsyncPlayAnimationTrigger(int objectId, string triggerName)
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

        bool completed = await tcs.Task;
        FinishOverrideAwait(objectId, tcs, expectedFinishName, completed);
        Debug.Log(
            $"[AnimAwait] {(completed ? "END" : "CANCELLED")} objectId={objectId} trigger='{triggerName}' " +
            $"mode=default elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
        return completed;
    }

    public async Task<bool> AsyncPlayAnimationRaceOverrides(int objectId, string triggerName , string overrideAnimationName)
    {
        float startedAt = Time.time;
        string expectedFinishName = SkillAnimationDatabase.ResolveOverrideCompletionEventName(triggerName);
        if (_tcsMap.TryGetValue(objectId, out var oldTcs))
        {
            Debug.Log(
                $"[AnimAwait] SUPERSEDE objectId={objectId} newTrigger='{triggerName}' " +
                $"override='{overrideAnimationName}' (prev await → false, no WAIT_RETURN)");
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

        bool completed = await tcs.Task;
        FinishOverrideAwait(objectId, tcs, expectedFinishName, completed);
        Debug.Log(
            $"[AnimAwait] {(completed ? "END" : "CANCELLED")} objectId={objectId} trigger='{triggerName}' " +
            $"override='{overrideAnimationName}' mode=override elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
        return completed;
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

    public async Task<bool> AsyncAwaitOverrideAnimationFinish(int objectId, string expectedFinishNameOrTrigger)
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

        bool completed = await tcs.Task;
        FinishOverrideAwait(objectId, tcs, sanitizedExpected, completed);

        Debug.Log(
            $"[AnimAwait] {(completed ? "END" : "CANCELLED")} objectId={objectId} expectedFinish='{sanitizedExpected}' " +
            $"mode=await_finish elapsed={Time.time - startedAt:F3}s now={Time.time:F3}");
        return completed;
    }

    /// <summary>
    /// After await: clear maps only if this TCS still owns the slot / expected name.
    /// Superseded awaits must not wipe the newer cast's expectedFinish.
    /// </summary>
    private void FinishOverrideAwait(
        int objectId,
        TaskCompletionSource<bool> tcs,
        string expectedFinishName,
        bool completed)
    {
        if (_tcsMap.TryGetValue(objectId, out TaskCompletionSource<bool> live) &&
            ReferenceEquals(live, tcs))
        {
            _tcsMap.Remove(objectId);
        }

        if (!completed)
        {
            return;
        }

        if (_expectedFinishNameByObjectId.TryGetValue(objectId, out string exp) &&
            string.Equals(exp, expectedFinishName, StringComparison.Ordinal))
        {
            _expectedFinishNameByObjectId.Remove(objectId);
        }
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
        TryCompleteAwaitFromPhaseFinish(objectId, name, "clip_complete");
    }

    /// <summary>
    /// SMB-driven phase end (Cast/MagicShot / SpAtk). Completes runner await
    /// without relying on clip OnAnimationComplete.
    /// </summary>
    public void NotifyMagicPhaseFinished(int objectId, string phaseName)
    {
        TryCompleteAwaitFromPhaseFinish(objectId, phaseName, "smb_phase");
    }

    private bool TryCompleteAwaitFromPhaseFinish(int objectId, string name, string source)
    {
        Debug.Log(
            $"[AnimFinishedEvent] objectId={objectId} finishedName='{name}' source={source} now={Time.time:F3}");

        // SpAtk / jatk / Cast / MagicShot awaits are completed by SMB NotifyMagicPhaseFinished.
        // Early clip OnAnimationComplete must not finish the skill runner mid-dual.
        if (string.Equals(source, "clip_complete", StringComparison.Ordinal) &&
            IsSmbOwnedCombatOrMagicPhase(name))
        {
            Debug.Log(
                $"[AnimFinishedEvent] IGNORE clip_complete objectId={objectId} finishedName='{name}' " +
                $"(SMB owns phase finish)");
            return false;
        }

        if (_expectedFinishNameByObjectId.TryGetValue(objectId, out string expectedName))
        {
            if (string.Equals(source, "clip_complete", StringComparison.Ordinal) &&
                IsSmbOwnedCombatOrMagicPhase(expectedName))
            {
                Debug.Log(
                    $"[AnimFinishedEvent] IGNORE clip_complete objectId={objectId} " +
                    $"finishedName='{name}' expected='{expectedName}' (SMB owns phase finish)");
                return false;
            }

            if (!string.Equals(name, expectedName, StringComparison.Ordinal) &&
                !string.Equals(SkillAnimationDatabase.ResolveOverrideCompletionEventName(name), expectedName, StringComparison.Ordinal))
            {
                Debug.Log(
                    $"[AnimFinishedEvent] IGNORE objectId={objectId} finishedName='{name}' expected='{expectedName}' " +
                    $"source={source} now={Time.time:F3}");
                return false;
            }
        }

        if (_tcsMap.TryGetValue(objectId, out var tcs))
        {
            _tcsMap.Remove(objectId);
            tcs.TrySetResult(true);
            return true;
        }

        return false;
    }

    private static bool IsSmbOwnedCombatOrMagicPhase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.StartsWith("SpAtk", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("jatk", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("CastMid", StringComparison.Ordinal)
            || name.StartsWith("CastEnd", StringComparison.Ordinal)
            || name.StartsWith("MagicShot", StringComparison.Ordinal);
    }

    public void PlayerAnimationTrigger(int objectId , string animationName , bool useFinalName = true)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return;

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

        // Skill runner / async path — same CrossFade for jatk / SpAtk as PlayAnimationTrigger.
        if (TryPlayCombatCrossFade(objectId, controller, entity, animationName))
        {
            return;
        }

        controller.ToggleAnimationTrigger(animationName);
        SetRecentName(objectId, animationName);

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
        PlayMonsterAnimation(objectId, animationName, null);
    }

    public void PlayMonsterAnimation(int objectId, string animationName, float? fixedDuration)
    {
        IAnimationController controller = GetMonsterController(objectId);
        if (controller == null)
        {
            Debug.LogWarning(
                $"[ANIM] PlayMonster MISS id={objectId} request='{animationName}' reason=no_controller");
            return;
        }

        if (MonsterAnim.TryResolve(animationName, out string stateName, out MonsterAnimState family))
        {
            SetMonsterRecentName(objectId, stateName);
            float duration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
            Debug.Log(
                $"[ANIM] PlayMonster id={objectId} request='{animationName}' resolved='{stateName}' family={family} duration={duration:F3}s");
            MonsterCrossFade.TryPlay(controller, stateName, family, duration);
            return;
        }

        SetMonsterRecentName(objectId, animationName);
        float rawDuration = fixedDuration ?? LocomotionCrossFadeSettings.FixedDuration;
        Debug.Log(
            $"[ANIM] PlayMonster RAW id={objectId} request='{animationName}' duration={rawDuration:F3}s");
        MonsterCrossFade.TryPlayRaw(controller, animationName, rawDuration);
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
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return;

        controller.SetInt(SP_TIME_ATK, timeAtk);
    }

    public void ResetPlayerAnimatorSpeed(int objectId, float speed = 1f)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
        {
            Debug.LogWarning($"[AnimSpeed] reset skipped objectId={objectId} reason=no_controller");
            return;
        }

        controller.SetAnimatorSpeed(speed);
        Debug.Log($"[AnimSpeed] reset objectId={objectId} speed={speed:F3}");
    }

    public void SetPAtkSpeed(int objectId, float patkspd)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return;

        controller.SetPAtkSpeed(patkspd);
    }

    public float ApplyLinearMeleePAtkSpeed(int objectId, string animName, float clipLengthSec)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        Entity entity = GetEntity(objectId);
        if (controller == null)
            return 1f;

        float clipSec = clipLengthSec;
        if (clipSec <= 0f)
            clipSec = ResolveAttackClipLength(controller.GetAnimator(), animName);

        float cycleMs = AttackTimingHelper.ResolveAttackCycleMs(entity, animName);
        float linear = AttackTimingHelper.ComputeLinearPAtkSpeed(clipSec, cycleMs);
        controller.SetPAtkSpeed(linear);

        Debug.Log(
            $"[PATKSPD] ApplyLinear objectId={objectId} entity={entity?.name} anim={animName} " +
            $"pAtkSpd={AttackTimingHelper.ResolvePAtkSpd(entity):F1} cycleMs={cycleMs:F1} " +
            $"clipSec={clipSec:F3} patkspd={linear:F3}");
        return linear;
    }

    static float ResolveAttackClipLength(Animator animator, string stateName)
    {
        if (animator == null)
            return 1f;

        if (animator.IsInTransition(0))
        {
            AnimatorClipInfo[] next = animator.GetNextAnimatorClipInfo(0);
            if (next != null && next.Length > 0 && next[0].clip != null)
                return next[0].clip.length;
        }

        if (TryFindNamedClipLength(animator, stateName, out float namedLength))
            return namedLength;

        return 1f;
    }

    static bool TryFindNamedClipLength(Animator animator, string nameContains, out float length)
    {
        length = 0f;
        if (animator == null || string.IsNullOrEmpty(nameContains))
            return false;

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        if (rac != null)
        {
            AnimationClip[] clips = rac.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                    continue;
                if (clip.name == nameContains ||
                    clip.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    length = clip.length;
                    return true;
                }
            }
        }

        float cached = AnimationDataCache.GetOverrideLength(animator, nameContains);
        if (cached > 0.01f)
        {
            length = cached;
            return true;
        }

        return false;
    }

    float GetClipLengthByName(int objectId, string nameContains, float fallback)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return fallback;

        if (!TryFindNamedClipLength(controller.GetAnimator(), nameContains, out float length))
            return fallback;
        return length;
    }

    public float PlayExactAnimatorState(int objectId, string stateName, bool snapToEnd = false)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null || string.IsNullOrEmpty(stateName))
            return 1.6f;

        AnimationEventForwarder.BindAnimator(controller);
        controller.SetEnabled(true);
        DesableLastPlayerAnimationElseTrue(objectId, controller);
        SetRecentName(objectId, stateName);
        controller.SetAnimatorSpeed(1f);
        controller.SetPAtkSpeed(1f);

        Animator animator = controller.GetAnimator();
        if (snapToEnd && animator != null)
        {
            animator.Play(stateName, 0, 1f);
            animator.Update(0f);
        }
        else
        {
            controller.CrossFadeInFixedTime(stateName, LocomotionCrossFadeSettings.FixedDuration);
        }

        float duration = GetClipLengthByName(objectId, stateName, 1.6f);
        return Mathf.Max(0.4f, duration);
    }

    public float[] GetOverrideClipsDurations(int objectId, string[]cycle)
    {
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return new float[cycle != null ? cycle.Length : 0];

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
        IAnimationController controller = GetRegisteredController(objectId);
        if (controller == null)
            return 0;

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
