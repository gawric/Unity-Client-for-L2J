using UnityEngine;



public class PlayerStateJAtk : StateMachineBehaviour
{
    private float _startTime = -1;
    private float _endTime = -1;

    public string parameterName;
    private AnimationCurve _animationCurve;
    private bool _isSwitchIdle = false;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
  
        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(0);

        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        }

        _animationCurve = new AnimationCurve();
        _isSwitchIdle = false;

         float timeAtk  = CalcBaseParam.CalculateTimeL2j(PlayerEntity.Instance.Stats.BasePAtkSpeed) / 2;

        //float timeAtk = CalcBaseParam.CalculateTimeL2j(554) / 2;
        _startTime = Time.time;
        _endTime = TimeUtils.ConvertMsToSec(timeAtk);
        float timeAnimation = clipInfos[0].clip.length;
        RecreateAnimationCurve(_animationCurve, _endTime, timeAnimation);

        PlayerAnimationController.Instance.UpdateAnimatorAtkSpeedL2j(timeAtk , timeAnimation);

        StopAnimationTrigger(animator , parameterName);
        //Debug.Log("Attack Sate to Intention> начало новой атакие");
        //Debug.Log(" Attack Sate to Intention TIMEOUT Запуск е1 base p atk" + PlayerEntity.Instance.Stats.BasePAtkSpeed + " end time " + _endTime + "  start time " + _startTime +  " timeAtk " + timeAtk + " timeAnimation " + timeAnimation);
    }
     
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = Time.time;
        float timeOut = currentTime - _startTime;

        

        float normalizedTime = timeOut / _endTime;
        float speed = _animationCurve.Evaluate(normalizedTime);


        

        if (timeOut >= _endTime) SwitchToIdle(stateInfo);

        PlayerAnimationController.Instance.SetPAtkSpeed(speed);
    }

    private void SwitchToIdle(AnimatorStateInfo stateInfo)
    {
        //  float currentNormalizedTime = stateInfo.normalizedTime;
        float currentNormalizedTime = stateInfo.normalizedTime;

        if (!_isSwitchIdle & currentNormalizedTime > 0.9f & IsDieTarget() | !_isSwitchIdle & currentNormalizedTime > 0.9f & !PlayerEntity.Instance.IsAttack )
        {
            PlayerEntity.Instance.IsAttack = false;
            _isSwitchIdle = true;
            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
            PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
            Debug.Log("Attack Sate to Intention> конец атаки IDLE");
            //  Debug.Log("Attack Sate to Intention switch OK");
        }
    }

    private bool IsDieTarget()
    {
        Entity entity = World.Instance.GetEntityNoLockSync(PlayerEntity.Instance.TargetId);
        if (entity != null) return entity.IsDead();
        return false;
    }
    private void StopAnimationTrigger(Animator animator , string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopCurrentAnimation(parameterName);
        }

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Attack Sate to Intention> конец атаки OnStateExit");
        PlayerEntity.Instance.IsAttack = false;
    }


    private void RecreateAnimationCurve(AnimationCurve animationCurve , float timeAtk , float timeAnimation)
    {
        Keyframe startKey = new Keyframe(0f, 0f); 
        Keyframe endKey = new Keyframe(timeAtk, timeAnimation);
        //float speedAtk = (float)0.362 / timeAtk;
        float speedAtk = (float)0.3585 / timeAtk;
        //default to sword slow down
        //test 0,603 для стандартной атакие или 0.36293652 0.1 на еденицу времени
        Keyframe slowDownAttackKey = new Keyframe(0.07511136f, speedAtk);
        // Keyframe slowDownAttackKey = new Keyframe(0.07511136f, 0.4373413f);
        //Keyframe slowDownAttackKey = new Keyframe(0.07511136f, 0.9073413f);

        animationCurve.AddKey(startKey);
        animationCurve.AddKey(slowDownAttackKey);
        animationCurve.AddKey(endKey);
    }

  

}