using UnityEngine;

public class PlayerStateRebird : PlayerStateBase
{
    public string parameterName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // CrossFade path does not set the rebirth bool; clear leftover AnyState bool if any.
        if (!string.IsNullOrEmpty(parameterName) && animator.GetBool(parameterName))
        {
            AnimationManager.Instance.StopCurrentAnimation(
                animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
    }
}
