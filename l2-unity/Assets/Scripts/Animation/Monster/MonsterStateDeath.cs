using UnityEngine;

public class MonsterStateDeath : MonsterStateBase
{
    public const float HoldNorm = 0.999f;

    public string parameterName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        StopAnimationTrigger(animator, parameterName);

        // A stale corpse flag must not freeze a new fall that is still blending in from run.
        if (IsFreshFall(animator, stateInfo, layerIndex))
        {
            if (_entity != null && _entity.ActionSlot != null)
                _entity.ActionSlot.DeathPoseHeld = false;
        }
        else if (IsPoseHeld())
        {
            SetSpeed(animator, 0f);
        }
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

        LoadComponents(animator);
        SetSpeed(animator, 1f);
        if (_entity != null && _entity.ActionSlot != null)
            _entity.ActionSlot.DeathPoseHeld = false;
    }

    bool IsFreshFall(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null || stateInfo.normalizedTime >= 0.05f)
            return false;
        if (!animator.IsInTransition(layerIndex))
            return !animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(MonsterAnim.Death);

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (current.IsName(MonsterAnim.Death))
            return false;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layerIndex);
        if (clips != null && clips.Length > 0 && clips[0].clip != null &&
            clips[0].clip.name.IndexOf("death", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        return true;
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
