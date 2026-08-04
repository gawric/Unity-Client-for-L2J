using UnityEngine;

/// <summary>
/// L2 bow jatk: shoot = OnAnimationShoot notify fraction.
/// Arrow flight uses ANProjectile accel (dirMul=3000, path (t/T)²).
/// </summary>
public class PlayerStateJatkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _clipLength;
    private float _eventTimeInClip;
    private float _linearSpeed;
    private bool _isSwitchIdle;

    public string parameterName;
    public string motionName;
    public string eventShootName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _isSwitchIdle = false;
        _startTime = Time.time;

        _clipLength = AnimationDataCache.GetOverrideLength(animator, motionName);
        _eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);

        float atkSpd = PlayerEntity.Instance.Stats.BasePAtkSpeed;
        float serverTimeMs = CalcBaseParam.CalculateTimeL2j(atkSpd);
        _endTime = serverTimeMs / 1000f;

        if (_clipLength <= 0.01f)
        {
            _clipLength = Mathf.Max(stateInfo.length, 0.01f);
        }

        if (_eventTimeInClip <= 0.01f)
        {
            _eventTimeInClip = _clipLength * 0.637f;
        }

        _linearSpeed = _clipLength / _endTime;
        PlayerAnimationController.Instance.SetPAtkSpeed(_linearSpeed);

        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float timeOut = Time.time - _startTime;

        if (timeOut >= _endTime)
        {
            PlayerAnimationController.Instance.SetPAtkSpeed(1.0f);
            SwitchToIdle(animator);
            return;
        }

        PlayerAnimationController.Instance.SetPAtkSpeed(_linearSpeed);
    }

    private void SwitchToIdle(Animator animator)
    {
        if (_isSwitchIdle)
        {
            return;
        }

        _isSwitchIdle = true;
        PlayerAnimationController.Instance.SetPAtkSpeed(1.0f);
        PlayerEntity.Instance.IsAttack = false;
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN, NewIdleState.WaitReturnFromCombatSmb);
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName, "player");
        }
    }
}
