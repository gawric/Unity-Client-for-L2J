using UnityEngine;

/// <summary>
/// L2 bow jatk: shoot = OnAnimationShoot notify fraction.
/// Arrow flight uses ANProjectile accel (dirMul=3000, path (t/T)²).
/// Animation timing mirrors Interlude BowAttackProcess:
/// PlayAnim(GetAtk01AnimRate * AttackSpeedMultiplier, 0.2 / PlayRate).
/// Projectile distance affects only flight, never the AttackShot frame.
/// </summary>
public class PlayerStateJatkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _releaseAt;
    private float _tweenSeconds;
    private float _motionSpeed = 1f;
    private bool _isSwitchIdle;

    public string parameterName;
    public string motionName;
    public string eventShootName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _startTime = Time.time;

        AnimationClip clip = AnimationDataCache.GetOverrideClip(animator, motionName);
        float clipLength = clip != null ? clip.length : AnimationDataCache.GetOverrideLength(animator, motionName);
        float eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);
        if (clipLength <= 0.01f)
            clipLength = Mathf.Max(stateInfo.length, 0.01f);
        if (eventTimeInClip <= 0.01f)
            eventTimeInClip = clipLength * 0.637f;
        if (eventTimeInClip > clipLength)
            eventTimeInClip = clipLength;

        int objectId = AnimatorUtils.GetObjectId(animator);
        Entity entity = World.Instance != null ? World.Instance.GetEntityNoLockSync(objectId) : null;
        float unityClipFps = clip != null ? clip.frameRate : 24f;
        AttackTimingHelper.ResolveClientBowAnimation(
            entity,
            unityClipFps,
            out float attackMultiplier,
            out float clientAnimRate,
            out float clientPlayRate,
            out _tweenSeconds,
            out _motionSpeed);

        _releaseAt = _tweenSeconds + eventTimeInClip / _motionSpeed;
        _endTime = _tweenSeconds + clipLength / _motionSpeed;
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.SetPAtkSpeed(objectId, 0f);

        // ReplayAttackTrace.Write(
            // "BOW_CLIP_ENTER",
            // entity,
            // entity != null ? entity.GetTargetEntity() : null,
            // "clipLength=" + clipLength.ToString("F3") +
            // " shootAt=" + eventTimeInClip.ToString("F3") +
            // " sourceFps=30.000" +
            // " unityFps=" + unityClipFps.ToString("F3") +
            // " animRate=" + clientAnimRate.ToString("F6") +
            // " atkMul=" + attackMultiplier.ToString("F6") +
            // " playRate=" + clientPlayRate.ToString("F6") +
            // " tween=" + _tweenSeconds.ToString("F6") +
            // " R=" + _releaseAt.ToString("F3") +
            // " motionSpeed=" + _motionSpeed.ToString("F6") +
            // " endAt=" + _endTime.ToString("F3") +
            // " parameter=" + parameterName);

        if (AnimatorUtils.IsLocalPlayerAnimator(animator))
            StopAnimationTrigger(animator, parameterName);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_startTime < 0f || AnimationManager.Instance == null)
            return;

        int objectId = AnimatorUtils.GetObjectId(animator);
        float elapsed = Time.time - _startTime;
        // UE2 starts the sequence at a negative frame during TweenTime. Holding
        // Unity on the first sample for that wall time reproduces the notify timing.
        float speed = elapsed < _tweenSeconds ? 0f : _motionSpeed;
        AnimationManager.Instance.SetPAtkSpeed(objectId, speed);

        if (!AnimatorUtils.IsLocalPlayerAnimator(animator))
            return;

        if (elapsed >= _endTime)
            SwitchToIdle();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.SetPAtkSpeed(AnimatorUtils.GetObjectId(animator), 1f);
    }

    private void SwitchToIdle()
    {
        if (_isSwitchIdle)
            return;

        _isSwitchIdle = true;
        // ReplayAttackTrace.Write(
            // "BOW_SWITCH_IDLE",
            // PlayerEntity.Instance,
            // PlayerEntity.Instance != null ? PlayerEntity.Instance.GetTargetEntity() : null,
            // "elapsed=" + (Time.time - _startTime).ToString("F3") +
            // " endAt=" + _endTime.ToString("F3") +
            // " releaseAt=" + _releaseAt.ToString("F3"));
        if (PlayerEntity.Instance != null)
            PlayerEntity.Instance.IsAttack = false;
        if (PlayerStateMachine.Instance != null && PlayerStateMachine.Instance.ShouldKeepLocomotion())
            return;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }

    private void StopAnimationTrigger(Animator animator, string animParameterName)
    {
        if (animator.GetBool(animParameterName))
        {
            AnimationManager.Instance.StopCurrentAnimation(
                AnimatorUtils.GetObjectId(animator), animParameterName, "player");
        }
    }
}
