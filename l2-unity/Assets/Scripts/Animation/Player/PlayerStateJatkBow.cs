using UnityEngine;

/// <summary>
/// L2 bow jatk: shoot = OnAnimationShoot notify fraction.
/// Arrow flight uses ANProjectile accel (dirMul=3000, path (t/T)²).
/// patkspd is applied on this animator's entity (PlayerEntity and UserEntity / CharInfo).
/// </summary>
public class PlayerStateJatkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _linearSpeed;
    private bool _isSwitchIdle;

    public string parameterName;
    public string motionName;
    public string eventShootName;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _startTime = Time.time;

        float clipLength = AnimationDataCache.GetOverrideLength(animator, motionName);
        float eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);
        if (clipLength <= 0.01f)
            clipLength = Mathf.Max(stateInfo.length, 0.01f);
        if (eventTimeInClip <= 0.01f)
            eventTimeInClip = clipLength * 0.637f;

        int objectId = AnimatorUtils.GetObjectId(animator);
        _linearSpeed = AnimationManager.Instance.ApplyLinearMeleePAtkSpeed(
            objectId, parameterName, clipLength);
        float cycleMs = AttackTimingHelper.ResolveAttackCycleMs(
            World.Instance != null ? World.Instance.GetEntityNoLockSync(objectId) : null,
            parameterName);
        _endTime = Mathf.Max(0.01f, cycleMs / 1000f);

        if (AnimatorUtils.IsLocalPlayerAnimator(animator))
            StopAnimationTrigger(animator, parameterName);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int objectId = AnimatorUtils.GetObjectId(animator);
        if (_startTime >= 0f)
            AnimationManager.Instance.SetPAtkSpeed(objectId, _linearSpeed);

        if (!AnimatorUtils.IsLocalPlayerAnimator(animator))
            return;

        float timeOut = Time.time - _startTime;
        if (timeOut >= _endTime)
            SwitchToIdle();
    }

    private void SwitchToIdle()
    {
        if (_isSwitchIdle)
            return;

        _isSwitchIdle = true;
        if (PlayerEntity.Instance != null)
            PlayerEntity.Instance.IsAttack = false;
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
