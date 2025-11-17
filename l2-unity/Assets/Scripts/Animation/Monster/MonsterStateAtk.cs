
using UnityEngine;

public class MonsterStateAtk : MonsterStateBase
{
    private float _startTime = -1;
    private float _endTime = -1;

    public string parameterName;
    private AnimationCurve _animationCurve;
    private bool _isSwitchIdle = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

        LoadComponents(animator);

        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(0);

        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        }

        _animationCurve = new AnimationCurve();
        //float test = CalcBaseParam.CalculateTimeL2j(_entity.Stats.PAtkRealSpeed);
        float timeAtk = CalcBaseParam.CalculateTimeL2j(_entity.Stats.PAtkRealSpeed);
        _startTime = Time.time;
        _endTime = TimeUtils.ConvertMsToSec(timeAtk);
        float timeAnimation = clipInfos[0].clip.length;
        RecreateAnimationCurve(_animationCurve, _endTime, timeAnimation);

        Debug.Log("Calc Time Monster Atk>>> All Time Animation " + timeAnimation + " timeAtk " + timeAtk);
        _networkAnimationController.UpdateAnimatorAtkSpeedL2j(timeAtk, timeAnimation);
        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

        float currentTime = Time.time;
        float timeOut = currentTime - _startTime;

        if (timeOut >= _endTime) SwitchToIdle(stateInfo);

        float normalizedTime = timeOut / _endTime;
        float speed = _animationCurve.Evaluate(normalizedTime);
        _networkAnimationController.SetPAtkSpd(speed);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

    private void SwitchToIdle(AnimatorStateInfo stateInfo)
    {
        float currentNormalizedTime = stateInfo.normalizedTime;

        if (!_isSwitchIdle & currentNormalizedTime > 0.9f)
        {

            _isSwitchIdle = true;

            if (!_entity.IsDead())
            {
                _monsterStateMachine.ChangeIntention(MonsterIntention.INTENTION_IDLE);
                _monsterStateMachine.NotifyEvent(Event.ENTER_WORLD);
            }
        }
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator , parameterName);
        }

    }

    private void RecreateAnimationCurve(AnimationCurve animationCurve, float timeAtk, float timeAnimation)
    {
        Keyframe startKey = new Keyframe(0f, 0f);
        Keyframe endKey = new Keyframe(timeAtk, timeAnimation);
        //default to sword slow down
        Keyframe slowDownAttackKey = new Keyframe(0.07511136f, 0.4373413f);

        animationCurve.AddKey(startKey);
        animationCurve.AddKey(slowDownAttackKey);
        animationCurve.AddKey(endKey);
    }
}
