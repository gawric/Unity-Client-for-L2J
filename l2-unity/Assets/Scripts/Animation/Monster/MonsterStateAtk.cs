
using UnityEngine;

public class MonsterStateAtk : MonsterStateBase
{
    private float _startTime = -1;
    private float _endTime = -1;

    public string parameterName;
    private AnimationCurve _animationCurve;
    private bool _isSwitchIdle = false;
    private bool _loggedSkillHold;
    private bool _loggedSkillStuck;
    private int _swingId;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        _isSwitchIdle = false;
        _loggedSkillHold = false;
        _loggedSkillStuck = false;
        _startTime = Time.time;

        LoadComponents(animator);
        _swingId = _entity != null ? _entity.ActionSlot.AttackSwingId : 0;

        AnimatorClipInfo[] clipInfos = animator.GetNextAnimatorClipInfo(0);

        if (clipInfos == null || clipInfos.Length == 0)
        {
            clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        }

        _animationCurve = new AnimationCurve();
        //float test = CalcBaseParam.CalculateTimeL2j(_entity.Stats.PAtkRealSpeed);
        float timeAtk = CalcBaseParam.CalculateTimeL2j(_entity.Stats.PAtkRealSpeed);
        _endTime = TimeUtils.ConvertMsToSec(timeAtk);
        float timeAnimation = clipInfos[0].clip.length;
        RecreateAnimationCurve(_animationCurve, _endTime, timeAnimation);

        // Debug.Log("Calc Time Monster Atk>>> All Time Animation " + timeAnimation + " timeAtk " + timeAtk);
        _networkAnimationController.UpdateAnimatorAtkSpeedL2j(timeAtk, timeAnimation);
        if (_entity != null && _entity.ActionSlot.Action == EntityActionKind.Skill)
        {
            // MobSkillLog.Enter(_entity, timeAnimation, timeAtk, animator.GetFloat("patkspd"));
        }
        StopAnimationTrigger(animator, parameterName);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (TryFinishSkill(stateInfo))
            return;
        if (_entity != null && _entity.ActionSlot.Action != EntityActionKind.Attack)
            return;
        if (IsStaleSwing())
            return;

        float currentTime = Time.time;
        float timeOut = currentTime - _startTime;

        if (timeOut >= _endTime) SwitchToIdle(stateInfo);

        float normalizedTime = timeOut / _endTime;
        float speed = _animationCurve.Evaluate(normalizedTime);
        _networkAnimationController.SetPAtkSpd(speed);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

    bool TryFinishSkill(AnimatorStateInfo stateInfo)
    {
        if (_entity == null || _entity.ActionSlot.Action != EntityActionKind.Skill)
            return false;

        if (!_loggedSkillHold && stateInfo.normalizedTime >= 1f)
        {
            _loggedSkillHold = true;
            // MobSkillLog.ClipEnd(_entity);
        }

        if (_isSwitchIdle || stateInfo.normalizedTime < 1f || _entity.IsDead())
        {
            if (!_loggedSkillStuck &&
                _isSwitchIdle &&
                stateInfo.IsName("atk01") &&
                stateInfo.normalizedTime >= 1.05f)
            {
                _loggedSkillStuck = true;
                // MobSkillLog.Stuck(_entity);
            }
            return true;
        }
        if (EntityActionMachine.Instance == null)
            return true;

        string restore = (_entity.Identity != null && MoveAllCharacters.Instance != null && MoveAllCharacters.Instance.IsMoving(_entity.Identity.Id)) ? (_entity.Running ? "run" : "walk") : "wait";
        _isSwitchIdle = true;
        EntityActionMachine.Instance.Set(_entity, EntityActionKind.Idle, restore);
        // MobSkillLog.Done(_entity, restore);
        return true;
    }

    private void SwitchToIdle(AnimatorStateInfo stateInfo)
    {
        float currentNormalizedTime = stateInfo.normalizedTime;

        if (_isSwitchIdle || currentNormalizedTime <= 0.9f)
            return;
        if (_entity == null || _entity.IsDead() || EntityActionMachine.Instance == null)
            return;
        if (_entity.ActionSlot.Action != EntityActionKind.Attack)
            return;
        if (IsStaleSwing())
            return;

        _isSwitchIdle = true;
        EntityActionMachine.Instance.Set(_entity, EntityActionKind.Idle, null);
    }

    bool IsStaleSwing()
    {
        return _entity != null &&
            _swingId != 0 &&
            _entity.ActionSlot.AttackSwingId != _swingId;
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
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
