using UnityEngine;

/// <summary>
/// L2 bow skill (SpAtk01_bow): same model as <see cref="PlayerStateJatkBow"/>.
/// Shoot = OnAnimationShoot notify; arrow Accel via AbstractAttackEvents.CallBackStartShoot.
/// Requires Animator state Speed Parameter = patkspd (clip scales to sptimeatk).
/// </summary>
public class PlayerStateSpAtkBow : StateMachineBehaviour
{
    private float _startTime;
    private float _endTime;
    private float _clipLength;
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
        float eventTimeInClip = AnimationDataCache.GetEventTimeByName(animator, motionName, eventShootName);

        int serverTimeMs = animator.GetInteger("sptimeatk");
        _endTime = serverTimeMs / 1000f;
        if (_endTime <= 0.01f)
        {
            _endTime = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 1000f;
        }

        if (_clipLength <= 0.01f)
        {
            _clipLength = Mathf.Max(stateInfo.length, 0.01f);
        }

        if (eventTimeInClip <= 0.01f)
        {
            eventTimeInClip = _clipLength * 0.637f;
        }

        _linearSpeed = _clipLength / _endTime;
        PlayerAnimationController.Instance.SetPAtkSpeed(_linearSpeed);
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
        PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
    }
}
