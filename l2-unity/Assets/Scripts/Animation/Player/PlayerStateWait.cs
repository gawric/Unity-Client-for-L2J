using UnityEngine;

public class PlayerStateWait : StateMachineBehaviour
{
    public string parameterName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimationManager.Instance.StopCurrentAnimation(animator.GetInteger(AnimatorUtils.OBJECT_ID), parameterName);
    }
}
