using UnityEngine;

public class MonsterStateDeath : MonsterStateBase
{
    public const float HoldNorm = 0.999f;

    public string parameterName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        StopAnimationTrigger(animator, parameterName);

        if (IsPoseHeld())
            SetSpeed(animator, 0f);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null || animator.IsInTransition(layerIndex))
            return;

        if (IsPoseHeld())
        {
            SetSpeed(animator, 0f);
            return;
        }

        // Clip Loop Time is off. This only catches a clip that still wraps past the end.
        if (stateInfo.normalizedTime < 1f)
            return;

        if (_entity != null && _entity.ActionSlot != null)
            _entity.ActionSlot.DeathPoseHeld = true;
        animator.Play(stateInfo.fullPathHash, layerIndex, HoldNorm);
        SetSpeed(animator, 0f);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        bool backToDeath = animator != null
            && animator.IsInTransition(layerIndex)
            && animator.GetNextAnimatorStateInfo(layerIndex).IsName(MonsterAnim.Death);
        if (backToDeath)
            return;

        SetSpeed(animator, 1f);
        if (_entity != null && _entity.ActionSlot != null)
            _entity.ActionSlot.DeathPoseHeld = false;
    }

    bool IsPoseHeld()
    {
        return _entity != null && _entity.ActionSlot != null && _entity.ActionSlot.DeathPoseHeld;
    }

    void SetSpeed(Animator animator, float speed)
    {
        if (_networkAnimationController != null)
            _networkAnimationController.SetAnimatorSpeed(speed);
        else if (animator != null)
            animator.speed = speed;
    }

    private void StopAnimationTrigger(Animator animator, string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return;
        if (animator.GetBool(parameterName) != false)
        {
            AnimationManager.Instance.StopMonsterCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
        }
    }
}
